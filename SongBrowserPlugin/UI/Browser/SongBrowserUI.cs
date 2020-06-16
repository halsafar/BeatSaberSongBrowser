using UnityEngine;
using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using HMUI;
using SongBrowser.DataAccess;
using TMPro;
using Logger = SongBrowser.Logging.Logger;
using System.Collections;
using SongCore.Utilities;
using SongBrowser.Internals;
using SongDataCore.BeatStar;

namespace SongBrowser.UI
{
    public enum UIState
    {
        Disabled,
        Main,
        SortBy,
        FilterBy
    }

    /// <summary>
    /// Hijack the flow coordinator.  Have access to all StandardLevel easily.
    /// </summary>
    public class SongBrowserUI : MonoBehaviour
    {
        // Logging
        public const String Name = "SongBrowserUI";

        private const float SEGMENT_PERCENT = 0.1f;
        private const int LIST_ITEMS_VISIBLE_AT_ONCE = 6;

        // BeatSaber Internal UI structures
        DataAccess.BeatSaberUIController _beatUi;

        // New UI Elements
        private List<SongSortButton> _sortButtonGroup;
        private List<SongFilterButton> _filterButtonGroup;

        private Button _sortByButton;
        private Button _sortByDisplay;
        private Button _filterByButton;
        private Button _filterByDisplay;
        private Button _randomButton;

        private Button _clearSortFilterButton;

        private SimpleDialogPromptViewController _deleteDialog;
        private Button _deleteButton;        

        private Button _pageUpFastButton;
        private Button _pageDownFastButton;

        private SearchKeyboardViewController _searchViewController;

        private RectTransform _ppStatButton;
        private RectTransform _starStatButton;
        private RectTransform _njsStatButton;

        private IAnnotatedBeatmapLevelCollection _lastLevelCollection;

        private SongBrowserModel _model;
        public SongBrowserModel Model
        {
            set
            {
                _model = value;
            }

            get
            {
                return _model;
            }
        }

        private bool _uiCreated = false;
        
        private UIState _currentUiState = UIState.Disabled;

        private bool _asyncUpdating = false;

        /// <summary>
        /// Builds the UI for this plugin.
        /// </summary>
        public void CreateUI(MainMenuViewController.MenuButton mode)
        {
            Logger.Trace("CreateUI()");

            // Determine the flow controller to use
            FlowCoordinator flowCoordinator = null;
            if (mode == MainMenuViewController.MenuButton.SoloFreePlay)
            {
                Logger.Debug("Entering SOLO mode...");
                flowCoordinator = Resources.FindObjectsOfTypeAll<SoloFreePlayFlowCoordinator>().First();
            }
            else if (mode == MainMenuViewController.MenuButton.Party)
            {
                Logger.Debug("Entering PARTY mode...");
                flowCoordinator = Resources.FindObjectsOfTypeAll<PartyFreePlayFlowCoordinator>().First();
            }
            else
            {
                Logger.Debug("Entering SOLO CAMPAIGN mode...");
                flowCoordinator = Resources.FindObjectsOfTypeAll<CampaignFlowCoordinator>().First();
                return;
            }

            Logger.Debug("Done fetching Flow Coordinator for the appropriate mode...");

            _beatUi = new DataAccess.BeatSaberUIController(flowCoordinator);
            _lastLevelCollection = null;

            // returning to the menu and switching modes could trigger this.
            if (_uiCreated)
            {
                return;
            }

            try
            {                
                // delete dialog
                this._deleteDialog = UnityEngine.Object.Instantiate<SimpleDialogPromptViewController>(_beatUi.SimpleDialogPromptViewControllerPrefab);
                this._deleteDialog.name = "DeleteDialogPromptViewController";
                this._deleteDialog.gameObject.SetActive(false);

                // create song browser main ui
                CreateOuterUi();
                CreateSortButtons();
                CreateFilterButtons();
                CreateDeleteButton();
                CreateFastPageButtons();

                this.InstallHandlers();

                this.ModifySongStatsPanel();
                this.ResizeSongUI();

                _uiCreated = true;

                RefreshSortButtonUI();

                Logger.Debug("Done Creating UI...");
            }
            catch (Exception e)
            {
                Logger.Exception("Exception during CreateUI: ", e);
            }
        }

        /// <summary>
        /// Create the outer ui.
        /// </summary>
        private void CreateOuterUi()
        {
            Logger.Debug("Creating outer UI...");

            float clearButtonX = -32.5f;
            float clearButtonY = 34.5f;
            float buttonY = 37f;
            float buttonHeight = 5.0f;
            float sortByButtonX = -22.5f + buttonHeight;
            float outerButtonFontSize = 3.0f;
            float displayButtonFontSize = 2.5f;
            float outerButtonWidth = 24.0f;
            float randomButtonWidth = 8.0f;

            // clear button
            _clearSortFilterButton = CreateClearButton(clearButtonX, clearButtonY, buttonHeight, () =>
            {                
                if (_currentUiState == UIState.FilterBy || _currentUiState == UIState.SortBy)
                {
                    RefreshOuterUIState(UIState.Main);
                }
                else
                {
                    OnClearButtonClickEvent();
                }
            });

            // create SortBy button and its display
            float curX = sortByButtonX;
            _sortByButton = _beatUi.LevelCollectionViewController.CreateUIButton("ApplyButton", new Vector2(curX, buttonY), new Vector2(outerButtonWidth, buttonHeight), () =>
            {
                RefreshOuterUIState(UIState.SortBy);
            }, "Sort By");
            _sortByButton.SetButtonTextSize(outerButtonFontSize);
            _sortByButton.ToggleWordWrapping(false);

            curX += outerButtonWidth;

            _sortByDisplay = _beatUi.LevelCollectionViewController.CreateUIButton("ApplyButton", new Vector2(curX, buttonY), new Vector2(outerButtonWidth, buttonHeight), () =>
            {
                OnSortButtonClickEvent(_model.Settings.sortMode);
            }, "");
            _sortByDisplay.SetButtonTextSize(displayButtonFontSize);
            _sortByDisplay.ToggleWordWrapping(false);
            curX += outerButtonWidth;

            // create FilterBy button and its display
            _filterByButton = _beatUi.LevelCollectionViewController.CreateUIButton("ApplyButton", new Vector2(curX, buttonY), new Vector2(outerButtonWidth, buttonHeight), () =>
            {
                RefreshOuterUIState(UIState.FilterBy);
            }, "Filter By");
            _filterByButton.SetButtonTextSize(outerButtonFontSize);
            _filterByButton.ToggleWordWrapping(false);

            curX += outerButtonWidth;

            _filterByDisplay = _beatUi.LevelCollectionViewController.CreateUIButton("ApplyButton", new Vector2(curX, buttonY), new Vector2(outerButtonWidth, buttonHeight), () =>
            {
                _model.Settings.filterMode = SongFilterMode.None;
                CancelFilter();
                ProcessSongList();
                RefreshSongUI();
            }, "");
            _filterByDisplay.SetButtonTextSize(displayButtonFontSize);
            _filterByDisplay.ToggleWordWrapping(false);

            // random button
            _randomButton = _beatUi.LevelCollectionViewController.CreateUIButton("HowToPlayButton", new Vector2(curX + (outerButtonWidth / 2.0f) + (randomButtonWidth / 2.0f), clearButtonY), new Vector2(randomButtonWidth, buttonHeight), () =>
            {
                OnSortButtonClickEvent(SongSortMode.Random);
            }, "",
            Base64Sprites.RandomIcon);
            _randomButton.GetComponentsInChildren<HorizontalLayoutGroup>().First(btn => btn.name == "Content").padding = new RectOffset(0, 0, 0, 0);
            var textRect = _randomButton.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(c => c.name == "Text");
            if (textRect != null)
            {
                UnityEngine.Object.Destroy(textRect.gameObject);
            }
            BeatSaberUI.SetButtonBorderActive(_randomButton, false);
        }

