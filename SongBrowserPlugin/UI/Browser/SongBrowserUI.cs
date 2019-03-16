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
        private LevelListViewController _levelListViewController;
        private StandardLevelDetailViewController _levelDetailViewController;
        private BeatmapDifficultyViewController _levelDifficultyViewController;
        private BeatmapCharacteristicSelectionViewController _beatmapCharacteristicSelectionViewController; 
        private DismissableNavigationController _levelSelectionNavigationController;
        private LevelListTableView _levelListTableView;
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
                _levelSelectionFlowCoordinator = soloFlow;
            }
            else
            {
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
                if (_levelListViewController == null)
                {
                    _levelListViewController = _levelSelectionFlowCoordinator.GetPrivateField<LevelListViewController>("_levelListViewController");
                }

                if (_levelDetailViewController == null)
                {
                    _levelDetailViewController = _levelSelectionFlowCoordinator.GetPrivateField<StandardLevelDetailViewController>("_levelDetailViewController");
                }
                
                if (_beatmapCharacteristicSelectionViewController == null)
                {
                    _beatmapCharacteristicSelectionViewController = _levelSelectionFlowCoordinator.GetPrivateField<BeatmapCharacteristicSelectionViewController>("_beatmapCharacteristicSelectionViewController");
                }

                if (_levelSelectionNavigationController == null)
                {
                    _levelSelectionNavigationController = _levelSelectionFlowCoordinator.GetPrivateField<DismissableNavigationController>("_navigationController");
                }

                if (_levelDifficultyViewController == null)
                {
                    _levelDifficultyViewController = soloFlow.GetPrivateField<BeatmapDifficultyViewController>("_beatmapDifficultyViewControllerViewController");
                }

                if (_levelListTableView == null)
                {
                    _levelListTableView = this._levelListViewController.GetComponentInChildren<LevelListTableView>();
                }

                _playButton = _levelDetailViewController.GetComponentsInChildren<Button>().FirstOrDefault(x => x.name == "PlayButton");

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

                // handlers
                TableView tableView = ReflectionUtil.GetPrivateField<TableView>(_levelListTableView, "_tableView");
                tableView.didSelectRowEvent += HandleDidSelectTableViewRow;
                _levelListViewController.didSelectLevelEvent += OnDidSelectLevelEvent;
                _levelDifficultyViewController.didSelectDifficultyEvent += OnDidSelectDifficultyEvent;
                _beatmapCharacteristicSelectionViewController.didSelectBeatmapCharacteristicEvent += OnDidSelectBeatmapCharacteristic;

                // modify details view
                var statsPanel = this._levelDetailViewController.GetComponentsInChildren<CanvasRenderer>(true).First(x => x.name == "LevelParamsPanel");
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

                _rebuildUI = false;
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
                _tableViewRectTransform = _levelListViewController.GetComponentsInChildren<RectTransform>().First(x => x.name == "TableViewContainer");                
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
                    "Song", "Author", "Original", "Newest", "Plays", "PP", "Difficult", "Random"
                };

                SongSortMode[] sortModes = new SongSortMode[]
                {
                    SongSortMode.Default, SongSortMode.Author, SongSortMode.Original, SongSortMode.Newest, SongSortMode.PlayCount, SongSortMode.PP, SongSortMode.Difficulty, SongSortMode.Random
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
                RectTransform detailContainerRect = _levelDetailViewController.GetComponentsInChildren<RectTransform>().First(x => x.name == "Container");
                RectTransform detailButtonRect = _levelDetailViewController.GetComponentsInChildren<RectTransform>().First(x => x.name == "Buttons");
                detailButtonRect.anchoredPosition = new Vector2(detailButtonRect.anchoredPosition.x, detailButtonRect.anchoredPosition.y + 5.0f);

                // clone existing button group
                RectTransform newButtonRect = UnityEngine.Object.Instantiate(detailButtonRect, detailContainerRect, false);
                newButtonRect.name = "Buttons2";
                newButtonRect.anchoredPosition = new Vector2(newButtonRect.anchoredPosition.x, newButtonRect.anchoredPosition.y - 10.0f);

                // Create add favorite button
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
                _deleteButton = newButtonRect.GetComponentsInChildren<Button>().First(x => x.name == "PlayButton");
                _deleteButton.name = "DeleteButton";
                _deleteButton.onClick.RemoveAllListeners();
                _deleteButton.GetComponentsInChildren<HorizontalLayoutGroup>().First(c => c.name == "Content").padding = new RectOffset(7, 7, 0, 0);
                UIBuilder.SetButtonText(_deleteButton, "Delete");
                _deleteButton.onClick.AddListener(delegate () {
                    HandleDeleteSelectedLevel();
                });

                // Create fast scroll buttons
                _pageUpFastButton = UIBuilder.CreateIconButton(sortButtonTransform, otherButtonTemplate, arrowIcon,
                    new Vector2(32, -12),
                    new Vector2(6.0f, 5.5f),
                    new Vector2(1.5f, 1.5f),
                    new Vector2(1.0f, 1.0f), 
                    180);
                UnityEngine.GameObject.Destroy(_pageUpFastButton.GetComponentsInChildren<UnityEngine.UI.Image>().First(btn => btn.name == "Stroke"));
                _pageUpFastButton.onClick.AddListener(delegate () {
                    this.JumpSongList(-1, SEGMENT_PERCENT);
                });

                _pageDownFastButton = UIBuilder.CreateIconButton(sortButtonTransform, otherButtonTemplate, arrowIcon,
                    new Vector2(32, -78.5f),
                    new Vector2(6.0f, 5.5f),
                    new Vector2(1.5f, 1.5f),
                    new Vector2(1.0f, 1.0f), 
                    0);
                UnityEngine.GameObject.Destroy(_pageDownFastButton.GetComponentsInChildren<UnityEngine.UI.Image>().First(btn => btn.name == "Stroke"));
                _pageDownFastButton.onClick.AddListener(delegate () {
                    this.JumpSongList(1, SEGMENT_PERCENT);
                });

                // Create enter folder button
                if (_model.Settings.folderSupportEnabled)
                {
                    _enterFolderButton = UIBuilder.CreateUIButton(otherButtonTransform, _playButton);
                    _enterFolderButton.onClick.AddListener(delegate ()
                    {
                        _model.PushDirectory(_levelListViewController.selectedLevel);
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
            }
            catch (Exception e)
            {
                Logger.Exception("Exception CreateUIElements:", e);
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

            // Handle instant queue logic, avoid picking a folder.
            if (_model.Settings.sortMode == SongSortMode.Random && _model.Settings.randomInstantQueue)
            {
                for (int i = 0; i < _model.SortedSongList.Count; i++)
                {
                    if (!_model.SortedSongList[i].levelID.StartsWith("Folder_"))
                    {
                        this.SelectAndScrollToLevel(_levelListTableView, _model.SortedSongList[i].levelID);
                        this._levelDifficultyViewController.HandleDifficultyTableViewDidSelectRow(null, _model.SortedSongList[i].difficultyBeatmaps.Length-1);
                        _playButton.onClick.Invoke();
                        break;
                    }
                }                                                    
            }
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
            }
        }

        /// <summary>
        /// Adjust UI based on level selected.
        /// Various ways of detecting if a level is not properly selected.  Seems most hit the first one.
        /// </summary>
        private void OnDidSelectLevelEvent(LevelListViewController view, IBeatmapLevel level)
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
        private void OnDidSelectBeatmapCharacteristic(BeatmapCharacteristicSelectionViewController view, BeatmapCharacteristicSO bc)
        {
            Logger.Trace("OnDidSelectBeatmapCharacteristic({0}", bc.name);
            _model.UpdateSongLists(bc);
            this.RefreshSongList();
        }

        /// <summary>
        /// Handle difficulty level selection.
        /// </summary>
        private void OnDidSelectDifficultyEvent(BeatmapDifficultyViewController view, IDifficultyBeatmap beatmap)
        {
            _deleteButton.interactable = (beatmap.level.levelID.Length >= 32);

            this.RefreshScoreSaberData(_levelListViewController.selectedLevel);
        }

        /// <summary>
        /// Turn play button into enter folder button.
        /// </summary>
        private void HandleDidSelectFolderRow(IBeatmapLevel level)
        {
            _enterFolderButton.gameObject.SetActive(true);
            _playButton.gameObject.SetActive(false);
        }

        /// <summary>
        /// Turn enter folder button into play button.
        /// </summary>
        /// <param name="level"></param>
        private void HandleDidSelectLevelRow(IBeatmapLevel level)
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
            IBeatmapLevel level = this._levelListViewController.selectedLevel;
            if (level == null)
            {
                Logger.Info("No level selected, cannot delete nothing...");
                return;
            }

            if (!_model.LevelIdToCustomSongInfos.ContainsKey(level.levelID))
            {
                Logger.Debug("Cannot delete non-custom levels.");
                return;
            }

            if (level.levelID.StartsWith("Folder"))
            {
                Logger.Debug("Cannot delete folders.");
                return;
            }

            SongLoaderPlugin.OverrideClasses.CustomLevel customLevel = _model.LevelIdToCustomSongInfos[level.levelID];

            this._deleteDialog.Init(
                "Delete level warning!", 
                String.Format("<color=#00AAFF>Permanently delete level: {0}</color>\n  Do you want to continue?", customLevel.songName), 
                "YES", 
                "NO",
                this.HandleDeleteDialogPromptViewControllerDidFinish);

            _levelSelectionFlowCoordinator.InvokePrivateMethod("PresentViewController", new object[] { this._deleteDialog, null, false });            
        }

        /// <summary>
        /// Handle delete dialog resolution.
        /// </summary>
        /// <param name="viewController"></param>
        /// <param name="ok"></param>
        public void HandleDeleteDialogPromptViewControllerDidFinish(int buttonNum)
        {
            _levelSelectionFlowCoordinator.InvokePrivateMethod("DismissViewController", new object[] { _deleteDialog, null, false });
            if (buttonNum == 0)            
            {
                SongDownloader.Instance.DeleteSong(new Song(SongLoader.CustomLevels.First(x => x.levelID == _levelDetailViewController.selectedDifficultyBeatmap.level.levelID)));

                List<IBeatmapLevel> levels = _levelListViewController.GetPrivateField<IBeatmapLevel[]>("_levels").ToList();
                int selectedIndex = levels.IndexOf(_levelDetailViewController.selectedDifficultyBeatmap.level);

                if (selectedIndex > -1)
                {
                    levels.Remove(_levelDetailViewController.selectedDifficultyBeatmap.level);

                    if (selectedIndex > 0)
                    {
                        selectedIndex--;
                    }

                    _levelListViewController.SetLevels(levels.ToArray());
                    TableView listTableView = _levelListViewController.GetPrivateField<LevelListTableView>("_levelListTableView").GetPrivateField<TableView>("_tableView");
                    listTableView.ScrollToRow(selectedIndex, false);
                    listTableView.SelectRow(selectedIndex, true);
                }               

                this.UpdateSongList();
                this.RefreshSongList();
            }
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

            TableView tableView = ReflectionUtil.GetPrivateField<TableView>(_levelListTableView, "_tableView");
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
            this.SelectAndScrollToLevel(_levelListTableView, _model.SortedSongList[newRow].levelID);
        }

        /// <summary>
        /// Add/Remove song from favorites depending on if it already exists.
        /// </summary>
        private void ToggleSongInPlaylist()
        {
            IBeatmapLevel songInfo = this._levelListViewController.selectedLevel;
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
        public void RefreshScoreSaberData(IBeatmapLevel level)
        {            
            // TODO - fix this obvious mess...
            // use controllers level...
            if (level == null)
            {
                level = _levelListViewController.selectedLevel;
            }

            // abort!
            if (level == null)
            {
                Logger.Debug("Aborting RefreshScoreSaberData()");
                return;
            }

            Logger.Trace("RefreshScoreSaberData({0})", level.levelID);

            // display pp potentially
            if (this._model.LevelIdToScoreSaberData != null && this._levelDifficultyViewController.selectedDifficultyBeatmap != null)
            {
                /*if (this._ppText == null)
                {
                    // Create the PP and Star rating labels
                    //RectTransform bmpTextRect = Resources.FindObjectsOfTypeAll<RectTransform>().First(x => x.name == "BPMText");
                    var text = BeatSaberUI.CreateText(this._levelDetailViewController.rectTransform, "PP", new Vector2(-15, -32), new Vector2(10f, 6f));
                    text.fontSize = 2.5f;
                    text.alignment = TextAlignmentOptions.Left;

                    text = BeatSaberUI.CreateText(this._levelDetailViewController.rectTransform, "STAR", new Vector2(-15, -34.5f), new Vector2(10f, 6f));
                    text.fontSize = 2.5f;
                    text.alignment = TextAlignmentOptions.Left;

                    _ppText = BeatSaberUI.CreateText(this._levelDetailViewController.rectTransform, "?", new Vector2(-20, -32), new Vector2(20f, 6f));
                    _ppText.fontSize = 2.5f;
                    _ppText.alignment = TextAlignmentOptions.Right;

                    _starText = BeatSaberUI.CreateText(this._levelDetailViewController.rectTransform, "", new Vector2(-20, -34.5f), new Vector2(20f, 6f));
                    _starText.fontSize = 2.5f;
                    _starText.alignment = TextAlignmentOptions.Right;

                    _nText = BeatSaberUI.CreateText(this._levelDetailViewController.rectTransform, "", new Vector2(-20, -37.0f), new Vector2(20f, 6f));
                    _nText.fontSize = 2.5f;
                    _nText.alignment = TextAlignmentOptions.Right;
                }*/

                BeatmapDifficulty difficulty = this._levelDifficultyViewController.selectedDifficultyBeatmap.difficulty;
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
                if (_model.SortedSongList == null)
                {
                    Logger.Debug("Songs are not sorted yet, nothing to refresh.");
                    return;
                }

                BeatmapLevelSO[] levels = _model.SortedSongList.ToArray();

                //_levelListViewController.SetLevels(levels);

                LevelListViewController songListViewController = this._levelSelectionFlowCoordinator.GetPrivateField<LevelListViewController>("_levelListViewController");
                ReflectionUtil.SetPrivateField(_levelListTableView, "_levels", levels);                
                ReflectionUtil.SetPrivateField(songListViewController, "_levels", levels);
                TableView tableView = ReflectionUtil.GetPrivateField<TableView>(_levelListTableView, "_tableView");
                if (tableView == null)
                {
                    Logger.Debug("TableView is not available yet, cannot refresh...");
                    return;
                }
                tableView.ReloadData();

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
                if (levels.Length > 6 && !String.IsNullOrEmpty(selectedLevelID) && levels.Any(x => x.levelID == selectedLevelID))
                {
                    SelectAndScrollToLevel(_levelListTableView, selectedLevelID);
                }

                RefreshSortButtonUI();
                RefreshQuickScrollButtons();
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
        private void SelectAndScrollToLevel(LevelListTableView table, string levelID)
        {
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

            int row = table.RowNumberForLevelID(levelID);
            TableView tableView = table.GetComponentInChildren<TableView>();
            tableView.SelectRow(row, true);
            tableView.ScrollToRow(row, true);
            _lastRow = row;
        }

        /// <summary>
        /// Helper for updating the model (which updates the song list)c
        /// </summary>
        public void UpdateSongList()
        {
            Logger.Trace("UpdateSongList()");

            // UI not created yet. 
            BeatmapCharacteristicSO bc = null;
            if (_beatmapCharacteristicSelectionViewController != null)
            {
                bc = _beatmapCharacteristicSelectionViewController.selectedBeatmapCharacteristic;
            }

            _model.UpdateSongLists(bc);
            this.RefreshDirectoryButtons();
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
                if (this._levelListViewController != null && this._levelListViewController.isActiveAndEnabled)
                {
                    bool isShiftKeyDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

                    if (isShiftKeyDown && Input.GetKeyDown(KeyCode.X))
                    {
                        this._beatmapCharacteristicSelectionViewController.HandleBeatmapCharacteristicSegmentedControlDidSelectCell(null, 1);
                    }
                    else if (Input.GetKeyDown(KeyCode.X))
                    {
                        this._beatmapCharacteristicSelectionViewController.HandleBeatmapCharacteristicSegmentedControlDidSelectCell(null, 0);
                    }

                    // back
                    if (Input.GetKeyDown(KeyCode.Escape))
                    {
                        this._levelSelectionNavigationController.DismissButtonWasPressed();
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
                        this.SelectAndScrollToLevel(_levelListTableView, _model.SortedSongList[0].levelID);
                        this._levelDifficultyViewController.HandleDifficultyTableViewDidSelectRow(null, 0);
                    }

                    // v start a song or enter a folder
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
                        this.SelectAndScrollToLevel(_levelListTableView, _model.SortedSongList[_lastRow].levelID);
                    }

                    if (isShiftKeyDown && Input.GetKeyDown(KeyCode.M))
                    {
                        _pageDownFastButton.onClick.Invoke();
                    }
                    else if (Input.GetKeyDown(KeyCode.M))
                    {
                        _lastRow = (_lastRow + 1) % this._model.SortedSongList.Count;
                        this.SelectAndScrollToLevel(_levelListTableView, _model.SortedSongList[_lastRow].levelID);
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
 