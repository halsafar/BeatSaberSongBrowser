﻿using UnityEngine;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine.UI;
using HMUI;
using VRUI;
using SongBrowser.DataAccess;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using Logger = SongBrowser.Logging.Logger;
using SongBrowser.DataAccess.BeatSaverApi;
using System.Collections;
using SongCore.Utilities;
using SongBrowser.Internals;

namespace SongBrowser.UI
{
    /// <summary>
    /// Hijack the flow coordinator.  Have access to all StandardLevel easily.
    /// </summary>
    public class SongBrowserUI : MonoBehaviour
    {
        // Logging
        public const String Name = "SongBrowserUI";

        private const float SEGMENT_PERCENT = 0.1f;
        private const int LIST_ITEMS_VISIBLE_AT_ONCE = 6;

        // Beat Saber UI Elements
        private FlowCoordinator _levelSelectionFlowCoordinator;

        private LevelPacksViewController _levelPackViewController;
        private LevelPacksTableView _levelPacksTableView;
        private LevelPackDetailViewController _levelPackDetailViewController;

        private LevelPackLevelsViewController _levelPackLevelsViewController;
        private LevelPackLevelsTableView _levelPackLevelsTableView;
        private StandardLevelDetailViewController _levelDetailViewController;
        private StandardLevelDetailView _standardLevelDetailView;

        private BeatmapDifficultySegmentedControlController _levelDifficultyViewController;
        private BeatmapCharacteristicSegmentedControlController _beatmapCharacteristicSelectionViewController; 

        private DismissableNavigationController _levelSelectionNavigationController;        

        private RectTransform _levelPackLevelsTableViewRectTransform;

        private Button _tableViewPageUpButton;
        private Button _tableViewPageDownButton;
        private Button _playButton;

        // New UI Elements
        private List<SongSortButton> _sortButtonGroup;
        private List<SongFilterButton> _filterButtonGroup;

        private Button _clearSortFilterButton;

        private Button _addFavoriteButton;

        private SimpleDialogPromptViewController _simpleDialogPromptViewControllerPrefab;
        private SimpleDialogPromptViewController _deleteDialog;
        private Button _deleteButton;        

        private Button _pageUpFastButton;
        private Button _pageDownFastButton;

        private SearchKeyboardViewController _searchViewController;

        private PlaylistFlowCoordinator _playListFlowCoordinator;

        private RectTransform _ppStatButton;
        private RectTransform _starStatButton;
        private RectTransform _njsStatButton;

        // Cached items
        private Sprite _addFavoriteSprite;
        private Sprite _removeFavoriteSprite;
        private Sprite _currentAddFavoriteButtonSprite;

        // Plugin Compat checks
        private bool _detectedTwitchPluginQueue = false;
        private bool _checkedForTwitchPlugin = false;

        // Debug
        private int _sortButtonLastPushedIndex = 0;
        private int _lastRow = 0;

        // Model
        private SongBrowserModel _model;

        // UI Created
        private bool _uiCreated = false;

        public SongBrowserModel Model
        {
            get
            {
                return _model;
            }
        }

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

            _sortButtonLastPushedIndex = (int)(_model.Settings.sortMode);
        }

