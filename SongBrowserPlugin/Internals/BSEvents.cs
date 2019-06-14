using SongCore.Utilities;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SongBrowser.Internals
{
    class BSEvents : MonoBehaviour
    {
        static BSEvents Instance;

        //Scene Events
        public static event Action menuSceneActive;
        public static event Action menuSceneLoaded;
        public static event Action menuSceneLoadedFresh;
        public static event Action gameSceneActive;
        public static event Action gameSceneLoaded;

        // Menu Events
        public static event Action<StandardLevelDetailViewController, IDifficultyBeatmap> difficultySelected;
        public static event Action<BeatmapCharacteristicSegmentedControlController, BeatmapCharacteristicSO> characteristicSelected;
        public static event Action<LevelPacksViewController, IBeatmapLevelPack> levelPackSelected;
        public static event Action<LevelPackLevelsViewController, IBeatmapLevel> levelSelected;

        // Game Events
        public static event Action songPaused;
        public static event Action songUnpaused;
        public static event Action<StandardLevelScenesTransitionSetupDataSO, LevelCompletionResults> levelCleared;
        public static event Action<StandardLevelScenesTransitionSetupDataSO, LevelCompletionResults> levelQuit;
        public static event Action<StandardLevelScenesTransitionSetupDataSO, LevelCompletionResults> levelFailed;
        public static event Action<StandardLevelScenesTransitionSetupDataSO, LevelCompletionResults> levelRestarted;

        public static event Action<NoteData, NoteCutInfo, int> noteWasCut;
        public static event Action<NoteData, int> noteWasMissed;
        public static event Action<int, float> multiplierDidChange;
        public static event Action<int> multiplierDidIncrease;
        public static event Action<int> comboDidChange;
        public static event Action comboDidBreak;
        public static event Action<int, int> scoreDidChange;
        public static event Action<float> energyDidChange;
        public static event Action energyReachedZero;

        public static event Action<BeatmapEventData> beatmapEvent;

        public static event Action<Saber.SaberType> sabersStartCollide;
        public static event Action<Saber.SaberType> sabersEndCollide;

        const string Menu = "MenuCore";
        const string Game = "GameCore";
        const string EmptyTransition = "EmptyTransition";

        GameScenesManager gameScenesManager;

        public static void OnLoad()
        {
            if (Instance != null) return;
            GameObject go = new GameObject("BSEvents");
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
                    InvokeAll(gameSceneActive);

                    gameScenesManager = Resources.FindObjectsOfTypeAll<GameScenesManager>().FirstOrDefault();

                    if (gameScenesManager != null)
                    {
                        gameScenesManager.transitionDidFinishEvent -= GameSceneSceneWasLoaded;
                        gameScenesManager.transitionDidFinishEvent += GameSceneSceneWasLoaded;
                    }
                }
                else if (arg1.name == Menu)
                {
                    gameScenesManager = Resources.FindObjectsOfTypeAll<GameScenesManager>().FirstOrDefault();

                    InvokeAll(menuSceneActive);

                    if (gameScenesManager != null)
                    {

                        if (arg0.name == EmptyTransition)
                        {
                            gameScenesManager.transitionDidFinishEvent -= OnMenuSceneWasLoadedFresh;
                            gameScenesManager.transitionDidFinishEvent += OnMenuSceneWasLoadedFresh;
                        }
                        else
                        {
                            gameScenesManager.transitionDidFinishEvent -= OnMenuSceneWasLoaded;
                            gameScenesManager.transitionDidFinishEvent += OnMenuSceneWasLoaded;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("[BSEvents] " + e);
            }
        }

        private void OnMenuSceneWasLoaded()
        {
            gameScenesManager.transitionDidFinishEvent -= OnMenuSceneWasLoaded;
            InvokeAll(menuSceneLoaded);
        }

        private void OnMenuSceneWasLoadedFresh()
        {
            gameScenesManager.transitionDidFinishEvent -= OnMenuSceneWasLoadedFresh;

            var levelDetailViewController = Resources.FindObjectsOfTypeAll<StandardLevelDetailViewController>().FirstOrDefault();
            levelDetailViewController.didChangeDifficultyBeatmapEvent += delegate (StandardLevelDetailViewController vc, IDifficultyBeatmap beatmap) { InvokeAll(difficultySelected, vc, beatmap); };

            var characteristicSelect = Resources.FindObjectsOfTypeAll<BeatmapCharacteristicSegmentedControlController>().FirstOrDefault();
            characteristicSelect.didSelectBeatmapCharacteristicEvent += delegate (BeatmapCharacteristicSegmentedControlController controller, BeatmapCharacteristicSO characteristic) { InvokeAll(characteristicSelected, controller, characteristic); };

            var packSelectViewController = Resources.FindObjectsOfTypeAll<LevelPacksViewController>().FirstOrDefault();
            packSelectViewController.didSelectPackEvent += delegate (LevelPacksViewController controller, IBeatmapLevelPack pack) { InvokeAll(levelPackSelected, controller, pack); };
            var levelSelectViewController = Resources.FindObjectsOfTypeAll<LevelPackLevelsViewController>().FirstOrDefault();
            levelSelectViewController.didSelectLevelEvent += delegate (LevelPackLevelsViewController controller, IPreviewBeatmapLevel level) { InvokeAll(levelSelected, controller, level); };

            InvokeAll(menuSceneLoadedFresh);
        }

        private void GameSceneSceneWasLoaded()
        {
            // Prevent firing this event when returning to menu
            Resources.FindObjectsOfTypeAll<GameScenesManager>().FirstOrDefault().transitionDidFinishEvent -= GameSceneSceneWasLoaded;

            var pauseManager = Resources.FindObjectsOfTypeAll<GamePauseManager>().FirstOrDefault();
            pauseManager.GetPrivateField<Signal>("_gameDidResumeSignal").Subscribe(delegate () { InvokeAll(songUnpaused); });
            pauseManager.GetPrivateField<Signal>("_gameDidPauseSignal").Subscribe(delegate () { InvokeAll(songPaused); });

            var scoreController = Resources.FindObjectsOfTypeAll<ScoreController>().FirstOrDefault();
            scoreController.noteWasCutEvent += delegate (NoteData noteData, NoteCutInfo noteCutInfo, int multiplier) { InvokeAll(noteWasCut, noteData, noteCutInfo, multiplier); };
            scoreController.noteWasMissedEvent += delegate (NoteData noteData, int multiplier) { InvokeAll(noteWasMissed, noteData, multiplier); }; ;
            scoreController.multiplierDidChangeEvent += delegate (int multiplier, float progress) { InvokeAll(multiplierDidChange, multiplier, progress); if (multiplier > 1 && progress < 0.1f) InvokeAll(multiplierDidIncrease, multiplier); };
            scoreController.comboDidChangeEvent += delegate (int combo) { InvokeAll(comboDidChange, combo); };
            scoreController.comboBreakingEventHappenedEvent += delegate () { InvokeAll(comboDidBreak); };
            scoreController.scoreDidChangeEvent += delegate (int rawScore, int multipliedScore) { InvokeAll(scoreDidChange); };

            var saberCollisionManager = Resources.FindObjectsOfTypeAll<ObstacleSaberSparkleEffectManager>().FirstOrDefault();
            saberCollisionManager.sparkleEffectDidStartEvent += delegate (Saber.SaberType saber) { InvokeAll(sabersStartCollide, saber); };
            saberCollisionManager.sparkleEffectDidEndEvent += delegate (Saber.SaberType saber) { InvokeAll(sabersEndCollide, saber); };

            var gameEnergyCounter = Resources.FindObjectsOfTypeAll<GameEnergyCounter>().FirstOrDefault();
            gameEnergyCounter.gameEnergyDidReach0Event += delegate () { InvokeAll(energyReachedZero); };
            gameEnergyCounter.gameEnergyDidChangeEvent += delegate (float energy) { InvokeAll(energyDidChange, energy); };

            var beatmapObjectCallbackController = Resources.FindObjectsOfTypeAll<BeatmapObjectCallbackController>().FirstOrDefault();
            beatmapObjectCallbackController.beatmapEventDidTriggerEvent += delegate (BeatmapEventData songEvent) { InvokeAll(beatmapEvent, songEvent); };

            var transitionSetup = Resources.FindObjectsOfTypeAll<StandardLevelScenesTransitionSetupDataSO>().FirstOrDefault();
            transitionSetup.didFinishEvent += delegate (StandardLevelScenesTransitionSetupDataSO data, LevelCompletionResults results)
            {
                switch (results.levelEndStateType)
                {
                    case LevelCompletionResults.LevelEndStateType.Cleared:
                        InvokeAll(levelCleared, data, results);
                        break;
                    case LevelCompletionResults.LevelEndStateType.Failed:
                        InvokeAll(levelFailed, data, results);
                        break;
                };
                switch (results.levelEndAction)
                {
                    case LevelCompletionResults.LevelEndAction.Quit:
                        InvokeAll(levelQuit, data, results);
                        break;
                    case LevelCompletionResults.LevelEndAction.Restart:
                        InvokeAll(levelRestarted, data, results);
                        break;
                }
            };

            InvokeAll(gameSceneLoaded);
        }

        public void InvokeAll<T1, T2, T3>(Action<T1, T2, T3> action, params object[] args)
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
        public void InvokeAll<T1, T2>(Action<T1, T2> action, params object[] args)
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

        public void InvokeAll<T>(Action<T> action, params object[] args)
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
        public void InvokeAll(Action action, params object[] args)
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