        /// <summary>
        /// Create the back button
        /// </summary>
        /// <returns></returns>
        private Button CreateClearButton(float x, float y, float h, UnityEngine.Events.UnityAction callback)
        {
            Button b = _beatUi.LevelCollectionViewController.CreateUIButton("HowToPlayButton", new Vector2(x, y), new Vector2(h, h), callback, "", Base64Sprites.XIcon);
            b.GetComponentsInChildren<HorizontalLayoutGroup>().First(btn => btn.name == "Content").padding = new RectOffset(1, 1, 0, 0);
            RectTransform textRect = b.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(c => c.name == "Text");
            if (textRect != null)
            {
                UnityEngine.Object.Destroy(textRect.gameObject);
            }
            BeatSaberUI.SetButtonBorderActive(b, false);

            return b;
        }

        /// <summary>
        /// Create the sort button ribbon
        /// </summary>
        private void CreateSortButtons()
        {
            Logger.Debug("Create sort buttons...");

            float sortButtonFontSize = 2.15f;
            float sortButtonX = -23.0f;
            float sortButtonWidth = 12.0f;
            float buttonSpacing = 0.25f;
            float buttonY = 37f;
            float buttonHeight = 5.0f;

            string[] sortButtonNames = new string[]
            {
                    "Title", "Author", "Newest", "YourPlays", "PP", "Stars", "UpVotes", "Rating", "Heat"
            };

            SongSortMode[] sortModes = new SongSortMode[]
            {
                    SongSortMode.Default, SongSortMode.Author, SongSortMode.Newest, SongSortMode.YourPlayCount, SongSortMode.PP, SongSortMode.Stars,  SongSortMode.UpVotes, SongSortMode.Rating, SongSortMode.Heat
            };

            _sortButtonGroup = new List<SongSortButton>();
            for (int i = 0; i < sortButtonNames.Length; i++)
            {
                float curButtonX = sortButtonX + (sortButtonWidth * i) + (buttonSpacing * i);
                SongSortButton sortButton = new SongSortButton();
                sortButton.SortMode = sortModes[i];
                sortButton.Button = _beatUi.LevelCollectionViewController.CreateUIButton("ApplyButton",
                    new Vector2(curButtonX, buttonY), new Vector2(sortButtonWidth, buttonHeight),
                    () =>
                    {
                        OnSortButtonClickEvent(sortButton.SortMode);
                        RefreshOuterUIState(UIState.Main);
                    },
                    sortButtonNames[i]);
                sortButton.Button.SetButtonTextSize(sortButtonFontSize);
                sortButton.Button.GetComponentsInChildren<HorizontalLayoutGroup>().First(btn => btn.name == "Content").padding = new RectOffset(4, 4, 2, 2);                
                sortButton.Button.ToggleWordWrapping(false);
                sortButton.Button.name = "Sort" + sortModes[i].ToString() + "Button";

                _sortButtonGroup.Add(sortButton);
            }
        }

        /// <summary>
        /// Create the filter by buttons
        /// </summary>
        private void CreateFilterButtons()
        {
            Logger.Debug("Creating filter buttons...");

            float filterButtonFontSize = 2.25f;
            float filterButtonX = -23.0f;
            float filterButtonWidth = 12.25f;
            float buttonSpacing = 0.5f;
            float buttonY = 37f;
            float buttonHeight = 5.0f;

            string[] filterButtonNames = new string[]
            {
                    "Favorites", "Search", "Ranked", "Unranked"
            };

            SongFilterMode[] filterModes = new SongFilterMode[]
            {
                    SongFilterMode.Favorites, SongFilterMode.Search, SongFilterMode.Ranked, SongFilterMode.Unranked
            };

            _filterButtonGroup = new List<SongFilterButton>();
            for (int i = 0; i < filterButtonNames.Length; i++)
            {
                float curButtonX = filterButtonX + (filterButtonWidth * i) + (buttonSpacing * i);
                SongFilterButton filterButton = new SongFilterButton();
                filterButton.FilterMode = filterModes[i];
                filterButton.Button = _beatUi.LevelCollectionViewController.CreateUIButton("ApplyButton",
                    new Vector2(curButtonX, buttonY), new Vector2(filterButtonWidth, buttonHeight),
                    () =>
                    {
                        OnFilterButtonClickEvent(filterButton.FilterMode);
                        RefreshOuterUIState(UIState.Main);
                    },
                    filterButtonNames[i]);
                filterButton.Button.SetButtonTextSize(filterButtonFontSize);
                filterButton.Button.GetComponentsInChildren<HorizontalLayoutGroup>().First(btn => btn.name == "Content").padding = new RectOffset(4, 4, 2, 2);
                filterButton.Button.ToggleWordWrapping(false);
                filterButton.Button.name = "Filter" + filterButtonNames[i] + "Button";

                _filterButtonGroup.Add(filterButton);
            }
        }