        /// <summary>
        /// Builds the UI for this plugin.
        /// </summary>
        public void CreateUI(MainMenuViewController.MenuButton mode)
        {
            Logger.Trace("CreateUI()");

            // Determine the flow controller to use
            if (mode == MainMenuViewController.MenuButton.SoloFreePlay)
            {
                Logger.Debug("Entering SOLO mode...");
                _levelSelectionFlowCoordinator = Resources.FindObjectsOfTypeAll<SoloFreePlayFlowCoordinator>().First();
            }
            else if (mode == MainMenuViewController.MenuButton.Party)
            {
                Logger.Debug("Entering PARTY mode...");
                _levelSelectionFlowCoordinator = Resources.FindObjectsOfTypeAll<PartyFreePlayFlowCoordinator>().First();
            }
            else
            {
                Logger.Debug("Entering SOLO CAMPAIGN mode...");
                _levelSelectionFlowCoordinator = Resources.FindObjectsOfTypeAll<CampaignFlowCoordinator>().First();
            }

            // returning to the menu and switching modes could trigger this.
            if (_uiCreated)
            {
                return;
            }

            try
            {
                // gather controllers and ui elements.
                _levelPackViewController = _levelSelectionFlowCoordinator.GetPrivateField<LevelPacksViewController>("_levelPacksViewController");
                Logger.Debug("Acquired LevelPacksViewController [{0}]", _levelPackViewController.GetInstanceID());

                _levelPackDetailViewController = _levelSelectionFlowCoordinator.GetPrivateField<LevelPackDetailViewController>("_levelPackDetailViewController");
                Logger.Debug("Acquired LevelPackDetailViewController [{0}]", _levelPackDetailViewController.GetInstanceID());

                _levelPacksTableView = _levelPackViewController.GetPrivateField<LevelPacksTableView>("_levelPacksTableView");
                Logger.Debug("Acquired LevelPacksTableView [{0}]", _levelPacksTableView.GetInstanceID());

                _levelPackLevelsViewController = _levelSelectionFlowCoordinator.GetPrivateField<LevelPackLevelsViewController>("_levelPackLevelsViewController");
                Logger.Debug("Acquired LevelPackLevelsViewController [{0}]", _levelPackLevelsViewController.GetInstanceID());

                _levelPackLevelsTableView = this._levelPackLevelsViewController.GetPrivateField<LevelPackLevelsTableView>("_levelPackLevelsTableView");
                Logger.Debug("Acquired LevelPackLevelsTableView [{0}]", _levelPackLevelsTableView.GetInstanceID());

                _levelDetailViewController = _levelSelectionFlowCoordinator.GetPrivateField<StandardLevelDetailViewController>("_levelDetailViewController");
                Logger.Debug("Acquired StandardLevelDetailViewController [{0}]", _levelDetailViewController.GetInstanceID());

                _standardLevelDetailView = _levelDetailViewController.GetPrivateField<StandardLevelDetailView>("_standardLevelDetailView");
                Logger.Debug("Acquired StandardLevelDetailView [{0}]", _standardLevelDetailView.GetInstanceID());

                _beatmapCharacteristicSelectionViewController = Resources.FindObjectsOfTypeAll<BeatmapCharacteristicSegmentedControlController>().First();
                Logger.Debug("Acquired BeatmapCharacteristicSegmentedControlController [{0}]", _beatmapCharacteristicSelectionViewController.GetInstanceID());

                _levelSelectionNavigationController = _levelSelectionFlowCoordinator.GetPrivateField<DismissableNavigationController>("_navigationController");
                Logger.Debug("Acquired DismissableNavigationController [{0}]", _levelSelectionNavigationController.GetInstanceID());

                _levelDifficultyViewController = _standardLevelDetailView.GetPrivateField<BeatmapDifficultySegmentedControlController>("_beatmapDifficultySegmentedControlController");
                Logger.Debug("Acquired BeatmapDifficultySegmentedControlController [{0}]", _levelDifficultyViewController.GetInstanceID());

                _levelPackLevelsTableViewRectTransform = _levelPackLevelsTableView.transform as RectTransform;
                Logger.Debug("Acquired TableViewRectTransform from LevelPackLevelsTableView [{0}]", _levelPackLevelsTableViewRectTransform.GetInstanceID());

                TableView tableView = ReflectionUtil.GetPrivateField<TableView>(_levelPackLevelsTableView, "_tableView");
                _tableViewPageUpButton = tableView.GetPrivateField<Button>("_pageUpButton");
                _tableViewPageDownButton = tableView.GetPrivateField<Button>("_pageDownButton");
                Logger.Debug("Acquired Page Up and Down buttons...");

                _playButton = _standardLevelDetailView.GetComponentsInChildren<Button>().FirstOrDefault(x => x.name == "PlayButton");
                Logger.Debug("Acquired PlayButton [{0}]", _playButton.GetInstanceID());

                _simpleDialogPromptViewControllerPrefab = Resources.FindObjectsOfTypeAll<SimpleDialogPromptViewController>().First();

                if (_playListFlowCoordinator == null)
                {
                    _playListFlowCoordinator = UIBuilder.CreateFlowCoordinator<PlaylistFlowCoordinator>("PlaylistFlowCoordinator");
                    _playListFlowCoordinator.didFinishEvent += HandleDidSelectPlaylist;
                }

                // delete dialog
                this._deleteDialog = UnityEngine.Object.Instantiate<SimpleDialogPromptViewController>(this._simpleDialogPromptViewControllerPrefab);
                this._deleteDialog.name = "DeleteDialogPromptViewController";
                this._deleteDialog.gameObject.SetActive(false);

                // sprites
                this._addFavoriteSprite = Base64Sprites.AddToFavoritesIcon;
                this._removeFavoriteSprite = Base64Sprites.RemoveFromFavoritesIcon;

                // create song browser main ui
                this.CreateUIElements();

                this.InstallHandlers();

                this.ResizeStatsPanel();
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
        /// Builds the SongBrowser UI
        /// </summary>
        private void CreateUIElements()
        {
            Logger.Trace("CreateUIElements");

            try
            {
                // Gather some transforms and templates to use.
                RectTransform sortButtonTransform = this._levelSelectionNavigationController.transform as RectTransform;
                RectTransform otherButtonTransform = this._levelDetailViewController.transform as RectTransform;
                Button sortButtonTemplate = Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "HowToPlayButton"));
                Button otherButtonTemplate = Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "HowToPlayButton"));

                RectTransform playContainerRect = _standardLevelDetailView.GetComponentsInChildren<RectTransform>().First(x => x.name == "PlayContainer");
                RectTransform playButtonsRect = playContainerRect.GetComponentsInChildren<RectTransform>().First(x => x.name == "PlayButtons");

                Button practiceButton = playButtonsRect.GetComponentsInChildren<Button>().First(x => x.name == "PracticeButton");
                RectTransform practiceButtonRect = (practiceButton.transform as RectTransform);

                Button playButton = Resources.FindObjectsOfTypeAll<Button>().First(x => x.name == "PlayButton");
                RectTransform playButtonRect = (playButton.transform as RectTransform);
                Sprite arrowIcon = Resources.FindObjectsOfTypeAll<Sprite>().First(x => x.name == "ArrowIcon");
                Sprite borderSprite = Resources.FindObjectsOfTypeAll<Sprite>().First(x => x.name == "RoundRectBigStroke");

