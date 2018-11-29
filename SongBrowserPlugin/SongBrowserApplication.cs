using SongBrowserPlugin.DataAccess;
using SongBrowserPlugin.UI;
using SongLoaderPlugin;
using SongLoaderPlugin.OverrideClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace SongBrowserPlugin
{
    public class SongBrowserApplication : MonoBehaviour
    {
        public static SongBrowserApplication Instance;

        private Logger _log = new Logger("SongBrowserApplication");

        // Song Browser UI Elements
        private SongBrowserUI _songBrowserUI;
        private ScoreSaberDatabaseDownloader _ppDownloader;

        public Dictionary<String, Sprite> CachedIcons;

        public static SongBrowserPlugin.UI.ProgressBar MainProgressBar;


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
            SongBrowserApplication.MainProgressBar = SongBrowserPlugin.UI.ProgressBar.Create();

            Console.WriteLine("SongBrowser Plugin Loaded()");
        }

        /// <summary>
        /// It has awaken!
        /// </summary>
        private void Awake()
        {
            _log.Trace("Awake()");

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
            _log.Trace("Start()");

            AcquireUIElements();

            StartCoroutine(WaitForSongListUI());
        }

        /// <summary>
        /// Wait for the song list to be visible to draw it.
        /// </summary>
        /// <returns></returns>
        private IEnumerator WaitForSongListUI()
        {
            _log.Trace("WaitForSongListUI()");

            yield return new WaitUntil(delegate () { return Resources.FindObjectsOfTypeAll<SoloFreePlayFlowCoordinator>().Any(); });

            _log.Debug("Found StandardLevelSelectionFlowCoordinator...");

            _songBrowserUI.CreateUI();

            if (SongLoaderPlugin.SongLoader.AreSongsLoaded)
            {
                OnSongLoaderLoadedSongs(null, SongLoader.CustomLevels);
            }
            else
            {
                SongLoader.SongsLoadedEvent += OnSongLoaderLoadedSongs;
            }

            _songBrowserUI.RefreshSongList();
        }

        /// <summary>
        /// Only gets called once during boot of BeatSaber.  
        /// </summary>
        /// <param name="loader"></param>
        /// <param name="levels"></param>
        private void OnSongLoaderLoadedSongs(SongLoader loader, List<CustomLevel> levels)
        {
            _log.Trace("OnSongLoaderLoadedSongs");
            try
            {
                _songBrowserUI.UpdateSongList();
            }
            catch (Exception e)
            {
                _log.Exception("Exception during OnSongLoaderLoadedSongs: ", e);
            }
        }

        /// <summary>
        /// Inform browser score saber data is available.
        /// </summary>
        /// <param name="loader"></param>
        /// <param name="levels"></param>
        private void OnScoreSaberDataDownloaded()
        {
            _log.Trace("OnScoreSaberDataDownloaded");
            try
            {
                // TODO - this should be in the SongBrowserUI which is acting like the view controller for the SongBrowser
                _songBrowserUI.Model.UpdateScoreSaberDataMapping();
                _songBrowserUI.RefreshScoreSaberData(null);
                if (_songBrowserUI.Model.Settings.sortMode == SongSortMode.PP)
                {
                    _songBrowserUI.Model.ProcessSongList();
                    _songBrowserUI.RefreshSongList();
                }
            }
            catch (Exception e)
            {
                _log.Exception("Exception during OnSongLoaderLoadedSongs: ", e);
            }
        }

        /// <summary>
        /// Get a handle to the view controllers we are going to add elements to.
        /// </summary>
        public void AcquireUIElements()
        {
            _log.Trace("AcquireUIElements()");        
            try
            {
                CachedIcons = new Dictionary<String, Sprite>();
                foreach (Sprite sprite in Resources.FindObjectsOfTypeAll<Sprite>())
                {
                    if (CachedIcons.ContainsKey(sprite.name))
                    {
                        continue;
                    }

                    //_log.Debug("Adding Icon: {0}", sprite.name);
                    CachedIcons.Add(sprite.name, sprite);
                }
                // Append our own event to appropriate events so we can refresh the song list before the user sees it.
                MainFlowCoordinator mainFlow = Resources.FindObjectsOfTypeAll<MainFlowCoordinator>().First();
                Button soloFreePlayButton = Resources.FindObjectsOfTypeAll<Button>().First(x => x.name == "SoloFreePlayButton");
                Button partyFreePlayButton = Resources.FindObjectsOfTypeAll<Button>().First(x => x.name == "PartyFreePlayButton");

                soloFreePlayButton.onClick.AddListener(HandleModeSelection);
                partyFreePlayButton.onClick.AddListener(HandleModeSelection);
            }
            catch (Exception e)
            {
                _log.Exception("Exception AcquireUIElements(): ", e);
            }
        }

        /// <summary>
        /// Perfect time to refresh the level list on first entry.
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        private void HandleModeSelection()
        {
            _log.Trace("HandleModeSelection()");

            this._songBrowserUI.UpdateSongList();
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

            if (Input.GetKeyDown(KeyCode.B))
            {
                //InvokeBeatSaberButton("ContinueButton");
                _log.Debug("Invoking OK Button");
                InvokeBeatSaberButton("Ok");
            }
        }
    }
}