        /// <summary>
        /// Create the fast page up and down buttons
        /// </summary>
        private void CreateFastPageButtons()
        {
            Logger.Debug("Creating fast scroll button...");

            _pageUpFastButton = Instantiate(_beatUi.TableViewPageUpButton, _beatUi.LevelCollectionTableViewTransform, false);
            (_pageUpFastButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 1f);
            (_pageUpFastButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 1f);
            (_pageUpFastButton.transform as RectTransform).anchoredPosition = new Vector2(-26f, 1f);
            (_pageUpFastButton.transform as RectTransform).sizeDelta = new Vector2(8f, 6f);
            _pageUpFastButton.GetComponentsInChildren<RectTransform>().First(x => x.name == "BG").sizeDelta = new Vector2(8f, 6f);
            _pageUpFastButton.GetComponentsInChildren<UnityEngine.UI.Image>().First(x => x.name == "Arrow").sprite = Base64Sprites.DoubleArrow;
            _pageUpFastButton.onClick.AddListener(delegate ()
            {
                this.JumpSongList(-1, SEGMENT_PERCENT);
            });

            _pageDownFastButton = Instantiate(_beatUi.TableViewPageDownButton, _beatUi.LevelCollectionTableViewTransform, false);
            (_pageDownFastButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 0f);
            (_pageDownFastButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 0f);
            (_pageDownFastButton.transform as RectTransform).anchoredPosition = new Vector2(-26f, -1f);
            (_pageDownFastButton.transform as RectTransform).sizeDelta = new Vector2(8f, 6f);
            _pageDownFastButton.GetComponentsInChildren<RectTransform>().First(x => x.name == "BG").sizeDelta = new Vector2(8f, 6f);
            _pageDownFastButton.GetComponentsInChildren<UnityEngine.UI.Image>().First(x => x.name == "Arrow").sprite = Base64Sprites.DoubleArrow;
            _pageDownFastButton.onClick.AddListener(delegate ()
            {
                this.JumpSongList(1, SEGMENT_PERCENT);
            });
        }

        /// <summary>
        /// Create the delete button in the play button container
        /// </summary>
        private void CreateDeleteButton()
        {
            // Create delete button
            Logger.Debug("Creating delete button...");
            _deleteButton = BeatSaberUI.CreateIconButton(_beatUi.PlayButtons, _beatUi.PracticeButton, Base64Sprites.DeleteIcon);
            _deleteButton.onClick.AddListener(delegate () {
                HandleDeleteSelectedLevel();
            });
            BeatSaberUI.DestroyHoverHint(_deleteButton.transform as RectTransform);
        }

        /// <summary>
        /// Resize the stats panel to fit more stats.
        /// </summary>
        private void ModifySongStatsPanel()
        {
            // modify details view
            Logger.Debug("Resizing Stats Panel...");

            var statsPanel = _beatUi.StandardLevelDetailView.GetPrivateField<LevelParamsPanel>("_levelParamsPanel");
            var statTransforms = statsPanel.GetComponentsInChildren<RectTransform>();
            var valueTexts = statsPanel.GetComponentsInChildren<TextMeshProUGUI>().Where(x => x.name == "ValueText").ToList();
            RectTransform panelRect = (statsPanel.transform as RectTransform);
            panelRect.sizeDelta = new Vector2(panelRect.sizeDelta.x * 1.2f, panelRect.sizeDelta.y * 1.2f);

            for (int i = 0; i < statTransforms.Length; i++)
            {
                var r = statTransforms[i];
                if (r.name == "Separator")
                {
                    continue;
                }
                r.sizeDelta = new Vector2(r.sizeDelta.x * 0.9f, r.sizeDelta.y * 0.9f);
            }

            for (int i = 0; i < valueTexts.Count; i++)
            {
                var text = valueTexts[i];
                text.fontSize -= 0.75f;
            }

            // inject our components
            _ppStatButton = UnityEngine.Object.Instantiate(statTransforms[1], statsPanel.transform, false);
            BeatSaberUI.SetStatButtonIcon(_ppStatButton, Base64Sprites.GraphIcon);
            BeatSaberUI.DestroyHoverHint(_ppStatButton);
            //BeatSaberUI.SetHoverHint(_ppStatButton, "songBrowser_ppValue", "PP Value");

            _starStatButton = UnityEngine.Object.Instantiate(statTransforms[1], statsPanel.transform, false);
            BeatSaberUI.SetStatButtonIcon(_starStatButton, Base64Sprites.StarFullIcon);
            BeatSaberUI.DestroyHoverHint(_starStatButton);
            //BeatSaberUI.SetHoverHint(_starStatButton, "songBrowser_starValue", "Star Difficulty Rating");

            _njsStatButton = UnityEngine.Object.Instantiate(statTransforms[1], statsPanel.transform, false);
            BeatSaberUI.SetStatButtonIcon(_njsStatButton, Base64Sprites.SpeedIcon);
            BeatSaberUI.DestroyHoverHint(_njsStatButton);
            //BeatSaberUI.SetHoverHint(_njsStatButton, "songBrowser_njsValue", "Note Jump Speed");

            // shrink title
            var titleText = _beatUi.LevelDetailViewController.GetComponentsInChildren<TextMeshProUGUI>(true).First(x => x.name == "SongNameText");            
            titleText.fontSize = 5.0f;
        }

        /// <summary>
        /// Resize some of the song table elements.
        /// </summary>
        public void ResizeSongUI()
        {
            // Reposition the table view a bit
            _beatUi.LevelCollectionTableViewTransform.anchoredPosition = new Vector2(0f, -2.5f);

            // Move the page up/down buttons a bit
            TableView tableView = ReflectionUtil.GetPrivateField<TableView>(_beatUi.LevelCollectionTableView, "_tableView");
            RectTransform pageUpButton = _beatUi.TableViewPageUpButton.transform as RectTransform;
            RectTransform pageDownButton = _beatUi.TableViewPageDownButton.transform as RectTransform;
            pageUpButton.anchoredPosition = new Vector2(pageUpButton.anchoredPosition.x, pageUpButton.anchoredPosition.y - 1f);
            pageDownButton.anchoredPosition = new Vector2(pageDownButton.anchoredPosition.x, pageDownButton.anchoredPosition.y + 1f);

            // shrink play button container
            RectTransform playContainerRect = _beatUi.StandardLevelDetailView.GetComponentsInChildren<RectTransform>().First(x => x.name == "PlayContainer");
            RectTransform playButtonsRect = playContainerRect.GetComponentsInChildren<RectTransform>().First(x => x.name == "PlayButtons");
            playButtonsRect.localScale = new Vector3(0.825f, 0.825f, 0.825f);
        }

