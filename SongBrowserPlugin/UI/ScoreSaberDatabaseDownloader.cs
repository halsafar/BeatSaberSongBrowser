using Mobcast.Coffee.AssetSystem;
using SongBrowserPlugin.DataAccess;
using SongBrowserPlugin.DataAccess.Network;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace SongBrowserPlugin.UI
{
    public class ScoreSaberDatabaseDownloader : MonoBehaviour
    {
        public const String SCRAPED_SCORE_SABER_JSON_URL = "https://raw.githubusercontent.com/halsafar/beat-saber-scraped-data/master/scoresaber/score_saber_data_v1.json";

        private Logger _log = new Logger("ScoreSaberDatabaseDownloader");

        public static ScoreSaberDatabaseDownloader Instance;

        public static ScoreSaberDataFile ScoreSaberDataFile = null;        

        public Action onScoreSaberDataDownloaded;

        private readonly byte[] _buffer = new byte[4 * 1048576];

        /// <summary>
        /// Awake.
        /// </summary>
        private void Awake()
        {
            _log.Trace("Awake()");

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
            _log.Trace("Start()");

            StartCoroutine(WaitForDownload());
        }

        private void DownloadScoreSaberData()
        {

        }

        /// <summary>
        /// Wait for the tsv file from DuoVR to download.
        /// </summary>
        /// <returns></returns>
        private IEnumerator WaitForDownload()
        {
            if (ScoreSaberDatabaseDownloader.ScoreSaberDataFile != null)
            {
                _log.Info("Using cached copy of DuoVR ScoreSaberData...");
            }
            else
            {
                SongBrowserApplication.MainProgressBar.ShowMessage("Downloading DuoVR ScoreSaber data...");

                _log.Info("Attempting to download: {0}", ScoreSaberDatabaseDownloader.SCRAPED_SCORE_SABER_JSON_URL);
                using (UnityWebRequest www = UnityWebRequest.Get(ScoreSaberDatabaseDownloader.SCRAPED_SCORE_SABER_JSON_URL))
                {
                    // Use 4MB cache, large enough for this file to grow for awhile.
                    www.SetCacheable(new CacheableDownloadHandlerScoreSaberData(www, _buffer));
                    yield return www.SendWebRequest();

                    _log.Debug("Returned from web request!...");

                    try
                    {
                        ScoreSaberDatabaseDownloader.ScoreSaberDataFile = (www.downloadHandler as CacheableDownloadHandlerScoreSaberData).ScoreSaberDataFile;
                        _log.Info("Success downloading DuoVR ScoreSaber data!");

                        SongBrowserApplication.MainProgressBar.ShowMessage("Success downloading DuoVR ScoreSaber data...");
                        onScoreSaberDataDownloaded?.Invoke();
                    }
                    catch (System.InvalidOperationException)
                    {
                        _log.Error("Failed to download DuoVR ScoreSaber data file...");
                    }
                    catch (Exception e)
                    {
                        _log.Exception("Exception trying to download DuoVR ScoreSaber data file...", e);
                    }
                }
            }
        }
    }
}
