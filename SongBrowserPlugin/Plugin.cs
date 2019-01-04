using UnityEngine.SceneManagement;
using IllusionPlugin;
using UnityEngine;
using SongBrowserPlugin.UI;
using Logger = SongBrowserPlugin.Logging.Logger;

namespace SongBrowserPlugin
{
    public class Plugin : IPlugin
    {
        public const string VERSION_NUMBER = "v2.4.5";

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

            Base64Sprites.Init();
        }

        private void SceneManager_activeSceneChanged(Scene from, Scene to)
        {
            Logger.Info($"Active scene changed from \"{from.name}\" to \"{to.name}\"");

            if (from.name == "EmptyTransition" && to.name.Contains("Menu"))
            {
                OnMenuSceneEnabled();
            }
        }

        private void SceneManager_sceneLoaded(Scene to, LoadSceneMode loadMode)
        {
            Logger.Debug($"Loaded scene \"{to.name}\"");
        }

        private void OnMenuSceneEnabled()
        {
            SongBrowserApplication.OnLoad();
        }

        public void OnApplicationQuit()
        {

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
