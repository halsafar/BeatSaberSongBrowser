using UnityEngine.SceneManagement;
using IllusionPlugin;
using UnityEngine;

namespace SongBrowserPlugin
{
    public class Plugin : IPlugin
    {
        public const string VERSION_NUMBER = "v2.4.0-Beta1";

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
            SceneEvents _sceneEvents;
            _sceneEvents = new GameObject("menu-signal").AddComponent<SceneEvents>();
            _sceneEvents.MenuSceneEnabled += OnMenuSceneEnabled;
        }

        private void OnMenuSceneEnabled()
        {
            SongBrowserApplication.OnLoad();
            Downloader.OnLoad();
        }

        public void OnApplicationQuit()
        {

        }

        private void SceneManagerOnActiveSceneChanged(Scene arg0, Scene scene)
        {
        
        }

        private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
        }

        public void OnLevelWasLoaded(int level)
        {
            /*if (SceneManager.GetSceneByBuildIndex(level).name == "Menu")
            {
                SongBrowserApplication.OnLoad();
                Downloader.OnLoad();
            }*/
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