                // Create Sorting Songs By-Buttons
                Logger.Debug("Start creation of UI...");
                float buttonSpacing = 0.5f;                                
                float fontSize = 2.0f;
                float buttonWidth = 12.25f;
                float buttonHeight = 5.0f;
                float startButtonX = 22.50f;
                float curButtonX = 0.0f;
                float buttonY = -5.25f;
                Vector2 iconButtonSize = new Vector2(buttonHeight, buttonHeight);

                // Create cancel button
                Logger.Debug("Creating cancel button...");
                _clearSortFilterButton = UIBuilder.CreateIconButton(
                    sortButtonTransform, 
                    otherButtonTemplate, 
                    Base64Sprites.XIcon, 
                    new Vector2(startButtonX - buttonHeight, buttonY), 
                    new Vector2(iconButtonSize.x, iconButtonSize.y),
                    new Vector2(3.5f, 3.5f),
                    new Vector2(1.0f, 1.0f),
                    0);
                _clearSortFilterButton.onClick.RemoveAllListeners();
                _clearSortFilterButton.onClick.AddListener(delegate () {
                    OnClearButtonClickEvent();
                });

                startButtonX += (buttonHeight);

                // define sort buttons
                Logger.Debug("Create sort buttons...");

                string[] sortButtonNames = new string[]
                {
                    "Song", "Author", "Newest", "Plays", "PP", "Difficult", "Random"
                };

                SongSortMode[] sortModes = new SongSortMode[]
                {
                    SongSortMode.Default, SongSortMode.Author, SongSortMode.Newest, SongSortMode.PlayCount, SongSortMode.PP, SongSortMode.Difficulty, SongSortMode.Random
                };
                
                _sortButtonGroup = new List<SongSortButton>();
                for (int i = 0; i < sortButtonNames.Length; i++)
                {
                    curButtonX = startButtonX + (buttonWidth*i) + (buttonSpacing*i);
                    SongSortButton newButton = UIBuilder.CreateSortButton(sortButtonTransform, sortButtonTemplate, arrowIcon, borderSprite,
                        sortButtonNames[i],
                        fontSize,
                        curButtonX,
                        buttonY,
                        buttonWidth,
                        buttonHeight,
                        sortModes[i],
                        OnSortButtonClickEvent);
                    _sortButtonGroup.Add(newButton);
                    newButton.Button.name = "Sort" + sortModes[i].ToString() + "Button";
                }

                // Create filter buttons
                Logger.Debug("Creating filter buttons...");

