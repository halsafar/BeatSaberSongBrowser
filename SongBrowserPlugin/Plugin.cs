using UnityEngine.SceneManagement;
using IllusionPlugin;
using SongBrowser.UI;
using Logger = SongBrowser.Logging.Logger;
using SongBrowser.DataAccess;
using System.Collections.Generic;
using SongBrowser.Internals;
using System;
using IPA;

namespace SongBrowser
{
    public class Plugin : IBeatSaberPlugin
    {
        public const string VERSION_NUMBER = "5.2.2";
        public static Plugin Instance;
        public static IPA.Logging.Logger Log;

        public void Init(object nullObject, IPA.Logging.Logger logger)
        {
            Log = logger;
        }

        public void OnApplicationStart()
        {
            Instance = this;

            PluginConfig.LoadOrCreateConfig();

            Base64Sprites.Init();

            PlaylistsCollection.ReloadPlaylists();
            SongCore.Loader.SongsLoadedEvent += SongCore_SongsLoadedEvent;

            BSEvents.OnLoad();
            BSEvents.menuSceneLoadedFresh += OnMenuSceneLoadedFresh;
        }

        public void OnApplicationQuit()
        {            
        }

        private void OnMenuSceneLoadedFresh()
        {
            try
            {
                SongBrowserApplication.OnLoad();
            }
            catch (Exception e)
            {
                Logger.Exception("Exception on fresh menu scene change: " + e);
            }
        }

        public void SongCore_SongsLoadedEvent(SongCore.Loader sender, Dictionary<string, CustomPreviewBeatmapLevel> levels)
        {
            try
            {
                PlaylistsCollection.MatchSongsForAllPlaylists(true);
            }
            catch (Exception e)
            {
                Logger.Exception("Unable to match songs for all playlists! Exception: " + e);
            }
        }

        public void OnUpdate()
        {

        }

        public void OnFixedUpdate()
        {

        }

        public void OnSceneLoaded(Scene scene, LoadSceneMode sceneMode)
        {
 
        }

        public void OnSceneUnloaded(Scene scene)
        {

        }

        public void OnActiveSceneChanged(Scene prevScene, Scene nextScene)
        {

        }
    }
}