        /// <summary>
        /// Add our handlers into BeatSaber.
        /// </summary>
        private void InstallHandlers()
        {
            // level collection, level, difficulty handlers, characteristics
            TableView tableView = ReflectionUtil.GetPrivateField<TableView>(_beatUi.LevelCollectionTableView, "_tableView");

            // update stats
            _beatUi.LevelCollectionViewController.didSelectLevelEvent -= OnDidSelectLevelEvent;
            _beatUi.LevelCollectionViewController.didSelectLevelEvent += OnDidSelectLevelEvent;

            _beatUi.LevelDetailViewController.didPresentContentEvent -= OnDidPresentContentEvent;
            _beatUi.LevelDetailViewController.didPresentContentEvent += OnDidPresentContentEvent;

            _beatUi.LevelDetailViewController.didChangeDifficultyBeatmapEvent -= OnDidChangeDifficultyEvent;
            _beatUi.LevelDetailViewController.didChangeDifficultyBeatmapEvent += OnDidChangeDifficultyEvent;

            // update our view of the game state
            _beatUi.LevelFilteringNavigationController.didSelectAnnotatedBeatmapLevelCollectionEvent -= _levelFilteringNavController_didSelectAnnotatedBeatmapLevelCollectionEvent;
            _beatUi.LevelFilteringNavigationController.didSelectAnnotatedBeatmapLevelCollectionEvent += _levelFilteringNavController_didSelectAnnotatedBeatmapLevelCollectionEvent;

            _beatUi.AnnotatedBeatmapLevelCollectionsViewController.didSelectAnnotatedBeatmapLevelCollectionEvent -= handleDidSelectAnnotatedBeatmapLevelCollection;
            _beatUi.AnnotatedBeatmapLevelCollectionsViewController.didSelectAnnotatedBeatmapLevelCollectionEvent += handleDidSelectAnnotatedBeatmapLevelCollection;

            // Respond to characteristics changes
            _beatUi.BeatmapCharacteristicSelectionViewController.didSelectBeatmapCharacteristicEvent -= OnDidSelectBeatmapCharacteristic;
            _beatUi.BeatmapCharacteristicSelectionViewController.didSelectBeatmapCharacteristicEvent += OnDidSelectBeatmapCharacteristic;

            // make sure the quick scroll buttons don't desync with regular scrolling
            _beatUi.TableViewPageDownButton.onClick.AddListener(delegate ()
            {
                StartCoroutine(RefreshQuickScrollButtonsAsync());
            });
            _beatUi.TableViewPageUpButton.onClick.AddListener(delegate ()
            {
                StartCoroutine(RefreshQuickScrollButtonsAsync());
            });
        }

        /// <summary>
        /// Waits for the song UI to be available before trying to update.
        /// </summary>
        /// <returns></returns>
        public IEnumerator AsyncWaitForSongUIUpdate()
        {
            if (_asyncUpdating)
            {
                yield break;
            }

            if (!_uiCreated)
            {
                yield break;
            }

            if (!_model.SortWasMissingData)
            {
                yield break;
            }

            _asyncUpdating = true;

            while (_beatUi.LevelSelectionNavigationController.GetPrivateField<bool>("_isInTransition") ||
                   _beatUi.LevelDetailViewController.GetPrivateField<bool>("_isInTransition") ||
                   !_beatUi.LevelSelectionNavigationController.isInViewControllerHierarchy ||
                   !_beatUi.LevelDetailViewController.isInViewControllerHierarchy ||
                   !_beatUi.LevelSelectionNavigationController.isActiveAndEnabled ||
                   !_beatUi.LevelDetailViewController.isActiveAndEnabled)
            {
                yield return null;
            }

            //yield return new WaitForEndOfFrame();

            if (_model.Settings.sortMode.NeedsScoreSaberData() && SongDataCore.Plugin.Songs.IsDataAvailable())
            {
                ProcessSongList();
                RefreshSongUI();
            }

            _asyncUpdating = false;
        }

        /// <summary>
        /// Helper to reduce code duplication...
        /// </summary>
        public void RefreshSongUI(bool scrollToLevel=true)
        {
            if (!_uiCreated)
            {
                return;
            }

            RefreshSongList();
            RefreshSortButtonUI();
            if (!scrollToLevel)
            {
                _beatUi.ScrollToLevelByRow(0);
            }
            RefreshQuickScrollButtons();
            RefreshCurrentSelectionDisplay();
        }

        /// <summary>
        /// External helper.
        /// </summary>
        public void ProcessSongList()
        {
            if (!_uiCreated)
            {
                return;
            }

            this._model.ProcessSongList(_lastLevelCollection, _beatUi.LevelCollectionViewController, _beatUi.LevelSelectionNavigationController);
        }

        /// <summary>
        /// Helper for common filter cancellation logic.
        /// </summary>
        public void CancelFilter()
        {
            Logger.Debug($"Cancelling filter, levelCollection {_lastLevelCollection}");
            _model.Settings.filterMode = SongFilterMode.None;

            GameObject _noDataGO = _beatUi.LevelCollectionViewController.GetPrivateField<GameObject>("_noDataInfoGO");
            string _headerText = _beatUi.LevelCollectionTableView.GetPrivateField<string>("_headerText");
            Sprite _headerSprite = _beatUi.LevelCollectionTableView.GetPrivateField<Sprite>("_headerSprite");

            IBeatmapLevelCollection levelCollection = _beatUi.GetCurrentSelectedAnnotatedBeatmapLevelCollection().beatmapLevelCollection;
            _beatUi.LevelCollectionViewController.SetData(levelCollection, _headerText, _headerSprite, false, _noDataGO);
        }

        /// <summary>
        /// Playlists (fancy name for AnnotatedBeatmapLevelCollection)
        /// </summary>
        /// <param name="annotatedBeatmapLevelCollection"></param>
        public virtual void handleDidSelectAnnotatedBeatmapLevelCollection(IAnnotatedBeatmapLevelCollection annotatedBeatmapLevelCollection)
        {
            Logger.Trace("handleDidSelectAnnotatedBeatmapLevelCollection()");
            _lastLevelCollection = annotatedBeatmapLevelCollection;
            Logger.Debug("Selected Level Collection={0}", _lastLevelCollection);
        }

        /// <summary>
        /// Handler for level collection selection, controller.
        /// Sets the current level collection into the model and updates.
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        /// <param name="arg4"></param>
        private void _levelFilteringNavController_didSelectAnnotatedBeatmapLevelCollectionEvent(LevelFilteringNavigationController arg1, IAnnotatedBeatmapLevelCollection arg2, 
            GameObject arg3, BeatmapCharacteristicSO arg4)
        {
            Logger.Trace("_levelFilteringNavController_didSelectAnnotatedBeatmapLevelCollectionEvent(levelCollection={0})", arg2);

            if (arg2 == null)
            {
                // Probably means we transitioned between Music Packs and Playlists
                arg2 = _beatUi.GetCurrentSelectedAnnotatedBeatmapLevelCollection();
                if (arg2 == null)
                {
                    Logger.Warning("Nothing selected.  This is likely an error.");
                    return;
                }
            }

            Logger.Debug("Selected Level Collection={0}", arg2);

            // Do something about preview level packs, they can't be used past this point
            if (arg2 as PreviewBeatmapLevelPackSO)
            {
                Logger.Info("Hiding SongBrowser, previewing a song pack.");
                //CancelFilter();
                Hide();
                return;
            }
            else
            {
                Show();
            }

            // Skip the first time - Effectively ignores BeatSaber forcing OST1 on us on first load.
            // Skip when we have a playlist
            if (_lastLevelCollection == null)
            {
                return;
            }

            SelectLevelCollection(arg2);
        }

