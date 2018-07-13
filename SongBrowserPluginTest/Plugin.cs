using IllusionPlugin;
using SongBrowserPluginTests;
using System;
using UnityEngine.SceneManagement;

namespace SongBrowserPluginTest
{
    public class Plugin : IPlugin
    {
        public const bool RunTests = true;

        private bool _didRunTests = false;

        public string Name
        {
            get { return "Song Browser Plugin Test"; }
        }

        public string Version
        {
            get { return "v1.0-test"; }
        }

        public void OnApplicationStart()
        {

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
            if (RunTests && !_didRunTests)
            {
                new SongBrowserTestRunner().RunTests();
                _didRunTests = true;
            }
        }

        public void OnLevelWasInitialized(int level)
        {
            //Console.WriteLine("OnLevelWasInitialized=" + level);
        }

        public void OnUpdate()
        {

        }

        public void OnFixedUpdate()
        {

        }
    }
}
