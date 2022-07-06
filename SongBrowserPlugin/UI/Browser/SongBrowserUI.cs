using BeatSaberMarkupLanguage.Components;
using HMUI;
using SongBrowser.Internals;
using SongDataCore.BeatStar;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IPA.Utilities;
using UnityEngine;
using UnityEngine.UI;
using VRUIControls;
using System.Reflection;
using SongBrowser.Configuration;
using BeatSaberPlaylistsLib.Types;

namespace SongBrowser.UI
{
    public enum UIState
    {
        Disabled,
        Main,
        SortBy,
        FilterBy
    }

    public class SongBrowserViewController : ViewController
    {
        // Named instance
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
        private const float CLEAR_BUTTON_Y = -31.5f;
        private const float BUTTON_ROW_Y = -31.5f;

        // BeatSaber Internal UI structures
        DataAccess.BeatSaberUIController _beatUi;

        // New UI Elements
        private SongBrowserViewController _viewController;

        private List<SongSortButton> _sortButtonGroup;
        private List<SongFilterButton> _filterButtonGroup;

        private Button _sortByButton;
        private Button _sortByDisplay;
        private Button _filterByButton;
        private Button _filterByDisplay;
        private Button _randomButton;
        private Button _playlistExportButton;

        private Button _clearSortFilterButton;

        private SimpleDialogPromptViewController _deleteDialog;
        private Button _deleteButton;

        private Button _pageUpFastButton;
        private Button _pageDownFastButton;

        private RectTransform _ppStatButton;
        private RectTransform _starStatButton;
        private RectTransform _njsStatButton;
        private RectTransform _noteJumpStartBeatOffsetLabel;

        private IAnnotatedBeatmapLevelCollection _lastLevelCollection;
        bool _selectingCategory = false;
        private bool _deletingSong = false;

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
            Plugin.Log.Trace("CreateUI()");

            // Determine the flow controller to use
            LevelSelectionFlowCoordinator flowCoordinator;
            if (mode == MainMenuViewController.MenuButton.SoloFreePlay)
            {
                Plugin.Log.Debug("Entering SOLO mode...");
                flowCoordinator = Resources.FindObjectsOfTypeAll<SoloFreePlayFlowCoordinator>().Last();
            }
            else if (mode == MainMenuViewController.MenuButton.Party)
            {
                Plugin.Log.Debug("Entering PARTY mode...");
                flowCoordinator = Resources.FindObjectsOfTypeAll<PartyFreePlayFlowCoordinator>().Last();
            }
            else if (mode == MainMenuViewController.MenuButton.Multiplayer)
            {
                Plugin.Log.Debug("Entering Multiplayer mode...");
                flowCoordinator = Resources.FindObjectsOfTypeAll<MultiplayerLevelSelectionFlowCoordinator>().Last();
            }
            else
            {
                Plugin.Log.Info("Entering Unsupported mode...");
                return;
            }

            Plugin.Log.Debug("Done fetching Flow Coordinator for the appropriate mode...");

            _beatUi = new DataAccess.BeatSaberUIController(flowCoordinator);
            _lastLevelCollection = null;

            // returning to the menu and switching modes could trigger this.
            if (_uiCreated)
            {
                var screenContainer = Resources.FindObjectsOfTypeAll<Transform>().First(x => x.name == "ScreenContainer");
                var curvedCanvasSettings = screenContainer.GetComponents<CurvedCanvasSettings>().First();
                Plugin.Log.Debug($"CurvedCanvasRadius: {curvedCanvasSettings.radius}");

                var vcCanvasSettings = _viewController.GetComponent<CurvedCanvasSettings>();
                vcCanvasSettings.SetRadius(curvedCanvasSettings.radius);
                return;
            }

            try
            {
                // Create a view controller to store all SongBrowser elements
                if (_viewController)
                {
                    UnityEngine.GameObject.Destroy(_viewController);
                }

                var screenContainer = Resources.FindObjectsOfTypeAll<Transform>().First(x => x.name == "ScreenContainer");
                var curvedCanvasSettings = screenContainer.GetComponents<CurvedCanvasSettings>().First();
                Plugin.Log.Debug($"CurvedCanvasRadius: {curvedCanvasSettings.radius}");

                _viewController = BeatSaberUI.CreateCurvedViewController<SongBrowserViewController>("SongBrowserViewController", curvedCanvasSettings.radius);
                _viewController.rectTransform.SetParent(_beatUi.LevelCollectionNavigationController.rectTransform, false);
                _viewController.rectTransform.anchorMin = new Vector2(0f, 0f);
                _viewController.rectTransform.anchorMax = new Vector2(1f, 1f);
                _viewController.rectTransform.anchoredPosition = new Vector2(0, 0);
                _viewController.rectTransform.sizeDelta = new Vector2(curvedCanvasSettings.radius, 25);
                _viewController.gameObject.SetActive(true);

                // create song browser main ui
                CreateOuterUi();
                CreateSortButtons();
                CreateFilterButtons();
                CreateDeleteUI();
                CreateFastPageButtons();

                this.InstallHandlers();

                this.ModifySongStatsPanel();
                this.ResizeSongUI();

                _uiCreated = true;

                RefreshSortButtonUI();

                Plugin.Log.Debug("Done Creating UI...");
            }
            catch (Exception e)
            {
                Plugin.Log.Critical($"Exception during CreateUI: {e}");
            }
        }

