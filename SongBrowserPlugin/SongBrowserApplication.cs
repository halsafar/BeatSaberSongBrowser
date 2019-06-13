using SongBrowser.DataAccess;
using SongBrowser.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Logger = SongBrowser.Logging.Logger;

namespace SongBrowser
{
    public class SongBrowserApplication : MonoBehaviour
    {
        public static SongBrowserApplication Instance;

        // Song Browser UI Elements
        private SongBrowserUI _songBrowserUI;
        private ScoreSaberDatabaseDownloader _ppDownloader;

        public Dictionary<String, Sprite> CachedIcons;

        public static SongBrowser.UI.ProgressBar MainProgressBar;


        /// <summary>
        /// Load the main song browser app.
        /// </summary>
        internal static void OnLoad()
        {            
            if (Instance != null)
            {
                return;
            }

            new GameObject("Beat Saber SongBrowser Plugin").AddComponent<SongBrowserApplication>();

            SongBrowserApplication.MainProgressBar = SongBrowser.UI.ProgressBar.Create();

            Console.WriteLine("SongBrowser Plugin Loaded()");
        }

        /// <summary>
        /// It has awaken!
        /// </summary>
        private void Awake()
        {
            Logger.Trace("Awake-SongBrowserApplication()");

            Instance = this;

            _songBrowserUI = gameObject.AddComponent<SongBrowserUI>();
            _ppDownloader = gameObject.AddComponent<ScoreSaberDatabaseDownloader>();
            _ppDownloader.onScoreSaberDataDownloaded += OnScoreSaberDataDownloaded;
        }

        /// <summary>
        /// Acquire any UI elements from Beat saber that we need.  Wait for the song list to be loaded.
        /// </summary>
        public void Start()
        {
            Logger.Trace("Start-SongBrowserApplication()");

            AcquireUIElements();
            InstallHandlers();

            StartCoroutine(ScrappedData.Instance.DownloadScrappedData((List<ScrappedSong> songs) => { }));

            if (SongCore.Loader.AreSongsLoaded)
            {
                OnSongLoaderLoadedSongs(null, SongCore.Loader.CustomLevels);
            }
            else
            {
                SongCore.Loader.SongsLoadedEvent += OnSongLoaderLoadedSongs;
            }
        }

        /// <summary>
        /// Only gets called once during boot of BeatSaber.  
        /// </summary>
        /// <param name="loader"></param>
        /// <param name="levels"></param>
        private void OnSongLoaderLoadedSongs(SongCore.Loader loader, Dictionary<string, CustomPreviewBeatmapLevel> levels)
        {
            Logger.Trace("OnSongLoaderLoadedSongs-SongBrowserApplication()");
            try
            {
                _songBrowserUI.UpdateLevelDataModel();
                _songBrowserUI.RefreshSongList();
            }
            catch (Exception e)
            {
                Logger.Exception("Exception during OnSongLoaderLoadedSongs: ", e);
            }
        }

        /// <summary>
        /// Inform browser score saber data is available.
        /// </summary>
        /// <param name="loader"></param>
        /// <param name="levels"></param>
        private void OnScoreSaberDataDownloaded()
        {
            Logger.Trace("OnScoreSaberDataDownloaded");
            try
            {
                _songBrowserUI.Model.UpdateScoreSaberDataMapping();
                if (_songBrowserUI.Model.Settings.sortMode == SongSortMode.PP)
                {
                    _songBrowserUI.ProcessSongList();
                    _songBrowserUI.RefreshSongList();
                }
            }
            catch (Exception e)
            {
                Logger.Exception("Exception during OnScoreSaberDataDownloaded: ", e);
            }
        }

        /// <summary>
        /// Get a handle to the view controllers we are going to add elements to.
        /// </summary>
        private void AcquireUIElements()
        {
            Logger.Trace("AcquireUIElements()");        
            try
            {
                CachedIcons = new Dictionary<String, Sprite>();
                foreach (Sprite sprite in Resources.FindObjectsOfTypeAll<Sprite>())
                {
                    if (CachedIcons.ContainsKey(sprite.name))
                    {
                        continue;
                    }

                    //Logger.Debug("Adding Icon: {0}", sprite.name);
                    CachedIcons.Add(sprite.name, sprite);
                }

                /*foreach (RectTransform rect in Resources.FindObjectsOfTypeAll<RectTransform>())
                {
                    Logger.Debug("RectTransform: {0}", rect.name);
                }*/
            }
            catch (Exception e)
            {
                Logger.Exception("Exception AcquireUIElements(): ", e);
            }
        }