                float filterButtonX = curButtonX + (buttonWidth);

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
                    Button b = UIBuilder.CreateIconButton(sortButtonTransform, otherButtonTemplate,
                        t.Item3,
                        new Vector2(filterButtonX + (iconButtonSize.x * i) + (buttonSpacing * i), buttonY),
                        new Vector2(iconButtonSize.x, iconButtonSize.y), 
                        new Vector2(3.5f, 3.5f),
                        new Vector2(1.0f, 1.0f),
                        0);
                    SongFilterButton filterButton = new SongFilterButton
                    {
                        Button = b,
                        FilterMode = t.Item1
                    };
                    b.onClick.AddListener(t.Item2);
                    filterButton.Button.name = "Filter" + t.Item1.ToString() + "Button";
                    _filterButtonGroup.Add(filterButton);                    
                }

                // Create add favorite button                
                Logger.Debug("Creating Add to favorites button...");
                _addFavoriteButton = UIBuilder.CreateIconButton(playButtonsRect,
                    practiceButton,
                    Base64Sprites.AddToFavoritesIcon
                );
                _addFavoriteButton.onClick.RemoveAllListeners();
                _addFavoriteButton.onClick.AddListener(delegate () {
                    ToggleSongInPlaylist();
                });

                // Create delete button          
                Logger.Debug("Creating delete button...");
                _deleteButton = UIBuilder.CreateIconButton(playButtonsRect,
                    practiceButton,
                    Base64Sprites.DeleteIcon
                );
                _deleteButton.onClick.RemoveAllListeners();
                _deleteButton.onClick.AddListener(delegate () {
                    HandleDeleteSelectedLevel();
                });

                // Create fast scroll buttons
                int pageFastButtonX = 25;
                Vector2 pageFastSize = new Vector2(12.5f, 7.75f);
                Vector2 pageFastIconSize = new Vector2(0.1f, 0.1f);
                Vector2 pageFastIconScale = new Vector2(0.4f, 0.4f);

                Logger.Debug("Creating fast scroll button...");
                _pageUpFastButton = UIBuilder.CreateIconButton(sortButtonTransform, otherButtonTemplate, arrowIcon,
                    new Vector2(pageFastButtonX, -13f),
                    pageFastSize,
                    pageFastIconSize,
                    pageFastIconScale,
                    180);
                UnityEngine.GameObject.Destroy(_pageUpFastButton.GetComponentsInChildren<UnityEngine.UI.Image>().First(btn => btn.name == "Stroke"));
                _pageUpFastButton.onClick.AddListener(delegate ()
                {
                    this.JumpSongList(-1, SEGMENT_PERCENT);
                });
                
                _pageDownFastButton = UIBuilder.CreateIconButton(sortButtonTransform, otherButtonTemplate, arrowIcon,
                    new Vector2(pageFastButtonX, -80f),
                    pageFastSize,
                    pageFastIconSize,
                    pageFastIconScale,
                    0);
                
                UnityEngine.GameObject.Destroy(_pageDownFastButton.GetComponentsInChildren<UnityEngine.UI.Image>().First(btn => btn.name == "Stroke"));
                _pageDownFastButton.onClick.AddListener(delegate ()
                {
                    this.JumpSongList(1, SEGMENT_PERCENT);
                });
                                
                RefreshSortButtonUI();

                Logger.Debug("Done Creating UIElements");
            }
            catch (Exception e)
            {
                Logger.Exception("Exception CreateUIElements:", e);
            }
        }

        /// <summary>
        /// Resize the stats panel to fit more stats.
        /// </summary>
        private void ResizeStatsPanel()
        {
            // modify details view
            Logger.Debug("Resizing Stats Panel...");

            var statsPanel = _standardLevelDetailView.GetPrivateField<LevelParamsPanel>("_levelParamsPanel");
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

            _ppStatButton = UnityEngine.Object.Instantiate(statTransforms[1], statsPanel.transform, false);
            UIBuilder.SetStatButtonIcon(_ppStatButton, Base64Sprites.GraphIcon);

            _starStatButton = UnityEngine.Object.Instantiate(statTransforms[1], statsPanel.transform, false);
            UIBuilder.SetStatButtonIcon(_starStatButton, Base64Sprites.StarIcon);

            _njsStatButton = UnityEngine.Object.Instantiate(statTransforms[1], statsPanel.transform, false);
            UIBuilder.SetStatButtonIcon(_njsStatButton, Base64Sprites.SpeedIcon);

            // shrink title
            var titleText = this._levelDetailViewController.GetComponentsInChildren<TextMeshProUGUI>(true).First(x => x.name == "SongNameText");            
            titleText.fontSize = 5.0f;
        }

        /// <summary>
        /// Resize some of the song table elements.
        /// </summary>
        public void ResizeSongUI()
        {
            // Reposition the table view a bit
            _levelPackLevelsTableViewRectTransform.anchoredPosition = new Vector2(0f, -2.5f);

            // Move the page up/down buttons a bit
            TableView tableView = ReflectionUtil.GetPrivateField<TableView>(_levelPackLevelsTableView, "_tableView");
            RectTransform pageUpButton = _tableViewPageUpButton.transform as RectTransform;
            RectTransform pageDownButton = _tableViewPageDownButton.transform as RectTransform;
            pageUpButton.anchoredPosition = new Vector2(pageUpButton.anchoredPosition.x, pageUpButton.anchoredPosition.y - 1f);
            pageDownButton.anchoredPosition = new Vector2(pageDownButton.anchoredPosition.x, pageDownButton.anchoredPosition.y + 1f);

            // shrink play button container
            RectTransform playContainerRect = _standardLevelDetailView.GetComponentsInChildren<RectTransform>().First(x => x.name == "PlayContainer");
            RectTransform playButtonsRect = playContainerRect.GetComponentsInChildren<RectTransform>().First(x => x.name == "PlayButtons");
            playButtonsRect.localScale = new Vector3(0.825f, 0.825f, 0.825f);
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

            _clearSortFilterButton.gameObject.SetActive(visible);
            _sortButtonGroup.ForEach(x => x.Button.gameObject.SetActive(visible));
            _filterButtonGroup.ForEach(x => x.Button.gameObject.SetActive(visible));

            _addFavoriteButton.gameObject.SetActive(visible);
            _deleteButton.gameObject.SetActive(visible);

            _pageUpFastButton.gameObject.SetActive(visible);
            _pageDownFastButton.gameObject.SetActive(visible);
        }

        /// <summary>
        /// Add our handlers into BeatSaber.
        /// </summary>
        private void InstallHandlers()
        {
            // level pack, level, difficulty handlers, characteristics
            TableView tableView = ReflectionUtil.GetPrivateField<TableView>(_levelPackLevelsTableView, "_tableView");

            tableView.didSelectCellWithIdxEvent -= HandleDidSelectTableViewRow;
            tableView.didSelectCellWithIdxEvent += HandleDidSelectTableViewRow;

            _levelPackLevelsViewController.didSelectLevelEvent -= OnDidSelectLevelEvent;
            _levelPackLevelsViewController.didSelectLevelEvent += OnDidSelectLevelEvent;

            _levelDifficultyViewController.didSelectDifficultyEvent -= OnDidSelectDifficultyEvent;
            _levelDifficultyViewController.didSelectDifficultyEvent += OnDidSelectDifficultyEvent;

            _levelPacksTableView.didSelectPackEvent -= _levelPacksTableView_didSelectPackEvent;
            _levelPacksTableView.didSelectPackEvent += _levelPacksTableView_didSelectPackEvent;
            _levelPackViewController.didSelectPackEvent -= _levelPackViewController_didSelectPackEvent;
            _levelPackViewController.didSelectPackEvent += _levelPackViewController_didSelectPackEvent;

            _beatmapCharacteristicSelectionViewController.didSelectBeatmapCharacteristicEvent -= OnDidSelectBeatmapCharacteristic;
            _beatmapCharacteristicSelectionViewController.didSelectBeatmapCharacteristicEvent += OnDidSelectBeatmapCharacteristic;

            // make sure the quick scroll buttons don't desync with regular scrolling
            _tableViewPageDownButton.onClick.AddListener(delegate ()
            {
                this.RefreshQuickScrollButtons();
            });
            _tableViewPageUpButton.onClick.AddListener(delegate ()
            {
                this.RefreshQuickScrollButtons();
            });

            // finished level
            ResultsViewController resultsViewController = _levelSelectionFlowCoordinator.GetPrivateField<ResultsViewController>("_resultsViewController");
            resultsViewController.continueButtonPressedEvent += ResultsViewController_continueButtonPressedEvent;
        }

        /// <summary>
        /// Helper to reduce code duplication...
        /// </summary>
        private void RefreshSongUI(bool scrollToLevel=true)
        {
            RefreshSongList(scrollToLevel);
            RefreshSortButtonUI();
            RefreshQuickScrollButtons();
        }

        /// <summary>
        /// External helper.
        /// </summary>
        public void ProcessSongList()
        {
            this._model.ProcessSongList(GetCurrentSelectedLevelPack());
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
                    _model.ProcessSongList(GetCurrentSelectedLevelPack());
                }
                SelectAndScrollToLevel(_levelPackLevelsTableView, _model.LastSelectedLevelId);
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
                
                if (!scrollToLevel)
                {
                    ScrollToLevelByRow(0);
                }
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
                this.Model.Settings.randomSongSeed = Guid.NewGuid().GetHashCode();
            }

            _model.Settings.Save();

            this.ProcessSongList();
            RefreshSongUI();

            //Scroll to start of the list
            TableView listTableView = _levelPackLevelsTableView.GetPrivateField<TableView>("_tableView");
            listTableView.ScrollToCellWithIdx(0, TableView.ScrollPositionType.Beginning, false);
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
                SelectLevelPack(PluginConfig.CUSTOM_SONG_LEVEL_PACK_ID);
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
                SelectLevelPack(PluginConfig.CUSTOM_SONG_LEVEL_PACK_ID);
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
                SelectLevelPack(PluginConfig.CUSTOM_SONG_LEVEL_PACK_ID);
                _playListFlowCoordinator.parentFlowCoordinator = _levelSelectionFlowCoordinator;
                _levelSelectionFlowCoordinator.InvokePrivateMethod("PresentFlowCoordinator", new object[] { _playListFlowCoordinator, null, false, false });                                
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

            _deleteButton.interactable = (_levelDetailViewController.selectedDifficultyBeatmap.level.levelID.Length >= 32);
            this.RefreshScoreSaberData(_levelDetailViewController.selectedDifficultyBeatmap.level);
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
        /// Track the current row.
        /// </summary>
        /// <param name="tableView"></param>
        /// <param name="row"></param>
        private void HandleDidSelectTableViewRow(TableView tableView, int row)
        {
            Logger.Trace("HandleDidSelectTableViewRow({0})", row);
            _lastRow = row;
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

            IBeatmapLevel level = _levelDetailViewController.selectedDifficultyBeatmap.level;
            _deleteDialog.Init("Delete song", $"Do you really want to delete \"{ level.songName} {level.songSubName}\"?", "Delete", "Cancel",
                (selectedButton) =>
                {
                    _levelSelectionFlowCoordinator.InvokePrivateMethod("DismissViewController", new object[] { _deleteDialog, null, false });
                    if (selectedButton == 0)
                    {
                        try
                        {
                            // determine the index we are deleting so we can keep the cursor near the same spot after
                            // the header counts as an index, so if the index came from the level array we have to add 1.
                            var levelsTableView = _levelPackLevelsViewController.GetPrivateField<LevelPackLevelsTableView>("_levelPackLevelsTableView");
                            List<IPreviewBeatmapLevel> levels = levelsTableView.GetPrivateField<IBeatmapLevelPack>("_pack").beatmapLevelCollection.beatmapLevels.ToList();
                            int selectedIndex = levels.FindIndex(x => x.levelID == _standardLevelDetailView.selectedDifficultyBeatmap.level.levelID);

                            if (selectedIndex > -1)
                            {
                                var song = new Song(SongCore.Loader.CustomLevels.First(x => x.Value.levelID == _levelDetailViewController.selectedDifficultyBeatmap.level.levelID).Value);
                                SongCore.Loader.Instance.DeleteSong(song.path);
                                this._model.RemoveSongFromLevelPack(GetCurrentSelectedLevelPack(), _levelDetailViewController.selectedDifficultyBeatmap.level.levelID);

                                this.UpdateLevelDataModel();
                                this.RefreshSongList();

                                int removedLevels = levels.RemoveAll(x => x.levelID == _standardLevelDetailView.selectedDifficultyBeatmap.level.levelID);
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
            _levelSelectionFlowCoordinator.InvokePrivateMethod("PresentViewController", new object[] { _deleteDialog, null, false });
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
            _levelSelectionFlowCoordinator.InvokePrivateMethod("PresentViewController", new object[] { _searchViewController, null, false });
        }

        /// <summary>
        /// Handle back button event from search keyboard.
        /// </summary>
        private void SearchViewControllerbackButtonPressed()
        {
            _levelSelectionFlowCoordinator.InvokePrivateMethod("DismissViewController", new object[] { _searchViewController, null, false });

            // force disable search filter.
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
            _levelSelectionFlowCoordinator.InvokePrivateMethod("DismissViewController", new object[] { _searchViewController, null, false });

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
            var levels = GetCurrentLevelPackLevels();
            if (levels == null)
            {
                return;
            }

            int totalSize = GetLevelPackLevelCount();
            int segmentSize = (int)(totalSize * segmentPercent);

            // Jump at least one scree size.
            if (segmentSize < LIST_ITEMS_VISIBLE_AT_ONCE)
            {
                segmentSize = LIST_ITEMS_VISIBLE_AT_ONCE;
            }

            TableView tableView = ReflectionUtil.GetPrivateField<TableView>(_levelPackLevelsTableView, "_tableView");
            int jumpDirection = Math.Sign(numJumps);
            int newRow = _lastRow + (jumpDirection * segmentSize);
            
            if (newRow <= 0)
            {
                newRow = 0;
            }
            else if (newRow >= totalSize)
            {
                newRow = totalSize - 1;
            }
            
            Logger.Debug("jumpDirection: {0}, newRow: {1}", jumpDirection, newRow);
            _lastRow = newRow;
            this.SelectAndScrollToLevel(_levelPackLevelsTableView, levels[newRow].levelID);
        }

        /// <summary>
        /// Add/Remove song from favorites depending on if it already exists.
        /// </summary>
        private void ToggleSongInPlaylist()
        {
            IBeatmapLevel songInfo = _levelDetailViewController.selectedDifficultyBeatmap.level;
            if (_model.CurrentEditingPlaylist != null)
            {
                if (_model.CurrentEditingPlaylistLevelIds.Contains(songInfo.levelID))
                {
                    Logger.Info("Remove {0} from editing playlist", songInfo.songName);
                    _model.RemoveSongFromEditingPlaylist(songInfo);

                    if (_model.Settings.filterMode == SongFilterMode.Favorites)
                    {
                        this._model.ProcessSongList(GetCurrentSelectedLevelPack());
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

            // use controllers level...
            if (level == null)
            {
                level = _levelDetailViewController.selectedDifficultyBeatmap.level;
            }

            // abort!
            if (level == null)
            {
                Logger.Debug("Aborting RefreshScoreSaberData()");
                return;
            }

            BeatmapDifficulty difficulty = this._levelDifficultyViewController.selectedDifficulty;
            string difficultyString = difficulty.ToString();
            Logger.Debug(difficultyString);

            // check if we have score saber data
            if (this._model.LevelIdToScoreSaberData != null)
            {
                // Check for PP
                Logger.Debug("Checking if have info for song {0}", level.songName);
                if (this._model.LevelIdToScoreSaberData.ContainsKey(level.levelID))
                {
                    Logger.Debug("Checking if have difficulty for song {0} difficulty {1}", level.songName, difficultyString);
                    ScoreSaberData ppData = this._model.LevelIdToScoreSaberData[level.levelID];
                    if (ppData.difficultyToSaberDifficulty.ContainsKey(difficultyString))
                    {
                        Logger.Debug("Display pp for song.");
                        float pp = ppData.difficultyToSaberDifficulty[difficultyString].pp;
                        float star = ppData.difficultyToSaberDifficulty[difficultyString].star;

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
                    UIBuilder.SetStatButtonText(_ppStatButton, "?");
                    UIBuilder.SetStatButtonText(_starStatButton, "?");
                }                
            }
            else
            {
                Logger.Debug("No ScoreSaberData available...  Cannot display pp/star stats...");
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
            // Refresh the fast scroll buttons
            if (_tableViewPageUpButton != null && _pageUpFastButton != null)
            {
                _pageUpFastButton.interactable = _tableViewPageUpButton.interactable;
                _pageUpFastButton.gameObject.SetActive(_tableViewPageUpButton.IsActive());
            }
            else
            {
                _pageUpFastButton.gameObject.SetActive(false);
            }

            if (_tableViewPageDownButton != null && _pageDownFastButton != null)
            {
                _pageDownFastButton.interactable = _tableViewPageDownButton.interactable;
                _pageDownFastButton.gameObject.SetActive(_tableViewPageDownButton.IsActive());                
            }
            else
            {
                _pageDownFastButton.gameObject.SetActive(false);
            }
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
                    _currentAddFavoriteButtonSprite = _removeFavoriteSprite;
                }
                else
                {
                    _currentAddFavoriteButtonSprite = _addFavoriteSprite;
                }
            }

            UIBuilder.SetButtonIcon(_addFavoriteButton, _currentAddFavoriteButtonSprite);
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
        }

        /// <summary>
        /// Try to refresh the song list.  Broken for now.
        /// </summary>
        public void RefreshSongList(bool scrollToLevel=true)
        {
            Logger.Info("Refreshing the song list view.");
            try
            {
                var levels = GetCurrentLevelPackLevels();
                if (levels == null)
                {
                    Logger.Debug("Songs are not sorted yet, nothing to refresh.");
                    return;
                }

                Logger.Debug("Checking if TableView is initialized...");
                TableView tableView = ReflectionUtil.GetPrivateField<TableView>(_levelPackLevelsTableView, "_tableView");
                bool tableViewInit = ReflectionUtil.GetPrivateField<bool>(tableView, "_isInitialized");
                if (!tableViewInit)
                {
                    Logger.Debug("LevelPackLevelListTableView.TableView is not initialized... nothing to reload...");
                    return;
                }

                Logger.Debug("Reloading SongList TableView");
                tableView.ReloadData();

                Logger.Debug("Attempting to scroll to level...");
                String selectedLevelID = null;
                if (_model.LastSelectedLevelId != null)
                {
                    selectedLevelID = _model.LastSelectedLevelId;
                    Logger.Debug("Scrolling to row for level ID: {0}", selectedLevelID);                    
                }
                else
                {
                    if (levels.Length > 0)
                    {
                        selectedLevelID = levels.FirstOrDefault().levelID;
                    }
                }

                if (scrollToLevel)
                {
                    SelectAndScrollToLevel(_levelPackLevelsTableView, selectedLevelID);
                }
            }
            catch (Exception e)
            {
                Logger.Exception("Exception refreshing song list:", e);
            }
        }

        /// <summary>
        /// Acquire the level pack collection.
        /// </summary>
        /// <returns></returns>
        private IBeatmapLevelPackCollection GetLevelPackCollection()
        {
            if (_levelPackViewController == null)
            {
                return null;
            }

            IBeatmapLevelPackCollection levelPackCollection = _levelPackViewController.GetPrivateField<IBeatmapLevelPackCollection>("_levelPackCollection");
            return levelPackCollection;
        }

        /// <summary>
        /// Get the currently selected level pack within the LevelPackLevelViewController hierarchy.
        /// </summary>
        /// <returns></returns>
        public IBeatmapLevelPack GetCurrentSelectedLevelPack()
        {
            if (_levelPackLevelsTableView == null)
            {
                return null;
            }

            var pack = _levelPackLevelsTableView.GetPrivateField<IBeatmapLevelPack>("_pack");
            return pack;
        }

        /// <summary>
        /// Get level pack by level pack id.
        /// </summary>
        /// <param name="levelPackId"></param>
        /// <returns></returns>
        private IBeatmapLevelPack GetLevelPackByPackId(String levelPackId)
        {
            IBeatmapLevelPackCollection levelPackCollection = GetLevelPackCollection();
            if (levelPackCollection == null)
            {
                return null;
            }

            IBeatmapLevelPack levelPack = levelPackCollection.beatmapLevelPacks.ToList().FirstOrDefault(x => x.packID == levelPackId);
            return levelPack;
        }

        /// <summary>
        /// Get level pack index by level pack id.
        /// </summary>
        /// <param name="levelPackId"></param>
        /// <returns></returns>
        private int GetLevelPackIndexByPackId(String levelPackId)
        {
            IBeatmapLevelPackCollection levelPackCollection = GetLevelPackCollection();
            if (levelPackCollection == null)
            {
                return -1;
            }

            int index = levelPackCollection.beatmapLevelPacks.ToList().FindIndex(x => x.packID == levelPackId);
            return index;
        }


        /// <summary>
        /// Get Current levels from current level pack.
        /// </summary>
        /// <returns></returns>
        public IPreviewBeatmapLevel[] GetCurrentLevelPackLevels()
        {
            var levelPack = GetCurrentSelectedLevelPack();
            if (levelPack == null)
            {
                return null;
            }

            return levelPack.beatmapLevelCollection.beatmapLevels;
        }

        /// <summary>
        /// Get level count helper.
        /// </summary>
        /// <returns></returns>
        public int GetLevelPackLevelCount()
        {
            var levels = GetCurrentLevelPackLevels();
            if (levels == null)
            {
                return 0;
            }

            return levels.Length;
        }

        /// <summary>
        /// Select a level pack.
        /// </summary>
        /// <param name="levelPackId"></param>
        public void SelectLevelPack(String levelPackId)
        {
            Logger.Trace("SelectLevelPack({0})", levelPackId);

            try
            {
                var levelPacks = GetLevelPackCollection();
                var index = GetLevelPackIndexByPackId(levelPackId);
                var pack = GetLevelPackByPackId(levelPackId);

                if (index < 0)
                {
                    Logger.Debug("Cannot select level packs yet...");
                    return;
                }

                Logger.Info("Selecting level pack index: {0}", pack.packName);
                var tableView = _levelPacksTableView.GetPrivateField<TableView>("_tableView");

                _levelPacksTableView.SelectCellWithIdx(index);
                tableView.SelectCellWithIdx(index, true);
                tableView.ScrollToCellWithIdx(0, TableView.ScrollPositionType.Beginning, false);
                for (int i = 0; i < index; i++)
                {
                    tableView.PageScrollDown();
                }

                Logger.Debug("Done selecting level pack!");
            }
            catch (Exception e)
            {
                Logger.Exception(e);
            }
        }

        /// <summary>
        /// Scroll TableView to proper row, fire events.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="levelID"></param>
        private void SelectAndScrollToLevel(LevelPackLevelsTableView table, string levelID)
        {
            Logger.Debug("Scrolling to LevelID: {0}", levelID);

            // Check once per load
            if (!_checkedForTwitchPlugin)
            {
                Logger.Info("Checking for BeatSaber Twitch Integration Plugin...");

                // Try to detect BeatSaber Twitch Integration Plugin
                _detectedTwitchPluginQueue = Resources.FindObjectsOfTypeAll<VRUIViewController>().Any(x => x.name == "RequestInfo");

                Logger.Info("BeatSaber Twitch Integration plugin detected: " + _detectedTwitchPluginQueue);

                _checkedForTwitchPlugin = true;
            }

            // Skip scrolling to level if twitch plugin has queue active.
            if (_detectedTwitchPluginQueue)
            {
                Logger.Debug("Skipping SelectAndScrollToLevel() because we detected Twitch Integration Plugin has a Queue active...");
                return;
            }

            // try to find the index and scroll to it
            int selectedIndex = 0;
            List<IPreviewBeatmapLevel> levels = table.GetPrivateField<IBeatmapLevelPack>("_pack").beatmapLevelCollection.beatmapLevels.ToList();
            if (levels.Count <= 0)
            {
                return;
            }

            // acquire the index or try the last row
            selectedIndex = levels.FindIndex(x => x.levelID == levelID);
            if (selectedIndex < 0)
            {
                // this might look like an off by one error but the _level list we keep is missing the header entry BeatSaber.
                // so the last row is +1 the max index, the count.
                int maxCount = GetLevelPackLevelCount();
                Logger.Debug("Song is not in the level pack, cannot scroll to it...  Using last known row {0}/{1}", _lastRow, maxCount);
                selectedIndex = Math.Min(maxCount, _lastRow);
            }
            else
            {
                // the header counts as an index, so if the index came from the level array we have to add 1.
                selectedIndex += 1;
            }

            ScrollToLevelByRow(selectedIndex);
        }

        /// <summary>
        /// Scroll to a level by Row
        /// </summary>
        /// <param name="selectedIndex"></param>
        public void ScrollToLevelByRow(int selectedIndex)
        {
            Logger.Debug("Scrolling level list to idx: {0}", selectedIndex);

            TableView tableView = _levelPackLevelsTableView.GetPrivateField<TableView>("_tableView");
            _levelPackLevelsTableView.HandleDidSelectRowEvent(tableView, selectedIndex);
            tableView.ScrollToCellWithIdx(selectedIndex, TableView.ScrollPositionType.Beginning, true);
            tableView.SelectCellWithIdx(selectedIndex);

            RefreshQuickScrollButtons();

            _lastRow = selectedIndex;
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
                if (_model.CurrentBeatmapCharacteristicSO == null && _beatmapCharacteristicSelectionViewController != null)
                {
                    _model.CurrentBeatmapCharacteristicSO = _beatmapCharacteristicSelectionViewController.GetPrivateField<BeatmapCharacteristicSO>("_selectedBeatmapCharacteristic");
                }

                _model.UpdateLevelRecords();

                bool didUpdateLevelPack = UpdateLevelPackSelection();
                if (!didUpdateLevelPack)
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
            if (_levelPackViewController != null)
            {
                IBeatmapLevelPack currentSelected = GetCurrentSelectedLevelPack();
                Logger.Debug("Current selected level pack: {0}", currentSelected);

                if (String.IsNullOrEmpty(_model.Settings.currentLevelPackId))
                {
                    if (currentSelected == null)
                    {
                        Logger.Debug("No level pack selected, acquiring the first available...");
                        var levelPackCollection = _levelPackViewController.GetPrivateField<IBeatmapLevelPackCollection>("_levelPackCollection");
                        currentSelected = levelPackCollection.beatmapLevelPacks[0];
                    }
                }
                else if (currentSelected == null || (currentSelected.packID != _model.Settings.currentLevelPackId))
                {
                    Logger.Debug("Automatically selecting level pack: {0}", _model.Settings.currentLevelPackId);

                    // HACK - BeatSaber seems to always go back to OST1 internally.
                    //      - Lets force it to the last pack id but not have SongBrowser functions fire.
                    // Turn off our event processing
                    _levelPackViewController.didSelectPackEvent -= _levelPackViewController_didSelectPackEvent;
                    _levelPacksTableView.didSelectPackEvent -= _levelPacksTableView_didSelectPackEvent;

                    var levelPack = GetLevelPackByPackId(_model.Settings.currentLevelPackId);
                    this.SelectLevelPack(_model.Settings.currentLevelPackId);

                    ProcessSongList();

                    _levelPackViewController.didSelectPackEvent += _levelPackViewController_didSelectPackEvent;
                    _levelPacksTableView.didSelectPackEvent += _levelPacksTableView_didSelectPackEvent;
                    
                    return true;
                }
                else
                {
                   // this._model.SetCurrentLevelPack(currentSelected);
                }
            }

            return false;
        }

        //Pull njs from a difficulty, based on private function from SongLoader
        public float GetNoteJump(string json)
        {
            float noteJumpSpeed = 0;
            var split = json.Split(':');
            for (var i = 0; i < split.Length; i++)
            {
                if (split[i].Contains("_noteJumpSpeed"))
                {
                    noteJumpSpeed = Convert.ToSingle(split[i + 1].Split(',')[0], CultureInfo.InvariantCulture);
                }
            }

            return noteJumpSpeed;
        }
    }
}
 