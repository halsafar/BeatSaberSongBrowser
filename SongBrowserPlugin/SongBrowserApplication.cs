using SongLoaderPlugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SongBrowserPlugin
{
    public class SongBrowserApplication : MonoBehaviour
    {
        public static SongBrowserApplication Instance;

        private Logger _log = new Logger("SongBrowserMasterViewController");

        // BeatSaber UI Elements
        private SongSelectionMasterViewController _songSelectionMasterView;
        private SongDetailViewController _songDetailViewController;
        private SongListViewController _songListViewController;
        private MainMenuViewController _mainMenuViewController;
        private MenuMasterViewController _menuMasterViewController;

        // Song Browser UI Elements
        private SongBrowserMasterViewController _songBrowserMasterViewController;

        public Dictionary<String, Sprite> CachedIcons;
        public Button ButtonTemplate;

        internal static void OnLoad()
        {
            if (Instance != null)
            {
                return;
            }
            new GameObject("BeatSaber SongBrowser Mod").AddComponent<SongBrowserApplication>();
            //DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// 
        /// </summary>
        private void Awake()
        {
            Instance = this;

            SceneManager.activeSceneChanged += SceneManagerOnActiveSceneChanged;
            SongLoader.SongsLoaded.AddListener(OnSongLoaderLoadedSongs);
        }

        private void OnSongLoaderLoadedSongs()
        {
            try
            {
                _log.Debug("Attempting to take over the didSelectModeEvent Button");
                SoloModeSelectionViewController view = Resources.FindObjectsOfTypeAll<SoloModeSelectionViewController>().First();

                if (view.didSelectModeEvent != null)
                {
                    Delegate[] delegates = view.didSelectModeEvent.GetInvocationList();
                    view.didSelectModeEvent -= delegates[0] as Action<SoloModeSelectionViewController, GameplayMode>;
                }

                view.didSelectModeEvent += HandleSoloModeSelectionViewControllerDidSelectMode;
            }
            catch (Exception e)
            {
                _log.Exception("Exception during OnSongLoaderLoadedSongs: " + e);
            }
        }

        /// <summary>
        /// Bind to some UI events.
        /// </summary>
        /// <param name="arg0"></param>
        /// <param name="scene"></param>
        private void SceneManagerOnActiveSceneChanged(Scene arg0, Scene scene)
        {
            if (scene.buildIndex != SongBrowserMasterViewController.MenuIndex)
            {
                return;
            }

            AcquireUIElements();

            // Clone and override the default song-browser.
            if (_songBrowserMasterViewController == null)
            {
                _log.Debug("Attempting to clone SongBrowserMasterViewController");
                _songBrowserMasterViewController = UIBuilder.CreateViewController<SongBrowserMasterViewController>(SongBrowserMasterViewController.Name);
                System.Reflection.FieldInfo[] fields = typeof(SongSelectionMasterViewController).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (System.Reflection.FieldInfo field in fields)
                {
                    //_log.Debug(field.Name);
                    field.SetValue(_songBrowserMasterViewController, field.GetValue(_songSelectionMasterView));
                }
            }

            _log.Debug("Overriding Song Browser");
            ReflectionUtil.SetPrivateField(_menuMasterViewController, "_songSelectionMasterViewController", _songBrowserMasterViewController);
        }

        /// <summary>
        /// Get a handle to the view controllers we are going to add elements to.
        /// </summary>
        public void AcquireUIElements()
        {
            _log.Debug("Acquiring important UI elements.");
            CachedIcons = new Dictionary<String, Sprite>();
            foreach (Sprite sprite in Resources.FindObjectsOfTypeAll<Sprite>())
            {
                if (CachedIcons.ContainsKey(sprite.name))
                {
                    continue;
                }
                CachedIcons.Add(sprite.name, sprite);
            }

            try
            {
                ButtonTemplate = Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PlayButton"));
                _mainMenuViewController = Resources.FindObjectsOfTypeAll<MainMenuViewController>().First();
                _menuMasterViewController = Resources.FindObjectsOfTypeAll<MenuMasterViewController>().First();
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
        /// Hijack the result of clicking into the song browser.
        /// </summary>
        /// <param name="viewController"></param>
        /// <param name="gameplayMode"></param>
        private void HandleSoloModeSelectionViewControllerDidSelectMode(SoloModeSelectionViewController viewController, GameplayMode gameplayMode)
        {
            _log.Debug("Hi jacking solo mode buttons");
            try
            {
                

                ReflectionUtil.SetPrivateField(_menuMasterViewController, "_gameplayMode", gameplayMode);
                ReflectionUtil.SetPrivateField(_menuMasterViewController, "_songSelectionMasterViewController", _songBrowserMasterViewController);

                GameBuildMode gameBuildMode = ReflectionUtil.GetPrivateField<GameBuildMode>(_menuMasterViewController, "_gameBuildMode");
                GameObject dismissButton = ReflectionUtil.GetPrivateField<GameObject>(_songSelectionMasterView, "_dismissButton");
                ReflectionUtil.SetPrivateField(_songBrowserMasterViewController, "_dismissButton", dismissButton);

                LevelStaticData[] levelsForGameplayMode = _menuMasterViewController.GetLevelsForGameplayMode(gameplayMode, gameBuildMode);

                bool _canUseGlobalLeaderboards = ReflectionUtil.GetPrivateField<bool>(_menuMasterViewController, "_canUseGlobalLeaderboards");
                bool showDismissButton = true;
                bool useLocalLeaderboards = !_canUseGlobalLeaderboards || gameplayMode == GameplayMode.PartyStandard;
                bool showPlayerStats = ArePlayerStatsUsed(gameplayMode);

                _songBrowserMasterViewController.Init(null, LevelStaticData.Difficulty.Easy, levelsForGameplayMode, useLocalLeaderboards, showDismissButton, showPlayerStats, gameplayMode);
                viewController.PresentModalViewController(_songBrowserMasterViewController, null, false);
                _songSelectionMasterView = _songBrowserMasterViewController;

                _log.Debug("Success hijacking ...");
            }
            catch (Exception e)
            {
                _log.Exception("Exception replacing in-game song browser: {0}\n{1}", e.Message, e.StackTrace);
            }
        }

        // Token: 0x06000C43 RID: 3139 RVA: 0x00035DE8 File Offset: 0x00033FE8
        private bool ArePlayerStatsUsed(GameplayMode gameplayMode)
        {
            return gameplayMode == GameplayMode.SoloStandard || gameplayMode == GameplayMode.SoloNoArrows || gameplayMode == GameplayMode.SoloOneSaber;
        }

        /// <summary>
        /// Helper for invoking buttons.
        /// </summary>
        /// <param name="buttonName"></param>
        private void InvokeBeatSaberButton(String buttonName)
        {
            Button buttonInstance = Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == buttonName));
            buttonInstance.onClick.Invoke();
        }

        /// <summary>
        /// Map some key presses directly to UI interactions to make testing easier.
        /// </summary>
        private void Update()
        {
            // z,x,c,v can be used to get into a song, b will hit continue button after song ends
            if (Input.GetKeyDown(KeyCode.Z))
            {
                InvokeBeatSaberButton("SoloButton");
            }

            if (Input.GetKeyDown(KeyCode.X))
            {
                InvokeBeatSaberButton("FreePlayButton");
            }

            if (Input.GetKeyDown(KeyCode.B))
            {
                InvokeBeatSaberButton("ContinueButton");
            }
        }
    }
}
