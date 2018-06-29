using UnityEngine;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Text;
using HMUI;
using System.Text.RegularExpressions;

namespace SongBrowserPlugin
{
    public class SongBrowser : MonoBehaviour
    {       
        public static readonly UnityEvent SongsLoaded = new UnityEvent();

        public const int MenuIndex = 1;

        private Logger _log = new Logger("SongBrowserPlugin");

        private SongSelectionMasterViewController _songSelectionMasterView;
        private SongDetailViewController _songDetailViewController;
        private SongListViewController _songListViewController;
        private MainMenuViewController _mainMenuViewController;

        private List<Sprite> _icons = new List<Sprite>();

        private Button _buttonInstance;
        private Button _favoriteButton;
        private Button _defaultButton;
        private Button _originalButton;

        private Button _addFavoriteButton;
        private String _addFavoriteButtonText = null;
    
        private RectTransform _songSelectRectTransform;

        private SongBrowserSettings _settings;

        /// <summary>
        /// Unity OnLoad
        /// </summary>
        public static void OnLoad()
        {
            if (Instance != null) return;
            new GameObject("Song Browser").AddComponent<SongBrowser>();
        }

        public static SongBrowser Instance;

        /// <summary>
        /// Builds the UI for this plugin.
        /// </summary>
        private void Awake()
        {
            _log.Debug("Awake()");

            Instance = this;

            _settings = SongBrowserSettings.Load();
          
            SceneManager.activeSceneChanged += SceneManagerOnActiveSceneChanged;

            //SceneManagerOnActiveSceneChanged(new Scene(), new Scene());

            SongLoaderPlugin.SongLoader.SongsLoaded.AddListener(OnSongLoaderLoadedSongs);

            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Get a handle to the view controllers we are going to add elements to.
        /// </summary>
        public void AcquireUIElements()
        {
            foreach (Sprite sprite in Resources.FindObjectsOfTypeAll<Sprite>())
            {
                _icons.Add(sprite);
            }

            try
            {
                _buttonInstance = Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PlayButton"));
                _mainMenuViewController = Resources.FindObjectsOfTypeAll<MainMenuViewController>().First();
                _songSelectionMasterView = Resources.FindObjectsOfTypeAll<SongSelectionMasterViewController>().First();                
                _songDetailViewController = Resources.FindObjectsOfTypeAll<SongDetailViewController>().First();
                _songListViewController = Resources.FindObjectsOfTypeAll<SongListViewController>().First();
                _songSelectRectTransform = _songListViewController.transform as RectTransform;
            }
            catch (Exception e)
            {
                _log.Exception("Exception AcquireUIElements(): " + e);
            }
        }

        /// <summary>
        /// Builds the SongBrowser UI
        /// </summary>
        public void CreateUI()
        {
            _log.Debug("CreateUI");

            // _icons.ForEach(i => Console.WriteLine(i.ToString()));

            try
            {
                // Create Sorting Songs By-Buttons
                // Fav button
                RectTransform rect = _songDetailViewController.transform as RectTransform; 
                _favoriteButton = UIBuilder.CreateUIButton(rect, "PlayButton", _buttonInstance);
                _favoriteButton.interactable = true;
                (_favoriteButton.transform as RectTransform).anchoredPosition = new Vector2(30f, 74f);
                (_favoriteButton.transform as RectTransform).sizeDelta = new Vector2(15f, 10f);
                
                UIBuilder.SetButtonText(ref _favoriteButton, "Fav");
                //UIBuilder.SetButtonIconEnabled(ref _favoriteButton, false);
                UIBuilder.SetButtonIcon(ref _favoriteButton, _icons.First(x => (x.name == "AllDirectionsIcon")));

                _favoriteButton.onClick.RemoveAllListeners();
                _favoriteButton.onClick.AddListener(delegate () {
                    _log.Debug("Sort button - favorites - pressed.");
                    _settings.sortMode = SongSortMode.Favorites;
                    var sortedSongList = ProcessSongList();
                    RefreshSongList(sortedSongList);
                });

                // Default button
                _defaultButton = UIBuilder.CreateUIButton(rect, "PlayButton", _buttonInstance);
                _defaultButton.interactable = true;
                (_defaultButton.transform as RectTransform).anchoredPosition = new Vector2(15f, 74f);
                (_defaultButton.transform as RectTransform).sizeDelta = new Vector2(15f, 10f);

                UIBuilder.SetButtonText(ref _defaultButton, "Def");
                //UIBuilder.SetButtonIconEnabled(ref _defaultButton, false);
                UIBuilder.SetButtonIcon(ref _defaultButton, _icons.First(x => (x.name == "SettingsIcon")));

                _defaultButton.onClick.RemoveAllListeners();
                _defaultButton.onClick.AddListener(delegate () {
                    _log.Debug("Sort button - default - pressed.");
                    _settings.sortMode = SongSortMode.Default;
                    var sortedSongList = ProcessSongList();
                    RefreshSongList(sortedSongList);
                });

                // Original button
                _originalButton = UIBuilder.CreateUIButton(rect, "PlayButton", _buttonInstance);
                _originalButton.interactable = true;
                (_originalButton.transform as RectTransform).anchoredPosition = new Vector2(0f, 74f);
                (_originalButton.transform as RectTransform).sizeDelta = new Vector2(15f, 10f);

                UIBuilder.SetButtonText(ref _originalButton, "Org");
                //UIBuilder.SetButtonIconEnabled(ref _originalButton, false);
                UIBuilder.SetButtonIcon(ref _originalButton, _icons.First(x => (x.name == "SoloIcon")));

                _originalButton.onClick.RemoveAllListeners();
                _originalButton.onClick.AddListener(delegate () {
                    _log.Debug("Sort button - original - pressed.");
                    _settings.sortMode = SongSortMode.Original;
                    var sortedSongList = ProcessSongList();
                    RefreshSongList(sortedSongList);
                });

                // Creaate Add to Favorites Button
                RectTransform transform = _songDetailViewController.transform as RectTransform;
                _addFavoriteButton = UIBuilder.CreateUIButton(transform, "QuitButton", _buttonInstance);
                (_addFavoriteButton.transform as RectTransform).anchoredPosition = new Vector2(40f, 0f);
                (_addFavoriteButton.transform as RectTransform).sizeDelta = new Vector2(25f, 10f);

                if (_addFavoriteButtonText == null)
                {
                    LevelStaticData level = getSelectedSong();
                    RefreshAddFavoriteButton(level);
                }
                
                UIBuilder.SetButtonText(ref _addFavoriteButton, _addFavoriteButtonText);
                UIBuilder.SetButtonIcon(ref _addFavoriteButton, _icons.First(x => (x.name == "AllDirectionsIcon")));

                _addFavoriteButton.onClick.RemoveAllListeners();
                _addFavoriteButton.onClick.AddListener(delegate () {                    
                    ToggleSongInFavorites();
                });

                RefreshUI();
            }
            catch (Exception e)
            {
                _log.Exception("Exception CreateUI: " + e.Message);
            }
        }

        /// <summary>
        /// Bind to some UI events.
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="scene"></param>
        private void SceneManagerOnActiveSceneChanged(Scene arg0, Scene scene)
        {
            _log.Debug("scene.buildIndex==" + scene.buildIndex);
            try
            {
                if (scene.buildIndex == SongBrowser.MenuIndex)
                {
                    _log.Debug("SceneManagerOnActiveSceneChanged - Setting Up UI");

                    AcquireUIElements();
                    CreateUI();
                                                            
                    //SongListTableView tableView = _songListViewController.GetComponentInChildren<SongListTableView>();
                    //MainMenuViewController _mainMenuViewController = Resources.FindObjectsOfTypeAll<MainMenuViewController>().First();
                    //tableView.songListTableViewDidSelectRow += TableView_songListTableViewDidSelectRow;

                    _songListViewController.didSelectSongEvent += OnDidSelectSongEvent;
                }
            }
            catch (Exception e)
            {
                _log.Exception("Exception during scene change: " + e);
            }       
        }

        /// <summary>
        /// Song Loader has loaded the songs, lets sort them.
        /// </summary>
        private void OnSongLoaderLoadedSongs()
        {
            _log.Debug("--OnSongLoaderLoadedSongs");
            List<LevelStaticData>  sortedSongList = ProcessSongList();
            RefreshSongList(sortedSongList);
            RefreshAddFavoriteButton(sortedSongList[0]);
        }

        /// <summary>
        /// Adjust UI based on song selected.
        /// Various ways of detecting if a song is not properly selected.  Seems most hit the first one.
        /// </summary>
        /// <param name="songListViewController"></param>
        private void OnDidSelectSongEvent(SongListViewController songListViewController)
        {
            LevelStaticData level = getSelectedSong();
            if (level == null)
            {
                _log.Debug("No song selected?");
                return;
            }

            if (_settings == null)
            {
                _log.Debug("Settings not instantiated yet?");
                return;
            }

            RefreshAddFavoriteButton(level);
        }

        /// <summary>
        /// Return LevelStaticData or null.
        /// </summary>
        private LevelStaticData getSelectedSong()
        {
            // song list not even visible
            if (!_songSelectionMasterView.isActiveAndEnabled)
            {
                return null;
            }

            int selectedIndex = _songSelectionMasterView.GetSelectedSongIndex();
            //_log.Debug("Selected song index: " + selectedIndex);
            if (selectedIndex < 0)
            {
                return null;
            }

            LevelStaticData level = _songSelectionMasterView.GetLevelStaticDataForSelectedSong();

            return level;
        }

        /// <summary>
        /// Add/Remove song from favorites depending on if it already exists.
        /// </summary>
        private void ToggleSongInFavorites()
        {
            LevelStaticData songInfo = _songSelectionMasterView.GetLevelStaticDataForSelectedSong();
            if (_settings.favorites.Contains(songInfo.levelId))
            {
                _log.Info("Remove {0} from favorites", songInfo.name);                
                _settings.favorites.Remove(songInfo.levelId);
                _addFavoriteButtonText = "+1";
            }
            else
            {
                _log.Info("Add {0} to favorites", songInfo.name);
                _settings.favorites.Add(songInfo.levelId);
                _addFavoriteButtonText = "-1";                
            }

            UIBuilder.SetButtonText(ref _addFavoriteButton, _addFavoriteButtonText);

            _settings.Save();
            ProcessSongList();
        }

        /// <summary>
        /// Helper to quickly refresh add to favorites button
        /// </summary>
        /// <param name="levelId"></param>
        private void RefreshAddFavoriteButton(LevelStaticData level)
        {
            if (level == null)
            {
                _addFavoriteButtonText = "0";
            }
            else if (_settings.favorites.Contains(level.levelId))
            {
                _addFavoriteButtonText = "-1";
            }
            else
            {
                _addFavoriteButtonText = "+1";                
            }

            UIBuilder.SetButtonText(ref _addFavoriteButton, _addFavoriteButtonText);
        }

        /// <summary>
        /// Fetch the existing song list.
        /// </summary>
        /// <returns></returns>
        public List<LevelStaticData> AcquireSongList()
        {
            _log.Debug("AcquireSongList()");

            var gameScenesManager = Resources.FindObjectsOfTypeAll<GameScenesManager>().FirstOrDefault();        
            var gameDataModel = PersistentSingleton<GameDataModel>.instance;

            List<LevelStaticData> songList = gameDataModel.gameStaticData.worldsData[0].levelsData.ToList();
            _log.Debug("SongBrowser songList.Count={0}", songList.Count);

            return songList;
        }

        /// <summary>
        /// Helper to overwrite the existing song list and refresh it.
        /// </summary>
        /// <param name="newSongList"></param>
        public void OverwriteSongList(List<LevelStaticData> newSongList)
        {
            var gameDataModel = PersistentSingleton<GameDataModel>.instance;
            ReflectionUtil.SetPrivateField(gameDataModel.gameStaticData.worldsData[0], "_levelsData", newSongList.ToArray());                        
        }

        /// <summary>
        /// Sort the song list based on the settings.
        /// </summary>
        public List<LevelStaticData> ProcessSongList()
        {
            _log.Debug("ProcessSongList()");

            List<LevelStaticData> songList = AcquireSongList();
            //songList.ForEach(x => _log.Debug(x.levelId));
            switch(_settings.sortMode)
            {
                case SongSortMode.Favorites:
                    _log.Debug("  Sorting list as favorites");
                    songList = songList
                        .AsQueryable()
                        .OrderBy(x => x.authorName)
                        .OrderBy(x => x.songName)
                        .OrderBy(x => _settings.favorites.Contains(x.levelId) == false).ThenBy(x => x.songName)
                        .ToList();
                    break;
                case SongSortMode.Original:
                    _log.Debug("  Sorting list as original");

                    // Invert the weights from the game so we can order by descending and make LINQ work with us...
                    /*  Level4, Level2, Level9, Level5, Level10, Level6, Level7, Level1, Level3, Level8, */
                    Dictionary<string, int> weights = new Dictionary<string, int>();
                    weights["Level4"] = 10;
                    weights["Level2"] = 9;
                    weights["Level9"] = 8;
                    weights["Level5"] = 7;
                    weights["Level10"] = 6;
                    weights["Level6"] = 5;
                    weights["Level7"] = 4;
                    weights["Level1"] = 3;
                    weights["Level3"] = 2;
                    weights["Level8"] = 1;
                    
                    songList = songList
                        .AsQueryable()
                        .OrderByDescending(x => weights.ContainsKey(x.levelId) ? weights[x.levelId] : 0)
                        .ThenBy(x => x.songName)
                        .ToList();                    
                    break;
                case SongSortMode.Default:                    
                default:
                    _log.Debug("  Sorting list as default");
                    songList = songList
                        .AsQueryable()
                        .OrderBy(x => x.authorName)
                        .ThenBy(x => x.songName).ToList();                    
                    break;
            }

            //songList.ForEach(x => _log.Debug(x.songName));

            OverwriteSongList(songList);

            return songList;
        }

        /// <summary>
        /// Try to refresh the song list.  Broken for now.
        /// </summary>
        public void RefreshSongList(List<LevelStaticData> songList)
        {
            _log.Debug("Trying to refresh song list - {0}", _songSelectionMasterView);
            try
            {
                if (!_songSelectionMasterView.isActiveAndEnabled)
                {
                    _log.Debug("No song list to refresh.");
                    return;
                }

                // Refresh the master view
                bool useLocalLeaderboards = ReflectionUtil.GetPrivateField<bool>(_songSelectionMasterView, "_useLocalLeaderboards");
                bool showDismissButton = true;
                bool showPlayerStats = ReflectionUtil.GetPrivateField<bool>(_songSelectionMasterView, "_showPlayerStats");
                GameplayMode gameplayMode = ReflectionUtil.GetPrivateField<GameplayMode>(_songSelectionMasterView, "_gameplayMode");

                _songSelectionMasterView.Init(
                    _songSelectionMasterView.levelId,
                    _songSelectionMasterView.difficulty,
                    songList.ToArray(),
                    useLocalLeaderboards, showDismissButton, showPlayerStats, gameplayMode);

                // Refresh the song list
                SongListTableView songTableView = _songListViewController.GetComponentInChildren<SongListTableView>();
                if (songTableView == null)
                {
                    return;
                }
                
                ReflectionUtil.SetPrivateField(songTableView, "_levels", songList.ToArray());
                TableView tableView = ReflectionUtil.GetPrivateField<TableView>(songTableView, "_tableView");
                if (tableView == null)
                {
                    return;
                }

                tableView.ReloadData();

                // Clear Force selection of index 0 so we don't end up in a weird state.
                songTableView.ClearSelection();
                _songListViewController.SelectSong(0);
                _songSelectionMasterView.HandleSongListDidSelectSong(_songListViewController);
                RefreshUI();
                RefreshAddFavoriteButton(songList[0]);

                // Old method of force refresh
                //Action showMethod = delegate () { };
                //_songSelectionMasterView.DismissModalViewController(showMethod);            
            }
            catch (Exception e)
            {
                _log.Exception("Exception refreshing song list: {0}", e.Message);
            }
        }

        /// <summary>
        /// Adjust the UI colors.
        /// </summary>
        public void RefreshUI()
        {
            UIBuilder.SetButtonBorder(ref _favoriteButton, Color.black);
            UIBuilder.SetButtonBorder(ref _defaultButton, Color.black);
            UIBuilder.SetButtonBorder(ref _originalButton, Color.black);

            switch (_settings.sortMode)
            {
                case SongSortMode.Favorites:
                    UIBuilder.SetButtonBorder(ref _favoriteButton, Color.red);
                    break;
                case SongSortMode.Default:
                    UIBuilder.SetButtonBorder(ref _defaultButton, Color.red);
                    break;
                case SongSortMode.Original:
                    UIBuilder.SetButtonBorder(ref _originalButton, Color.red);
                    break;
                default:
                    break;
            }
            
        }

        /// <summary>
        /// Map some key presses directly to UI interactions to make testing easier.
        /// </summary>
        private void Update()
        {
            // cycle sort modes
            if (Input.GetKeyDown(KeyCode.T))
            {
                if (_settings.sortMode == SongSortMode.Favorites)
                    _settings.sortMode = SongSortMode.Original;
                else if (_settings.sortMode == SongSortMode.Original)
                    _settings.sortMode = SongSortMode.Default;
                else if (_settings.sortMode == SongSortMode.Default)
                    _settings.sortMode = SongSortMode.Favorites;

                var sortedSongList = ProcessSongList();
                RefreshSongList(sortedSongList);
            }

            // z,x,c,v can be used to get into a song, b will hit continue button after song ends
            if (Input.GetKeyDown(KeyCode.Z))
            {
                Button _buttonInstance = Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "SoloButton"));
                _buttonInstance.onClick.Invoke();
            }

