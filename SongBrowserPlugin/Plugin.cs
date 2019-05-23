using UnityEngine.SceneManagement;
using IllusionPlugin;
using UnityEngine;
using SongBrowser.UI;
using Logger = SongBrowser.Logging.Logger;
using SongBrowser.DataAccess;
using System.Collections.Generic;
using SongBrowser.Internals;
using System;
using SongLoaderPlugin;
using SongLoaderPlugin.OverrideClasses;

namespace SongBrowser
{
    public class Plugin : IPlugin
    {
        public const string VERSION_NUMBER = "4.0.0";

        public string Name
        {
            get { return "Song Browser"; }
        }

        public string Version
        {
            get { return VERSION_NUMBER; }
        }

        public void OnApplicationStart()
        {
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            SceneManager.activeSceneChanged += SceneManager_activeSceneChanged;

            PluginConfig.LoadOrCreateConfig();

            Base64Sprites.Init();

            PlaylistsCollection.ReloadPlaylists();
            SongLoader.SongsLoadedEvent += SongLoader_SongsLoadedEvent;

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

        private void SongLoader_SongsLoadedEvent(SongLoader sender, List<CustomLevel> levels)
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

        private void SceneManager_activeSceneChanged(Scene from, Scene to)
        {
            Logger.Info($"Active scene changed from \"{from.name}\" to \"{to.name}\"");
        }

        private void SceneManager_sceneLoaded(Scene to, LoadSceneMode loadMode)
        {
            Logger.Debug($"Loaded scene \"{to.name}\"");
        }

        public void OnLevelWasLoaded(int level)
        {

        }

        public void OnLevelWasInitialized(int level)
        {

        }

        public void OnUpdate()
        {

        }

        public void OnFixedUpdate()
        {

        }
    }
}
