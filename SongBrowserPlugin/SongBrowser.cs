using UnityEngine;
using System.Linq;
using System;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


namespace SongBrowserPlugin
{
    public class SongBrowser : MonoBehaviour
    {       
        public static readonly UnityEvent SongsLoaded = new UnityEvent();

        public const int MenuIndex = 1;

        private Logger _log = new Logger("SongBrowserPlugin");

        private SongSelectionMasterViewController _songSelectionMasterView;
        private SongDetailViewController _songDetailViewController;

        private Button _buttonInstance;
        private Button _favoriteButton;
        private Button _addFavoriteButton;


        private RectTransform _songSelectRectTransform;

        public static List<Sprite> _icons = new List<Sprite>();


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

            SongBrowserSettings bs = SongBrowserSettings.Load();
            bs.Save();

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
                _favoriteButton = UIBuilder.CreateUIButton(_songSelectRectTransform, "QuitButton", _buttonInstance);
                (_favoriteButton.transform as RectTransform).anchoredPosition = new Vector2(0, 80f);
                (_favoriteButton.transform as RectTransform).sizeDelta = new Vector2(20f, 10f);

                UIBuilder.SetButtonText(ref _favoriteButton, "Fav!");            
                UIBuilder.SetButtonIcon(ref _favoriteButton, _icons.First(x => (x.name == "SettingsIcon")));

                _favoriteButton.onClick.AddListener(delegate () {
                    _log.Info("Sorting by Favorites!");

                });

                // Creaate Add to Favorites Button
                RectTransform transform = _songDetailViewController.transform as RectTransform;
                _addFavoriteButton = UIBuilder.CreateUIButton(transform, "QuitButton", _buttonInstance);
                (_addFavoriteButton.transform as RectTransform).anchoredPosition = new Vector2(40f, 0f);
                (_addFavoriteButton.transform as RectTransform).sizeDelta = new Vector2(20f, 10f);

                UIBuilder.SetButtonText(ref _addFavoriteButton, "+1");
                UIBuilder.SetButtonIcon(ref _addFavoriteButton, _icons.First(x => (x.name == "AllDirectionsIcon")));

                _addFavoriteButton.onClick.AddListener(delegate () {
                    _log.Info("Add to Favorites!");

                });
            }
            catch (Exception e)
            {
                _log.Exception("Exception CreateUI: " + e.Message);
            }
        }

        private void SceneManagerOnActiveSceneChanged(Scene arg0, Scene scene)
        {
            //AcquireSongs();

            Console.WriteLine("scene.buildIndex=" + scene.buildIndex);

            try
            {
                MainMenuViewController _mainMenuViewController = Resources.FindObjectsOfTypeAll<MainMenuViewController>().First();
                Button _buttonInstance = Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "SoloButton"));
                _buttonInstance.onClick.AddListener(delegate ()
                {
                    AcquireSongs();
                });
            }
            catch (Exception e)
            {
                _log.Exception("Exception: " + e);
            }            
        }

        private void OnDidSelectSongEvent(SongListViewController songListViewController)
        {

        }

        public void AcquireSongs()
        {
            _log.Debug("SceneManager.GetActiveScene().buildIndex=" + SceneManager.GetActiveScene().buildIndex);
            if (SceneManager.GetActiveScene().buildIndex != MenuIndex) return;
            _log.Debug("Acquiring Songs");
           
            var gameScenesManager = Resources.FindObjectsOfTypeAll<GameScenesManager>().FirstOrDefault();

            var gameDataModel = PersistentSingleton<GameDataModel>.instance;
            var oldData = gameDataModel.gameStaticData.worldsData[0].levelsData.ToList();

            _log.Debug("SongBrowser got oldData.Count={0}", oldData.Count);
            oldData.ForEach(i => Console.WriteLine(i.levelId));


            var sorted_old_data = oldData.AsQueryable().OrderBy(x => x.authorName).OrderBy(x => x.songName);

            ReflectionUtil.SetPrivateField(gameDataModel.gameStaticData.worldsData[0], "_levelsData", sorted_old_data.ToArray());
        }

        /// <summary>
        /// Map some key presses directly to UI interactions to make testing easier.
        /// </summary>
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                AcquireSongs();
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
                _favoriteButton.onClick.Invoke();
            }
        }
    }
}