        /// <summary>
        /// Create the outer ui.
        /// </summary>
        private void CreateOuterUi()
        {
            Plugin.Log.Debug("Creating outer UI...");

            float clearButtonX = -72.5f;
            float clearButtonY = CLEAR_BUTTON_Y;
            float buttonY = BUTTON_ROW_Y;
            float buttonHeight = 5.0f;
            float sortByButtonX = -62.5f + buttonHeight;
            float outerButtonFontSize = 3.0f;
            float displayButtonFontSize = 2.5f;
            float outerButtonWidth = 24.0f;
            float randomButtonWidth = 10.0f;

            // clear button
            _clearSortFilterButton = _viewController.CreateIconButton(
                "ClearSortAndFilterButton",
                "PracticeButton",
                new Vector2(clearButtonX, clearButtonY),
                new Vector2(randomButtonWidth, randomButtonWidth),
                () =>
                {
                    if (_currentUiState == UIState.FilterBy || _currentUiState == UIState.SortBy)
                    {
                        RefreshOuterUIState(UIState.Main);
                    }
                    else
                    {
                        OnClearButtonClickEvent();
                    }
                },
                Base64Sprites.XIcon,
                "Clear");
            _clearSortFilterButton.SetButtonBackgroundActive(false);

            // create SortBy button and its display
            float curX = sortByButtonX;

            Plugin.Log.Debug("Creating Sort By...");
            _sortByButton = _viewController.CreateUIButton("sortBy", "PracticeButton", new Vector2(curX, buttonY), new Vector2(outerButtonWidth, buttonHeight), () =>
            {
                RefreshOuterUIState(UIState.SortBy);
            }, "Sort By");
            _sortByButton.SetButtonTextSize(outerButtonFontSize);
            _sortByButton.ToggleWordWrapping(false);

            curX += outerButtonWidth;

            Plugin.Log.Debug("Creating Sort By Display...");
            _sortByDisplay = _viewController.CreateUIButton("sortByValue", "PracticeButton", new Vector2(curX, buttonY), new Vector2(outerButtonWidth, buttonHeight), () =>
            {
                OnSortButtonClickEvent(PluginConfig.Instance.SortMode);
            }, "");
            _sortByDisplay.SetButtonTextSize(displayButtonFontSize);
            _sortByDisplay.ToggleWordWrapping(false);

            curX += outerButtonWidth;

            // create FilterBy button and its display
            Plugin.Log.Debug("Creating Filter By...");
            _filterByButton = _viewController.CreateUIButton("filterBy", "PracticeButton", new Vector2(curX, buttonY), new Vector2(outerButtonWidth, buttonHeight), () =>
            {
                RefreshOuterUIState(UIState.FilterBy);
            }, "Filter By");
            _filterByButton.SetButtonTextSize(outerButtonFontSize);
            _filterByButton.ToggleWordWrapping(false);

            curX += outerButtonWidth;

            Plugin.Log.Debug("Creating Filter By Display...");
            _filterByDisplay = _viewController.CreateUIButton("filterValue", "PracticeButton", new Vector2(curX, buttonY), new Vector2(outerButtonWidth, buttonHeight), () =>
            {
                PluginConfig.Instance.FilterMode = SongFilterMode.None;
                CancelFilter();
                ProcessSongList();
                RefreshSongUI();
            }, "");
            _filterByDisplay.SetButtonTextSize(displayButtonFontSize);
            _filterByDisplay.ToggleWordWrapping(false);

            curX += (outerButtonWidth / 2.0f);

            // random button
            Plugin.Log.Debug("Creating Random Button...");
            _randomButton = _viewController.CreateIconButton("randomButton", "PracticeButton", new Vector2(curX + (randomButtonWidth / 4.0f), clearButtonY), new Vector2(randomButtonWidth, randomButtonWidth), () =>
            {
                OnSortButtonClickEvent(SongSortMode.Random);
            }, Base64Sprites.RandomIcon, "Random");
            _randomButton.SetButtonBackgroundActive(false);

            curX += (randomButtonWidth / 4.0f) * 2.0f;

            // playlist export
            Plugin.Log.Debug("Creating playlist export button...");
            _playlistExportButton = _viewController.CreateIconButton("playlistExportButton", "PracticeButton", new Vector2(curX + (randomButtonWidth / 4.0f), clearButtonY), new Vector2(randomButtonWidth, randomButtonWidth), () =>
            {
                ShowInputKeyboard(CreatePlaylistButtonPressed);
            }, Base64Sprites.PlaylistIcon, "Export Playlist");
            _playlistExportButton.SetButtonBackgroundActive(false);
        }

        /// <summary>
        /// Create the sort button ribbon
        /// </summary>
        private void CreateSortButtons()
        {
            Plugin.Log.Debug("Create sort buttons...");

            float sortButtonFontSize = 2.0f;
            float sortButtonX = -63.0f;
            float sortButtonWidth = 11.75f;
            float buttonSpacing = 0.25f;
            float buttonY = BUTTON_ROW_Y;
            float buttonHeight = 5.0f;

            List<KeyValuePair<string, SongSortMode>> sortModes = new List<KeyValuePair<string, SongSortMode>>()
            {
                new KeyValuePair<string, SongSortMode>("Title", SongSortMode.Default),
                new KeyValuePair<string, SongSortMode>("Author", SongSortMode.Author),
                new KeyValuePair<string, SongSortMode>("Mapper", SongSortMode.Mapper),
                new KeyValuePair<string, SongSortMode>("Newest", SongSortMode.Newest),
                new KeyValuePair<string, SongSortMode>("LastPlay", SongSortMode.LastPlayed),
                new KeyValuePair<string, SongSortMode>("Vanilla", SongSortMode.Vanilla),
                new KeyValuePair<string, SongSortMode>("#Plays", SongSortMode.YourPlayCount),
                new KeyValuePair<string, SongSortMode>("BPM", SongSortMode.Bpm),
                new KeyValuePair<string, SongSortMode>("Time", SongSortMode.Length),
                new KeyValuePair<string, SongSortMode>("PP", SongSortMode.PP),
                new KeyValuePair<string, SongSortMode>("Stars", SongSortMode.Stars),
                new KeyValuePair<string, SongSortMode>("UpVotes", SongSortMode.UpVotes),
                new KeyValuePair<string, SongSortMode>("Rating", SongSortMode.Rating),
                new KeyValuePair<string, SongSortMode>("Heat", SongSortMode.Heat),
            };

            _sortButtonGroup = new List<SongSortButton>();
            for (int i = 0; i < sortModes.Count; i++)
            {
                float curButtonX = sortButtonX + (sortButtonWidth * i) + (buttonSpacing * i);
                SongSortButton sortButton = new SongSortButton
                {
                    SortMode = sortModes[i].Value
                };

                sortButton.Button = _viewController.CreateUIButton(String.Format("Sort{0}Button", sortButton.SortMode), "PracticeButton",
                    new Vector2(curButtonX, buttonY), new Vector2(sortButtonWidth, buttonHeight),
                    () =>
                    {
                        OnSortButtonClickEvent(sortButton.SortMode);
                        RefreshOuterUIState(UIState.Main);
                    },
                    sortModes[i].Key);
                sortButton.Button.SetButtonTextSize(sortButtonFontSize);
                sortButton.Button.ToggleWordWrapping(false);

                _sortButtonGroup.Add(sortButton);
            }
        }

