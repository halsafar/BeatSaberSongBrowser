using Mobcast.Coffee.AssetSystem;
using SongBrowserPlugin.DataAccess;
using SongBrowserPlugin.DataAccess.Network;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Logger = SongBrowserPlugin.Logging.Logger;

namespace SongBrowserPlugin.UI
{
    public class ScoreSaberDatabaseDownloader : MonoBehaviour
    {
        public const String SCRAPED_SCORE_SABER_JSON_URL = "https://wes.ams3.digitaloceanspaces.com/beatstar/bssb.json";

        public static ScoreSaberDatabaseDownloader Instance;

        public static ScoreSaberDataFile ScoreSaberDataFile = null;        

        public Action onScoreSaberDataDownloaded;

        private readonly byte[] _buffer = new byte[4 * 1048576];

        /// <summary>
        /// Awake.
        /// </summary>
        private void Awake()
        {
            Logger.Trace("Awake-ScoreSaberDatabaseDownloader()");

            if (Instance == null)
            {
                Instance = this;
            }
        }

        /// <summary>
        /// Acquire any UI elements from Beat saber that we need.  Wait for the song list to be loaded.
        /// </summary>
        public void Start()
        {
            Logger.Trace("Start()");

            StartCoroutine(WaitForDownload());
        }

        private void DownloadScoreSaberData()
        {

        }

        /// <summary>
        /// Wait for score saber related files to download.
        /// </summary>
        /// <returns></returns>
        private IEnumerator WaitForDownload()
        {
            if (ScoreSaberDatabaseDownloader.ScoreSaberDataFile != null)
            {
                Logger.Info("Using cached copy of ScoreSaberData...");
            }
            else
            {
                SongBrowserApplication.MainProgressBar.ShowMessage("Downloading BeatStar data...");

                Logger.Info("Attempting to download: {0}", ScoreSaberDatabaseDownloader.SCRAPED_SCORE_SABER_JSON_URL);
                using (UnityWebRequest www = UnityWebRequest.Get(ScoreSaberDatabaseDownloader.SCRAPED_SCORE_SABER_JSON_URL))
                {
                    // Use 4MB cache, large enough for this file to grow for awhile.
                    www.SetCacheable(new CacheableDownloadHandlerScoreSaberData(www, _buffer));
                    yield return www.SendWebRequest();

                    Logger.Debug("Returned from web request!...");

                    try
                    {
                        ScoreSaberDatabaseDownloader.ScoreSaberDataFile = (www.downloadHandler as CacheableDownloadHandlerScoreSaberData).ScoreSaberDataFile;
                        Logger.Info("Success downloading ScoreSaber data!");

                        SongBrowserApplication.MainProgressBar.ShowMessage("Success downloading BeatStar data...", 10.0f);
                        onScoreSaberDataDownloaded?.Invoke();
                    }
                    catch (System.InvalidOperationException)
                    {
                        Logger.Error("Failed to download ScoreSaber data file...");
                    }
                    catch (Exception e)
                    {
                        Logger.Exception("Exception trying to download ScoreSaber data file...", e);
                    }
                }
            }
        }
    }
}
