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

        private Logger _log = new Logger(Name);

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
        private TextMeshProUGUI _ppText;
        private TextMeshProUGUI _starText;
        private TextMeshProUGUI _nText;
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
            _log.Trace("CreateUI()");
            
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

                // delete dialog
                this._deleteDialog = UnityEngine.Object.Instantiate<SimpleDialogPromptViewController>(this._simpleDialogPromptViewControllerPrefab);
                this._deleteDialog.gameObject.SetActive(false);

                // sprites
                this._addFavoriteSprite = Base64Sprites.Base64ToSprite(Base64Sprites.AddToFavoritesIcon);
                this._removeFavoriteSprite = Base64Sprites.Base64ToSprite(Base64Sprites.RemoveFromFavoritesIcon);

                // create song browser main ui
                this.CreateUIElements();

                // handlers
                TableView tableView = ReflectionUtil.GetPrivateField<TableView>(_levelListTableView, "_tableView");
                tableView.didSelectRowEvent += HandleDidSelectTableViewRow;
                _levelListViewController.didSelectLevelEvent += OnDidSelectLevelEvent;
                _levelDifficultyViewController.didSelectDifficultyEvent += OnDidSelectDifficultyEvent;
                _beatmapCharacteristicSelectionViewController.didSelectBeatmapCharacteristicEvent += OnDidSelectBeatmapCharacteristic;

                _rebuildUI = false;
            }
            catch (Exception e)
            {
                _log.Exception("Exception during CreateUI: ", e);
            }
        }

        /// <summary>
        /// Builds the SongBrowser UI
        /// </summary>
        private void CreateUIElements()
        {
            _log.Trace("CreateUIElements");

            try
            {
                // Gather some transforms and templates to use.
                RectTransform sortButtonTransform = this._levelSelectionNavigationController.transform as RectTransform;
                RectTransform otherButtonTransform = this._levelDetailViewController.transform as RectTransform;
                Button sortButtonTemplate = Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "SettingsButton"));
                Button otherButtonTemplate = Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "SettingsButton"));                
                Sprite arrowIcon = SongBrowserApplication.Instance.CachedIcons["ArrowIcon"];
                
                // Resize some of the UI
                _tableViewRectTransform = _levelListViewController.GetComponentsInChildren<RectTransform>().First(x => x.name == "TableViewContainer");                
                _tableViewRectTransform.sizeDelta = new Vector2(0f, -20f);
                _tableViewRectTransform.anchoredPosition = new Vector2(0f, -2.5f);
                
                // Create Sorting Songs By-Buttons
                _log.Debug("Creating sort by buttons...");
                                
                float fontSize = 2.0f;
                float buttonWidth = 14.50f;
                float buttonHeight = 5.5f;
                float buttonX = 25.0f;
                float buttonY = -5.5f;

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
                    _sortButtonGroup.Add(UIBuilder.CreateSortButton(sortButtonTransform, sortButtonTemplate, arrowIcon,
                        sortButtonNames[i], 
                        fontSize, 
                        buttonX + (buttonWidth * i) + 2.5f, 
                        buttonY, 
                        buttonWidth, 
                        buttonHeight, 
                        sortModes[i],
                        OnSortButtonClickEvent));
                }

                // Create filter buttons
                _log.Debug("Creating filter buttons...");

                float filterButtonX = buttonX + (buttonWidth * (sortButtonNames.Length - 1)) + (buttonWidth / 2.0f) + 2.5f;
                Vector2 iconButtonSize = new Vector2(5.5f, buttonHeight);
                Sprite playlistSprite = Base64Sprites.Base64ToSprite(Base64Sprites.PlaylistIcon);
                Sprite searchSprite = Base64Sprites.Base64ToSprite(Base64Sprites.SearchIcon);

                List<Tuple<SongFilterMode, UnityEngine.Events.UnityAction, Sprite>> filterButtonSetup = new List<Tuple<SongFilterMode, UnityEngine.Events.UnityAction, Sprite>>()
                {
                    Tuple.Create<SongFilterMode, UnityEngine.Events.UnityAction, Sprite>(SongFilterMode.Favorites, OnFavoriteFilterButtonClickEvent, _addFavoriteSprite),
                    //Tuple.Create<SongFilterMode, UnityEngine.Events.UnityAction, Sprite>(SongFilterMode.Playlist, OnPlaylistButtonClickEvent, playlistSprite),
                    Tuple.Create<SongFilterMode, UnityEngine.Events.UnityAction, Sprite>(SongFilterMode.Search, OnSearchButtonClickEvent, searchSprite),
                };

                _filterButtonGroup = new List<SongFilterButton>();
                for (int i = 0; i < filterButtonSetup.Count; i++)
                {
                    Tuple<SongFilterMode, UnityEngine.Events.UnityAction, Sprite> t = filterButtonSetup[i];
                    Button b = UIBuilder.CreateIconButton(sortButtonTransform, otherButtonTemplate,
                        t.Item3,
                        new Vector2(filterButtonX + (iconButtonSize.x * i), buttonY),
                        new Vector2(iconButtonSize.x, iconButtonSize.y),
                        new Vector2(3.0f, -2.5f),
                        new Vector2(3.5f, 3.5f),
                        new Vector2(1.0f, 1.0f),
                        0);
                    SongFilterButton filterButton = new SongFilterButton
                    {
                        Button = b,
                        FilterMode = t.Item1
                    };
                    b.onClick.AddListener(t.Item2);
                    _filterButtonGroup.Add(filterButton);
                }

                // Create Add to Favorites Button
                Vector2 addFavoritePos = new Vector2(50f, -37.75f);
                _addFavoriteButton = UIBuilder.CreateIconButton(otherButtonTransform, otherButtonTemplate, null, 
                    new Vector2(addFavoritePos.x, addFavoritePos.y), 
                    new Vector2(7.0f, 7.0f), 
                    new Vector2(3.5f, -3.5f),
                    new Vector2(4.0f, 4.0f), 
                    new Vector2(1.0f, 1.0f),
                    0.0f);
                _addFavoriteButton.onClick.AddListener(delegate () {
                    ToggleSongInPlaylist();
                });

                /* TODO - address, this code now coupled with model load, oops.
                if (_currentAddFavoriteButtonSprite == null)
                {
                   IBeatmapLevel level = this._levelListViewController.selectedLevel;
                   if (level != null)
                   {
                       RefreshAddFavoriteButton(level.levelID);
                   }                    
                }*/

                // Create delete button
                _deleteButton = UIBuilder.CreateButton(otherButtonTransform, otherButtonTemplate, "Delete", fontSize, 30f, -80.0f, 15f, 5f);                
                _deleteButton.onClick.AddListener(delegate () {
                    HandleDeleteSelectedLevel();
                });

                // Create fast scroll buttons
                /*_pageUpFastButton = UIBuilder.CreateIconButton(sortButtonTransform, otherButtonTemplate, arrowIcon,
                    new Vector2(0, 0),
                    new Vector2(6.0f, 5.5f),
                    new Vector2(0f, 0f),
                    new Vector2(1.5f, 1.5f),
                    new Vector2(2.0f, 2.0f), 
                    180);
                _pageUpFastButton.onClick.AddListener(delegate () {
                    this.JumpSongList(-1, SEGMENT_PERCENT);
                });

                _pageDownFastButton = UIBuilder.CreateIconButton(sortButtonTransform, otherButtonTemplate, arrowIcon,
                    new Vector2(0, -40f),
                    new Vector2(6.0f, 5.5f),
                    new Vector2(0f, 0f),
                    new Vector2(1.5f, 1.5f),
                    new Vector2(2.0f, 2.0f), 
                    0);
                _pageDownFastButton.onClick.AddListener(delegate () {
                    this.JumpSongList(1, SEGMENT_PERCENT);
                });*/

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
                    UIBuilder.SetButtonText(ref _enterFolderButton, "Enter");

                    // Create up folder button
                    _upFolderButton = UIBuilder.CreateIconButton(sortButtonTransform, sortButtonTemplate, arrowIcon,
                        new Vector2(filterButtonX + (iconButtonSize.x* filterButtonSetup.Count), buttonY),
                        new Vector2(iconButtonSize.x, iconButtonSize.y),
                        new Vector2(0f, 0f),
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
                _log.Exception("Exception CreateUIElements:", e);
            }
        }

        /// <summary>
        /// Sort button clicked.
        /// </summary>
        private void OnSortButtonClickEvent(SongSortMode sortMode)
        {
            _log.Debug("Sort button - {0} - pressed.", sortMode.ToString());
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
            _log.Debug("Filter button - {0} - pressed.", SongFilterMode.Favorites.ToString());

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
            _log.Debug("Filter button - {0} - pressed.", SongFilterMode.Search.ToString());
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
            _log.Debug("Filter button - {0} - pressed.", SongFilterMode.Playlist.ToString());
            _model.LastSelectedLevelId = null;

            if (_model.Settings.filterMode != SongFilterMode.Playlist)
            {
                if (_playListFlowCoordinator == null || !_playListFlowCoordinator.isActiveAndEnabled)
                {
                    _playListFlowCoordinator = UIBuilder.CreateFlowCoordinator<PlaylistFlowCoordinator>("PlaylistFlowCoordinator");
                    _playListFlowCoordinator.didSelectPlaylist += HandleDidSelectPlaylist;
                    //_playListFlowCoordinator.Present(_levelSelectionNavigationController);
                }                
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
                _log.Trace("OnDidSelectLevelEvent()");
                if (level == null)
                {
                    _log.Debug("No level selected?");
                    return;
                }

                if (_model.Settings == null)
                {
                    _log.Debug("Settings not instantiated yet?");
                    return;
                }

                _model.LastSelectedLevelId = level.levelID;

                RefreshAddFavoriteButton(level.levelID);
                RefreshQuickScrollButtons();

                if (level.levelID.StartsWith("Folder_"))
                {
                    _log.Debug("Folder selected!  Adjust PlayButton logic...");
                    HandleDidSelectFolderRow(level);
                }
                else
                {
                    HandleDidSelectLevelRow(level);
                }
            }
            catch (Exception e)
            {
                _log.Exception("Exception selecting song:", e);
            }
        }


        private void OnDidSelectBeatmapCharacteristic(BeatmapCharacteristicSelectionViewController view, BeatmapCharacteristicSO bc)
        {
            _log.Trace("OnDidSelectBeatmapCharacteristic({0}", bc.name);
            _model.UpdateSongLists(bc);
            this.RefreshSongList();
        }

        /// <summary>
        /// Handle difficulty level selection.
        /// </summary>
        private void OnDidSelectDifficultyEvent(BeatmapDifficultyViewController view, IDifficultyBeatmap beatmap)
        {
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
            _log.Trace("HandleDidSelectTableViewRow({0})", row);
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
                _log.Info("No level selected, cannot delete nothing...");
                return;
            }

            if (!_model.LevelIdToCustomSongInfos.ContainsKey(level.levelID))
            {
                _log.Debug("Cannot delete non-custom levels.");
                return;
            }

            if (level.levelID.StartsWith("Folder"))
            {
                _log.Debug("Cannot delete folders.");
                return;
            }

            SongLoaderPlugin.OverrideClasses.CustomLevel customLevel = _model.LevelIdToCustomSongInfos[level.levelID];

            this._deleteDialog.Init("Delete level warning!", String.Format("<color=#00AAFF>Permanently delete level: {0}</color>\n  Do you want to continue?", customLevel.songName), "YES", "NO");
            this._deleteDialog.didFinishEvent -= this.HandleDeleteDialogPromptViewControllerDidFinish;
            this._deleteDialog.didFinishEvent += this.HandleDeleteDialogPromptViewControllerDidFinish;

            _levelSelectionFlowCoordinator.InvokePrivateMethod("PresentViewController", new object[] { this._deleteDialog, null, false });            
        }

        /// <summary>
        /// Handle delete dialog resolution.
        /// </summary>
        /// <param name="viewController"></param>
        /// <param name="ok"></param>
        public void HandleDeleteDialogPromptViewControllerDidFinish(SimpleDialogPromptViewController viewController, bool ok)
        {
            viewController.didFinishEvent -= this.HandleDeleteDialogPromptViewControllerDidFinish;
            //_levelSelectionFlowCoordinator.InvokePrivateMethod("DismissViewController", new object[] { _deleteDialog, null, false });
            if (!ok)
            {
                viewController.DismissViewControllerCoroutine(null, false);
            }
            else
            {
                string customSongsPath = Path.Combine(Environment.CurrentDirectory, "CustomSongs");
                IBeatmapLevel level = this._levelListViewController.selectedLevel;                
                SongLoaderPlugin.OverrideClasses.CustomLevel customLevel = _model.LevelIdToCustomSongInfos[level.levelID];
                string songPath = customLevel.customSongInfo.path;
                bool isZippedSong = false;

                viewController.DismissViewControllerCoroutine(null, false);
                _log.Debug("Deleting: {0}", songPath);

                if (!string.IsNullOrEmpty(songPath) && songPath.Contains("/.cache/"))
                {
                    isZippedSong = true;
                }

                if (isZippedSong)
                {
                    DirectoryInfo songHashDir = Directory.GetParent(songPath);
                    _log.Debug("Deleting zipped song cache: {0}", songHashDir.FullName);
                    Directory.Delete(songHashDir.FullName, true);

                    foreach (string file in Directory.GetFiles(customSongsPath, "*.zip"))
                    {
                        string hash = CreateMD5FromFile(file);
                        if (hash != null)
                        {
                            if (hash == songHashDir.Name)
                            {
                                _log.Debug("Deleting zipped song: {0}", file);
                                File.Delete(file);
                                break;
                            }
                        }
                    }
                }
                else
                {
                    // Just delete the song we know about.
                    FileAttributes attr = File.GetAttributes(songPath);
                    if (attr.HasFlag(FileAttributes.Directory))
                    {
                        _log.Debug("Deleting song: {0}", songPath);
                        Directory.Delete(songPath, true);
                    }

                    // check if this is in the BeatSaberDownloader format
                    if (_model.Settings.deleteNumberedSongFolder)
                    {                        
                        String[] splitPath = songPath.Split('/');
                        if (splitPath.Length > 2)
                        {
                            String numberedDir = splitPath[splitPath.Length - 2];
                            Regex r = new Regex(@"^\d{1,}-\d{1,}");
                            if (r.Match(numberedDir).Success)
                            {
                                DirectoryInfo songNumberedDirPath = Directory.GetParent(songPath);
                                _log.Debug("Deleting song numbered folder: {0}", songNumberedDirPath.FullName);
                                Directory.Delete(songNumberedDirPath.FullName, true);
                            }
                        }
                    }
                }

                int newRow = _model.SortedSongList.FindIndex(x => x.levelID == level.levelID) - 1;
                if (newRow > 0 && newRow < _model.SortedSongList.Count)
                {
                    _model.LastSelectedLevelId = _model.SortedSongList[newRow].levelID;
                }
                else
                {
                    _model.LastSelectedLevelId = null;
                }

                SongLoaderPlugin.SongLoader.Instance.RemoveSongWithPath(songPath);
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
            if (_playListFlowCoordinator != null)
            {
                _levelSelectionNavigationController.PopViewControllerFullscreenModeCoroutine(null, true);
                _playListFlowCoordinator.gameObject.SetActive(false);
                UnityEngine.Object.DestroyImmediate(_playListFlowCoordinator);
            }

            if (p != null)
            {
                _log.Debug("Showing songs for playlist: {0}", p.Title);
                _model.Settings.filterMode = SongFilterMode.Playlist;
                _model.CurrentPlaylist = p;
                _model.Settings.Save();
            }
            else
            {
                _log.Debug("No playlist selected");
            }

            this.UpdateSongList();
            this.RefreshSongList();
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

            _log.Debug("Presenting search keyboard");
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

            _log.Debug("Searching for \"{0}\"...", searchFor);

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
            
            _log.Debug("jumpDirection: {0}, newRow: {1}", jumpDirection, newRow);
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
                    _log.Info("Remove {0} from editing playlist", songInfo.songName);
                    _model.RemoveSongFromEditingPlaylist(songInfo);
                }
                else
                {
                    _log.Info("Add {0} to editing playlist", songInfo.songName);
                    _model.AddSongToEditingPlaylist(songInfo);
                }
            }

            RefreshAddFavoriteButton(songInfo.levelID);

            _model.Settings.Save();
        }

        /// <summary>
        /// Update GUI elements that show score saber data.
        /// TODO - make better
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
                _log.Debug("Aborting RefreshScoreSaberData()");
                return;
            }

            _log.Trace("RefreshScoreSaberData({0})", level.levelID);

            // display pp potentially
            if (this._model.LevelIdToScoreSaberData != null && this._levelDifficultyViewController.selectedDifficultyBeatmap != null)
            {
                if (this._ppText == null)
                {
                    // Create the PP and Star rating labels
                    //RectTransform bmpTextRect = Resources.FindObjectsOfTypeAll<RectTransform>().First(x => x.name == "BPMText");
                    var text = UIBuilder.CreateText(this._levelDetailViewController.rectTransform, "PP", new Vector2(-15, -32), new Vector2(10f, 6f));
                    text.fontSize = 2.5f;
                    text.alignment = TextAlignmentOptions.Left;

                    text = UIBuilder.CreateText(this._levelDetailViewController.rectTransform, "STAR", new Vector2(-15, -34.5f), new Vector2(10f, 6f));
                    text.fontSize = 2.5f;
                    text.alignment = TextAlignmentOptions.Left;

                    _ppText = UIBuilder.CreateText(this._levelDetailViewController.rectTransform, "?", new Vector2(-20, -32), new Vector2(20f, 6f));
                    _ppText.fontSize = 2.5f;
                    _ppText.alignment = TextAlignmentOptions.Right;

                    _starText = UIBuilder.CreateText(this._levelDetailViewController.rectTransform, "", new Vector2(-20, -34.5f), new Vector2(20f, 6f));
                    _starText.fontSize = 2.5f;
                    _starText.alignment = TextAlignmentOptions.Right;

                    _nText = UIBuilder.CreateText(this._levelDetailViewController.rectTransform, "", new Vector2(-20, -37.0f), new Vector2(20f, 6f));
                    _nText.fontSize = 2.5f;
                    _nText.alignment = TextAlignmentOptions.Right;
                }

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
                    njsText = "NJS " + njs.ToString();
                }
                _nText.text = njsText;

                _log.Debug("Checking if have info for song {0}", level.songName);
                if (this._model.LevelIdToScoreSaberData.ContainsKey(level.levelID))
                {
                    _log.Debug("Checking if have difficulty for song {0} difficulty {1}", level.songName, difficultyString);
                    ScoreSaberData ppData = this._model.LevelIdToScoreSaberData[level.levelID];
                    if (ppData.difficultyToSaberDifficulty.ContainsKey(difficultyString))
                    {
                        _log.Debug("Display pp for song.");
                        float pp = ppData.difficultyToSaberDifficulty[difficultyString].pp;
                        float star = ppData.difficultyToSaberDifficulty[difficultyString].star;

                        _ppText.SetText(String.Format("{0:0.##}", pp));
                        _starText.SetText(String.Format("{0:0.##}", star));
                    }
                    else
                    {
                        _ppText.SetText("?");
                        _starText.SetText("?");
                    }
                }
                else
                {
                    _ppText.SetText("?");
                    _starText.SetText("?");
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

            UIBuilder.SetButtonIcon(ref _addFavoriteButton, _currentAddFavoriteButtonSprite);
        }

        /// <summary>
        /// Adjust the UI colors.
        /// </summary>
        public void RefreshSortButtonUI()
        {
            // So far all we need to refresh is the sort buttons.
            foreach (SongSortButton sortButton in _sortButtonGroup)
            {
                UIBuilder.SetButtonBorder(ref sortButton.Button, Color.black);
                if (sortButton.SortMode == _model.Settings.sortMode)
                {
                    if (this._model.Settings.invertSortResults)
                    {
                        UIBuilder.SetButtonBorder(ref sortButton.Button, Color.red);
                    }
                    else
                    {
                        UIBuilder.SetButtonBorder(ref sortButton.Button, Color.green);
                    }
                }
            }
            // refresh filter buttons
            foreach (SongFilterButton filterButton in _filterButtonGroup)
            {
                UIBuilder.SetButtonBorder(ref filterButton.Button, Color.clear);
                if (filterButton.FilterMode == _model.Settings.filterMode)
                {
                    UIBuilder.SetButtonBorder(ref filterButton.Button, Color.green);
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
            _log.Info("Refreshing the song list view.");
            try
            {
                // TODO - remove OR
                if (_model.SortedSongList == null || _model.SortedSongList.Count <= 0)
                {
                    _log.Debug("Songs are not sorted yet, nothing to refresh.");
                    return;
                }
                
                LevelSO[] levels = _model.SortedSongList.ToArray();
                LevelListViewController songListViewController = this._levelSelectionFlowCoordinator.GetPrivateField<LevelListViewController>("_levelListViewController");
                ReflectionUtil.SetPrivateField(_levelListTableView, "_levels", levels);                
                ReflectionUtil.SetPrivateField(songListViewController, "_levels", levels);
                TableView tableView = ReflectionUtil.GetPrivateField<TableView>(_levelListTableView, "_tableView");
                if (tableView == null)
                {
                    _log.Debug("TableView is not available yet, cannot refresh...");
                    return;
                }
                tableView.ReloadData();

                String selectedLevelID = null;
                if (_model.LastSelectedLevelId != null)
                {
                    selectedLevelID = _model.LastSelectedLevelId;
                    _log.Debug("Scrolling to row for level ID: {0}", selectedLevelID);                    
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
                _log.Exception("Exception refreshing song list:", e);
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
                _log.Info("Checking for BeatSaber Twitch Integration Plugin...");

                // Try to detect BeatSaber Twitch Integration Plugin
                _detectedTwitchPluginQueue = Resources.FindObjectsOfTypeAll<VRUIViewController>().Any(x => x.name == "RequestInfo");

                _log.Info("BeatSaber Twitch Integration plugin detected: " + _detectedTwitchPluginQueue);

                _checkedForTwitchPlugin = true;
            }

            // Skip scrolling to level if twitch plugin has queue active.
            if (_detectedTwitchPluginQueue)
            {
                _log.Debug("Skipping SelectAndScrollToLevel() because we detected Twitch Integrtion Plugin has a Queue active...");
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
            _log.Trace("UpdateSongList()");

            // UI not created yet. 
            if (_beatmapCharacteristicSelectionViewController == null)
            {
                return;
            }

            BeatmapCharacteristicSO bc = _beatmapCharacteristicSelectionViewController.selectedBeatmapCharacteristic;
            _model.UpdateSongLists(bc);
            this.RefreshDirectoryButtons();
        }

        /// <summary>
        /// Not normally called by the game-engine.  Dependent on SongBrowserApplication to call it.
        /// </summary>
        public void LateUpdate()
        {
            if (Input.GetKeyDown(KeyCode.B))
            {
                _log.Debug("Invoking OK Button");
                VRUIViewController view = Resources.FindObjectsOfTypeAll<VRUIViewController>().First(x => x.name == "StandardLevelResultsViewController");
                view.GetComponentsInChildren<Button>().First(x => x.name == "Ok").onClick.Invoke();
            }

            CheckDebugUserInput();
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

                    if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.X))
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

                    // cycle sort modes
                    if (Input.GetKeyDown(KeyCode.T))
                    {
                        _sortButtonLastPushedIndex = (_sortButtonLastPushedIndex + 1) % _sortButtonGroup.Count;
                        _sortButtonGroup[_sortButtonLastPushedIndex].Button.onClick.Invoke();
                    }

                    // select current sort mode again (toggle inverting)
                    if (Input.GetKeyDown(KeyCode.Y))
                    {
                        _sortButtonGroup[_sortButtonLastPushedIndex].Button.onClick.Invoke();
                    }

                    // filter playlists
                    if (Input.GetKeyDown(KeyCode.P))
                    {
                        OnPlaylistButtonClickEvent();
                    }

                    // filter search
                    if (Input.GetKeyDown(KeyCode.S))
                    {
                        OnSearchButtonClickEvent();
                    }

                    // filter favorites
                    if (Input.GetKeyDown(KeyCode.F))
                    {
                        OnFavoriteFilterButtonClickEvent();
                    }

                    // delete
                    if (Input.GetKeyDown(KeyCode.D))
                    {
                        _deleteButton.onClick.Invoke();
                    }

                    // c - select difficulty for top song
                    if (Input.GetKeyDown(KeyCode.C))
                    {
                        this.SelectAndScrollToLevel(_levelListTableView, _model.SortedSongList[0].levelID);
                        this._levelDifficultyViewController.HandleDifficultyTableViewDidSelectRow(null, 0);
                        //TODO - this._levelSelectionFlowCoordinator.HandleDifficultyViewControllerDidSelectDifficulty(_levelDifficultyViewController, _model.SortedSongList[0].GetDifficultyBeatmap(BeatmapDifficulty.Easy));
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
                        _deleteDialog.GetPrivateField<TextMeshProButton>("_okButton").button.onClick.Invoke();
                    }

                    if (Input.GetKeyDown(KeyCode.Escape))
                    {
                        _deleteDialog.GetPrivateField<TextMeshProButton>("_cancelButton").button.onClick.Invoke();
                    }
                }
            }
            catch (Exception e)
            {
                _log.Exception("Debug Input caused Exception: ", e);
            }
        }
    }
}
 