            if (Input.GetKeyDown(KeyCode.X))
            {
                Button _buttonInstance = Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "FreePlayButton"));
                _buttonInstance.onClick.Invoke();                
            }

            if (Input.GetKeyDown(KeyCode.C))
            {                
                _songListViewController.SelectSong(0);
                _songSelectionMasterView.HandleSongListDidSelectSong(_songListViewController);

                DifficultyViewController _difficultyViewController = Resources.FindObjectsOfTypeAll<DifficultyViewController>().First();
                _difficultyViewController.SelectDifficulty(LevelStaticData.Difficulty.Hard);
                _songSelectionMasterView.HandleDifficultyViewControllerDidSelectDifficulty(_difficultyViewController);
            }

            if (Input.GetKeyDown(KeyCode.V))
            {
                _songSelectionMasterView.HandleSongDetailViewControllerDidPressPlayButton(_songDetailViewController);
            }

            if (Input.GetKeyDown(KeyCode.B))
            {
                Button _buttonInstance = Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "ContinueButton"));
                _buttonInstance.onClick.Invoke();
            }

            // change song index
            if (Input.GetKeyDown(KeyCode.N))
            {
                int newIndex = _songSelectionMasterView.GetSelectedSongIndex() - 1;
                _songListViewController.SelectSong(newIndex);
                _songSelectionMasterView.HandleSongListDidSelectSong(_songListViewController);

                SongListTableView songTableView = Resources.FindObjectsOfTypeAll<SongListTableView>().First();
                _songListViewController.HandleSongListTableViewDidSelectRow(songTableView, newIndex);
            }

            if (Input.GetKeyDown(KeyCode.M))
            {
                int newIndex = _songSelectionMasterView.GetSelectedSongIndex() + 1;

                _songListViewController.SelectSong(newIndex);
                _songSelectionMasterView.HandleSongListDidSelectSong(_songListViewController);

                SongListTableView songTableView = Resources.FindObjectsOfTypeAll<SongListTableView>().First();
                _songListViewController.HandleSongListTableViewDidSelectRow(songTableView, newIndex);
            }

            // add to favorites
            if (Input.GetKeyDown(KeyCode.F))
            {
                ToggleSongInFavorites();
            }
        }
    }
}
 