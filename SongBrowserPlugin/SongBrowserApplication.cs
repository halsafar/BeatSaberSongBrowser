﻿using SongBrowser.UI;
using System;
using System.Collections;
using System.Collections.Concurrent;
using UnityEngine;
using Logger = SongBrowser.Logging.Logger;

namespace SongBrowser
{
    public class SongBrowserApplication : MonoBehaviour
    {
        public static SongBrowserApplication Instance;

        // Song Browser UI Elements
        private SongBrowserUI _songBrowserUI;
        private SongBrowserModel _songBrowserModel;

        public static ProgressBar MainProgressBar;

        private bool _hasShownProgressBar;


        public SongBrowserModel Model => _songBrowserModel;

        public SongBrowserUI Ui => _songBrowserUI;

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

            MainProgressBar = ProgressBar.Create();

            Plugin.Log.Info("SongBrowser Plugin OnLoad Complete");
        }

        /// <summary>
        /// It has awaken!
        /// </summary>
        protected void Awake()
        {
            Logger.Trace("Awake-SongBrowserApplication()");

            Instance = this;

            // Init Model, load settings
            _songBrowserModel = new SongBrowserModel();
            _songBrowserModel.Init();

            // Init browser UI
            _songBrowserUI = gameObject.AddComponent<SongBrowserUI>();
            _songBrowserUI.Model = _songBrowserModel;
        }

        /// <summary>
        /// Acquire any UI elements from Beat saber that we need.  Wait for the song list to be loaded.
        /// </summary>
        public void Start()
        {
            Logger.Trace("Start-SongBrowserApplication()");

            SongDataCore.Plugin.Songs.OnDataFinishedProcessing += OnScoreSaberDataDownloaded;

            if (SongCore.Loader.AreSongsLoaded)
            {
                OnSongLoaderLoadedSongs(null, SongCore.Loader.CustomLevels);
            }
            else
            {
                SongCore.Loader.SongsLoadedEvent += OnSongLoaderLoadedSongs;
            }

            // Useful to dump game objects.
            /*foreach (RectTransform rect in Resources.FindObjectsOfTypeAll<RectTransform>())
            {
                Logger.Debug("RectTransform: {0}", rect.name);
            }*/

            /*foreach (Sprite sprite in Resources.FindObjectsOfTypeAll<Sprite>())
            {
                Logger.Debug("Adding Icon: {0}", sprite.name);
            }*/
        }

        /// <summary>
        /// Only gets called once during boot of BeatSaber.  
        /// </summary>
        /// <param name="loader"></param>
        /// <param name="levels"></param>
        private void OnSongLoaderLoadedSongs(SongCore.Loader loader, ConcurrentDictionary<string, CustomPreviewBeatmapLevel> levels)
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
                // It is okay if SongDataCore beats us to initialization
                if (_songBrowserUI == null)
                {
                    return;
                }

                StartCoroutine(_songBrowserUI.AsyncWaitForSongUIUpdate());
            }
            catch (Exception e)
            {
                Logger.Exception("Exception during OnScoreSaberDataDownloaded: ", e);
            }
        }

        /// <summary>
        /// Handle Solo Mode
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        public void HandleSoloModeSelection()
        {
            Logger.Trace("HandleSoloModeSelection()");
            HandleModeSelection(MainMenuViewController.MenuButton.SoloFreePlay);
            _songBrowserUI.Show();
        }

        /// <summary>
        /// Handle Party Mode
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        public void HandlePartyModeSelection()
        {
            Logger.Trace("HandlePartyModeSelection()");
            HandleModeSelection(MainMenuViewController.MenuButton.Party);
            _songBrowserUI.Show();
        }

        /// <summary>
        /// Handle Party Mode
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        public void HandleCampaignModeSelection()
        {
            Logger.Trace("HandleCampaignModeSelection()");
            HandleModeSelection(MainMenuViewController.MenuButton.SoloCampaign);
            _songBrowserUI.Hide();
        }

        /// <summary>
        /// Handle Multiplayer Mode.
        /// Triggers when level select is clicked inside a host lobby.
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        public void HandleMultiplayerModeSelection()
        {
            Logger.Trace("HandleCampaignModeSelection()");
            HandleModeSelection(MainMenuViewController.MenuButton.Multiplayer);
            _songBrowserUI.Hide();
        }

        /// <summary>
        /// Handle Mode
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        private void HandleModeSelection(MainMenuViewController.MenuButton mode)
        {
            Logger.Trace("HandleModeSelection()");
            _songBrowserUI.CreateUI(mode);

            if (!_hasShownProgressBar)
            {
                MainProgressBar.ShowMessage("");
                _hasShownProgressBar = true;
            }

            StartCoroutine(UpdateBrowserUI());
        }

        /// <summary>
        /// Wait until the end of the frame to finish updating everything.
        /// </summary>
        /// <returns></returns>
        public IEnumerator UpdateBrowserUI()
        {
            yield return new WaitForEndOfFrame();

            _songBrowserUI.UpdateLevelDataModel();
            _songBrowserUI.UpdateLevelCollectionSelection();
            _songBrowserUI.RefreshSongList();
        }
    }
}
