using BS_Utils.Utilities;
using IPA;
using IPA.Config.Stores;
using SongBrowser.UI;
using System;
using System.Linq;
using System.Reflection;
using IPA.Loader;
using Logger = SongBrowser.Logging.Logger;
using Config = IPA.Config.Config;
using HarmonyLib;
using SongBrowser.Installers;
using SiraUtil.Zenject;

namespace SongBrowser
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        public static string VersionNumber { get; private set; }
        public static bool IsCustomJsonDataEnabled { get; private set; }
        public static Plugin Instance { get; private set; }
        public static IPA.Logging.Logger Log { get; private set; }

        public const string HarmonyId = "com.halsafar.BeatSaber.SongBrowserPlugin";
        internal static Harmony harmony;

        [Init]
        public void Init(IPA.Logging.Logger logger, Zenjector zenjector, PluginMetadata metadata)
        {
            Log = logger;
            VersionNumber = metadata.Version?.ToString() ?? Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
            harmony = new Harmony(HarmonyId);
            zenjector.Install<SongBrowserMenuInstaller>(Location.Menu);
        }

        #region BSIPA Config
        [Init]
        public void InitWithConfig(Config conf)
        {
            Configuration.PluginConfig.Instance = conf.Generated<Configuration.PluginConfig>();
            Log.Debug("Config loaded");
        }
        #endregion

        [OnStart]
        public void OnApplicationStart()
        {
            Instance = this;
            IsCustomJsonDataEnabled = PluginManager.EnabledPlugins.FirstOrDefault(p => p.Name == "CustomJSONData")?.Version >= new SemVer.Version("2.0.0");

            Base64Sprites.Init();

            BSEvents.OnLoad();
            BSEvents.lateMenuSceneLoadedFresh += OnMenuSceneLoadedFresh;
        }

        [OnExit]
        public void OnApplicationQuit()
        {
        }

        private void OnMenuSceneLoadedFresh(ScenesTransitionSetupDataSO data)
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

        [OnEnable]
        public void OnEnable()
        {
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        [OnDisable]
        public void OnDisable()
        {
            Harmony.UnpatchID(HarmonyId);
        }
    }
}
