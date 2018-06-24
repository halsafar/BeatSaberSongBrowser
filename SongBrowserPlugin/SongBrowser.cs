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

        private List<Sprite> _icons = new List<Sprite>();

        private Button _buttonInstance;
        private Button _favoriteButton;
        private Button _defaultButton;
        private Button _originalButton;

        private Button _addFavoriteButton;
    
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
            Instance = this;

            _settings = SongBrowserSettings.Load();

            AcquireUIElements();
            CreateUI();
           
            SceneManager.activeSceneChanged += SceneManagerOnActiveSceneChanged;
            SceneManagerOnActiveSceneChanged(new Scene(), new Scene());
            
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
                _buttonInstance = Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "QuitButton"));

                _songSelectionMasterView = Resources.FindObjectsOfTypeAll<SongSelectionMasterViewController>().First();                

                _songDetailViewController = Resources.FindObjectsOfTypeAll<SongDetailViewController>().FirstOrDefault();

                _songListViewController = Resources.FindObjectsOfTypeAll<SongListViewController>().First();

                _songSelectRectTransform = _songSelectionMasterView.transform.parent as RectTransform;
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
                _favoriteButton = UIBuilder.CreateUIButton(_songSelectRectTransform, "QuitButton", _buttonInstance);
                _favoriteButton.interactable = true;
                (_favoriteButton.transform as RectTransform).anchoredPosition = new Vector2(145, 70f);
                (_favoriteButton.transform as RectTransform).sizeDelta = new Vector2(15f, 10f);

                UIBuilder.SetButtonText(ref _favoriteButton, "Fav");
                UIBuilder.SetButtonIconEnabled(ref _favoriteButton, false);
                //UIBuilder.SetButtonIcon(ref _favoriteButton, _icons.First(x => (x.name == "AllDirectionsIcon")));

                _favoriteButton.onClick.AddListener(delegate () {
                    _settings.sortMode = SongSortMode.Favorites;
                    ProcessSongList();
                    RefreshSongList();
                });

                // Default button
                _defaultButton = UIBuilder.CreateUIButton(_songSelectRectTransform, "QuitButton", _buttonInstance);
                _defaultButton.interactable = true;
                (_defaultButton.transform as RectTransform).anchoredPosition = new Vector2(130f, 70f);
                (_defaultButton.transform as RectTransform).sizeDelta = new Vector2(15f, 10f);

                UIBuilder.SetButtonText(ref _defaultButton, "Def");
                UIBuilder.SetButtonIconEnabled(ref _defaultButton, false);
                //UIBuilder.SetButtonIcon(ref _defaultButton, _icons.First(x => (x.name == "SettingsIcon")));

                _defaultButton.onClick.AddListener(delegate () {
                    _settings.sortMode = SongSortMode.Default;
                    ProcessSongList();
                    RefreshSongList();
                });

                // Original button
                _originalButton = UIBuilder.CreateUIButton(_songSelectRectTransform, "QuitButton", _buttonInstance);
                _originalButton.interactable = true;
                (_originalButton.transform as RectTransform).anchoredPosition = new Vector2(115f, 70f);
                (_originalButton.transform as RectTransform).sizeDelta = new Vector2(15f, 10f);

                UIBuilder.SetButtonText(ref _originalButton, "Org");
                UIBuilder.SetButtonIconEnabled(ref _originalButton, false);
                //UIBuilder.SetButtonIcon(ref _originalButton, _icons.First(x => (x.name == "SoloIcon")));

                _originalButton.onClick.AddListener(delegate () {
                    _settings.sortMode = SongSortMode.Default;
                    ProcessSongList();
                    RefreshSongList();
                });

                // Creaate Add to Favorites Button
                RectTransform transform = _songDetailViewController.transform as RectTransform;
                _addFavoriteButton = UIBuilder.CreateUIButton(transform, "QuitButton", _buttonInstance);
                (_addFavoriteButton.transform as RectTransform).anchoredPosition = new Vector2(40f, 0f);
                (_addFavoriteButton.transform as RectTransform).sizeDelta = new Vector2(25f, 10f);

                UIBuilder.SetButtonText(ref _addFavoriteButton, "+1");
                UIBuilder.SetButtonIcon(ref _addFavoriteButton, _icons.First(x => (x.name == "AllDirectionsIcon")));

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
            //_log.Debug("scene.buildIndex==" + scene.buildIndex);
            try
            {
                if (scene.buildIndex == SongBrowser.MenuIndex || scene.buildIndex == -1)
                {
                    _log.Debug("SceneManagerOnActiveSceneChanged - binding to UI");
                    
                    SongLoaderPlugin.SongLoader.SongsLoaded.AddListener(OnSongLoaderLoadedSongs);

                    //SongListTableView table = Resources.FindObjectsOfTypeAll<SongListTableView>().FirstOrDefault();
                    //table.songListTableViewDidSelectRow += OnDidSelectSongRow;
                    //MainMenuViewController _mainMenuViewController = Resources.FindObjectsOfTypeAll<MainMenuViewController>().First();
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
            ProcessSongList();
            RefreshSongList();
        }

        /// <summary>
        /// Adjust UI based on song selected.
        /// </summary>
        /// <param name="songListViewController"></param>
        private void OnDidSelectSongEvent(SongListViewController songListViewController)
        {
            LevelStaticData level = _songSelectionMasterView.GetLevelStaticDataForSelectedSong();
            if (_settings.favorites.Contains(level.levelId))
            {
                UIBuilder.SetButtonText(ref _addFavoriteButton, "-1");
            }
            else
            {
                UIBuilder.SetButtonText(ref _addFavoriteButton, "+1");
            }
        }

        /*private void OnDidSelectSongRow(SongListTableView table, int index)
        {
            Console.WriteLine("OnDidSelectSongRow");
        }*/

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
                UIBuilder.SetButtonText(ref _addFavoriteButton, "+1");
            }
            else
            {
                _log.Info("Add {0} to favorites", songInfo.name);
                _settings.favorites.Add(songInfo.levelId);
                UIBuilder.SetButtonText(ref _addFavoriteButton, "-1");
            }

            _settings.Save();
            ProcessSongList();
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
        public void ProcessSongList()
        {
            _log.Debug("ProcessSongList()");

            List<LevelStaticData> songList = AcquireSongList();

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
                    songList = songList
                        .AsQueryable()
                        .OrderBy(x => !x.levelId.StartsWith("Level"))
                        .ThenBy(x => x.levelId.StartsWith("Level"))
                        .ThenBy(x => x.name)
                        .ToList();
                    break;
                case SongSortMode.Default:                    
                default:
                    _log.Debug("  Sorting list as default");
                    songList = songList
                        .AsQueryable()
                        .OrderBy(x => x.authorName)
                        .OrderBy(x => x.songName).ToList();
                    break;
            }
            
            OverwriteSongList(songList);
        }

        /// <summary>
        /// Try to refresh the song list.  Broken for now.
        /// </summary>
        public void RefreshSongList()
        {
            _log.Debug("Trying to refresh song list!");

            // Forcefully refresh the song view
            var newSongList = AcquireSongList();            
            SongListTableView songTableView = _songListViewController.GetComponentInChildren<SongListTableView>();
            ReflectionUtil.SetPrivateField(songTableView, "_levels", newSongList.ToArray());
            TableView tableView = ReflectionUtil.GetPrivateField<TableView>(songTableView, "_tableView");
            tableView.ReloadData();

            songTableView.ClearSelection();
            _songListViewController.SelectSong(0);
            _songSelectionMasterView.HandleSongListDidSelectSong(_songListViewController);

            RefreshUI();
            
            //Action showMethod = delegate () { };
            //_songSelectionMasterView.DismissModalViewController(showMethod);            
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
            if (Input.GetKeyDown(KeyCode.T))
            {
                _settings.sortMode = SongSortMode.Original;
                ProcessSongList();
                RefreshSongList();
            }

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

            if (Input.GetKeyDown(KeyCode.F))
            {
                ToggleSongInFavorites();
            }
        }
    }
}