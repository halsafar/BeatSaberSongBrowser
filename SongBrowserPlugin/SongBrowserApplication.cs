using UnityEngine;
using System.Linq;
using System;
using System.Collections.Generic;
using SongLoaderPlugin.OverrideClasses;
using UnityEngine.SceneManagement;
using SongLoaderPlugin;
using UnityEngine.UI;
using System.Reflection;
using SongBrowserPlugin.UI;

namespace SongBrowserPlugin
{
    public class SongBrowserApplication : MonoBehaviour
    {
        public const int MenuIndex = 1;

        public static SongBrowserApplication Instance;

        private Logger _log = new Logger("SongBrowserApplication");

        // BeatSaber UI Elements
        private StandardLevelSelectionFlowCoordinator _levelSelectionFlowCoordinator;
        private MainFlowCoordinator _mainFlowCoordinator;

        // Song Browser UI Elements
        private SongBrowserFlowCoordinator _songBrowserFlowCoordinator;
        public Dictionary<String, Sprite> CachedIcons;
        public Button ButtonTemplate;

        /// <summary>
        /// 
        /// </summary>
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
            _log.Debug("Awake()");

            Instance = this;

            SceneManager.activeSceneChanged += SceneManagerOnActiveSceneChanged;
            SongLoader.SongsLoadedEvent += OnSongLoaderLoadedSongs;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Start()
        {
            _log.Debug("Start()");

            AcquireUIElements();

            // Clone and override the default level flow controller.
            if (_songBrowserFlowCoordinator == null)
            {
                _log.Debug("Attempting to clone StandardLevelSelectionFlowCoordinator into our derived class.");
                _songBrowserFlowCoordinator = UIBuilder.CreateFlowCoordinator<SongBrowserFlowCoordinator>(SongBrowserFlowCoordinator.Name);
                System.Reflection.FieldInfo[] fields = typeof(StandardLevelSelectionFlowCoordinator).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (System.Reflection.FieldInfo field in fields)
                {
                    //_log.Debug(field.Name);
                    field.SetValue(_songBrowserFlowCoordinator, field.GetValue(_levelSelectionFlowCoordinator));
                }
            }

            _songBrowserFlowCoordinator.Init();
            if (SongLoaderPlugin.SongLoader.AreSongsLoaded)
            {
                _songBrowserFlowCoordinator.UpdateSongList();
            }

            _log.Debug("Overriding StandardLevelSelectionFlowCoordinator");
            _mainFlowCoordinator.SetPrivateField("_levelSelectionFlowCoordinator", _songBrowserFlowCoordinator);
            _log.Debug("Success Overriding StandardLevelSelectionFlowCoordinator");
        }

        /// <summary>
        /// Only gets called once during boot of BeatSaber.  
        /// </summary>
        /// <param name="loader"></param>
        /// <param name="levels"></param>
        private void OnSongLoaderLoadedSongs(SongLoader loader, List<CustomLevel> levels)
        {
            _log.Debug("OnSongLoaderLoadedSongs");
            try
            {
                _songBrowserFlowCoordinator.UpdateSongList();

                _log.Debug("Attempting to take over the didSelectModeEvent Button");
                SoloModeSelectionViewController view = Resources.FindObjectsOfTypeAll<SoloModeSelectionViewController>().First();

                if (view.didFinishEvent != null)
                {
                    Delegate[] delegates = view.didFinishEvent.GetInvocationList();
                    view.didFinishEvent -= delegates[0] as Action<SoloModeSelectionViewController, SoloModeSelectionViewController.SubMenuType>;
                }

                view.didFinishEvent += HandleSoloModeSelectionViewControllerDidSelectMode;
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
            _log.Debug("SceneManagerOnActiveSceneChanged");
        }

        /// <summary>
        /// Get a handle to the view controllers we are going to add elements to.
        /// </summary>
        public void AcquireUIElements()
        {
            _log.Debug("Acquiring BeatSaber UI elements.");
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

                _mainFlowCoordinator = Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().First();
                _levelSelectionFlowCoordinator = Resources.FindObjectsOfTypeAll<StandardLevelSelectionFlowCoordinator>().First();
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
        private void HandleSoloModeSelectionViewControllerDidSelectMode(SoloModeSelectionViewController viewController, SoloModeSelectionViewController.SubMenuType subMenuType)
        {
            _log.Debug("HandleSoloModeSelectionViewControllerDidSelectMode()");
            try
            {
                LevelCollectionsForGameplayModes collection = _mainFlowCoordinator.GetPrivateField<LevelCollectionsForGameplayModes>("_levelCollectionsForGameplayModes");

                switch (subMenuType)
                {
                    case SoloModeSelectionViewController.SubMenuType.FreePlayMode:
                        {
                            GameplayMode gameplayMode = GameplayMode.SoloStandard;
                            this._songBrowserFlowCoordinator.Present(viewController, collection.GetLevels(gameplayMode), gameplayMode);
                            break;
                        }
                    case SoloModeSelectionViewController.SubMenuType.NoArrowsMode:
                        {
                            GameplayMode gameplayMode2 = GameplayMode.SoloNoArrows;
                            this._songBrowserFlowCoordinator.Present(viewController, collection.GetLevels(gameplayMode2), gameplayMode2);
                            break;
                        }
                    case SoloModeSelectionViewController.SubMenuType.OneSaberMode:
                        {
                            GameplayMode gameplayMode3 = GameplayMode.SoloOneSaber;
                            this._songBrowserFlowCoordinator.Present(viewController, collection.GetLevels(gameplayMode3), gameplayMode3);
                            break;
                        }
                    case SoloModeSelectionViewController.SubMenuType.Back:
                        viewController.DismissModalViewController(null, false);
                        break;
                }                

                _log.Debug("Success HandleSoloModeSelectionViewControllerDidSelectMode ...");
            }
            catch (Exception e)
            {
                _log.Exception("Exception replacing in-game song browser: {0}\n{1}", e.Message, e.StackTrace);
            }
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
        private void LateUpdate()
        {
            // z,x,c,v can be used to get into a song, b will hit continue button after song ends
            if (Input.GetKeyDown(KeyCode.Z))
            {
                InvokeBeatSaberButton("SoloButton");
            }

            if (Input.GetKeyDown(KeyCode.X))
            {
                InvokeBeatSaberButton("StandardButton");
            }

            if (Input.GetKeyDown(KeyCode.B))
            {
                InvokeBeatSaberButton("ContinueButton");
            }

            _songBrowserFlowCoordinator.Update();
        }
    }
}
