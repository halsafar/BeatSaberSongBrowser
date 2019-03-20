using UnityEngine;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine.UI;
using HMUI;
using VRUI;
using SongBrowserPlugin.DataAccess;
using System.IO;
using SongLoaderPlugin;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using Logger = SongBrowserPlugin.Logging.Logger;
using SongBrowserPlugin.DataAccess.BeatSaverApi;
using CustomUI.BeatSaber;
using SongBrowserPlugin.Internals;

namespace SongBrowserPlugin.UI
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

        private RectTransform _tableViewRectTransform;

        private Button _tableViewPageUpButton;
        private Button _tableViewPageDownButton;
        private Button _playButton;

        // New UI Elements
        private List<SongSortButton> _sortButtonGroup;
        private List<SongFilterButton> _filterButtonGroup;

        private Button _addFavoriteButton;

        private SimpleDialogPromptViewController _simpleDialogPromptViewControllerPrefab;
        private SimpleDialogPromptViewController _deleteDialog;
        private Button _deleteButton;        

        private Button _pageUpFastButton;
        private Button _pageDownFastButton;

        private Button _enterFolderButton;
        private Button _upFolderButton;

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
        private bool _rebuildUI = true;

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
            
            var soloFlow = Resources.FindObjectsOfTypeAll<SoloFreePlayFlowCoordinator>().First();
            var partyFlow = Resources.FindObjectsOfTypeAll<PartyFreePlayFlowCoordinator>().First();
            if (mode == MainMenuViewController.MenuButton.SoloFreePlay)
            {
                Logger.Debug("Entering SOLO mode...");
                _levelSelectionFlowCoordinator = soloFlow;
            }
            else
            {
                Logger.Debug("Entering PARTY mode...");
                _levelSelectionFlowCoordinator = partyFlow;
            }

            // returning to the menu and switching modes could trigger this.
            if (!_rebuildUI)
            {
                return;
            }

            try
            {
                // gather controllers and ui elements.
                //if (_levelPackViewController == null)
                {
                    _levelPackViewController = _levelSelectionFlowCoordinator.GetPrivateField<LevelPacksViewController>("_levelPacksViewController");
                    Logger.Debug("Acquired LevelPacksViewController [{0}]", _levelPackViewController.GetInstanceID());
                }

                //if (_levelPackDetailViewController == null)
                {
                    _levelPackDetailViewController = _levelSelectionFlowCoordinator.GetPrivateField<LevelPackDetailViewController>("_levelPackDetailViewController");
                    Logger.Debug("Acquired LevelPackDetailViewController [{0}]", _levelPackDetailViewController.GetInstanceID());
                }

                //if (_levelPacksTableView == null)
                {
                    _levelPacksTableView = _levelPackViewController.GetPrivateField<LevelPacksTableView>("_levelPacksTableView");
                    Logger.Debug("Acquired LevelPacksTableView [{0}]", _levelPacksTableView.GetInstanceID());
                }

                //if (_levelListViewController == null)
                {
                    _levelPackLevelsViewController = _levelSelectionFlowCoordinator.GetPrivateField<LevelPackLevelsViewController>("_levelPackLevelsViewController");
                    Logger.Debug("Acquired LevelPackLevelsViewController [{0}]", _levelPackLevelsViewController.GetInstanceID());
                }

                //if (_levelListTableView == null)
                {
                    _levelPackLevelsTableView = this._levelPackLevelsViewController.GetComponentInChildren<LevelPackLevelsTableView>();
                    Logger.Debug("Acquired LevelPackLevelsTableView [{0}]", _levelPackLevelsTableView.GetInstanceID());
                }

                //if (_levelDetailViewController == null)
                {
                    _levelDetailViewController = _levelSelectionFlowCoordinator.GetPrivateField<StandardLevelDetailViewController>("_levelDetailViewController");
                    Logger.Debug("Acquired StandardLevelDetailViewController [{0}]", _levelDetailViewController.GetInstanceID());
                }

                //if (_standardLevelDetailView == null)
                {
                    _standardLevelDetailView = _levelDetailViewController.GetPrivateField<StandardLevelDetailView>("_standardLevelDetailView");
                    Logger.Debug("Acquired StandardLevelDetailView [{0}]", _standardLevelDetailView.GetInstanceID());
                }

                //if (_beatmapCharacteristicSelectionViewController == null)
                {
                    _beatmapCharacteristicSelectionViewController = Resources.FindObjectsOfTypeAll<BeatmapCharacteristicSegmentedControlController>().First();
                    Logger.Debug("Acquired BeatmapCharacteristicSegmentedControlController [{0}]", _beatmapCharacteristicSelectionViewController.GetInstanceID());
                }

                //if (_levelSelectionNavigationController == null)
                {
                    _levelSelectionNavigationController = _levelSelectionFlowCoordinator.GetPrivateField<DismissableNavigationController>("_navigationController");
                    Logger.Debug("Acquired DismissableNavigationController [{0}]", _levelSelectionNavigationController.GetInstanceID());
                }

                //if (_levelDifficultyViewController == null)
                {
                    _levelDifficultyViewController = Resources.FindObjectsOfTypeAll<BeatmapDifficultySegmentedControlController>().First();
                    Logger.Debug("Acquired BeatmapDifficultySegmentedControlController [{0}]", _levelDifficultyViewController.GetInstanceID());
                }

                //_tableViewRectTransform = _levelListViewController.GetComponentsInChildren<RectTransform>().First(x => x.name == "LevelPackLevelsTableView");                
                _tableViewRectTransform = _levelPackLevelsTableView.transform as RectTransform;
                Logger.Debug("Acquired TableViewRectTransform from LevelPackLevelsTableView [{0}]", _tableViewRectTransform.GetInstanceID());

                //if (_tableViewPageUpButton == null)
                {
                    _tableViewPageUpButton = _tableViewRectTransform.GetComponentsInChildren<Button>().First(x => x.name == "PageUpButton");
                }

                //if (_tableViewPageDownButton == null)
                {
                    _tableViewPageDownButton = _tableViewRectTransform.GetComponentsInChildren<Button>().First(x => x.name == "PageDownButton");
                }

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


                // make sure the quick scroll buttons don't desync with regular scrolling
                _tableViewPageDownButton.onClick.AddListener(delegate ()
                {
                    this.RefreshQuickScrollButtons();
                });
                _tableViewPageUpButton.onClick.AddListener(delegate ()
                {
                    this.RefreshQuickScrollButtons();
                });

                _rebuildUI = false;
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
                Button sortButtonTemplate = Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "SettingsButton"));
                Button otherButtonTemplate = Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "SettingsButton"));
                Button practiceButton = Resources.FindObjectsOfTypeAll<Button>().First(x => x.name == "PracticeButton");
                RectTransform practiceButtonRect = (practiceButton.transform as RectTransform);
                Button playButton = Resources.FindObjectsOfTypeAll<Button>().First(x => x.name == "PlayButton");
                RectTransform playButtonRect = (playButton.transform as RectTransform);
                Sprite arrowIcon = SongBrowserApplication.Instance.CachedIcons["ArrowIcon"];
                Sprite borderSprite = SongBrowserApplication.Instance.CachedIcons["RoundRectBigStroke"];

                // Resize some of the UI
                _tableViewRectTransform.sizeDelta = new Vector2(0f, -20f);
                _tableViewRectTransform.anchoredPosition = new Vector2(0f, -2.5f);
                
                // Create Sorting Songs By-Buttons
                Logger.Debug("Creating sort by buttons...");
                float buttonSpacing = 0.5f;                                
                float fontSize = 2.0f;
                float buttonWidth = 12.25f;
                float buttonHeight = 5.5f;
                float startButtonX = 24.50f;
                float curButtonX = 0.0f;
                float buttonY = -6.0f;

                string[] sortButtonNames = new string[]
                {
                    "Song", "Author", "Newest", "PP", "Difficult", "Random"
                };

                SongSortMode[] sortModes = new SongSortMode[]
                {
                    SongSortMode.Default, SongSortMode.Author, SongSortMode.Newest, SongSortMode.PP, SongSortMode.Difficulty, SongSortMode.Random
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

                float filterButtonX = curButtonX + (buttonWidth / 2.0f);
                Vector2 iconButtonSize = new Vector2(buttonHeight, buttonHeight);

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

                // Get element info to position properly
                Logger.Debug("Modifying and Cloning PlayButtons...");
                RectTransform detailButtonContainerRect = _standardLevelDetailView.GetComponentsInChildren<RectTransform>().First(x => x.name == "PlayContainer");
                RectTransform detailButtonRect = detailButtonContainerRect.GetComponentsInChildren<RectTransform>().First(x => x.name == "PlayButtons");
                detailButtonRect.anchoredPosition = new Vector2(detailButtonRect.anchoredPosition.x, detailButtonRect.anchoredPosition.y + 5.0f);

                // clone existing button group
                RectTransform newButtonRect = UnityEngine.Object.Instantiate(detailButtonRect, detailButtonContainerRect, false);
                newButtonRect.name = "PlayButtons2";
                newButtonRect.anchoredPosition = new Vector2(newButtonRect.anchoredPosition.x, newButtonRect.anchoredPosition.y - 10.0f);

                // Create add favorite button
                Logger.Debug("Creating Add to favorites button...");
                _addFavoriteButton = newButtonRect.GetComponentsInChildren<Button>().First(x => x.name == "PracticeButton");
                _addFavoriteButton.name = "AddFavoritesButton";
                _addFavoriteButton.onClick.RemoveAllListeners();
                (_addFavoriteButton.transform as RectTransform).sizeDelta = new Vector2(practiceButtonRect.sizeDelta.x, practiceButtonRect.sizeDelta.y);
                UnityEngine.UI.Image icon = _addFavoriteButton.GetComponentsInChildren<UnityEngine.UI.Image>().First(c => c.name == "Icon");
                RectTransform iconTransform = icon.rectTransform;
                iconTransform.localScale = new Vector2(0.75f, 0.75f);
                _addFavoriteButton.GetComponentsInChildren<HorizontalLayoutGroup>().First(c => c.name == "Content").padding = new RectOffset(1, 1, 0, 0);
                _addFavoriteButton.onClick.AddListener(delegate () {
                    ToggleSongInPlaylist();
                });

                // Create delete button          
                Logger.Debug("Creating delete button...");
                _deleteButton = newButtonRect.GetComponentsInChildren<Button>().First(x => x.name == "PlayButton");
                _deleteButton.name = "DeleteButton";
                _deleteButton.onClick.RemoveAllListeners();
                _deleteButton.GetComponentsInChildren<HorizontalLayoutGroup>().First(c => c.name == "Content").padding = new RectOffset(7, 7, 0, 0);
                UIBuilder.SetButtonText(_deleteButton, "Delete");
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

                

                // Create enter folder button
                if (_model.Settings.folderSupportEnabled)
                {
                    _enterFolderButton = UIBuilder.CreateUIButton(otherButtonTransform, _playButton);
                    _enterFolderButton.onClick.AddListener(delegate ()
                    {
                        _model.PushDirectory(_levelDetailViewController.selectedDifficultyBeatmap.level);
                        this.RefreshSongList();
                        this.RefreshDirectoryButtons();
                    });
                    UIBuilder.SetButtonText(_enterFolderButton, "Enter");

                    // Create up folder button
                    _upFolderButton = UIBuilder.CreateIconButton(sortButtonTransform, sortButtonTemplate, arrowIcon,
                        new Vector2(filterButtonX + (iconButtonSize.x* filterButtonSetup.Count), buttonY),
                        new Vector2(iconButtonSize.x, iconButtonSize.y),
                        new Vector2(0.85f, 0.85f),
                        new Vector2(2.0f, 2.0f),
                        180);
                    _upFolderButton.onClick.RemoveAllListeners();
                    _upFolderButton.onClick.AddListener(delegate ()
                    {
                        _model.PopDirectory();
                        this.RefreshSongList();
                        this.RefreshDirectoryButtons();
                    });
                }

                RefreshSortButtonUI();
                RefreshDirectoryButtons();

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
        /// Add our handlers into BeatSaber.
        /// </summary>
        private void InstallHandlers()
        {
            // handlers
            TableView tableView = ReflectionUtil.GetPrivateField<TableView>(_levelPackLevelsTableView, "_tableView");
            tableView.didSelectCellWithIdxEvent += HandleDidSelectTableViewRow;
            _levelPackLevelsViewController.didSelectLevelEvent += OnDidSelectLevelEvent;
            _levelDifficultyViewController.didSelectDifficultyEvent += OnDidSelectDifficultyEvent;

            var packListTableView = _levelPacksTableView;

            _levelPacksTableView.didSelectPackEvent += _levelPacksTableView_didSelectPackEvent;
            _levelPackViewController.didSelectPackEvent += _levelPackViewController_didSelectPackEvent;
                  
            _beatmapCharacteristicSelectionViewController.didSelectBeatmapCharacteristicEvent += OnDidSelectBeatmapCharacteristic;
        }

        /// <summary>
        /// Handler for level pack selection.
        /// UNUSED
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        private void _levelPacksTableView_didSelectPackEvent(LevelPacksTableView arg1, IBeatmapLevelPack arg2)
        {
            Logger.Trace("_levelPacksTableView_didSelectPackEvent(arg2={0})", arg2);

            try
            {
                this._model.CurrentLevelPack = arg2;

                UpdateSongList();
                RefreshSongList();
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

            }
            catch (Exception e)
            {
                Logger.Exception("Exception handling didSelectPackEvent...", e);
            }
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
            _model.Settings.Save();

            // update the seed
            if (_model.Settings.sortMode == SongSortMode.Random)
            {
                this.Model.Settings.randomSongSeed = Guid.NewGuid().GetHashCode();
                this.Model.Settings.Save();
            }

            UpdateSongList();
            RefreshSongList();
            RefreshSortButtonUI();
            RefreshQuickScrollButtons();

            // Handle instant queue logic, avoid picking a folder.
            if (_model.Settings.sortMode == SongSortMode.Random && _model.Settings.randomInstantQueue)
            {
                for (int i = 0; i < _model.SortedSongList.Count; i++)
                {
                    if (!_model.SortedSongList[i].levelID.StartsWith("Folder_"))
                    {
                        this.SelectAndScrollToLevel(_levelPackLevelsTableView, _model.SortedSongList[i].levelID);
                        var beatMapDifficulties = _model.SortedSongList[i].difficultyBeatmapSets
                            .Where(x => x.beatmapCharacteristic == _model.CurrentBeatmapCharacteristicSO)
                            .SelectMany(x => x.difficultyBeatmaps);
                        this._levelDifficultyViewController.HandleDifficultySegmentedControlDidSelectCell(null, beatMapDifficulties.Count()-1);
                        _playButton.onClick.Invoke();
                        break;
                    }
                }                                                    
            }

            //Scroll to start of the list
            var levelsTableView = _levelPackLevelsViewController.GetPrivateField<LevelPackLevelsTableView>("_levelPackLevelsTableView");
            TableView listTableView = levelsTableView.GetPrivateField<TableView>("_tableView");
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
            }
            else
            {
                _model.Settings.filterMode = SongFilterMode.None;
            }
            _model.Settings.Save();

            UpdateSongList();
            RefreshSongList();
            RefreshSortButtonUI();
            RefreshQuickScrollButtons();
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
                this.ShowSearchKeyboard();
            }
            else
            {
                _model.Settings.filterMode = SongFilterMode.None;
                UpdateSongList();
                RefreshSongList();
                RefreshSortButtonUI();
                RefreshQuickScrollButtons();
            }
            _model.Settings.Save();            
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
                _playListFlowCoordinator.parentFlowCoordinator = _levelSelectionFlowCoordinator;
                _levelSelectionFlowCoordinator.InvokePrivateMethod("PresentFlowCoordinator", new object[] { _playListFlowCoordinator, null, false, false });                                
            }
            else
            {
                _model.Settings.filterMode = SongFilterMode.None;
                _model.Settings.Save();
                UpdateSongList();
                RefreshSongList();
                RefreshSortButtonUI();
                RefreshQuickScrollButtons();
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

                RefreshAddFavoriteButton(level.levelID);
                RefreshQuickScrollButtons();

                if (level.levelID.StartsWith("Folder_"))
                {
                    Logger.Debug("Folder selected!  Adjust PlayButton logic...");
                    HandleDidSelectFolderRow(level);
                }
                else
                {
                    HandleDidSelectLevelRow(level);
                }
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
            _model.UpdateSongLists();
            this.RefreshSongList();
        }

        /// <summary>
        /// Handle difficulty level selection.
        /// </summary>
        private void OnDidSelectDifficultyEvent(BeatmapDifficultySegmentedControlController view, BeatmapDifficulty beatmap)
        {
            _deleteButton.interactable = (_levelDetailViewController.selectedDifficultyBeatmap.level.levelID.Length >= 32);

            this.RefreshScoreSaberData(_levelDetailViewController.selectedDifficultyBeatmap.level);
        }

        /// <summary>
        /// Turn play button into enter folder button.
        /// </summary>
        private void HandleDidSelectFolderRow(IPreviewBeatmapLevel level)
        {
            _enterFolderButton.gameObject.SetActive(true);
            _playButton.gameObject.SetActive(false);
        }

        /// <summary>
        /// Turn enter folder button into play button.
        /// </summary>
        /// <param name="level"></param>
        private void HandleDidSelectLevelRow(IPreviewBeatmapLevel level)
        {
            // deal with enter folder button
            if (_enterFolderButton != null)
            {
                _enterFolderButton.gameObject.SetActive(false);
            }
            _playButton.gameObject.SetActive(true);

            this.RefreshScoreSaberData(level);
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
        /// Pop up a delete dialog.
        /// </summary>
        private void HandleDeleteSelectedLevel()
        {
            IBeatmapLevel level = _levelDetailViewController.selectedDifficultyBeatmap.level;
            _deleteDialog.Init("Delete song", $"Do you really want to delete \"{ level.songName} {level.songSubName}\"?", "Delete", "Cancel",
                (selectedButton) =>
                {
                    _levelSelectionFlowCoordinator.InvokePrivateMethod("DismissViewController", new object[] { _deleteDialog, null, false });
                    if (selectedButton == 0)
                    {
                        try
                        {
                            var levelsTableView = _levelPackLevelsViewController.GetPrivateField<LevelPackLevelsTableView>("_levelPackLevelsTableView");
                            List<IPreviewBeatmapLevel> levels = levelsTableView.GetPrivateField<IBeatmapLevelPack>("_pack").beatmapLevelCollection.beatmapLevels.ToList();
                            int selectedIndex = levels.FindIndex(x => x.levelID == _levelDetailViewController.selectedDifficultyBeatmap.level.levelID);

                            SongDownloader.Instance.DeleteSong(new Song(SongLoader.CustomLevels.First(x => x.levelID == _levelDetailViewController.selectedDifficultyBeatmap.level.levelID)));

                            if (selectedIndex > -1)
                            {
                                int removedLevels = levels.RemoveAll(x => x.levelID == _levelDetailViewController.selectedDifficultyBeatmap.level.levelID);
                                Logger.Log("Removed " + removedLevels + " level(s) from song list!");

                                _levelPackLevelsViewController.SetData(CustomHelpers.GetLevelPackWithLevels(levels.Cast<BeatmapLevelSO>().ToArray(), _model.CurrentPlaylist?.playlistTitle ?? "Custom Songs", _model.CurrentPlaylist?.icon));
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
        /// Create MD5 of a file.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string CreateMD5FromFile(string path)
        {
            string hash = "";
            if (!File.Exists(path)) return null;
            using (MD5 md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(path))
                {
                    byte[] hashBytes = md5.ComputeHash(stream);

                    StringBuilder sb = new StringBuilder();
                    foreach (byte hashByte in hashBytes)
                    {
                        sb.Append(hashByte.ToString("X2"));
                    }

                    hash = sb.ToString();
                    return hash;
                }
            }
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

                this.UpdateSongList();
                this.RefreshSongList();
                this.RefreshSortButtonUI();
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

            RefreshSortButtonUI();
            RefreshQuickScrollButtons();
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
            this.UpdateSongList();
            this.RefreshSongList();

            RefreshSortButtonUI();
            RefreshQuickScrollButtons();
        }

        /// <summary>
        /// Make big jumps in the song list.
        /// </summary>
        /// <param name="numJumps"></param>
        private void JumpSongList(int numJumps, float segmentPercent)
        {
            int totalSize = _model.SortedSongList.Count;
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
            this.SelectAndScrollToLevel(_levelPackLevelsTableView, _model.SortedSongList[newRow].levelID);
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

            Logger.Trace("RefreshScoreSaberData({0})", level.levelID);

            // display pp potentially
            if (this._model.LevelIdToScoreSaberData != null)
            {
                BeatmapDifficulty difficulty = this._levelDifficultyViewController.selectedDifficulty;
                string njsText;
                string difficultyString = difficulty.ToString();

                //Grab NJS for difficulty
                //Default to 10 if a standard level
                float njs = 0;
                if (!_model.LevelIdToCustomSongInfos.ContainsKey(level.levelID))
                {
                    njsText = "OST";
                }
                else
                {
                    //Grab njs from custom level
                    SongLoaderPlugin.OverrideClasses.CustomLevel customLevel = _model.LevelIdToCustomSongInfos[level.levelID];
                    foreach (var diffLevel in customLevel.customSongInfo.difficultyLevels)
                    {

                        if (diffLevel.difficulty == difficultyString)
                        {
                            GetNoteJump(diffLevel.json, out njs);
                        }
                    }

                    //Show note jump speedS
                    njsText = njs.ToString();
                }
                UIBuilder.SetStatButtonText(_njsStatButton, njsText);

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
        }

        /// <summary>
        /// Update interactive state of the quick scroll buttons.
        /// </summary>
        private void RefreshQuickScrollButtons()
        {
            // if you are ever viewing the song list with less than 5 songs the up/down buttons do not exist.
            // just try and fetch them and ignore the exception.
            if (_tableViewPageUpButton == null)
            {
                try
                {
                    _tableViewPageUpButton = _tableViewRectTransform.GetComponentsInChildren<Button>().First(x => x.name == "PageUpButton");
                    (_tableViewPageUpButton.transform as RectTransform).anchoredPosition = new Vector2(0f, -1f);
                }
                catch (Exception)
                {
                    // We don't care if this fails.
                    return;
                }
            }

            if (_tableViewPageDownButton == null)
            {
                try
                {
                    _tableViewPageDownButton = _tableViewRectTransform.GetComponentsInChildren<Button>().First(x => x.name == "PageDownButton");
                    (_tableViewPageDownButton.transform as RectTransform).anchoredPosition = new Vector2(0f, 1f);
                }
                catch (Exception)
                {
                    // We don't care if this fails.
                    return;
                }
            }

            // Refresh the fast scroll buttons
            if (_tableViewPageUpButton != null && _pageUpFastButton != null)
            {
                _pageUpFastButton.interactable = _tableViewPageUpButton.interactable;
                _pageUpFastButton.gameObject.SetActive(_tableViewPageUpButton.IsActive());
            }

            if (_tableViewPageDownButton != null && _pageUpFastButton != null)
            {
                _pageDownFastButton.interactable = _tableViewPageDownButton.interactable;
                _pageDownFastButton.gameObject.SetActive(_tableViewPageDownButton.IsActive());
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
                //UIBuilder.SetButtonTextColor(sortButton.Button, Color.white);
                UIBuilder.SetButtonBorder(sortButton.Button, Color.white);
                if (sortButton.SortMode == _model.Settings.sortMode)
                {
                    if (this._model.Settings.invertSortResults)
                    {
                        //UIBuilder.SetButtonTextColor(sortButton.Button, Color.red);
                        UIBuilder.SetButtonBorder(sortButton.Button, Color.red);
                    }
                    else
                    {
                        //UIBuilder.SetButtonTextColor(sortButton.Button, Color.green);
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
        /// Refresh the UI state of any directory buttons.
        /// </summary>
        public void RefreshDirectoryButtons()
        {
            // bail if no button, likely folder support not enabled.
            if (_upFolderButton == null)
            {
                return;
            }

            if (_model.DirStackSize > 1)
            {
                _upFolderButton.interactable = true;
            }
            else
            {
                _upFolderButton.interactable = false;
            }
        }

        /// <summary>
        /// Try to refresh the song list.  Broken for now.
        /// </summary>
        public void RefreshSongList()
        {
            Logger.Info("Refreshing the song list view.");
            try
            {
                // TODO - remove as part of unifying the we handle the song lists
                if (_model.IsCurrentLevelPackPreview)
                {
                    return;
                }

                if (_model.SortedSongList == null)
                {
                    Logger.Debug("Songs are not sorted yet, nothing to refresh.");
                    return;
                }

                var levels = _model.SortedSongList.ToArray();

                Logger.Debug("Checking if TableView is initialized...");
                TableView tableView = ReflectionUtil.GetPrivateField<TableView>(_levelPackLevelsTableView, "_tableView");
                bool tableViewInit = ReflectionUtil.GetPrivateField<bool>(tableView, "_isInitialized");
                if (!tableViewInit)
                {
                    Logger.Debug("LevelPackLevelListTableView.TableView is not initialized... nothing to reload...");
                    return;
                }

                Logger.Debug("Overwriting levelPack.beatmapLevelCollection._beatmapLevels");
                IBeatmapLevelPack levelPack = this.Model.CurrentLevelPack;
                ReflectionUtil.SetPrivateField(levelPack.beatmapLevelCollection, "_beatmapLevels", levels);

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

                // HACK, seems like if 6 or less items scrolling to row causes the song list to disappear.
                //if (levels.Length > 6 && !String.IsNullOrEmpty(selectedLevelID) && levels.Any(x => x.levelID == selectedLevelID))
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
                Logger.Debug("Skipping SelectAndScrollToLevel() because we detected Twitch Integrtion Plugin has a Queue active...");
                return;
            }

            // try to find the index and scroll to it
            int selectedIndex = 0;
            List<IPreviewBeatmapLevel> levels = table.GetPrivateField<IBeatmapLevelPack>("_pack").beatmapLevelCollection.beatmapLevels.ToList();
            selectedIndex = levels.FindIndex(x => x.levelID == levelID);
            if (selectedIndex >= 0)
            {
                Logger.Debug("Scrolling to idx: {0}", selectedIndex);

                TableView listTableView = _levelPackLevelsViewController
                    .GetPrivateField<LevelPackLevelsTableView>("_levelPackLevelsTableView")
                    .GetPrivateField<TableView>("_tableView");

                var scrollPosType = TableView.ScrollPositionType.Center;
                if (selectedIndex == 0)
                {
                    scrollPosType = TableView.ScrollPositionType.Beginning;
                }
                if (selectedIndex == _model.SortedSongList.Count-1)
                {
                    scrollPosType = TableView.ScrollPositionType.End;
                }

                listTableView.ScrollToCellWithIdx(selectedIndex, scrollPosType, true);
                RefreshQuickScrollButtons();

                _lastRow = selectedIndex;
            }
            else
            {
                Logger.Debug("Song is not in the level pack, cannot scroll to it...");
            }
        }

        /// <summary>
        /// Helper for updating the model (which updates the song list)
        /// </summary>
        public void UpdateSongList()
        {
            try
            {
                Logger.Trace("UpdateSongList()");

                if (_model.CurrentLevelPack == null)
                {
                    // TODO - is this acceptable?  review....
                    Logger.Debug("No level pack selected, acquiring the first available...");
                    var levelPackCollection = _levelPackViewController.GetPrivateField<IBeatmapLevelPackCollection>("_levelPackCollection");
                    _model.CurrentLevelPack = levelPackCollection.beatmapLevelPacks[0];
                }

                _model.UpdateSongLists();
                this.RefreshDirectoryButtons();
            }
            catch (Exception e)
            {
                Logger.Exception("SongBrowser UI crashed trying to update the internal song lists: ", e);
            }
        }

        //Pull njs from a difficulty, based on private function from SongLoader
        public void GetNoteJump(string json, out float noteJumpSpeed)
        {
            noteJumpSpeed = 0;
            var split = json.Split(':');
            for (var i = 0; i < split.Length; i++)
            {
                if (split[i].Contains("_noteJumpSpeed"))
                {
                    noteJumpSpeed = Convert.ToSingle(split[i + 1].Split(',')[0], CultureInfo.InvariantCulture);
                }
            }
        }

#if DEBUG
        /// <summary>
        /// Not normally called by the game-engine.  Dependent on SongBrowserApplication to call it.
        /// </summary>
        public void LateUpdate()
        {
            CheckDebugUserInput();
        }

        /// <summary>
        /// Map some key presses directly to UI interactions to make testing easier.
        /// </summary>
        private void CheckDebugUserInput()
        {
            try
            {
                if (this._levelPackLevelsViewController != null && this._levelPackLevelsViewController.isActiveAndEnabled)
                {
                    bool isShiftKeyDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

                    if (isShiftKeyDown && Input.GetKeyDown(KeyCode.X))
                    {
                        this._beatmapCharacteristicSelectionViewController.HandleDifficultySegmentedControlDidSelectCell(null, 1);
                    }
                    else if (Input.GetKeyDown(KeyCode.X))
                    {
                        this._beatmapCharacteristicSelectionViewController.HandleDifficultySegmentedControlDidSelectCell(null, 0);
                    }

                    // back
                    if (Input.GetKeyDown(KeyCode.Escape))
                    {
                        this._levelSelectionNavigationController.GoBackButtonPressed();
                    }
                    
                    // select current sort mode again (toggle inverting)
                    if (isShiftKeyDown && Input.GetKeyDown(KeyCode.BackQuote))
                    {
                        _sortButtonGroup[_sortButtonLastPushedIndex].Button.onClick.Invoke();
                    }
                    // cycle sort modes
                    else if (Input.GetKeyDown(KeyCode.BackQuote))
                    {
                        _sortButtonLastPushedIndex = (_sortButtonLastPushedIndex + 1) % _sortButtonGroup.Count;
                        _sortButtonGroup[_sortButtonLastPushedIndex].Button.onClick.Invoke();
                    }

                    // filter favorites
                    if (Input.GetKeyDown(KeyCode.F1))
                    {
                        OnFavoriteFilterButtonClickEvent();
                    }

                    // filter playlists
                    if (Input.GetKeyDown(KeyCode.F2))
                    {
                        OnPlaylistButtonClickEvent();
                    }

                    // filter search
                    if (Input.GetKeyDown(KeyCode.F3))
                    {
                        OnSearchButtonClickEvent();
                    }

                    // delete
                    if (Input.GetKeyDown(KeyCode.Delete))
                    {
                        _deleteButton.onClick.Invoke();
                    }

                    // c - select difficulty for top song
                    if (Input.GetKeyDown(KeyCode.C))
                    {
                        _levelPacksTableView.SelectCellWithIdx(5);
                        _levelPacksTableView.HandleDidSelectColumnEvent(null, 2);

                        TableView listTableView = this._levelPackLevelsTableView.GetPrivateField<TableView>("_tableView");
                        this._levelPackLevelsTableView.HandleDidSelectRowEvent(listTableView, 2);
                        listTableView.ScrollToCellWithIdx(2, TableView.ScrollPositionType.Beginning, false);

                        //this._levelDifficultyViewController.HandleDifficultySegmentedControlDidSelectCell(null, 0);
                    }

                    // v - select difficulty for top song
                    if (Input.GetKeyDown(KeyCode.V))
                    {
                        this.SelectAndScrollToLevel(_levelPackLevelsTableView, _model.SortedSongList[0].levelID);
                        this._levelDifficultyViewController.HandleDifficultySegmentedControlDidSelectCell(null, 0);
                    }

                    // return - start a song or enter a folder
                    if (Input.GetKeyDown(KeyCode.Return))
                    {
                        if (_playButton.isActiveAndEnabled)
                        {
                            _playButton.onClick.Invoke();
                        }
                        else if (_enterFolderButton.isActiveAndEnabled)
                        {
                            _enterFolderButton.onClick.Invoke();
                        }
                    }

                    // backspace - up a folder
                    if (Input.GetKeyDown(KeyCode.Backspace))
                    {
                        _upFolderButton.onClick.Invoke();
                    }

                    // change song index
                    if (isShiftKeyDown && Input.GetKeyDown(KeyCode.N))
                    {
                        _pageUpFastButton.onClick.Invoke();
                    }
                    else if (Input.GetKeyDown(KeyCode.N))
                    {
                        _lastRow = (_lastRow - 1) != -1 ? (_lastRow - 1) % this._model.SortedSongList.Count : 0;
                        this.SelectAndScrollToLevel(_levelPackLevelsTableView, _model.SortedSongList[_lastRow].levelID);
                    }

                    if (isShiftKeyDown && Input.GetKeyDown(KeyCode.M))
                    {
                        _pageDownFastButton.onClick.Invoke();
                    }
                    else if (Input.GetKeyDown(KeyCode.M))
                    {                        
                        _lastRow = (_lastRow + 1) % this._model.SortedSongList.Count;
                        this.SelectAndScrollToLevel(_levelPackLevelsTableView, _model.SortedSongList[_lastRow].levelID);
                    }

                    // add to favorites
                    if (Input.GetKeyDown(KeyCode.KeypadPlus))
                    {
                        ToggleSongInPlaylist();
                    }
                }
                else if (_deleteDialog != null && _deleteDialog.isInViewControllerHierarchy)
                {
                    // accept delete
                    if (Input.GetKeyDown(KeyCode.Return))
                    {
                        _deleteDialog.GetPrivateField<Button>("_okButton").onClick.Invoke();
                    }

                    if (Input.GetKeyDown(KeyCode.Escape))
                    {
                        _deleteDialog.GetPrivateField<Button>("_cancelButton").onClick.Invoke();
                    }
                }
                else
                {
                    if (Input.GetKeyDown(KeyCode.B))
                    {
                        Logger.Debug("Invoking OK Button");
                        VRUIViewController view = Resources.FindObjectsOfTypeAll<VRUIViewController>().First(x => x.name == "StandardLevelResultsViewController");
                        view.GetComponentsInChildren<Button>().First(x => x.name == "Ok").onClick.Invoke();
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Exception("Debug Input caused Exception: ", e);
            }
        }
#endif
    }
}
 