        /// <summary>
        /// Install Our Handlers so we can react to ingame events.
        /// </summary>
        private void InstallHandlers()
        {
            // Append our own event to appropriate events so we can refresh the song list before the user sees it.
            MainFlowCoordinator mainFlow = Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().First();
            Button soloFreePlayButton = Resources.FindObjectsOfTypeAll<Button>().First(x => x.name == "SoloFreePlayButton");
            Button partyFreePlayButton = Resources.FindObjectsOfTypeAll<Button>().First(x => x.name == "PartyFreePlayButton");
            Button campaignButton = Resources.FindObjectsOfTypeAll<Button>().First(x => x.name == "CampaignButton");

            soloFreePlayButton.onClick.AddListener(HandleSoloModeSelection);
            partyFreePlayButton.onClick.AddListener(HandlePartyModeSelection);
            campaignButton.onClick.AddListener(HandleCampaignModeSelection);
        }

        /// <summary>
        /// Handle Solo Mode
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        private void HandleSoloModeSelection()
        {
            Logger.Trace("HandleSoloModeSelection()");
            HandleModeSelection(MainMenuViewController.MenuButton.SoloFreePlay);
            this._songBrowserUI.Show();
        }

        /// <summary>
        /// Handle Party Mode
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        private void HandlePartyModeSelection()
        {
            Logger.Trace("HandlePartyModeSelection()");
            HandleModeSelection(MainMenuViewController.MenuButton.Party);
            this._songBrowserUI.Show();
        }

        /// <summary>
        /// Handle Party Mode
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        private void HandleCampaignModeSelection()
        {
            Logger.Trace("HandleCampaignModeSelection()");
            HandleModeSelection(MainMenuViewController.MenuButton.SoloCampaign);
            this._songBrowserUI.Hide();
        }

        /// <summary>
        /// Handle Mode
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        private void HandleModeSelection(MainMenuViewController.MenuButton mode)
        {
            Logger.Trace("HandleModeSelection()");
            this._songBrowserUI.CreateUI(mode);
            StartCoroutine(this.UpdateBrowserUI());
        }

        /// <summary>
        /// Wait until the end of the frame to finish updating everything.
        /// </summary>
        /// <returns></returns>
        public IEnumerator UpdateBrowserUI()
        {
            yield return new WaitForEndOfFrame();

            this._songBrowserUI.UpdateLevelDataModel();
            this._songBrowserUI.RefreshSongList();
        }

        /// <summary>
        /// Helper for invoking buttons.
        /// </summary>
        /// <param name="buttonName"></param>
        public static void InvokeBeatSaberButton(String buttonName)
        {
            Button buttonInstance = Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == buttonName));
            buttonInstance.onClick.Invoke();
        }

#if DEBUG
        /// <summary>
        /// Map some key presses directly to UI interactions to make testing easier.
        /// </summary>
        private void LateUpdate()
        {            
            // z,x,c,v can be used to get into a song, b will hit continue button after song ends
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Z))
            {
                InvokeBeatSaberButton("PartyFreePlayButton");
            }
            else if (Input.GetKeyDown(KeyCode.Z))
            {
                InvokeBeatSaberButton("SoloFreePlayButton");
            }

            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                InvokeBeatSaberButton("SettingsButton");
            }

            if (Input.GetKeyDown(KeyCode.Alpha9))
            {
                InvokeBeatSaberButton("ApplyButton");
            }

            if (Input.GetKeyDown(KeyCode.Alpha8))
            {
                Console.WriteLine("CLICKING OK BUTTON");
                var settings = Resources.FindObjectsOfTypeAll<VRUI.VRUIViewController>().First(x => x.name == "SettingsViewController");
                var button = settings.GetComponentsInChildren<Button>().Where(x => x.name == "OkButton");
                foreach (Button b in button)
                {
                    b.onClick.Invoke();
                }                
            }
        }
#endif
    }
}