        /// <summary>
        /// Create the filter by buttons
        /// </summary>
        private void CreateFilterButtons()
        {
            Plugin.Log.Debug("Creating filter buttons...");

            float filterButtonFontSize = 2.25f;
            float filterButtonX = -63.0f;
            float filterButtonWidth = 14.25f;
            float buttonSpacing = 0.5f;
            float buttonY = BUTTON_ROW_Y;
            float buttonHeight = 5.0f;

            string[] filterButtonNames = new string[]
            {
                "Search", "Ranked", "Unranked", "Played", "Unplayed", "Requirements", "Easy", "Normal", "Hard", "Expert", "ExpertPlus"
            };

            SongFilterMode[] filterModes = new SongFilterMode[]
            {
                SongFilterMode.Search, SongFilterMode.Ranked, SongFilterMode.Unranked, SongFilterMode.Played, SongFilterMode.Unplayed, SongFilterMode.Requirements, SongFilterMode.Easy, SongFilterMode.Normal, SongFilterMode.Hard, SongFilterMode.Expert, SongFilterMode.ExpertPlus
            };

            _filterButtonGroup = new List<SongFilterButton>();
            for (int i = 0; i < filterButtonNames.Length; i++)
            {
                float curButtonX = filterButtonX + (filterButtonWidth * i) + (buttonSpacing * i);
                SongFilterButton filterButton = new SongFilterButton
                {
                    FilterMode = filterModes[i]
                };

                filterButton.Button = _viewController.CreateUIButton(String.Format("Filter{0}Button", filterButton.FilterMode), "PracticeButton",
                    new Vector2(curButtonX, buttonY), new Vector2(filterButtonWidth, buttonHeight),
                    () =>
                    {
                        OnFilterButtonClickEvent(filterButton.FilterMode);
                        RefreshOuterUIState(UIState.Main);
                    },
                    filterButtonNames[i]);
                filterButton.Button.SetButtonTextSize(filterButtonFontSize);
                filterButton.Button.ToggleWordWrapping(false);

                if (String.Equals(filterButtonNames[i], "Requirements"))
                {
                    filterButton.Button.interactable = Plugin.IsCustomJsonDataEnabled;
                }

                _filterButtonGroup.Add(filterButton);
            }
        }

        /// <summary>
        /// Create the fast page up and down buttons
        /// </summary>
        private void CreateFastPageButtons()
        {
            Plugin.Log.Debug("Creating fast scroll button...");
            _pageUpFastButton = BeatSaberUI.CreatePageButton("PageUpFast",
                _beatUi.LevelCollectionNavigationController.transform as RectTransform, "UpButton",
                new Vector2(2.0f, 24f),
                new Vector2(8f, 8f),
                delegate ()
                {
                    this.JumpSongList(-1, SEGMENT_PERCENT);
                }, Base64Sprites.DoubleArrow);

            _pageDownFastButton = BeatSaberUI.CreatePageButton("PageDownFast",
                _beatUi.LevelCollectionNavigationController.transform as RectTransform, "DownButton",
                new Vector2(2.0f, -24f),
                new Vector2(8f, 8f),
                delegate ()
                {
                    this.JumpSongList(1, SEGMENT_PERCENT);
                }, Base64Sprites.DoubleArrow);
        }

        /// <summary>
        /// Create the delete button in the play button container
        /// </summary>
        private void CreateDeleteUI()
        {
            Plugin.Log.Debug("Creating delete dialog...");
            _deleteDialog = UnityEngine.Object.Instantiate<SimpleDialogPromptViewController>(_beatUi.SimpleDialogPromptViewControllerPrefab);
            _deleteDialog.GetComponent<VRGraphicRaycaster>().SetField("_physicsRaycaster", BeatSaberUI.PhysicsRaycasterWithCache);
            _deleteDialog.name = "DeleteDialogPromptViewController";
            _deleteDialog.gameObject.SetActive(false);

            Plugin.Log.Debug("Creating delete button...");
            _deleteButton = BeatSaberUI.CreateIconButton("DeleteLevelButton", _beatUi.ActionButtons, "PracticeButton", Base64Sprites.DeleteIcon, "Delete Level");
            _deleteButton.transform.SetAsFirstSibling();
            _deleteButton.onClick.AddListener(delegate () {
                HandleDeleteSelectedLevel();
            });
        }

        /// <summary>
        /// Resize the stats panel to fit more stats.
        /// </summary>
        private void ModifySongStatsPanel()
        {
            // modify stat panel, inject extra row of stats
            Plugin.Log.Debug("Resizing Stats Panel...");

            var statsPanel = _beatUi.StandardLevelDetailView.GetField<LevelParamsPanel, StandardLevelDetailView>("_levelParamsPanel");
            (statsPanel.transform as RectTransform).Translate(0, 0.05f, 0);

            _ppStatButton = BeatSaberUI.CreateStatIcon("PPStatLabel",
                statsPanel.GetComponentsInChildren<RectTransform>().First(x => x.name == "NPS"),
                statsPanel.transform,
                Base64Sprites.GraphIcon,
                "PP Value");

            _starStatButton = BeatSaberUI.CreateStatIcon("StarStatLabel",
                statsPanel.GetComponentsInChildren<RectTransform>().First(x => x.name == "NotesCount"),
                statsPanel.transform,
                Base64Sprites.StarFullIcon,
                "Star Difficulty Rating");

            _njsStatButton = BeatSaberUI.CreateStatIcon("NoteJumpSpeedLabel",
                statsPanel.GetComponentsInChildren<RectTransform>().First(x => x.name == "ObstaclesCount"),
                statsPanel.transform,
                Base64Sprites.SpeedIcon,
                "Note Jump Speed");

            _noteJumpStartBeatOffsetLabel = BeatSaberUI.CreateStatIcon("NoteJumpStartBeatOffsetLabel",
                statsPanel.GetComponentsInChildren<RectTransform>().First(x => x.name == "BombsCount"),
                statsPanel.transform,
                Base64Sprites.NoteStartOffsetIcon,
                "Note Jump Start Beat Offset");
        }

        /// <summary>
        /// Resize some of the song table elements.
        /// </summary>
        public void ResizeSongUI()
        {
            // shrink play button container
            _beatUi.ActionButtons.localScale = new Vector3(0.875f, 0.875f, 0.875f);
        }