        /// <summary>
        /// Logic for selecting a level collection.
        /// </summary>
        /// <param name="levelPack"></param>
        public void SelectLevelCollection(IAnnotatedBeatmapLevelCollection levelCollection)
        {
            try
            {
                if (levelCollection == null)
                {
                    Logger.Debug("No level collection selected...");
                    return;
                }

                // store the real level collection
                if (levelCollection.collectionName != SongBrowserModel.FilteredSongsCollectionName && _lastLevelCollection != null)
                {
                    Logger.Debug("Recording levelCollection: {0}", levelCollection.collectionName);
                    _lastLevelCollection = levelCollection;
                }

                // reset level selection
                _model.LastSelectedLevelId = null;

                // save level collection
                this._model.Settings.currentLevelCollectionName = levelCollection.collectionName;
                this._model.Settings.Save();

                StartCoroutine(ProcessSongListEndOfFrame());
            }
            catch (Exception e)
            {
                Logger.Exception("Exception handling SelectLevelCollection...", e);
            }
        }

        /// <summary>
        /// End of frame update the song list, the game seems to stomp on us sometimes otherwise
        /// TODO - Might not be nice to other plugins
        /// </summary>
        /// <returns></returns>
        public IEnumerator ProcessSongListEndOfFrame()
        {
            yield return new WaitForEndOfFrame();

            ProcessSongList();
            RefreshSongUI();
        }

        /// <summary>
        /// Remove all filters, update song list, save.
        /// </summary>
        private void OnClearButtonClickEvent()
        {
            Logger.Debug("Clearing all sorts and filters.");

            _model.Settings.sortMode = SongSortMode.Original;
            _model.Settings.invertSortResults = false;
            _model.Settings.filterMode = SongFilterMode.None;
            _model.Settings.Save();

            CancelFilter();
            ProcessSongList();
            RefreshSongUI();
        }

        /// <summary>
        /// Sort button clicked.
        /// </summary>
        private void OnSortButtonClickEvent(SongSortMode sortMode)
        {
            Logger.Debug("Sort button - {0} - pressed.", sortMode.ToString());

            if ((sortMode.NeedsScoreSaberData() && !SongDataCore.Plugin.Songs.IsDataAvailable()))
            {
                Logger.Info("Data for sort type is not available.");
                return;
            }

            // Clear current selected level id so our song list jumps to the start
            _model.LastSelectedLevelId = null;

            if (_model.Settings.sortMode == sortMode)
            {
                _model.ToggleInverting();
            }

            _model.Settings.sortMode = sortMode;

            // update the seed
            if (_model.Settings.sortMode == SongSortMode.Random)
            {
                _model.Settings.randomSongSeed = Guid.NewGuid().GetHashCode();
            }

            _model.Settings.Save();

            ProcessSongList();
            RefreshSongUI();
        }

        /// <summary>
        /// Handle filter button logic.  Some filters have sub menus that need special logic.
        /// </summary>
        /// <param name="mode"></param>
        private void OnFilterButtonClickEvent(SongFilterMode mode)
        {
            Logger.Debug($"FilterButton {mode} clicked.");

            var curCollection = _beatUi.GetCurrentSelectedAnnotatedBeatmapLevelCollection();
            if (_lastLevelCollection == null || 
                (curCollection != null &&
                curCollection.collectionName != SongBrowserModel.FilteredSongsCollectionName &&
                curCollection.collectionName != SongBrowserModel.PlaylistSongsCollectionName))
            {
                _lastLevelCollection = _beatUi.GetCurrentSelectedAnnotatedBeatmapLevelCollection();
            }

            if (mode == SongFilterMode.Favorites)
            {
                _beatUi.SelectLevelCollection(SongBrowserSettings.CUSTOM_SONGS_LEVEL_COLLECTION_NAME);
            }
            else
            {
                GameObject _noDataGO = _beatUi.LevelCollectionViewController.GetPrivateField<GameObject>("_noDataInfoGO");
                string _headerText = _beatUi.LevelCollectionTableView.GetPrivateField<string>("_headerText");
                Sprite _headerSprite = _beatUi.LevelCollectionTableView.GetPrivateField<Sprite>("_headerSprite");

                IBeatmapLevelCollection levelCollection = _beatUi.GetCurrentSelectedAnnotatedBeatmapLevelCollection().beatmapLevelCollection;
                _beatUi.LevelCollectionViewController.SetData(levelCollection, _headerText, _headerSprite, false, _noDataGO);
            }

            // If selecting the same filter, cancel
            if (_model.Settings.filterMode == mode)
            {
                _model.Settings.filterMode = SongFilterMode.None;
            }
            else
            {
                _model.Settings.filterMode = mode;
            }

            switch (mode)
            {
                case SongFilterMode.Search:
                    OnSearchButtonClickEvent();
                    break;
                default:
                    _model.Settings.Save();
                    ProcessSongList();
                    RefreshSongUI();
                    break;
            }
        }

        /// <summary>
        /// Display the keyboard.
        /// </summary>
        /// <param name="sortMode"></param>
        private void OnSearchButtonClickEvent()
        {
            Logger.Debug("Filter button - {0} - pressed.", SongFilterMode.Search.ToString());

            this.ShowSearchKeyboard();
        }

        /// <summary>
        /// Adjust UI based on level selected.
        /// Various ways of detecting if a level is not properly selected.  Seems most hit the first one.
        /// </summary>
        private void OnDidSelectLevelEvent(LevelCollectionViewController view, IPreviewBeatmapLevel level)
        {
            try
            {
                Logger.Trace("OnDidSelectLevelEvent()");

                if (level == null)
                {
                    Logger.Debug("No level selected?");
                    return;
                }

                if (_model.Settings == null)
                {
                    Logger.Debug("Settings not instantiated yet?");
                    return;
                }

                _model.LastSelectedLevelId = level.levelID;

                HandleDidSelectLevelRow(level);
            }
            catch (Exception e)
            {
                Logger.Exception("Exception selecting song:", e);
            }
        }

