using SongLoaderPlugin;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SongBrowserPlugin.Internals
{
    class BSEvents : MonoBehaviour
    {
        static BSEvents Instance;

        //Scene Events
        public static event Action menuSceneLoaded;
        public static event Action menuSceneLoadedFresh;
        public static event Action gameSceneActive;
        public static event Action gameSceneLoaded;

        public static event Action songPaused;
        public static event Action songUnpaused;
        public static event Action songRestarted; // TODO

        public static event Action songSelected; // TODO
        public static event Action difficultySelected; // TODO
        public static event Action gameModeSelected; // TODO

        const string Menu = "MenuCore";
        const string Game = "GameCore";
        const string EmptyTransition = "EmptyTransition";

        public static void OnLoad()
        {
            if (Instance != null) return;
            GameObject go = new GameObject("BSSceneManager");
            go.AddComponent<BSEvents>();
        }

        private void Awake()
        {
            if (Instance != null) return;
            Instance = this;

            SceneManager.activeSceneChanged += SceneManagerOnActiveSceneChanged;

            DontDestroyOnLoad(gameObject);
        }

        private void SceneManagerOnActiveSceneChanged(Scene arg0, Scene arg1)
        {
            try
            {
                if (arg1.name == Game)
                {
                    InvokeSafe(gameSceneActive);

                    var sceneManager = Resources.FindObjectsOfTypeAll<GameScenesManager>().FirstOrDefault();

                    if (sceneManager != null)
                    {
                        sceneManager.transitionDidFinishEvent -= GameSceneSceneWasLoaded; // make sure we don't ever subscribe twice
                        sceneManager.transitionDidFinishEvent += GameSceneSceneWasLoaded;
                    }
                }
                else if (arg1.name == Menu)
                {
                    if (arg0.name == EmptyTransition)
                    {
                        StartCoroutine(WaitForMenuToLoad());
                    }
                    else
                    {
                        InvokeSafe(menuSceneLoaded);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[BSEvents] " + e);
            }
        }

        private void GameSceneSceneWasLoaded()
        {
            // Prevent firing this event when returning to menu
            Resources.FindObjectsOfTypeAll<GameScenesManager>().First().transitionDidFinishEvent -= GameSceneSceneWasLoaded;

            var pauseManager = Resources.FindObjectsOfTypeAll<GamePauseManager>().First();
            pauseManager.GetPrivateField<Signal>("_gameDidResumeSignal").Subscribe(delegate () { InvokeSafe(songUnpaused); });
            pauseManager.GetPrivateField<Signal>("_gameDidPauseSignal").Subscribe(delegate () { InvokeSafe(songPaused); });

            InvokeSafe(gameSceneLoaded);
        }

        private IEnumerator WaitForMenuToLoad()
        {
            List<Scene> menuScenes = new List<Scene>() { SceneManager.GetSceneByName("MenuCore"), SceneManager.GetSceneByName("MenuViewControllers"), SceneManager.GetSceneByName("MainMenu") };

            yield return new WaitUntil(() => { return menuScenes.All(x => x.isLoaded); });

            Console.WriteLine($"[BSEvents] Loaded {menuScenes.Count(x => x.isLoaded)} scenes out of {menuScenes.Count}: {string.Join(", ", menuScenes.Where(x => x.isLoaded).Select(x => x.name))}");

            InvokeSafe(menuSceneLoadedFresh);
        }

        public void InvokeSafe(Action action, params object[] args)
        {
            if (action == null) return;
            foreach (Delegate invoc in action.GetInvocationList())
            {
                try
                {
                    invoc?.DynamicInvoke(args);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Caught Exception when executing event");
                    Console.WriteLine(e);
                }
            }
        }
    }
}
