using UnityEngine.SceneManagement;
using SongBrowser.UI;
using Logger = SongBrowser.Logging.Logger;
using System;
using IPA;
using BS_Utils.Utilities;

namespace SongBrowser
{
    public class Plugin : IBeatSaberPlugin
    {
        public const string VERSION_NUMBER = "6.0.1";
        public static Plugin Instance;
        public static IPA.Logging.Logger Log;

        public void Init(object nullObject, IPA.Logging.Logger logger)
        {
            Log = logger;
        }

        public void OnApplicationStart()
        {
            Instance = this;

            Base64Sprites.Init();

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