        /// <summary>
        /// Switching one-saber modes for example.
        /// </summary>
        /// <param name="view"></param>
        /// <param name="bc"></param>
        private void OnDidSelectBeatmapCharacteristic(BeatmapCharacteristicSegmentedControlController view, BeatmapCharacteristicSO bc)
        {
            try
            {
                Logger.Trace("OnDidSelectBeatmapCharacteristic({0})", bc.compoundIdPartName);
                _model.CurrentBeatmapCharacteristicSO = bc;

                if (_beatUi.StandardLevelDetailView != null)
                {
                    RefreshScoreSaberData(_beatUi.StandardLevelDetailView.selectedDifficultyBeatmap.level);
                    RefreshNoteJumpSpeed(_beatUi.StandardLevelDetailView.selectedDifficultyBeatmap.noteJumpMovementSpeed);
                }
            }
            catch (Exception e)
            {
                Logger.Exception(e);
            }
        }

        /// <summary>
        /// Handle difficulty level selection.
        /// </summary>
        private void OnDidChangeDifficultyEvent(StandardLevelDetailViewController view, IDifficultyBeatmap beatmap)
        {
            Logger.Trace("OnDidChangeDifficultyEvent({0})", beatmap);

            if (view.selectedDifficultyBeatmap == null)
            {
                return;
            }

            _deleteButton.interactable = (view.selectedDifficultyBeatmap.level.levelID.Length >= 32);

            RefreshScoreSaberData(view.selectedDifficultyBeatmap.level);
            RefreshNoteJumpSpeed(beatmap.noteJumpMovementSpeed);
        }

        /// <summary>
        /// BeatSaber finished loading content.  This is when the difficulty is finally updated.
        /// </summary>
        /// <param name="view"></param>
        /// <param name="type"></param>
        private void OnDidPresentContentEvent(StandardLevelDetailViewController view, StandardLevelDetailViewController.ContentType type)
        {
            Logger.Trace("OnDidPresentContentEvent()");

            if (view.selectedDifficultyBeatmap == null)
            {
                return;
            }

            _deleteButton.interactable = (_beatUi.LevelDetailViewController.selectedDifficultyBeatmap.level.levelID.Length >= 32);

            RefreshScoreSaberData(view.selectedDifficultyBeatmap.level);
            RefreshNoteJumpSpeed(view.selectedDifficultyBeatmap.noteJumpMovementSpeed);
        }

        /// <summary>
        /// Refresh stats panel.
        /// </summary>
        /// <param name="level"></param>
        private void HandleDidSelectLevelRow(IPreviewBeatmapLevel level)
        {
            Logger.Trace("HandleDidSelectLevelRow({0})", level);

            _deleteButton.interactable = (level.levelID.Length >= 32);

            RefreshQuickScrollButtons();
        }

