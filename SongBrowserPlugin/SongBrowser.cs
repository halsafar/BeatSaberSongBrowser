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
using System.IO;

namespace SongBrowserPlugin
{
    public class SongSortButton
    {
        public SongSortMode SortMode;
        public Button Button;
    }

    public class SongBrowser : MonoBehaviour
    {       
        public const int MenuIndex = 1;

        private Logger _log = new Logger("SongBrowserPlugin");

        private SongSelectionMasterViewController _songSelectionMasterView;
        private SongDetailViewController _songDetailViewController;
        private SongListViewController _songListViewController;
        private MainMenuViewController _mainMenuViewController;

        private Dictionary<String, Sprite> _icons;

        private Button _buttonInstance;

        private List<SongSortButton> _sortButtonGroup;
        
        private Button _addFavoriteButton;
        private String _addFavoriteButtonText = null;
    
        private SongBrowserSettings _settings;

        private List<SongLoaderPlugin.CustomSongInfo> _customSongInfos;
        private Dictionary<String, SongLoaderPlugin.CustomSongInfo> _levelIdToCustomSongInfo;

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

            SongLoaderPlugin.SongLoader.SongsLoaded.AddListener(OnSongLoaderLoadedSongs);

            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Get a handle to the view controllers we are going to add elements to.
        /// </summary>
        public void AcquireUIElements()
        {
            _icons = new Dictionary<String, Sprite>();
            foreach (Sprite sprite in Resources.FindObjectsOfTypeAll<Sprite>())
            {
                if (_icons.ContainsKey(sprite.name))
                {
                    continue;
                }
                _icons.Add(sprite.name, sprite);
            }

            try
            {
                _buttonInstance = Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PlayButton"));
                _mainMenuViewController = Resources.FindObjectsOfTypeAll<MainMenuViewController>().First();
                _songSelectionMasterView = Resources.FindObjectsOfTypeAll<SongSelectionMasterViewController>().First();                
                _songDetailViewController = Resources.FindObjectsOfTypeAll<SongDetailViewController>().First();
                _songListViewController = Resources.FindObjectsOfTypeAll<SongListViewController>().First();
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
                RectTransform rect = _songSelectionMasterView.transform as RectTransform;

                // Create Sorting Songs By-Buttons
                _sortButtonGroup = new List<SongSortButton>
                {
                    CreateSortButton(rect, "PlayButton", "Fav", 3, "AllDirectionsIcon", 30f, 77.5f, 15f, 5f, SongSortMode.Favorites),
                    CreateSortButton(rect, "PlayButton", "Def", 3, "AllDirectionsIcon", 15f, 77.5f, 15f, 5f, SongSortMode.Default),
                    CreateSortButton(rect, "PlayButton", "Org", 3, "AllDirectionsIcon", 0f, 77.5f, 15f, 5f, SongSortMode.Original),
                    CreateSortButton(rect, "PlayButton", "New", 3, "AllDirectionsIcon", -15f, 77.5f, 15f, 5f, SongSortMode.Newest)
                };

                // Creaate Add to Favorites Button
                RectTransform transform = _songDetailViewController.transform as RectTransform;
                _addFavoriteButton = UIBuilder.CreateUIButton(transform, "QuitButton", _buttonInstance);
                (_addFavoriteButton.transform as RectTransform).anchoredPosition = new Vector2(45f, 5f);
                (_addFavoriteButton.transform as RectTransform).sizeDelta = new Vector2(10f, 10f);                

                if (_addFavoriteButtonText == null)
                {
                    LevelStaticData level = getSelectedSong();
                    RefreshAddFavoriteButton(level);
                }
                
                UIBuilder.SetButtonText(ref _addFavoriteButton, _addFavoriteButtonText);
                //UIBuilder.SetButtonIcon(ref _addFavoriteButton, _icons["AllDirectionsIcon"]);
                UIBuilder.SetButtonTextSize(ref _addFavoriteButton, 3);
                UIBuilder.SetButtonIconEnabled(ref _addFavoriteButton, false);

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
        /// Generic create sort button.
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="templateButtonName"></param>
        /// <param name="buttonText"></param>
        /// <param name="iconName"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="action"></param>
        private SongSortButton CreateSortButton(RectTransform rect, string templateButtonName, string buttonText, float fontSize, string iconName, float x, float y, float w, float h, SongSortMode sortMode)
        {
            SongSortButton sortButton = new SongSortButton();
            Button newButton = UIBuilder.CreateUIButton(rect, templateButtonName, _buttonInstance);
            
            newButton.interactable = true;
            (newButton.transform as RectTransform).anchoredPosition = new Vector2(x, y);
            (newButton.transform as RectTransform).sizeDelta = new Vector2(w, h);

            UIBuilder.SetButtonText(ref newButton, buttonText);
            //UIBuilder.SetButtonIconEnabled(ref _originalButton, false);
            UIBuilder.SetButtonIcon(ref newButton, _icons[iconName]);
            UIBuilder.SetButtonTextSize(ref newButton, fontSize);

            newButton.onClick.RemoveAllListeners();
            newButton.onClick.AddListener(delegate () {
                _log.Debug("Sort button - {0} - pressed.", sortMode.ToString());
                _settings.sortMode = sortMode;
                _settings.Save();
                List<LevelStaticData> sortedSongList = ProcessSongList();
                RefreshSongList(sortedSongList);
            });

            sortButton.Button = newButton;
            sortButton.SortMode = sortMode;

            return sortButton;
        }

        /// <summary>
        /// Bind to some UI events.
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="scene"></param>
        private void SceneManagerOnActiveSceneChanged(Scene arg0, Scene scene)
        {
            //_log.Debug("scene.buildIndex==" + scene.buildIndex);
            try
            {
                if (scene.buildIndex == SongBrowser.MenuIndex)
                {
                    _log.Debug("SceneManagerOnActiveSceneChanged - Setting Up UI");

                    AcquireUIElements();
                    CreateUI();
                                                            
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
            _log.Debug("OnSongLoaderLoadedSongs");
            List<LevelStaticData>  sortedSongList = ProcessSongList();
            RefreshSongList(sortedSongList);
            RefreshAddFavoriteButton(sortedSongList[0]);

            // Call into SongLoaderPlugin to get all the song info.
            try
            {
                _customSongInfos = ReflectionUtil.InvokeMethod<SongLoaderPlugin.SongLoader>(SongLoaderPlugin.SongLoader.Instance, "RetrieveAllSongs", null) as List<SongLoaderPlugin.CustomSongInfo>;
                _levelIdToCustomSongInfo = _customSongInfos.ToDictionary(x => x.GetIdentifier(), x => x);
            }
            catch (Exception e)
            {
                _log.Exception("Exception trying to sort by newest: {0}", e);
            }
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
            //ProcessSongList();
        }

        /// <summary>
        /// Helper to quickly refresh add to favorites button
        /// </summary>
        /// <param name="levelId"></param>
        private void RefreshAddFavoriteButton(LevelStaticData level)
        {
            var levelId = _songListViewController.levelId;
            if (levelId == null)
            {
                if (level != null)
                {
                    levelId = level.levelId;
                }
            }

            //if (level != null) _log.Debug(level.songName);
            //if (level != null)
            //    _log.Debug("Level.id=" + level.levelId);
            //_log.Debug("_songListViewController.levelId=" + _songListViewController.levelId);

            if (levelId == null)
            {
                _addFavoriteButtonText = "0";
                return;
            }

            if (levelId == null)
            {
                levelId = level.levelId;
            }

            if (_settings.favorites.Contains(levelId))
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

            return songList;
        }

        /// <summary>
        /// Helper to overwrite the existing song list.
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

            // Weights used for keeping the original songs in order
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

            List<LevelStaticData> songList = AcquireSongList();

            switch(_settings.sortMode)
            {
                case SongSortMode.Favorites:
                    _log.Debug("Sorting song list as favorites");
                    songList = songList
                        .AsQueryable()
                        .OrderBy(x => _settings.favorites.Contains(x.levelId) == false)
                        .ThenBy(x => x.songName)
                        .ThenBy(x => x.authorName)                        
                        .ToList();
                    break;
                case SongSortMode.Original:
                    _log.Debug("Sorting song list as original");
                   
                    songList = songList
                        .AsQueryable()
                        .OrderByDescending(x => weights.ContainsKey(x.levelId) ? weights[x.levelId] : 0)
                        .ThenBy(x => x.songName)
                        .ToList();                    
                    break;
                case SongSortMode.Newest:
                    _log.Debug("Sorting song list as newest.");
                    songList = songList
                        .AsQueryable()
                        .OrderBy(x => weights.ContainsKey(x.levelId) ? weights[x.levelId] : 0)
                        .ThenByDescending(x => x.levelId.StartsWith("Level") ? DateTime.MinValue : File.GetLastWriteTimeUtc(_levelIdToCustomSongInfo[x.levelId].path))
                        .ToList();
                    break;
                case SongSortMode.Default:                    
                default:
                    _log.Debug("Sorting song list as default");
                    songList = songList
                        .AsQueryable()
                        .OrderBy(x => x.authorName)
                        .ThenBy(x => x.songName).ToList();                    
                    break;
            }

            OverwriteSongList(songList);

            return songList;
        }

        /// <summary>
        /// Try to refresh the song list.  Broken for now.
        /// </summary>
        public void RefreshSongList(List<LevelStaticData> songList)
        {
            _log.Debug("Attempting to refresh the song list view.");
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
            // So far all we need to refresh is the sort buttons.
            foreach (SongSortButton sortButton in _sortButtonGroup)
            {
                UIBuilder.SetButtonBorder(ref sortButton.Button, Color.black);
                if (sortButton.SortMode == _settings.sortMode)
                {
                    UIBuilder.SetButtonBorder(ref sortButton.Button, Color.red);
                }
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
                    _settings.sortMode = SongSortMode.Newest;
                else if (_settings.sortMode == SongSortMode.Newest)
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
 