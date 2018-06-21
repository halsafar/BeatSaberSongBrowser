using UnityEngine;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Text;

namespace SongBrowserPlugin
{
    public class SongBrowser : MonoBehaviour
    {       
        public static readonly UnityEvent SongsLoaded = new UnityEvent();

        public const int MenuIndex = 1;

        private Logger _log = new Logger("SongBrowserPlugin");

        private SongSelectionMasterViewController _songSelectionMasterView;
        private SongDetailViewController _songDetailViewController;

        private List<Sprite> _icons = new List<Sprite>();

        private Button _buttonInstance;
        private Button _favoriteButton;
        private Button _defaultButton;
        private Button _addFavoriteButton;
    
        private RectTransform _songSelectRectTransform;

        private SongBrowserSettings _settings;

        
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

            AcquireUIElements();
            CreateUI();

            _settings = SongBrowserSettings.Load();

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

                _songSelectRectTransform = _songSelectionMasterView.transform.parent as RectTransform;

                _songDetailViewController = Resources.FindObjectsOfTypeAll<SongDetailViewController>().FirstOrDefault();
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
                (_favoriteButton.transform as RectTransform).anchoredPosition = new Vector2(0, 80f);
                (_favoriteButton.transform as RectTransform).sizeDelta = new Vector2(25f, 10f);

                UIBuilder.SetButtonText(ref _favoriteButton, "Fav!");            
                UIBuilder.SetButtonIcon(ref _favoriteButton, _icons.First(x => (x.name == "AllDirectionsIcon")));

                _favoriteButton.onClick.AddListener(delegate () {
                    _log.Info("Sorting by Favorites!");
                    _settings.sortMode = SongSortMode.Favorites;
                    ProcessSongList();
                    RefreshSongList();
                });

                // Default button
                _defaultButton = UIBuilder.CreateUIButton(_songSelectRectTransform, "QuitButton", _buttonInstance);
                (_defaultButton.transform as RectTransform).anchoredPosition = new Vector2(25f, 80f);
                (_defaultButton.transform as RectTransform).sizeDelta = new Vector2(25f, 10f);

                UIBuilder.SetButtonText(ref _defaultButton, "Def");
                UIBuilder.SetButtonIcon(ref _defaultButton, _icons.First(x => (x.name == "SettingsIcon")));

                _defaultButton.onClick.AddListener(delegate () {
                    _log.Info("Sorting by Favorites!");
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
                    _log.Info("Add to Favorites!");
                    AddSongToFavorites();
                });
            }
            catch (Exception e)
            {
                _log.Exception("Exception CreateUI: " + e.Message);
            }
        }

        /// <summary>
        /// Setup an event to fire the first time the user hits the SoloButton.  This is temporary.
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="scene"></param>
        private void SceneManagerOnActiveSceneChanged(Scene arg0, Scene scene)
        {
            //Console.WriteLine("scene.buildIndex=" + scene.buildIndex);

            try
            {
                MainMenuViewController _mainMenuViewController = Resources.FindObjectsOfTypeAll<MainMenuViewController>().First();
                Button _buttonInstance = Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "SoloButton"));
                _buttonInstance.onClick.AddListener(delegate ()
                {
                    //ProcessSongList();
                });
            }
            catch (Exception e)
            {
                _log.Exception("Exception during scene change: " + e);
            }       
        }

        public static byte[] GetHash(string inputString)
        {
            HashAlgorithm algorithm = MD5.Create();  //or use SHA256.Create();
            return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }

        /// <summary>
        /// Add song to favorites.
        /// </summary>
        private void AddSongToFavorites()
        {
            LevelStaticData songInfo = _songSelectionMasterView.GetLevelStaticDataForSelectedSong();
            String favorite_hash = songInfo.levelId;
            _settings.favorites.Add(favorite_hash);
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
            _log.Debug("SongBrowser Acquiring songList, songList.Count={0}", songList.Count);
            //oldData.ForEach(i => Console.WriteLine(i.levelId));

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
                        
            SongListViewController _songListViewController = Resources.FindObjectsOfTypeAll<SongListViewController>().FirstOrDefault();
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
                    _log.Debug("Sorting list as favorites");
                    songList = songList
                        .AsQueryable()
                        .OrderBy(x => x.authorName)
                        .OrderBy(x => x.songName)
                        .OrderBy(x => _settings.favorites.Contains(x.levelId) == false).ThenBy(x => x.songName)
                        .ToList();
                    break;
                case SongSortMode.Default:                    
                default:
                    _log.Debug("Sorting list as default");
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

            //gameDataModel.gameStaticData.SetDirty();
            //_songListViewController.Init(newSongList.ToArray());
            //_songSelectionMasterView.RefreshSongDetail();
            //_songSelectionMasterView.Init(_songSelectionMasterView.levelId, _songSelectionMasterView.difficulty, newSongList.ToArray(), true, true, true, GameplayMode.SoloStandard);
            //_songListViewController.DidActivate();


            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            //Button _buttonInstance = Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "FreePlayButton"));
            //_buttonInstance.onClick.Invoke();
            //LayoutRebuilder.ForceRebuildLayoutImmediate(_songListViewController.rectTransform);
            //LayoutRebuilder.ForceRebuildLayoutImmediate(_songSelectionMasterView.rectTransform);

        }

        /// <summary>
        /// Map some key presses directly to UI interactions to make testing easier.
        /// </summary>
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                ProcessSongList();
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

            SongListViewController _songListViewController = Resources.FindObjectsOfTypeAll<SongListViewController>().First();

            if (Input.GetKeyDown(KeyCode.C))
            {                

                _songListViewController.SelectSong(0);
                _songSelectionMasterView.HandleSongListDidSelectSong(_songListViewController);

                DifficultyViewController _difficultyViewController = Resources.FindObjectsOfTypeAll<DifficultyViewController>().First();
                _difficultyViewController.SelectDifficulty(LevelStaticData.Difficulty.Hard);
                _songSelectionMasterView.HandleDifficultyViewControllerDidSelectDifficulty(_difficultyViewController);
            }

            if (Input.GetKeyDown(KeyCode.N))
            {
                _songListViewController.SelectSong(_songSelectionMasterView.GetSelectedSongIndex() - 1);
                _songSelectionMasterView.HandleSongListDidSelectSong(_songListViewController);
            }

            if (Input.GetKeyDown(KeyCode.M))
            {
                _songListViewController.SelectSong(_songSelectionMasterView.GetSelectedSongIndex() + 1);
                _songSelectionMasterView.HandleSongListDidSelectSong(_songListViewController);
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                AddSongToFavorites();
                _favoriteButton.onClick.Invoke();
            }
        }
    }
}