        /// <summary>
        /// Pop up a delete dialog.
        /// </summary>
        private void HandleDeleteSelectedLevel()
        {
            IBeatmapLevel level = _beatUi.LevelDetailViewController.selectedDifficultyBeatmap.level;
            _deleteDialog.Init("Delete song", $"Do you really want to delete \"{ level.songName} {level.songSubName}\"?", "Delete", "Cancel",
                (selectedButton) =>
                {
                    _beatUi.LevelSelectionFlowCoordinator.InvokePrivateMethod("DismissViewController", new object[] { _deleteDialog, null, false });
                    if (selectedButton == 0)
                    {
                        try
                        {
                            // determine the index we are deleting so we can keep the cursor near the same spot after
                            // the header counts as an index, so if the index came from the level array we have to add 1.
                            var levelsTableView = _beatUi.LevelCollectionTableView;
                            List<IPreviewBeatmapLevel> levels = _beatUi.GetCurrentLevelCollectionLevels().ToList();
                            int selectedIndex = levels.FindIndex(x => x.levelID == _beatUi.StandardLevelDetailView.selectedDifficultyBeatmap.level.levelID);

                            if (selectedIndex > -1)
                            {
                                var song = SongCore.Loader.CustomLevels.First(x => x.Value.levelID == _beatUi.LevelDetailViewController.selectedDifficultyBeatmap.level.levelID).Value;

                                Logger.Info($"Deleting song: {song.customLevelPath}");
                                SongCore.Loader.Instance.DeleteSong(song.customLevelPath);
                                this._model.RemoveSongFromLevelCollection(_beatUi.GetCurrentSelectedAnnotatedBeatmapLevelCollection(), _beatUi.LevelDetailViewController.selectedDifficultyBeatmap.level.levelID);

                                int removedLevels = levels.RemoveAll(x => x.levelID == _beatUi.StandardLevelDetailView.selectedDifficultyBeatmap.level.levelID);
                                Logger.Info("Removed " + removedLevels + " level(s) from song list!");

                                this.UpdateLevelDataModel();

                                // if we have a song to select at the same index, set the last selected level id, UI updates takes care of the rest.
                                if (selectedIndex < levels.Count)
                                {
                                    if (levels[selectedIndex].levelID != null)
                                    {
                                        _model.LastSelectedLevelId = levels[selectedIndex].levelID;
                                    }
                                }

                                this.RefreshSongList();
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.Error("Unable to delete song! Exception: " + e);
                        }
                    }
                });
            _beatUi.LevelSelectionFlowCoordinator.InvokePrivateMethod("PresentViewController", new object[] { _deleteDialog, null, false });
        }        

        /// <summary>
        /// Display the search keyboard
        /// </summary>
        void ShowSearchKeyboard()
        {
            if (_searchViewController == null)
            {
                _searchViewController = BeatSaberUI.CreateViewController<SearchKeyboardViewController>("SearchKeyboardViewController");
                _searchViewController.searchButtonPressed += SearchViewControllerSearchButtonPressed;
                _searchViewController.backButtonPressed += SearchViewControllerbackButtonPressed;
            }

            Logger.Debug("Presenting search keyboard");
            _beatUi.LevelSelectionFlowCoordinator.InvokePrivateMethod("PresentViewController", new object[] { _searchViewController, null, false });
        }

        /// <summary>
        /// Handle back button event from search keyboard.
        /// </summary>
        private void SearchViewControllerbackButtonPressed()
        {
            _beatUi.LevelSelectionFlowCoordinator.InvokePrivateMethod("DismissViewController", new object[] { _searchViewController, null, false });

            this._model.Settings.filterMode = SongFilterMode.None;
            this._model.Settings.Save();

            RefreshSongUI();
        }

        /// <summary>
        /// Handle search.
        /// </summary>
        /// <param name="searchFor"></param>
        private void SearchViewControllerSearchButtonPressed(string searchFor)
        {
            _beatUi.LevelSelectionFlowCoordinator.InvokePrivateMethod("DismissViewController", new object[] { _searchViewController, null, false });

            Logger.Debug("Searching for \"{0}\"...", searchFor);

            _model.Settings.filterMode = SongFilterMode.Search;
            _model.Settings.searchTerms.Insert(0, searchFor);
            _model.Settings.Save();
            _model.LastSelectedLevelId = null;

            ProcessSongList();

            RefreshSongUI();
        }

        /// <summary>
        /// Make big jumps in the song list.
        /// </summary>
        /// <param name="numJumps"></param>
        private void JumpSongList(int numJumps, float segmentPercent)
        {
            var levels = _beatUi.GetCurrentLevelCollectionLevels();
            if (levels == null)
            {
                return;
            }

            int totalSize = levels.Count();
            int segmentSize = (int)(totalSize * segmentPercent);

            // Jump at least one scree size.
            if (segmentSize < LIST_ITEMS_VISIBLE_AT_ONCE)
            {
                segmentSize = LIST_ITEMS_VISIBLE_AT_ONCE;
            }

            TableView tableView = ReflectionUtil.GetPrivateField<TableView>(_beatUi.LevelCollectionTableView, "_tableView");
            int currentRow = _beatUi.LevelCollectionTableView.GetPrivateField<int>("_selectedRow");
            int jumpDirection = Math.Sign(numJumps);
            int newRow = currentRow + (jumpDirection * segmentSize);
            if (newRow <= 0)
            {
                newRow = 0;
            }
            else if (newRow >= totalSize)
            {
                newRow = totalSize - 1;
            }
            
            Logger.Debug("jumpDirection: {0}, newRow: {1}", jumpDirection, newRow);
            _beatUi.SelectAndScrollToLevel(levels[newRow].levelID);
            RefreshQuickScrollButtons();
        }
        
        /// <summary>
        /// Update GUI elements that show score saber data.
        /// </summary>
        public void RefreshScoreSaberData(IPreviewBeatmapLevel level)
        {
            Logger.Trace("RefreshScoreSaberData({0})", level.levelID);

            if (!SongDataCore.Plugin.Songs.IsDataAvailable())
            {
                return;
            }

            BeatmapDifficulty difficulty = _beatUi.LevelDifficultyViewController.selectedDifficulty;
            string difficultyString = difficulty.ToString();
            if (difficultyString.Equals("ExpertPlus"))
            {
                difficultyString = "Expert+";
            }
            Logger.Debug(difficultyString);

            // Check if we have data for this song
            Logger.Debug("Checking if have info for song {0}", level.songName);
            var hash = SongBrowserModel.GetSongHash(level.levelID);
            if (SongDataCore.Plugin.Songs.Data.Songs.ContainsKey(hash))
            {
                Logger.Debug("Checking if have difficulty for song {0} difficulty {1}", level.songName, difficultyString);
                BeatStarSong scoreSaberSong = SongDataCore.Plugin.Songs.Data.Songs[hash];
                BeatStarSongDifficultyStats scoreSaberSongDifficulty = scoreSaberSong.diffs.FirstOrDefault(x => String.Equals(x.diff, difficultyString));
                if (scoreSaberSongDifficulty != null)
                {
                    Logger.Debug("Display pp for song.");
                    double pp = scoreSaberSongDifficulty.pp;
                    double star = scoreSaberSongDifficulty.star;

                    BeatSaberUI.SetStatButtonText(_ppStatButton, String.Format("{0:0.#}", pp));
                    BeatSaberUI.SetStatButtonText(_starStatButton, String.Format("{0:0.#}", star));
                }
                else
                {
                    BeatSaberUI.SetStatButtonText(_ppStatButton, "NA");
                    BeatSaberUI.SetStatButtonText(_starStatButton, "NA");
                }
            }
            else
            {
                BeatSaberUI.SetStatButtonText(_ppStatButton, "NA");
                BeatSaberUI.SetStatButtonText(_starStatButton, "NA");
            }                

            Logger.Debug("Done refreshing score saber stats.");
        }

        /// <summary>
        /// Helper to refresh the NJS widget.
        /// </summary>
        /// <param name="noteJumpMovementSpeed"></param>
        private void RefreshNoteJumpSpeed(float noteJumpMovementSpeed)
        {
            BeatSaberUI.SetStatButtonText(_njsStatButton, String.Format("{0}", noteJumpMovementSpeed));
        }

        /// <summary>
        /// Update interactive state of the quick scroll buttons.
        /// </summary>
        private void RefreshQuickScrollButtons()
        {         
            if (!_uiCreated)
            {
                return;
            }

            _pageUpFastButton.interactable = _beatUi.TableViewPageUpButton.interactable;
            _pageUpFastButton.gameObject.SetActive(_beatUi.TableViewPageUpButton.IsActive());
            _pageDownFastButton.interactable = _beatUi.TableViewPageDownButton.interactable;
            _pageDownFastButton.gameObject.SetActive(_beatUi.TableViewPageDownButton.IsActive());
        }

        /// <summary>
        /// TODO - evaluate this sillyness...
        /// </summary>
        /// <returns></returns>
        public IEnumerator RefreshQuickScrollButtonsAsync()
        {
            yield return new WaitForEndOfFrame();

            RefreshQuickScrollButtons();
        }

        /// <summary>
        /// Show the UI.
        /// </summary>
        public void Show()
        {
            Logger.Trace("Show SongBrowserUI()");

            this.SetVisibility(true);
        }

        /// <summary>
        /// Hide the UI.
        /// </summary>
        public void Hide()
        {
            Logger.Trace("Hide SongBrowserUI()");

            this.SetVisibility(false);
        }

        /// <summary>
        /// Handle showing or hiding UI logic.
        /// </summary>
        /// <param name="visible"></param>
        private void SetVisibility(bool visible)
        {
            // UI not created, nothing visible to hide...
            if (!_uiCreated)
            {
                return;
            }

            _ppStatButton.gameObject.SetActive(visible);
            _starStatButton.gameObject.SetActive(visible);
            _njsStatButton.gameObject.SetActive(visible);

            RefreshOuterUIState(visible == true ? UIState.Main : UIState.Disabled);

            _deleteButton.gameObject.SetActive(visible);

            _pageUpFastButton.gameObject.SetActive(visible);
            _pageDownFastButton.gameObject.SetActive(visible);
        }

        /// <summary>
        /// Update the top UI state.
        /// Hides the outer ui, sort, and filter buttons depending on the state.
        /// </summary>
        private void RefreshOuterUIState(UIState state)
        {
            bool sortButtons = false;
            bool filterButtons = false;
            bool outerButtons = false;
            bool clearButton = true;
            if (state == UIState.SortBy)
            {
                sortButtons = true;
            }
            else if (state == UIState.FilterBy)
            {
                filterButtons = true;
            }
            else if (state == UIState.Main)
            {
                outerButtons = true;
            }
            else
            {
                clearButton = false;
            }

            _sortButtonGroup.ForEach(x => x.Button.gameObject.SetActive(sortButtons));
            _filterButtonGroup.ForEach(x => x.Button.gameObject.SetActive(filterButtons));

            _sortByButton.gameObject.SetActive(outerButtons);
            _sortByDisplay.gameObject.SetActive(outerButtons);
            _filterByButton.gameObject.SetActive(outerButtons);
            _filterByDisplay.gameObject.SetActive(outerButtons);
            _clearSortFilterButton.gameObject.SetActive(clearButton);
            _randomButton.gameObject.SetActive(outerButtons);

            RefreshCurrentSelectionDisplay();
            _currentUiState = state;
        }

        /// <summary>
        /// Adjust the text field of the sort by and filter by displays.
        /// </summary>
        private void RefreshCurrentSelectionDisplay()
        {
            string sortByDisplay = null;
            if (_model.Settings.sortMode == SongSortMode.Default)
            {
                sortByDisplay = "Title";
            }
            else
            {
                sortByDisplay = _model.Settings.sortMode.ToString();
            }
            _sortByDisplay.SetButtonText(sortByDisplay);
            if (_model.Settings.filterMode != SongFilterMode.Custom)
            {
                // Custom SongFilterMod implies that another mod has modified the text of this button (do not overwrite)
                _filterByDisplay.SetButtonText(_model.Settings.filterMode.ToString());
            }
        }

        /// <summary>
        /// Adjust the UI colors.
        /// </summary>
        public void RefreshSortButtonUI()
        {
            if (!_uiCreated)
            {
                return;
            }

            // So far all we need to refresh is the sort buttons.
            foreach (SongSortButton sortButton in _sortButtonGroup)
            {
                if (sortButton.SortMode.NeedsScoreSaberData() && !SongDataCore.Plugin.Songs.IsDataAvailable())
                {
                    BeatSaberUI.SetButtonBorder(sortButton.Button, Color.gray);
                }
                else
                {
                    BeatSaberUI.SetButtonBorder(sortButton.Button, Color.white);
                }

                if (sortButton.SortMode == _model.Settings.sortMode)
                {
                    if (this._model.Settings.invertSortResults)
                    {
                        BeatSaberUI.SetButtonBorder(sortButton.Button, Color.red);
                    }
                    else
                    {
                        BeatSaberUI.SetButtonBorder(sortButton.Button, Color.green);
                    }
                }
            }

            // refresh filter buttons
            foreach (SongFilterButton filterButton in _filterButtonGroup)
            {
                BeatSaberUI.SetButtonBorder(filterButton.Button, Color.white);
                if (filterButton.FilterMode == _model.Settings.filterMode)
                {
                    BeatSaberUI.SetButtonBorder(filterButton.Button, Color.green);
                }
            }

            if (this._model.Settings.invertSortResults)
            {
                BeatSaberUI.SetButtonBorder(_sortByDisplay, Color.red);
            }
            else
            {
                BeatSaberUI.SetButtonBorder(_sortByDisplay, Color.green);
            }

            if (this._model.Settings.filterMode != SongFilterMode.None)
            {
                BeatSaberUI.SetButtonBorder(_filterByDisplay, Color.green);
            }
            else
            {
                BeatSaberUI.SetButtonBorder(_filterByDisplay, Color.white);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void RefreshSongList()
        {
            if (!_uiCreated)
            {
                return;
            }

            _beatUi.RefreshSongList(_model.LastSelectedLevelId);
        }

        /// <summary>
        /// Helper for updating the model (which updates the song list)
        /// </summary>
        public void UpdateLevelDataModel()
        {
            try
            {
                Logger.Trace("UpdateLevelDataModel()");

                // get a current beatmap characteristic...
                if (_model.CurrentBeatmapCharacteristicSO == null && _uiCreated)
                {
                    _model.CurrentBeatmapCharacteristicSO = _beatUi.BeatmapCharacteristicSelectionViewController.GetPrivateField<BeatmapCharacteristicSO>("_selectedBeatmapCharacteristic");
                }

                _model.UpdateLevelRecords();
            }
            catch (Exception e)
            {
                Logger.Exception("SongBrowser UI crashed trying to update the internal song lists: ", e);
            }
        }

        /// <summary>
        /// Logic for fixing BeatSaber's level pack selection bugs.
        /// </summary>
        public bool UpdateLevelCollectionSelection()
        {
            if (_uiCreated)
            {
                IAnnotatedBeatmapLevelCollection currentSelected = _beatUi.GetCurrentSelectedAnnotatedBeatmapLevelCollection();
                Logger.Debug("Current selected level collection: {0}", currentSelected);

                if (String.IsNullOrEmpty(_model.Settings.currentLevelCollectionName))
                {
                    if (currentSelected == null)
                    {
                        Logger.Debug("No level collection selected, acquiring the first available, likely OST1...");
                        currentSelected = _beatUi.BeatmapLevelsModel.allLoadedBeatmapLevelPackCollection.beatmapLevelPacks[0];
                    }
                }
                else if (currentSelected == null || (currentSelected.collectionName != _model.Settings.currentLevelCollectionName))
                {
                    Logger.Debug("Automatically selecting level collection: {0}", _model.Settings.currentLevelCollectionName);
                    _beatUi.LevelFilteringNavigationController.didSelectAnnotatedBeatmapLevelCollectionEvent -= _levelFilteringNavController_didSelectAnnotatedBeatmapLevelCollectionEvent;

                    _lastLevelCollection = _beatUi.GetLevelCollectionByName(_model.Settings.currentLevelCollectionName);
                    if (_lastLevelCollection as PreviewBeatmapLevelPackSO)
                    {
                        Hide();
                    }
                    _beatUi.SelectLevelCollection(_model.Settings.currentLevelCollectionName);
                    _beatUi.LevelFilteringNavigationController.didSelectAnnotatedBeatmapLevelCollectionEvent += _levelFilteringNavController_didSelectAnnotatedBeatmapLevelCollectionEvent;
                }

                if (_lastLevelCollection == null)
                {
                    if (currentSelected.collectionName != SongBrowserModel.FilteredSongsCollectionName && currentSelected.collectionName != SongBrowserModel.PlaylistSongsCollectionName)
                    {
                        _lastLevelCollection = currentSelected;
                    }
                }

                Logger.Debug("Current Level Collection is: {0}", _lastLevelCollection);
                ProcessSongList();
            }

            return false;
        }
    }
}
 