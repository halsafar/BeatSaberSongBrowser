using UnityEngine;
using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine.UI;
using HMUI;
using VRUI;
using SongBrowser.DataAccess;
using TMPro;
using Logger = SongBrowser.Logging.Logger;
using SongBrowser.DataAccess.BeatSaverApi;
using System.Collections;
using SongCore.Utilities;
using SongBrowser.Internals;
using CustomUI.BeatSaber;

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

        private Button _addFavoriteButton;

        private SimpleDialogPromptViewController _deleteDialog;
        private Button _deleteButton;        

        private Button _pageUpFastButton;
        private Button _pageDownFastButton;

        private SearchKeyboardViewController _searchViewController;

        private PlaylistFlowCoordinator _playListFlowCoordinator;

        private RectTransform _ppStatButton;
        private RectTransform _starStatButton;
        private RectTransform _njsStatButton;
        
        private Sprite _currentAddFavoriteButtonSprite;

        // Model
        private SongBrowserModel _model;

        public SongBrowserModel Model
        {
            get
            {
                return _model;
            }
        }

        // UI Created
        private bool _uiCreated = false;

        /// <summary>
        /// Constructor
        /// </summary>
        public SongBrowserUI() : base()
        {
            if (_model == null)
            {
                _model = new SongBrowserModel();
            }

            _model.Init();
        }

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
            }

            _beatUi = new DataAccess.BeatSaberUIController(flowCoordinator);

            // returning to the menu and switching modes could trigger this.
            if (_uiCreated)
            {
                return;
            }

            try
            {                
                if (_playListFlowCoordinator == null)
                {
                    _playListFlowCoordinator = UIBuilder.CreateFlowCoordinator<PlaylistFlowCoordinator>("PlaylistFlowCoordinator");
                    _playListFlowCoordinator.didFinishEvent += HandleDidSelectPlaylist;
                }

                // delete dialog
                this._deleteDialog = UnityEngine.Object.Instantiate<SimpleDialogPromptViewController>(_beatUi.SimpleDialogPromptViewControllerPrefab);
                this._deleteDialog.name = "DeleteDialogPromptViewController";
                this._deleteDialog.gameObject.SetActive(false);

                // create song browser main ui
                CreateOuterUi();
                CreateSortButtons();
                CreateFilterButtons();
                CreateAddFavoritesButton();
                CreateDeleteButton();
                CreateFastPageButtons();

                RefreshSortButtonUI();

                this.InstallHandlers();

                this.ModifySongStatsPanel();
                this.ResizeSongUI();

                _uiCreated = true;
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

            // clear button
            _clearSortFilterButton = _beatUi.LevelPackLevelsViewController.CreateUIButton("HowToPlayButton", new Vector2(clearButtonX, clearButtonY), new Vector2(buttonHeight, buttonHeight), () =>
            {
                OnClearButtonClickEvent();
            }, "",
            Base64Sprites.XIcon);
            _clearSortFilterButton.GetComponentsInChildren<HorizontalLayoutGroup>().First(btn => btn.name == "Content").padding = new RectOffset(1, 1, 0, 0);
            RectTransform textRect = _clearSortFilterButton.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(c => c.name == "Text");
            if (textRect != null)
            {
                UnityEngine.Object.Destroy(textRect.gameObject);
            }

            // create SortBy button and its display
            float curX = sortByButtonX;
            _sortByButton = _beatUi.LevelPackLevelsViewController.CreateUIButton("CreditsButton", new Vector2(curX, buttonY), new Vector2(outerButtonWidth, buttonHeight), () =>
            {
                RefreshOuterUIState(UIState.SortBy);
            }, "Sort By");
            _sortByButton.SetButtonTextSize(outerButtonFontSize);

            curX += outerButtonWidth;

            _sortByDisplay = _beatUi.LevelPackLevelsViewController.CreateUIButton("ApplyButton", new Vector2(curX, buttonY), new Vector2(outerButtonWidth, buttonHeight), () =>
            {
                this.Model.ToggleInverting();
                ProcessSongList();
                RefreshSongUI();
            }, "");
            _sortByDisplay.SetButtonTextSize(displayButtonFontSize);
            _sortByDisplay.ToggleWordWrapping(false);
            curX += outerButtonWidth;

            // create FilterBy button and its display
            _filterByButton = _beatUi.LevelPackLevelsViewController.CreateUIButton("CreditsButton", new Vector2(curX, buttonY), new Vector2(outerButtonWidth, buttonHeight), () =>
            {
                RefreshOuterUIState(UIState.FilterBy);
            }, "Filter By");
            _filterByButton.SetButtonTextSize(outerButtonFontSize);

            curX += outerButtonWidth;

            _filterByDisplay = _beatUi.LevelPackLevelsViewController.CreateUIButton("ApplyButton", new Vector2(curX, buttonY), new Vector2(outerButtonWidth, buttonHeight), () =>
            {
                CancelFilter();
                RefreshSongUI();
            }, "");
            _filterByDisplay.SetButtonTextSize(displayButtonFontSize);
            _filterByDisplay.ToggleWordWrapping(false);

            // random button
            _randomButton = _beatUi.LevelPackLevelsViewController.CreateUIButton("HowToPlayButton", new Vector2(curX + (outerButtonWidth / 2.0f) + (buttonHeight), clearButtonY), new Vector2(buttonHeight, buttonHeight), () =>
            {
                OnSortButtonClickEvent(SongSortMode.Random);
            }, "",
            Base64Sprites.RandomIcon);
            _randomButton.GetComponentsInChildren<HorizontalLayoutGroup>().First(btn => btn.name == "Content").padding = new RectOffset(0, 0, 0, 0);
            textRect = _randomButton.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(c => c.name == "Text");
            if (textRect != null)
            {
                UnityEngine.Object.Destroy(textRect.gameObject);
            }
        }

        /// <summary>
        /// Create the sort button ribbon
        /// </summary>
        private void CreateSortButtons()
        {
            Logger.Debug("Create sort buttons...");

            float sortButtonFontSize = 2.25f;
            float sortButtonX = -24.5f;
            float sortButtonWidth = 12.25f;
            float buttonSpacing = 0.5f;
            float buttonY = 37f;
            float buttonHeight = 5.0f;

            string[] sortButtonNames = new string[]
            {
                    "Song", "Author", "Newest", "YourPlays", "PP", "Difficult", "UpVotes", "PlayCount", "Rating"
            };

            SongSortMode[] sortModes = new SongSortMode[]
            {
                    SongSortMode.Default, SongSortMode.Author, SongSortMode.Newest, SongSortMode.YourPlayCount, SongSortMode.PP, SongSortMode.Difficulty,  SongSortMode.UpVotes, SongSortMode.PlayCount, SongSortMode.Rating
            };

            _sortButtonGroup = new List<SongSortButton>();
            for (int i = 0; i < sortButtonNames.Length; i++)
            {
                float curButtonX = sortButtonX + (sortButtonWidth * i) + (buttonSpacing * i);
                SongSortButton sortButton = new SongSortButton();
                sortButton.SortMode = sortModes[i];
                sortButton.Button = _beatUi.LevelPackLevelsViewController.CreateUIButton("ApplyButton",
                    new Vector2(curButtonX, buttonY), new Vector2(sortButtonWidth, buttonHeight),
                    () =>
                    {
                        OnSortButtonClickEvent(sortButton.SortMode);
                        RefreshOuterUIState(UIState.Main);
                    },
                    sortButtonNames[i]);
                sortButton.Button.SetButtonTextSize(sortButtonFontSize);
                sortButton.Button.GetComponentsInChildren<HorizontalLayoutGroup>().First(btn => btn.name == "Content").padding = new RectOffset(3, 3, 3, 3);
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

            float sortButtonFontSize = 2.25f;
            float sortButtonX = -24.5f;
            float sortButtonWidth = 12.25f;
            float buttonSpacing = 0.5f;
            float buttonY = 37f;
            float buttonHeight = 5.0f;

            List<Tuple<SongFilterMode, UnityEngine.Events.UnityAction, Sprite>> filterButtonSetup = new List<Tuple<SongFilterMode, UnityEngine.Events.UnityAction, Sprite>>()
                {
                    Tuple.Create<SongFilterMode, UnityEngine.Events.UnityAction, Sprite>(SongFilterMode.Favorites, OnFavoriteFilterButtonClickEvent, Base64Sprites.StarFullIcon),
                    Tuple.Create<SongFilterMode, UnityEngine.Events.UnityAction, Sprite>(SongFilterMode.Playlist, OnPlaylistButtonClickEvent, Base64Sprites.PlaylistIcon),
                    Tuple.Create<SongFilterMode, UnityEngine.Events.UnityAction, Sprite>(SongFilterMode.Search, OnSearchButtonClickEvent, Base64Sprites.SearchIcon),
                };

            _filterButtonGroup = new List<SongFilterButton>();
            for (int i = 0; i < filterButtonSetup.Count; i++)
            {
                Tuple<SongFilterMode, UnityEngine.Events.UnityAction, Sprite> t = filterButtonSetup[i];
                float curButtonX = sortButtonX + (sortButtonWidth * i) + (buttonSpacing * i);
                SongFilterButton filterButton = new SongFilterButton();
                filterButton.FilterMode = t.Item1;
                filterButton.Button = _beatUi.LevelPackLevelsViewController.CreateUIButton("ApplyButton",
                    new Vector2(curButtonX, buttonY), new Vector2(sortButtonWidth, buttonHeight),
                    t.Item2,
                    t.Item1.ToString());
                filterButton.Button.SetButtonTextSize(sortButtonFontSize);
                filterButton.Button.GetComponentsInChildren<HorizontalLayoutGroup>().First(btn => btn.name == "Content").padding = new RectOffset(3, 3, 3, 3);
                filterButton.Button.ToggleWordWrapping(false);
                filterButton.Button.onClick.AddListener(() =>
                {
                    RefreshOuterUIState(UIState.Main);
                });
                filterButton.Button.name = "Filter" + t.Item1.ToString() + "Button";

                _filterButtonGroup.Add(filterButton);
            }
        }

        /// <summary>
        /// Create the fast page up and down buttons
        /// </summary>
        private void CreateFastPageButtons()
        {
            Logger.Debug("Creating fast scroll button...");

            _pageUpFastButton = Instantiate(_beatUi.TableViewPageUpButton, _beatUi.LevelPackLevelsTableViewRectTransform, false);
            (_pageUpFastButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 1f);
            (_pageUpFastButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 1f);
            (_pageUpFastButton.transform as RectTransform).anchoredPosition = new Vector2(-26f, 0.25f);
            (_pageUpFastButton.transform as RectTransform).sizeDelta = new Vector2(8f, 6f);
            _pageUpFastButton.GetComponentsInChildren<RectTransform>().First(x => x.name == "BG").sizeDelta = new Vector2(8f, 6f);
            _pageUpFastButton.GetComponentsInChildren<UnityEngine.UI.Image>().First(x => x.name == "Arrow").sprite = Base64Sprites.DoubleArrow;
            _pageUpFastButton.onClick.AddListener(delegate ()
            {
                this.JumpSongList(-1, SEGMENT_PERCENT);
            });

            _pageDownFastButton = Instantiate(_beatUi.TableViewPageDownButton, _beatUi.LevelPackLevelsTableViewRectTransform, false);
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
        /// Create the +/- favorite button in the play button container.
        /// </summary>
        private void CreateAddFavoritesButton()
        {
            // Create add favorite button
            Logger.Debug("Creating Add to favorites button...");
            _addFavoriteButton = UIBuilder.CreateIconButton(_beatUi.PlayButtons, _beatUi.PracticeButton, Base64Sprites.AddToFavoritesIcon);
            _addFavoriteButton.onClick.AddListener(delegate () {
                ToggleSongInPlaylist();
            });
        }

        /// <summary>
        /// Create the delete button in the play button container
        /// </summary>
        private void CreateDeleteButton()
        {
            // Create delete button
            Logger.Debug("Creating delete button...");
            _deleteButton = UIBuilder.CreateIconButton(_beatUi.PlayButtons, _beatUi.PracticeButton, Base64Sprites.DeleteIcon);
            _deleteButton.onClick.AddListener(delegate () {
                HandleDeleteSelectedLevel();
            });
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
                r.sizeDelta = new Vector2(r.sizeDelta.x * 0.75f, r.sizeDelta.y * 0.75f);
            }

            for (int i = 0; i < valueTexts.Count; i++)
            {
                var text = valueTexts[i];
                text.fontSize = 3.25f;
            }

            // inject our components
            _ppStatButton = UnityEngine.Object.Instantiate(statTransforms[1], statsPanel.transform, false);
            UIBuilder.SetStatButtonIcon(_ppStatButton, Base64Sprites.GraphIcon);

            _starStatButton = UnityEngine.Object.Instantiate(statTransforms[1], statsPanel.transform, false);
            UIBuilder.SetStatButtonIcon(_starStatButton, Base64Sprites.StarFullIcon);

            _njsStatButton = UnityEngine.Object.Instantiate(statTransforms[1], statsPanel.transform, false);
            UIBuilder.SetStatButtonIcon(_njsStatButton, Base64Sprites.SpeedIcon);

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
            _beatUi.LevelPackLevelsTableViewRectTransform.anchoredPosition = new Vector2(0f, -2.5f);

            // Move the page up/down buttons a bit
            TableView tableView = ReflectionUtil.GetPrivateField<TableView>(_beatUi.LevelPackLevelsTableView, "_tableView");
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
            // level pack, level, difficulty handlers, characteristics
            TableView tableView = ReflectionUtil.GetPrivateField<TableView>(_beatUi.LevelPackLevelsTableView, "_tableView");

            _beatUi.LevelPackLevelsViewController.didSelectLevelEvent -= OnDidSelectLevelEvent;
            _beatUi.LevelPackLevelsViewController.didSelectLevelEvent += OnDidSelectLevelEvent;

            _beatUi.LevelDifficultyViewController.didSelectDifficultyEvent -= OnDidSelectDifficultyEvent;
            _beatUi.LevelDifficultyViewController.didSelectDifficultyEvent += OnDidSelectDifficultyEvent;

            _beatUi.LevelPacksTableView.didSelectPackEvent -= _levelPacksTableView_didSelectPackEvent;
            _beatUi.LevelPacksTableView.didSelectPackEvent += _levelPacksTableView_didSelectPackEvent;
            _beatUi.LevelPackViewController.didSelectPackEvent -= _levelPackViewController_didSelectPackEvent;
            _beatUi.LevelPackViewController.didSelectPackEvent += _levelPackViewController_didSelectPackEvent;

            _beatUi.BeatmapCharacteristicSelectionViewController.didSelectBeatmapCharacteristicEvent -= OnDidSelectBeatmapCharacteristic;
            _beatUi.BeatmapCharacteristicSelectionViewController.didSelectBeatmapCharacteristicEvent += OnDidSelectBeatmapCharacteristic;

            // make sure the quick scroll buttons don't desync with regular scrolling
            _beatUi.TableViewPageDownButton.onClick.AddListener(delegate ()
            {
                this.RefreshQuickScrollButtons();
            });
            _beatUi.TableViewPageUpButton.onClick.AddListener(delegate ()
            {
                this.RefreshQuickScrollButtons();
            });

            // finished level
            ResultsViewController resultsViewController = _beatUi.LevelSelectionFlowCoordinator.GetPrivateField<ResultsViewController>("_resultsViewController");
            resultsViewController.continueButtonPressedEvent += ResultsViewController_continueButtonPressedEvent;
        }

        /// <summary>
        /// Helper to reduce code duplication...
        /// </summary>
        private void RefreshSongUI(bool scrollToLevel=true)
        {
            RefreshSongList(scrollToLevel);
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

            this._model.ProcessSongList(_beatUi.GetCurrentSelectedLevelPack());
        }

        /// <summary>
        /// Helper for common filter cancellation logic.
        /// </summary>
        public void CancelFilter()
        {
            _model.Settings.filterMode = SongFilterMode.None;
            SongCore.Loader.Instance.RefreshLevelPacks();            
        }

        /// <summary>
        /// Handle updating the level pack selection after returning from a song.
        /// </summary>
        /// <param name="obj"></param>
        private void ResultsViewController_continueButtonPressedEvent(ResultsViewController obj)
        {
            StartCoroutine(this.UpdateLevelPackSelectionEndOfFrame());
        }

        /// <summary>
        /// TODO - evaluate this sillyness...
        /// </summary>
        /// <returns></returns>
        public IEnumerator UpdateLevelPackSelectionEndOfFrame()
        {
            yield return new WaitForEndOfFrame();

            try
            {
                bool didUpdateLevelPack = this.UpdateLevelPackSelection();
                if (!didUpdateLevelPack)
                {
                    _model.ProcessSongList(_beatUi.GetCurrentSelectedLevelPack());
                }
                _beatUi.SelectAndScrollToLevel(_beatUi.LevelPackLevelsTableView, _model.LastSelectedLevelId);
                RefreshQuickScrollButtons();
            }
            catch (Exception e)
            {
                Logger.Exception("Exception:", e);
            }
        }

        /// <summary>
        /// Handler for level pack selection.
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        private void _levelPacksTableView_didSelectPackEvent(LevelPacksTableView arg1, IBeatmapLevelPack arg2)
        {
            Logger.Trace("_levelPacksTableView_didSelectPackEvent(arg2={0})", arg2);

            try
            {
                RefreshSortButtonUI();
                RefreshQuickScrollButtons();
            }
            catch (Exception e)
            {
                Logger.Exception("Exception handling didSelectPackEvent...", e);
            }
        }

        /// <summary>
        /// Handler for level pack selection, controller.
        /// Sets the current level pack into the model and updates.
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        private void _levelPackViewController_didSelectPackEvent(LevelPacksViewController arg1, IBeatmapLevelPack arg2)
        {
            Logger.Trace("_levelPackViewController_didSelectPackEvent(arg2={0})", arg2);

            try
            {
                // reset filter mode always here
                if (this._model.Settings.currentLevelPackId != arg2.packID)
                {
                    this._model.Settings.filterMode = SongFilterMode.None;
                }

                // save level pack
                this._model.Settings.currentLevelPackId = arg2.packID;
                this._model.Settings.Save();

                this._model.ProcessSongList(arg2);

                // trickery to handle Downloader playlist level packs
                // We need to avoid scrolling to a level and then select the header
                bool scrollToLevel = true;
                if (arg2.packID.Contains("Playlist_"))
                {
                    scrollToLevel = false;
                }

                RefreshSongUI(scrollToLevel);
            }
            catch (Exception e)
            {
                Logger.Exception("Exception handling didSelectPackEvent...", e);
            }
        }

        /// <summary>
        /// Remove all filters, update song list, save.
        /// </summary>
        private void OnClearButtonClickEvent()
        {
            Logger.Debug("Clearing all sorts and filters.");

            _model.Settings.sortMode = SongSortMode.Original;
            _model.Settings.invertSortResults = false;
            CancelFilter();
            _model.Settings.Save();

            ProcessSongList();
            RefreshSongUI();
        }

        /// <summary>
        /// Sort button clicked.
        /// </summary>
        private void OnSortButtonClickEvent(SongSortMode sortMode)
        {
            Logger.Debug("Sort button - {0} - pressed.", sortMode.ToString());
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

            //Scroll to start of the list
            TableView listTableView = _beatUi.LevelPackLevelsTableView.GetPrivateField<TableView>("_tableView");
            listTableView.ScrollToCellWithIdx(0, TableView.ScrollPositionType.Beginning, false);

            // update the display
            _sortByDisplay.SetButtonText(_model.Settings.sortMode.ToString());
        }

        /// <summary>
        /// Filter by favorites.
        /// </summary>
        private void OnFavoriteFilterButtonClickEvent()
        {
            Logger.Debug("Filter button - {0} - pressed.", SongFilterMode.Favorites.ToString());

            if (_model.Settings.filterMode != SongFilterMode.Favorites)
            {
                _model.Settings.filterMode = SongFilterMode.Favorites;
                _beatUi.SelectLevelPack(PluginConfig.CUSTOM_SONG_LEVEL_PACK_ID);
            }
            else
            {
                CancelFilter();
            }

            _model.Settings.Save();

            ProcessSongList();
            RefreshSongUI();
        }

        /// <summary>
        /// Filter button clicked.  
        /// </summary>
        /// <param name="sortMode"></param>
        private void OnSearchButtonClickEvent()
        {
            Logger.Debug("Filter button - {0} - pressed.", SongFilterMode.Search.ToString());
            if (_model.Settings.filterMode != SongFilterMode.Search)
            {
                _beatUi.SelectLevelPack(PluginConfig.CUSTOM_SONG_LEVEL_PACK_ID);
                this.ShowSearchKeyboard();
            }
            else
            {
                CancelFilter();
                ProcessSongList();
                RefreshSongUI();

                _model.Settings.Save();
            }                        
        }

        /// <summary>
        /// Display the playlist selector.
        /// </summary>
        /// <param name="sortMode"></param>
        private void OnPlaylistButtonClickEvent()
        {
            Logger.Debug("Filter button - {0} - pressed.", SongFilterMode.Playlist.ToString());
            _model.LastSelectedLevelId = null;

            if (_model.Settings.filterMode != SongFilterMode.Playlist)
            {
                _beatUi.SelectLevelPack(PluginConfig.CUSTOM_SONG_LEVEL_PACK_ID);
                _playListFlowCoordinator.parentFlowCoordinator = _beatUi.LevelSelectionFlowCoordinator;
                _beatUi.LevelSelectionFlowCoordinator.InvokePrivateMethod("PresentFlowCoordinator", new object[] { _playListFlowCoordinator, null, false, false });                                
            }
            else
            {
                CancelFilter();
                
                ProcessSongList();
                RefreshSongUI();

                _model.Settings.Save();
            }
        }

        /// <summary>
        /// Adjust UI based on level selected.
        /// Various ways of detecting if a level is not properly selected.  Seems most hit the first one.
        /// </summary>
        private void OnDidSelectLevelEvent(LevelPackLevelsViewController view, IPreviewBeatmapLevel level)
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
            Logger.Trace("OnDidSelectBeatmapCharacteristic({0}", bc.name);
            _model.CurrentBeatmapCharacteristicSO = bc;
            _model.UpdateLevelRecords();
            this.RefreshSongList();
        }

        /// <summary>
        /// Handle difficulty level selection.
        /// </summary>
        private void OnDidSelectDifficultyEvent(BeatmapDifficultySegmentedControlController view, BeatmapDifficulty beatmap)
        {
            Logger.Trace("OnDidSelectDifficultyEvent({0})", beatmap);

            _deleteButton.interactable = (_beatUi.LevelDetailViewController.selectedDifficultyBeatmap.level.levelID.Length >= 32);
            this.RefreshScoreSaberData(_beatUi.LevelDetailViewController.selectedDifficultyBeatmap.level);
            this.RefreshNoteJumpSpeed(beatmap);
        }

        /// <summary>
        /// Refresh stats panel.
        /// </summary>
        /// <param name="level"></param>
        private void HandleDidSelectLevelRow(IPreviewBeatmapLevel level)
        {
            Logger.Trace("HandleDidSelectLevelRow({0})", level);

            _deleteButton.interactable = (level.levelID.Length >= 32);

            RefreshScoreSaberData(level);
            RefreshQuickScrollButtons();
            RefreshAddFavoriteButton(level.levelID);
        }

        /// <summary>
        /// Call Downloader delete.
        /// </summary>
        private void CallDownloaderDelete()
        {
            BeatSaverDownloader.UI.SongListTweaks.Instance.DeletePressed();
        }

        /// <summary>
        /// Pop up a delete dialog.
        /// </summary>
        private void HandleDeleteSelectedLevel()
        {
            bool DownloaderInstalled = CustomHelpers.IsModInstalled("BeatSaverDownloader");
            if (DownloaderInstalled)
            {
                CallDownloaderDelete();
                return;
            }

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
                            var levelsTableView = _beatUi.LevelPackLevelsViewController.GetPrivateField<LevelPackLevelsTableView>("_levelPackLevelsTableView");
                            List<IPreviewBeatmapLevel> levels = levelsTableView.GetPrivateField<IBeatmapLevelPack>("_pack").beatmapLevelCollection.beatmapLevels.ToList();
                            int selectedIndex = levels.FindIndex(x => x.levelID == _beatUi.StandardLevelDetailView.selectedDifficultyBeatmap.level.levelID);

                            if (selectedIndex > -1)
                            {
                                var song = new Song(SongCore.Loader.CustomLevels.First(x => x.Value.levelID == _beatUi.LevelDetailViewController.selectedDifficultyBeatmap.level.levelID).Value);
                                SongCore.Loader.Instance.DeleteSong(song.path);
                                this._model.RemoveSongFromLevelPack(_beatUi.GetCurrentSelectedLevelPack(), _beatUi.LevelDetailViewController.selectedDifficultyBeatmap.level.levelID);

                                this.UpdateLevelDataModel();
                                this.RefreshSongList();

                                int removedLevels = levels.RemoveAll(x => x.levelID == _beatUi.StandardLevelDetailView.selectedDifficultyBeatmap.level.levelID);
                                Logger.Info("Removed " + removedLevels + " level(s) from song list!");

                                TableView listTableView = levelsTableView.GetPrivateField<TableView>("_tableView");
                                listTableView.ScrollToCellWithIdx(selectedIndex, TableView.ScrollPositionType.Beginning, false);
                                levelsTableView.SetPrivateField("_selectedRow", selectedIndex);
                                listTableView.SelectCellWithIdx(selectedIndex, true);
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
        /// Handle selection of a playlist.  Show just the songs in the playlist.
        /// </summary>
        /// <param name="p"></param>
        private void HandleDidSelectPlaylist(Playlist p)
        {
            if (p != null)
            {
                Logger.Debug("Showing songs for playlist: {0}", p.playlistTitle);
                _model.Settings.filterMode = SongFilterMode.Playlist;
                _model.CurrentPlaylist = p;
                _model.Settings.Save();

                ProcessSongList();

                RefreshSongUI();
            }
            else
            {
                Logger.Debug("No playlist selected");
            }
        }

        /// <summary>
        /// Display the search keyboard
        /// </summary>
        void ShowSearchKeyboard()
        {
            if (_searchViewController == null)
            {
                _searchViewController = UIBuilder.CreateViewController<SearchKeyboardViewController>("SearchKeyboardViewController");
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
            var levels = _beatUi.GetCurrentLevelPackLevels();
            if (levels == null)
            {
                return;
            }

            int totalSize = _beatUi.GetLevelPackLevelCount();
            int segmentSize = (int)(totalSize * segmentPercent);

            // Jump at least one scree size.
            if (segmentSize < LIST_ITEMS_VISIBLE_AT_ONCE)
            {
                segmentSize = LIST_ITEMS_VISIBLE_AT_ONCE;
            }

            TableView tableView = ReflectionUtil.GetPrivateField<TableView>(_beatUi.LevelPackLevelsTableView, "_tableView");
            int currentRow = _beatUi.LevelPackLevelsTableView.GetPrivateField<int>("_selectedRow");
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
            _beatUi.SelectAndScrollToLevel(_beatUi.LevelPackLevelsTableView, levels[newRow].levelID);
            RefreshQuickScrollButtons();
        }

        /// <summary>
        /// Add/Remove song from favorites depending on if it already exists.
        /// </summary>
        private void ToggleSongInPlaylist()
        {
            IBeatmapLevel songInfo = _beatUi.LevelDetailViewController.selectedDifficultyBeatmap.level;
            if (_model.CurrentEditingPlaylist != null)
            {
                if (_model.CurrentEditingPlaylistLevelIds.Contains(songInfo.levelID))
                {
                    Logger.Info("Remove {0} from editing playlist", songInfo.songName);
                    _model.RemoveSongFromEditingPlaylist(songInfo);

                    if (_model.Settings.filterMode == SongFilterMode.Favorites)
                    {
                        this._model.ProcessSongList(_beatUi.GetCurrentSelectedLevelPack());
                        this.RefreshSongList();
                    }
                }
                else
                {
                    Logger.Info("Add {0} to editing playlist", songInfo.songName);
                    _model.AddSongToEditingPlaylist(songInfo);
                }
            }

            RefreshAddFavoriteButton(songInfo.levelID);

            _model.Settings.Save();
        }

        /// <summary>
        /// Update GUI elements that show score saber data.
        /// </summary>
        public void RefreshScoreSaberData(IPreviewBeatmapLevel level)
        {
            Logger.Trace("RefreshScoreSaberData({0})", level.levelID);

            if (ScoreSaberDatabaseDownloader.ScoreSaberDataFile == null)
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

            // Check for PP
            Logger.Debug("Checking if have info for song {0}", level.songName);
            var hash = CustomHelpers.GetSongHash(level.levelID);
            if (ScoreSaberDatabaseDownloader.ScoreSaberDataFile.SongHashToScoreSaberData.ContainsKey(hash))
            {
                Logger.Debug("Checking if have difficulty for song {0} difficulty {1}", level.songName, difficultyString);
                ScoreSaberSong scoreSaberSong = ScoreSaberDatabaseDownloader.ScoreSaberDataFile.SongHashToScoreSaberData[hash];
                ScoreSaberSongDifficultyStats scoreSaberSongDifficulty = scoreSaberSong.diffs.FirstOrDefault(x => String.Equals(x.diff, difficultyString));
                if (scoreSaberSongDifficulty != null)
                {
                    Logger.Debug("Display pp for song.");
                    double pp = scoreSaberSongDifficulty.pp;
                    double star = scoreSaberSongDifficulty.star;

                    UIBuilder.SetStatButtonText(_ppStatButton, String.Format("{0:0.#}", pp));
                    UIBuilder.SetStatButtonText(_starStatButton, String.Format("{0:0.#}", star));
                }
                else
                {
                    UIBuilder.SetStatButtonText(_ppStatButton, "NA");
                    UIBuilder.SetStatButtonText(_starStatButton, "NA");
                }
            }
            else
            {
                UIBuilder.SetStatButtonText(_ppStatButton, "NA");
                UIBuilder.SetStatButtonText(_starStatButton, "NA");
            }                

            Logger.Debug("Done refreshing score saber stats.");
        }

        /// <summary>
        /// Helper to refresh the NJS widget.
        /// </summary>
        /// <param name="beatmap"></param>
        private void RefreshNoteJumpSpeed(BeatmapDifficulty beatmap)
        {
            UIBuilder.SetStatButtonText(_njsStatButton, String.Format("{0}", beatmap.NoteJumpMovementSpeed()));
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

            _addFavoriteButton.gameObject.SetActive(visible);
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

            _sortButtonGroup.ForEach(x => x.Button.gameObject.SetActive(sortButtons));
            _filterButtonGroup.ForEach(x => x.Button.gameObject.SetActive(filterButtons));

            _sortByButton.gameObject.SetActive(outerButtons);
            _sortByDisplay.gameObject.SetActive(outerButtons);
            _filterByButton.gameObject.SetActive(outerButtons);
            _filterByDisplay.gameObject.SetActive(outerButtons);
            _clearSortFilterButton.gameObject.SetActive(outerButtons);
            _randomButton.gameObject.SetActive(outerButtons);

            RefreshCurrentSelectionDisplay();
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
            _filterByDisplay.SetButtonText(_model.Settings.filterMode.ToString());
        }

        /// <summary>
        /// Helper to quickly refresh add to favorites button
        /// </summary>
        /// <param name="levelId"></param>
        private void RefreshAddFavoriteButton(String levelId)
        {
            if (levelId == null)
            {
                _currentAddFavoriteButtonSprite = null;
            }
            else
            {
                if (_model.CurrentEditingPlaylistLevelIds.Contains(levelId))
                {
                    _currentAddFavoriteButtonSprite = Base64Sprites.RemoveFromFavoritesIcon;
                }
                else
                {
                    _currentAddFavoriteButtonSprite = Base64Sprites.AddToFavoritesIcon;
                }
            }

            _addFavoriteButton.SetButtonIcon(_currentAddFavoriteButtonSprite);
        }

        /// <summary>
        /// Adjust the UI colors.
        /// </summary>
        public void RefreshSortButtonUI()
        {
            // So far all we need to refresh is the sort buttons.
            foreach (SongSortButton sortButton in _sortButtonGroup)
            {
                UIBuilder.SetButtonBorder(sortButton.Button, Color.white);
                if (sortButton.SortMode == _model.Settings.sortMode)
                {
                    if (this._model.Settings.invertSortResults)
                    {
                        UIBuilder.SetButtonBorder(sortButton.Button, Color.red);
                    }
                    else
                    {
                        UIBuilder.SetButtonBorder(sortButton.Button, Color.green);
                    }
                }
            }

            // refresh filter buttons
            foreach (SongFilterButton filterButton in _filterButtonGroup)
            {
                UIBuilder.SetButtonBorder(filterButton.Button, Color.white);
                if (filterButton.FilterMode == _model.Settings.filterMode)
                {
                    UIBuilder.SetButtonBorder(filterButton.Button, Color.green);
                }
            }

            if (this._model.Settings.invertSortResults)
            {
                UIBuilder.SetButtonBorder(_sortByDisplay, Color.red);
            }
            else
            {
                UIBuilder.SetButtonBorder(_sortByDisplay, Color.green);
            }

            if (this._model.Settings.filterMode != SongFilterMode.None)
            {
                UIBuilder.SetButtonBorder(_filterByDisplay, Color.green);
            }
            else
            {
                UIBuilder.SetButtonBorder(_filterByDisplay, Color.white);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void RefreshSongList(bool scrollToLevel = true)
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

                bool didUpdateLevelPack = UpdateLevelPackSelection();
                if (didUpdateLevelPack)
                {
                    ProcessSongList();
                }
            }
            catch (Exception e)
            {
                Logger.Exception("SongBrowser UI crashed trying to update the internal song lists: ", e);
            }
        }

        /// <summary>
        /// Logic for fixing BeatSaber's level pack selection bugs.
        /// </summary>
        public bool UpdateLevelPackSelection()
        {
            if (_uiCreated)
            {
                IBeatmapLevelPack currentSelected = _beatUi.GetCurrentSelectedLevelPack();
                Logger.Debug("Current selected level pack: {0}", currentSelected);

                if (String.IsNullOrEmpty(_model.Settings.currentLevelPackId))
                {
                    if (currentSelected == null)
                    {
                        Logger.Debug("No level pack selected, acquiring the first available...");
                        var levelPackCollection = _beatUi.LevelPackViewController.GetPrivateField<IBeatmapLevelPackCollection>("_levelPackCollection");
                        currentSelected = levelPackCollection.beatmapLevelPacks[0];
                    }
                }
                else if (currentSelected == null || (currentSelected.packID != _model.Settings.currentLevelPackId))
                {
                    Logger.Debug("Automatically selecting level pack: {0}", _model.Settings.currentLevelPackId);

                    // HACK - BeatSaber seems to always go back to OST1 internally.
                    //      - Lets force it to the last pack id but not have SongBrowser functions fire.
                    // Turn off our event processing
                    _beatUi.LevelPackViewController.didSelectPackEvent -= _levelPackViewController_didSelectPackEvent;
                    _beatUi.LevelPacksTableView.didSelectPackEvent -= _levelPacksTableView_didSelectPackEvent;

                    var levelPack = _beatUi.GetLevelPackByPackId(_model.Settings.currentLevelPackId);
                    _beatUi.SelectLevelPack(_model.Settings.currentLevelPackId);

                    ProcessSongList();

                    _beatUi.LevelPackViewController.didSelectPackEvent += _levelPackViewController_didSelectPackEvent;
                    _beatUi.LevelPacksTableView.didSelectPackEvent += _levelPacksTableView_didSelectPackEvent;
                    
                    return true;
                }
            }

            return false;
        }
    }
}
 