        /// <summary>
        /// Add our handlers into BeatSaber.
        /// </summary>
        private void InstallHandlers()
        {
            // level collection, level, difficulty handlers, characteristics
            _beatUi.LevelCollectionTableView.GetField<TableView, LevelCollectionTableView>("_tableView");

            // update stats
            _beatUi.LevelCollectionViewController.didSelectLevelEvent -= OnDidSelectLevelEvent;
            _beatUi.LevelCollectionViewController.didSelectLevelEvent += OnDidSelectLevelEvent;

            _beatUi.LevelDetailViewController.didChangeContentEvent -= OnDidPresentContentEvent;
            _beatUi.LevelDetailViewController.didChangeContentEvent += OnDidPresentContentEvent;

            _beatUi.LevelDetailViewController.didChangeDifficultyBeatmapEvent -= OnDidChangeDifficultyEvent;
            _beatUi.LevelDetailViewController.didChangeDifficultyBeatmapEvent += OnDidChangeDifficultyEvent;

            // update our view of the game state
            _beatUi.LevelFilteringNavigationController.didSelectAnnotatedBeatmapLevelCollectionEvent -= LevelFilteringNavController_didSelectAnnotatedBeatmapLevelCollectionEvent;
            _beatUi.LevelFilteringNavigationController.didSelectAnnotatedBeatmapLevelCollectionEvent += LevelFilteringNavController_didSelectAnnotatedBeatmapLevelCollectionEvent;

            _beatUi.AnnotatedBeatmapLevelCollectionsViewController.didSelectAnnotatedBeatmapLevelCollectionEvent -= HandleDidSelectAnnotatedBeatmapLevelCollection;
            _beatUi.AnnotatedBeatmapLevelCollectionsViewController.didSelectAnnotatedBeatmapLevelCollectionEvent += HandleDidSelectAnnotatedBeatmapLevelCollection;

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

            // stop add favorites from scrolling to the top
            _beatUi.StandardLevelDetailView.didFavoriteToggleChangeEvent += OnDidFavoriteToggleChangeEvent;

            _beatUi.LevelCollectionNavigationController.didPressActionButtonEvent += OnDidPressActionButton;
        }

        /// <summary>
        /// Fetch the metadata instance for the song being started.
        /// Store the current time of day as last played.
        /// </summary>
        /// <param name="viewController"></param>
        public virtual void OnDidPressActionButton(LevelCollectionNavigationController viewController)
        {
            SongMetadata metaData = SongMetadataStore.Instance.GetMetadataForLevelID(this._model.LastSelectedLevelId);
            metaData.LastPlayed = DateTime.UtcNow;
            _model.SortWasMissingData = true;
        }

        /// <summary>
        /// Handle favorite toggle event.
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        private void OnDidFavoriteToggleChangeEvent(StandardLevelDetailView arg1, Toggle arg2)
        {
            if (PluginConfig.Instance.CurrentLevelCategoryName == "Favorites")
            {
                // TODO - still scrolls to top in this view
            }
            else
            {
                StartCoroutine(AsyncForceScrollToPosition(_model.LastScrollIndex));
            }
        }

        /// <summary>
        /// Fix internal Beat Saber bug, tableview.reloadData() always resets position and Beat Saber does restore position.
        /// Wait until end of frame and forcefully set the scroll position.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public IEnumerator AsyncForceScrollToPosition(float position)
        {
            Plugin.Log.Debug($"Will attempt force scrolling to position [{_model.LastScrollIndex}] at end of frame.");

            yield return new WaitForEndOfFrame();

            var tv = _beatUi.LevelCollectionTableView.GetField<TableView, LevelCollectionTableView>("_tableView");
            var sv = tv.GetField<ScrollView, TableView>("_scrollView");
            Plugin.Log.Debug($"Force scrolling to {position}");
            sv.ScrollTo(position, false);
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

            if (!_model.SortWasMissingData && !_model.FilterWasMissingData)
            {
                yield break;
            }

            _asyncUpdating = true;

            while (_beatUi != null && (
                   _beatUi.LevelSelectionNavigationController.isInTransition ||
                   _beatUi.LevelDetailViewController.isInTransition ||
                   !_beatUi.LevelSelectionNavigationController.isInViewControllerHierarchy ||
                   !_beatUi.LevelSelectionNavigationController.isActiveAndEnabled))
            {
                yield return null;
            }

            //yield return new WaitForEndOfFrame();

            if (SongDataCore.Plugin.Songs.IsDataAvailable() &&
                    (PluginConfig.Instance.SortMode.NeedsScoreSaberData() ||
                    PluginConfig.Instance.FilterMode.NeedsScoreSaberData()) ||
                PluginConfig.Instance.SortMode.NeedsRefresh())
            {
                ProcessSongList();
                RefreshSongUI();
            }

            _asyncUpdating = false;
        }

        /// <summary>
        /// Helper to reduce code duplication...
        /// </summary>
        public void RefreshSongUI(bool scrollToLevel = true)
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

