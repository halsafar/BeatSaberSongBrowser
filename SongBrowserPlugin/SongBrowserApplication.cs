﻿using UnityEngine;
using System.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SimpleJSON;
using SongLoaderPlugin.Internals;
using SongLoaderPlugin.OverrideClasses;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using SongLoaderPlugin;
using UnityEngine.UI;
using System.Reflection;

namespace SongBrowserPlugin
{
    public class SongBrowserApplication : MonoBehaviour
    {
        public const int MenuIndex = 1;

        public static SongBrowserApplication Instance;

        private Logger _log = new Logger("SongBrowserApplication");

        // BeatSaber UI Elements
        private StandardLevelSelectionFlowCoordinator _songSelectionMasterView;
        private StandardLevelDetailViewController _songDetailViewController;
        private StandardLevelListViewController _songListViewController;
        private MainMenuViewController _mainMenuViewController;
        private MainFlowCoordinator _mainFlowCoordinator;

        // Song Browser UI Elements
        private SongBrowserMasterViewController _customLevelBrowserMasterViewController;

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
            _log.Debug("AWAKE");
            Instance = this;

            SceneManager.activeSceneChanged += SceneManagerOnActiveSceneChanged;
            SongLoader.SongsLoadedEvent += OnSongLoaderLoadedSongs;
        }

        public void Start()
        {
            _log.Debug("START");
            //_log.Debug("SceneManagerOnActiveSceneChanged - {0}", scene.buildIndex);
            /*if (scene.buildIndex != SongBrowserApplication.MenuIndex)
            {
                return;
            }*/

            AcquireUIElements();

            // Clone and override the default song-browser.
            if (_customLevelBrowserMasterViewController == null)
            {
                _log.Debug("Attempting to clone StandardLevelSelectionFlowCoordinator");
                _customLevelBrowserMasterViewController = UIBuilder.CreateFlowController<SongBrowserMasterViewController>(SongBrowserMasterViewController.Name);
                System.Reflection.FieldInfo[] fields = typeof(StandardLevelSelectionFlowCoordinator).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (System.Reflection.FieldInfo field in fields)
                {
                    //_log.Debug(field.Name);
                    field.SetValue(_customLevelBrowserMasterViewController, field.GetValue(_songSelectionMasterView));
                }
            }

            _log.Debug("Overriding Song Browser");
            ReflectionUtil.SetPrivateField(_mainFlowCoordinator, "_levelSelectionFlowCoordinator", _customLevelBrowserMasterViewController);
            _log.Debug("Success Overriding Song Browser");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="loader"></param>
        /// <param name="levels"></param>
        private void OnSongLoaderLoadedSongs(SongLoader loader, List<CustomLevel> levels)
        {
            _log.Debug("OnSongLoaderLoadedSongs");
            try
            {
                _customLevelBrowserMasterViewController.InitModel();
                _customLevelBrowserMasterViewController.InitUI();

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
                _mainFlowCoordinator = Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().First();
                _songSelectionMasterView = Resources.FindObjectsOfTypeAll<StandardLevelSelectionFlowCoordinator>().First();
                _songDetailViewController = Resources.FindObjectsOfTypeAll<StandardLevelDetailViewController>().First();
                _songListViewController = Resources.FindObjectsOfTypeAll<StandardLevelListViewController>().First();
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
            _log.Debug("Hi jacking solo mode buttons");
            try
            {
                LevelCollectionsForGameplayModes collection = _mainFlowCoordinator.GetPrivateField<LevelCollectionsForGameplayModes>("_levelCollectionsForGameplayModes");
                //_mainFlowCoordinator.SetPrivateField("_levelCollectionsForGameplayModes", _customLevelCollectionsForGameplayModes);

                switch (subMenuType)
                {
                    case SoloModeSelectionViewController.SubMenuType.FreePlayMode:
                        {
                            GameplayMode gameplayMode = GameplayMode.SoloStandard;
                            this._customLevelBrowserMasterViewController.Present(viewController, collection.GetLevels(gameplayMode), gameplayMode);
                            break;
                        }
                    case SoloModeSelectionViewController.SubMenuType.NoArrowsMode:
                        {
                            GameplayMode gameplayMode2 = GameplayMode.SoloNoArrows;
                            this._customLevelBrowserMasterViewController.Present(viewController, collection.GetLevels(gameplayMode2), gameplayMode2);
                            break;
                        }
                    case SoloModeSelectionViewController.SubMenuType.OneSaberMode:
                        {
                            GameplayMode gameplayMode3 = GameplayMode.SoloOneSaber;
                            this._customLevelBrowserMasterViewController.Present(viewController, collection.GetLevels(gameplayMode3), gameplayMode3);
                            break;
                        }
                    case SoloModeSelectionViewController.SubMenuType.Back:
                        viewController.DismissModalViewController(null, false);
                        break;
                }
                /*ReflectionUtil.SetPrivateField(_menuMasterViewController, "_gameplayMode", gameplayMode);
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
                _songSelectionMasterView = _songBrowserMasterViewController;*/

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
                InvokeBeatSaberButton("StandardButton");
            }

            if (Input.GetKeyDown(KeyCode.B))
            {
                InvokeBeatSaberButton("ContinueButton");
            }

            _customLevelBrowserMasterViewController.Update();
        }
    }
}