            this._model.ProcessSongList(_lastLevelCollection, _beatUi.LevelSelectionNavigationController);
        }

        /// <summary>
        /// Helper for common filter cancellation logic.
        /// </summary>
        public void CancelFilter()
        {
            Plugin.Log.Debug($"Cancelling filter, levelCollection {_lastLevelCollection}");
            PluginConfig.Instance.FilterMode = SongFilterMode.None;

            UpdateLevelCollectionSelection();
        }

        /// <summary>
        /// Playlists (fancy name for AnnotatedBeatmapLevelCollection)
        /// </summary>
        /// <param name="annotatedBeatmapLevelCollection"></param>
        public virtual void HandleDidSelectAnnotatedBeatmapLevelCollection(IAnnotatedBeatmapLevelCollection annotatedBeatmapLevelCollection)
        {
            Plugin.Log.Trace("HandleDidSelectAnnotatedBeatmapLevelCollection()");
            _lastLevelCollection = annotatedBeatmapLevelCollection;
            PluginConfig.Instance.CurrentLevelCategoryName = _beatUi.LevelFilteringNavigationController.selectedLevelCategory.ToString();
            Plugin.Log.Debug($"AnnotatedBeatmapLevelCollection, Selected Level Collection={_lastLevelCollection}");
        }

        /// <summary>
        /// Handler for level collection selection, controller.
        /// Sets the current level collection into the model and updates.
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        /// <param name="arg4"></param>
        private void LevelFilteringNavController_didSelectAnnotatedBeatmapLevelCollectionEvent(LevelFilteringNavigationController arg1, IAnnotatedBeatmapLevelCollection arg2,
            GameObject arg3, BeatmapCharacteristicSO arg4)
        {
            Plugin.Log.Trace($"_levelFilteringNavController_didSelectAnnotatedBeatmapLevelCollectionEvent(levelCollection={arg2})");

            if (arg2 == null)
            {
                // Probably means we transitioned between Music Packs and Playlists
                arg2 = _beatUi.GetCurrentSelectedAnnotatedBeatmapLevelCollection();
                if (arg2 == null)
                {
                    Plugin.Log.Warn("Nothing selected.  This is likely an error.");
                    return;
                }
            }

            Plugin.Log.Debug($"Selected Level Collection={arg2}");

            // Do something about preview level packs, they can't be used past this point
            if (arg2 as PreviewBeatmapLevelPackSO)
            {
                Plugin.Log.Info("Hiding SongBrowser, previewing a song pack.");
                Hide();
                return;
            }

            Show();

            // category transition, just record the new collection
            if (_selectingCategory)
            {
                Plugin.Log.Info("Transitioning level category");
                _lastLevelCollection = arg2;
                StartCoroutine(RefreshSongListEndOfFrame());
                return;
            }

            // Skip the first time - prevents a bunch of reload content spam
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
                    Plugin.Log.Debug("No level collection selected...");
                    return;
                }

                // store the real level collection
                if (levelCollection.collectionName != SongBrowserModel.FilteredSongsCollectionName && _lastLevelCollection != null)
                {
                    Plugin.Log.Debug($"Recording levelCollection: {levelCollection.collectionName}");
                    _lastLevelCollection = levelCollection;
                    PluginConfig.Instance.CurrentLevelCategoryName = _beatUi.LevelFilteringNavigationController.selectedLevelCategory.ToString();
                }

                // reset level selection
                if (!_deletingSong) {
                    _model.LastSelectedLevelId = null;                    
                }

                // save level collection
                PluginConfig.Instance.CurrentLevelCollectionName = levelCollection.collectionName;

                StartCoroutine(ProcessSongListEndOfFrame());
            }
            catch (Exception e)
            {
                Plugin.Log.Critical($"Exception handling SelectLevelCollection: {e}");
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

            bool scrollToLevel = true;
            if (_lastLevelCollection != null && _lastLevelCollection as IPlaylist != null)
            {
                scrollToLevel = false;
                PluginConfig.Instance.SortMode = SongSortMode.Original;
                RefreshSortButtonUI();
            }

            ProcessSongList();

            RefreshSongUI(scrollToLevel: scrollToLevel);
        }

        public IEnumerator RefreshSongListEndOfFrame()
        {
            yield return new WaitForEndOfFrame();
            RefreshSongUI();
        }

        /// <summary>
        /// Remove all filters, update song list, save.
        /// </summary>
        private void OnClearButtonClickEvent()
        {
            Plugin.Log.Debug("Clearing all sorts and filters.");

            PluginConfig.Instance.SortMode = SongSortMode.Original;
            PluginConfig.Instance.InvertSortResults = false;
            PluginConfig.Instance.FilterMode = SongFilterMode.None;

            CancelFilter();
            ProcessSongList();
            RefreshSongUI();
        }

        /// <summary>
        /// Sort button clicked.
        /// </summary>
        private void OnSortButtonClickEvent(SongSortMode sortMode)
        {
            Plugin.Log.Debug($"Sort button - {sortMode.ToString()} - pressed.");

            if ((sortMode.NeedsScoreSaberData() && !SongDataCore.Plugin.Songs.IsDataAvailable()))
            {
                Plugin.Log.Info("Data for sort type is not available.");
                return;
            }

            // Clear current selected level id so our song list jumps to the start
            _model.LastSelectedLevelId = null;

            if (PluginConfig.Instance.SortMode == sortMode)
            {
                _model.ToggleInverting();
            }

            PluginConfig.Instance.SortMode = sortMode;

            // update the seed
            if (PluginConfig.Instance.SortMode == SongSortMode.Random)
            {
                PluginConfig.Instance.RandomSongSeed = Guid.NewGuid().GetHashCode();

                if (PluginConfig.Instance.RandomInstantQueueSong)
                {
                    StartCoroutine(ForceStartSongEndOfFrame());
                }
            }

            ProcessSongList();
            RefreshSongUI();
        }

        /// <summary>
        /// Force a song to start end of frame (doing it earlier causes a stack of tracebacks).
        /// </summary>
        /// <returns></returns>
        private IEnumerator ForceStartSongEndOfFrame()
        {
            yield return new WaitForEndOfFrame();

            // Level loading is done async.
            // The level list might have been shuffled/reset.
            // Need to wait for the ActionButton to be active and enabled.
            var levelLoaded = false;
            var maxIter = 5;
            var i = 0;
            for (i = 0; i < maxIter && !levelLoaded; i++)
            {
                yield return new WaitForSeconds(0.5f);

                Button actionButton = _beatUi.ActionButtons.GetComponentsInChildren<Button>().FirstOrDefault(x => x.name == "ActionButton");
                if (actionButton != null)
                {
                    levelLoaded = actionButton.isActiveAndEnabled;
                }
            }

            _beatUi.LevelSelectionFlowCoordinator.InvokeMethod<LevelSelectionFlowCoordinator, LevelSelectionFlowCoordinator>("ActionButtonWasPressed", new object[0]);
        }

        /// <summary>
        /// Handle filter button logic.  Some filters have sub menus that need special logic.
        /// </summary>
        /// <param name="mode"></param>
        private void OnFilterButtonClickEvent(SongFilterMode mode)
        {
            Plugin.Log.Debug($"FilterButton {mode} clicked.");

            CancelFilter();

            var isAllSongs = _beatUi.LevelFilteringNavigationController.selectedLevelCategory == SelectLevelCategoryViewController.LevelCategory.All;

            var curCollection = _beatUi.GetCurrentSelectedAnnotatedBeatmapLevelCollection();
            if (_lastLevelCollection == null ||
                (curCollection != null &&
                curCollection.collectionName != SongBrowserModel.FilteredSongsCollectionName &&
                curCollection.collectionName != SongBrowserModel.PlaylistSongsCollectionName &&
                !isAllSongs))
            {
                _lastLevelCollection = _beatUi.GetCurrentSelectedAnnotatedBeatmapLevelCollection();
            }

            if (mode == SongFilterMode.Favorites)
            {
                _beatUi.SelectLevelCategory(SelectLevelCategoryViewController.LevelCategory.Favorites.ToString());
            }
            else
            {
                GameObject _noDataGO = _beatUi.LevelCollectionViewController.GetField<GameObject, LevelCollectionViewController>("_noDataInfoGO");
                string _headerText = _beatUi.LevelCollectionTableView.GetField<string, LevelCollectionTableView>("_headerText");
                Sprite _headerSprite = _beatUi.LevelCollectionTableView.GetField<Sprite, LevelCollectionTableView>("_headerSprite");

                IBeatmapLevelCollection levelCollection = _lastLevelCollection.beatmapLevelCollection;
                _beatUi.LevelCollectionViewController.SetData(levelCollection, _headerText, _headerSprite, false, _noDataGO);
            }

            // If selecting the same filter, cancel
            if (PluginConfig.Instance.FilterMode == mode)
            {
                PluginConfig.Instance.FilterMode = SongFilterMode.None;
            }
            else
            {
                PluginConfig.Instance.FilterMode = mode;
            }

            switch (mode)
            {
                case SongFilterMode.Search:
                    OnSearchButtonClickEvent();
                    break;
                default:
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
            Plugin.Log.Debug($"Filter button - {SongFilterMode.Search.ToString()} - pressed.");

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
                Plugin.Log.Trace("OnDidSelectLevelEvent()");

                if (level == null)
                {
                    Plugin.Log.Debug("No level selected?");
                    return;
                }

                if (PluginConfig.Instance == null)
                {
                    Plugin.Log.Debug("Settings not instantiated yet?");
                    return;
                }

                _model.LastSelectedLevelId = level.levelID;
                HandleDidSelectLevelRow(level);
            }
            catch (Exception e)
            {
                Plugin.Log.Critical($"Exception selecting song: {e}");
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
                Plugin.Log.Trace($"OnDidSelectBeatmapCharacteristic({bc.compoundIdPartName})");
                _model.CurrentBeatmapCharacteristicSO = bc;

                if (_beatUi.StandardLevelDetailView != null)
                {
                    RefreshScoreSaberData(_beatUi.StandardLevelDetailView.selectedDifficultyBeatmap.level);
                    RefreshNoteJumpSpeed(_beatUi.StandardLevelDetailView.selectedDifficultyBeatmap.noteJumpMovementSpeed,
                        _beatUi.StandardLevelDetailView.selectedDifficultyBeatmap.noteJumpStartBeatOffset);
                }
            }
            catch (Exception e)
            {
                Plugin.Log.Critical(e);
            }
        }

        /// <summary>
        /// Handle difficulty level selection.
        /// </summary>
        private void OnDidChangeDifficultyEvent(StandardLevelDetailViewController view, IDifficultyBeatmap beatmap)
        {
            Plugin.Log.Trace($"OnDidChangeDifficultyEvent({beatmap})");

            if (view.selectedDifficultyBeatmap == null)
            {
                return;
            }

            UpdateDeleteButtonState(view.selectedDifficultyBeatmap.level.levelID);
            RefreshScoreSaberData(view.selectedDifficultyBeatmap.level);
            RefreshNoteJumpSpeed(beatmap.noteJumpMovementSpeed, beatmap.noteJumpStartBeatOffset);
        }

        /// <summary>
        /// BeatSaber finished loading content.  This is when the difficulty is finally updated.
        /// </summary>
        /// <param name="view"></param>
        /// <param name="type"></param>
        private void OnDidPresentContentEvent(StandardLevelDetailViewController view, StandardLevelDetailViewController.ContentType type)
        {
            Plugin.Log.Trace("OnDidPresentContentEvent()");

            if (type != StandardLevelDetailViewController.ContentType.OwnedAndReady)
            {
                return;
            }

            if (view.selectedDifficultyBeatmap == null)
            {
                return;
            }

            // stash the scroll index
            var tv = _beatUi.LevelCollectionTableView.GetField<TableView, LevelCollectionTableView>("_tableView");
            var sv = tv.GetField<ScrollView, TableView>("_scrollView");
            _model.LastScrollIndex = sv.position;

            UpdateDeleteButtonState(_beatUi.LevelDetailViewController.selectedDifficultyBeatmap.level.levelID);
            RefreshScoreSaberData(view.selectedDifficultyBeatmap.level);
            RefreshNoteJumpSpeed(view.selectedDifficultyBeatmap.noteJumpMovementSpeed, view.selectedDifficultyBeatmap.noteJumpStartBeatOffset);
        }

        /// <summary>
        /// Refresh stats panel.
        /// </summary>
        /// <param name="level"></param>
        private void HandleDidSelectLevelRow(IPreviewBeatmapLevel level)
        {
            Plugin.Log.Trace($"HandleDidSelectLevelRow({level})");

            UpdateDeleteButtonState(level.levelID);
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
                    _deleteDialog.__DismissViewController(null);
                    _beatUi.ScreenSystem.titleViewController.gameObject.SetActive(true);

                    if (selectedButton == 0)
                    {
                        try
                        {
                            List<IPreviewBeatmapLevel> levels = _beatUi.GetCurrentLevelCollectionLevels().ToList();
                            String collection = _beatUi.GetCurrentSelectedAnnotatedBeatmapLevelCollection().collectionName;
                            String selectedLevelID = _beatUi.StandardLevelDetailView.selectedDifficultyBeatmap.level.levelID;
                            int selectedIndex = levels.FindIndex(x => x.levelID == selectedLevelID);

                            if (selectedIndex > -1)
                            {
                                CustomPreviewBeatmapLevel song = null;
                                Plugin.Log.Debug($"collection={collection}");
                                if (String.IsNullOrEmpty(collection))
                                {
                                    song = SongCore.Loader.CustomLevels.First(x => x.Value.levelID == selectedLevelID).Value;
                                }
                                else if (collection.Equals("WIP Levels"))
                                {
                                    song = SongCore.Loader.CustomWIPLevels.First(x => x.Value.levelID == selectedLevelID).Value;
                                }
                                else if (collection.Equals("Cached WIP Levels"))
                                {
                                    Plugin.Log.Warn("Cannot delete cached levels.");
                                    return;
                                }
                                else if (collection.Equals("Custom Levels"))
                                {
                                    song = SongCore.Loader.CustomLevels.First(x => x.Value.levelID == selectedLevelID).Value;
                                }
                                else
                                {
                                    var names = SongCore.Loader.SeperateSongFolders.Select(x => x.SongFolderEntry.Name);
                                    var separateFolders = SongCore.Loader.SeperateSongFolders;

                                    if (names.Count() > 0 && names.Contains(collection))
                                    {
                                        int folder_index = separateFolders.FindIndex(x => x.SongFolderEntry.Name.Equals(collection));
                                        song = separateFolders[folder_index].Levels.First(x => x.Value.levelID == selectedLevelID).Value;
                                    }
                                    else
                                    {
                                        // final guess - playlist
                                        song = SongCore.Loader.CustomLevels.First(x => x.Value.levelID == selectedLevelID).Value;
                                    }
                                }

                                if (song == null)
                                {
                                    Plugin.Log.Warn("Unable to find selected level.  Is it an official song?");
                                    return;
                                }

                                Plugin.Log.Info($"Deleting song: {song.customLevelPath}");
                                _deletingSong = true;
                                SongCore.Loader.Instance.DeleteSong(song.customLevelPath);
                                StartCoroutine(ClearSongDeletionFlag());

                                int removedLevels = levels.RemoveAll(x => x.levelID == selectedLevelID);
                                Plugin.Log.Info($"Removed [{removedLevels}] level(s) from song list!");

                                this.UpdateLevelDataModel();

                                // if we have a song to select at the same index, set the last selected level id, UI updates takes care of the rest.
                                if (selectedIndex < levels.Count)
                                {
                                    _model.LastSelectedLevelId = levels[selectedIndex].levelID;
                                }
                                else if (selectedIndex == levels.Count && selectedIndex >= 1)
                                {
                                    _model.LastSelectedLevelId = levels[selectedIndex - 1].levelID;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Plugin.Log.Error("Unable to delete song! Exception: " + e);
                        }
                    }
                });

            _beatUi.ScreenSystem.titleViewController.gameObject.SetActive(false);
            _beatUi.LevelSelectionNavigationController.__PresentViewController(_deleteDialog, null);
        }

        private IEnumerator ClearSongDeletionFlag() {
            yield return new WaitForEndOfFrame();
            _deletingSong = false;
        }

        /// <summary>
        /// Show Search Keyboard
        /// </summary>
        void ShowSearchKeyboard()
        {
            ShowInputKeyboard(SearchViewControllerSearchButtonPressed);
        }


        /// <summary>
        /// Display the onscreen keyboard
        /// </summary>
        /// <param name="enterPressedHandler">Handler for keyboard form submission.</param>
        void ShowInputKeyboard(Action<string> enterPressedHandler)
        {
            var modalKbTag = new BeatSaberMarkupLanguage.Tags.ModalKeyboardTag();
            var modalKbView = modalKbTag.CreateObject(_beatUi.LevelSelectionNavigationController.rectTransform);
            modalKbView.gameObject.SetActive(true);
            var modalKb = modalKbView.GetComponent<ModalKeyboard>();
            modalKb.gameObject.SetActive(true);
            modalKb.keyboard.EnterPressed += enterPressedHandler;
            modalKb.modalView.Show(true, true);
        }

        /// <summary>
        /// Handle search.
        /// </summary>
        /// <param name="searchFor"></param>
        private void SearchViewControllerSearchButtonPressed(string searchFor)
        {
            Plugin.Log.Debug($"Searching for \"{searchFor}\"...");

            PluginConfig.Instance.FilterMode = SongFilterMode.Search;
            PluginConfig.Instance.SearchTerms.Insert(0, searchFor);
            _model.LastSelectedLevelId = null;

            ProcessSongList();

            RefreshSongUI();
        }

        /// <summary>
        /// Handle playlist creation.
        /// </summary>
        /// <param name="searchFor"></param>
        private void CreatePlaylistButtonPressed(string playlistName)
        {
            if (string.IsNullOrWhiteSpace(playlistName))
            {
                return;
            }
            IPlaylist playlist = DataAccess.Playlist.CreateNew(playlistName, _beatUi.GetCurrentLevelCollectionLevels());
            if (playlist == null)
            {
                Plugin.Log.Error("Failed to create playlist.");
            }

            BeatSaberPlaylistsLib.PlaylistManager.DefaultManager.RequestRefresh(Assembly.GetExecutingAssembly().FullName);
            SongBrowserApplication.MainProgressBar.ShowMessage("Successfully Exported Playlist");
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

            int currentRow = _beatUi.LevelCollectionTableView.GetField<int, LevelCollectionTableView>("_selectedRow");
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

            Plugin.Log.Debug($"jumpDirection: {jumpDirection}, newRow: {newRow}");
            _beatUi.ScrollToLevelByRow(newRow);
            RefreshQuickScrollButtons();
        }

        /// <summary>
        /// Update GUI elements that show score saber data.
        /// </summary>
        public void RefreshScoreSaberData(IPreviewBeatmapLevel level)
        {
            Plugin.Log.Trace($"RefreshScoreSaberData({level.levelID})");

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
            Plugin.Log.Debug(difficultyString);

            // Check if we have data for this song
            Plugin.Log.Debug($"Checking if have info for song {level.songName}");
            var hash = SongBrowserModel.GetSongHash(level.levelID);
            if (SongDataCore.Plugin.Songs.Data.Songs.ContainsKey(hash))
            {
                Plugin.Log.Debug($"Checking if have difficulty for song [{level.songName}] difficulty [{difficultyString}]");
                BeatStarSong scoreSaberSong = SongDataCore.Plugin.Songs.Data.Songs[hash];
                BeatStarSongDifficultyStats scoreSaberSongDifficulty = scoreSaberSong.diffs.FirstOrDefault(x => String.Equals(x.diff, difficultyString));
                if (scoreSaberSongDifficulty != null)
                {
                    Plugin.Log.Debug("Display pp for song.");
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

            Plugin.Log.Debug("Done refreshing score saber stats.");
        }

        /// <summary>
        /// Helper to refresh the NJS widget.
        /// </summary>
        /// <param name="noteJumpMovementSpeed"></param>
        private void RefreshNoteJumpSpeed(float noteJumpMovementSpeed, float noteJumpStartBeatOffset)
        {
            BeatSaberUI.SetStatButtonText(_njsStatButton, String.Format("{0}", noteJumpMovementSpeed));
            BeatSaberUI.SetStatButtonText(_noteJumpStartBeatOffsetLabel, String.Format("{0}", noteJumpStartBeatOffset));
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
        /// Update delete button state.  Enable for custom levels, disable for all else.
        /// </summary>
        /// <param name="levelId"></param>
        public void UpdateDeleteButtonState(String levelId)
        {
            if (_deleteButton == null)
            {
                return;
            }

            _deleteButton.gameObject.SetActive(levelId.Length >= 32);
        }

        /// <summary>
        /// Show the UI.
        /// </summary>
        public void Show()
        {
            Plugin.Log.Trace("Show SongBrowserUI()");

            this.SetVisibility(true);
        }

        /// <summary>
        /// Hide the UI.
        /// </summary>
        public void Hide()
        {
            Plugin.Log.Trace("Hide SongBrowserUI()");

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

            _ppStatButton?.gameObject.SetActive(visible);
            _starStatButton?.gameObject.SetActive(visible);
            _njsStatButton?.gameObject.SetActive(visible);

            RefreshOuterUIState(visible == true ? UIState.Main : UIState.Disabled);

            _deleteButton?.gameObject.SetActive(visible);

            _pageUpFastButton?.gameObject.SetActive(visible);
            _pageDownFastButton?.gameObject.SetActive(visible);
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

            _sortByButton?.gameObject.SetActive(outerButtons);
            _sortByDisplay?.gameObject.SetActive(outerButtons);
            _filterByButton?.gameObject.SetActive(outerButtons);
            _filterByDisplay?.gameObject.SetActive(outerButtons);
            _clearSortFilterButton?.gameObject.SetActive(clearButton);
            _randomButton?.gameObject.SetActive(outerButtons);
            _playlistExportButton?.gameObject.SetActive(outerButtons);

            RefreshCurrentSelectionDisplay();
            _currentUiState = state;
        }

        /// <summary>
        /// Adjust the text field of the sort by and filter by displays.
        /// </summary>
        private void RefreshCurrentSelectionDisplay()
        {
            string sortByDisplay;
            if (PluginConfig.Instance.SortMode == SongSortMode.Default)
            {
                sortByDisplay = "Title";
            }
            else
            {
                sortByDisplay = PluginConfig.Instance.SortMode.ToString();
            }
            _sortByDisplay.SetButtonText(sortByDisplay);
            if (PluginConfig.Instance.FilterMode != SongFilterMode.Custom)
            {
                // Custom SongFilterMod implies that another mod has modified the text of this button (do not overwrite)
                _filterByDisplay.SetButtonText(PluginConfig.Instance.FilterMode.ToString());
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

            foreach (SongSortButton sortButton in _sortButtonGroup)
            {
                if (sortButton.SortMode.NeedsScoreSaberData() && !SongDataCore.Plugin.Songs.IsDataAvailable())
                {
                    sortButton.Button.SetButtonUnderlineColor(Color.gray);
                }
                else
                {
                    sortButton.Button.SetButtonUnderlineColor(Color.white);
                }

                if (sortButton.SortMode == PluginConfig.Instance.SortMode)
                {
                    if (PluginConfig.Instance.InvertSortResults)
                    {
                        sortButton.Button.SetButtonUnderlineColor(Color.red);
                    }
                    else
                    {
                        sortButton.Button.SetButtonUnderlineColor(Color.green);
                    }
                }
            }

            foreach (SongFilterButton filterButton in _filterButtonGroup)
            {
                filterButton.Button.SetButtonUnderlineColor(Color.white);
                if (filterButton.FilterMode == PluginConfig.Instance.FilterMode)
                {
                    filterButton.Button.SetButtonUnderlineColor(Color.green);
                }
            }

            if (PluginConfig.Instance.InvertSortResults)
            {
                _sortByDisplay.SetButtonUnderlineColor(Color.red);
            }
            else
            {
                _sortByDisplay.SetButtonUnderlineColor(Color.green);
            }

            if (PluginConfig.Instance.FilterMode != SongFilterMode.None)
            {
                _filterByDisplay.SetButtonUnderlineColor(Color.green);
            }
            else
            {
                _filterByDisplay.SetButtonUnderlineColor(Color.white);
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
                Plugin.Log.Trace("UpdateLevelDataModel()");

                // get a current beatmap characteristic...
                if (_model.CurrentBeatmapCharacteristicSO == null && _uiCreated)
                {
                    _model.CurrentBeatmapCharacteristicSO = _beatUi.BeatmapCharacteristicSelectionViewController.GetField<BeatmapCharacteristicSO, BeatmapCharacteristicSegmentedControlController>("_selectedBeatmapCharacteristic");
                }

                _model.UpdateLevelRecords();
            }
            catch (Exception e)
            {
                Plugin.Log.Critical($"SongBrowser UI crashed trying to update the internal song lists: {e}");
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
                Plugin.Log.Debug($"Updating level collection, current selected level collection: {currentSelected}");

                // select category
                if (!String.IsNullOrEmpty(PluginConfig.Instance.CurrentLevelCategoryName))
                {
                    _selectingCategory = true;
                    _beatUi.SelectLevelCategory(PluginConfig.Instance.CurrentLevelCategoryName);
                    _selectingCategory = false;
                }

                // select collection
                if (String.IsNullOrEmpty(PluginConfig.Instance.CurrentLevelCollectionName))
                {
                    if (currentSelected == null && String.IsNullOrEmpty(PluginConfig.Instance.CurrentLevelCategoryName))
                    {
                        Plugin.Log.Debug("No level collection selected, acquiring the first available, likely OST1...");
                        currentSelected = _beatUi.BeatmapLevelsModel.allLoadedBeatmapLevelPackCollection.beatmapLevelPacks[0];
                    }
                }
                else if (currentSelected == null || (currentSelected.collectionName != PluginConfig.Instance.CurrentLevelCollectionName))
                {
                    Plugin.Log.Debug($"Automatically selecting level collection: {PluginConfig.Instance.CurrentLevelCollectionName}");
                    _beatUi.LevelFilteringNavigationController.didSelectAnnotatedBeatmapLevelCollectionEvent -= LevelFilteringNavController_didSelectAnnotatedBeatmapLevelCollectionEvent;

                    _lastLevelCollection = _beatUi.GetLevelCollectionByName(PluginConfig.Instance.CurrentLevelCollectionName);
                    if (_lastLevelCollection as PreviewBeatmapLevelPackSO)
                    {
                        Hide();
                    }
                    _beatUi.SelectLevelCollection(PluginConfig.Instance.CurrentLevelCollectionName);
                    _beatUi.LevelFilteringNavigationController.didSelectAnnotatedBeatmapLevelCollectionEvent += LevelFilteringNavController_didSelectAnnotatedBeatmapLevelCollectionEvent;
                }

                if (_lastLevelCollection == null)
                {
                    if (currentSelected != null && currentSelected.collectionName != SongBrowserModel.FilteredSongsCollectionName && currentSelected.collectionName != SongBrowserModel.PlaylistSongsCollectionName)
                    {
                        _lastLevelCollection = currentSelected;
                    }
                }

                Plugin.Log.Debug($"Current Level Collection is: {_lastLevelCollection}");
                ProcessSongList();
            }

            return false;
        }
    }
}
