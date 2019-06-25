using Mobcast.Coffee.AssetSystem;
using SongBrowser.DataAccess;
using SongBrowser.DataAccess.Network;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using Logger = SongBrowser.Logging.Logger;

namespace SongBrowser.UI
{
    public class ScoreSaberDatabaseDownloader : MonoBehaviour
    {
        public const String SCRAPED_SCORE_SABER_ALL_JSON_URL = "https://cdn.wes.cloud/beatstar/bssb/v2-all.json";
        public const String SCRAPED_SCORE_SABER_RANKED_JSON_URL = "https://cdn.wes.cloud/beatstar/bssb/v2-ranked.json";

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

            StartCoroutine(DownloadScoreSaberDatabases());
        }

        /// <summary>
        /// Helper to download both databases.
        /// </summary>
        /// <returns></returns>
        private IEnumerator DownloadScoreSaberDatabases()
        {
            ScoreSaberDataFile = null;

            yield return DownloadScoreSaberData(SCRAPED_SCORE_SABER_ALL_JSON_URL);
            yield return DownloadScoreSaberData(SCRAPED_SCORE_SABER_RANKED_JSON_URL);

            if (ScoreSaberDataFile != null)
            {
                SongBrowserApplication.MainProgressBar.ShowMessage("Success downloading BeatStar data...", 5.0f);
                onScoreSaberDataDownloaded?.Invoke();
            }
        }

        /// <summary>
        /// Wait for score saber related files to download.
        /// </summary>
        /// <returns></returns>
        private IEnumerator DownloadScoreSaberData(String url)
        {
            SongBrowserApplication.MainProgressBar.ShowMessage("Downloading BeatStar data...", 5.0f);

            Logger.Info("Attempting to download: {0}", url);
            using (UnityWebRequest www = UnityWebRequest.Get(url))
            {
                // Use 4MB cache, large enough for this file to grow for awhile.
                www.SetCacheable(new CacheableDownloadHandlerScoreSaberData(www, _buffer));
                yield return www.SendWebRequest();

                Logger.Debug("Returned from web request!...");

                try
                {
                    // First time 
                    if (ScoreSaberDatabaseDownloader.ScoreSaberDataFile == null)
                    {
                        ScoreSaberDatabaseDownloader.ScoreSaberDataFile = (www.downloadHandler as CacheableDownloadHandlerScoreSaberData).ScoreSaberDataFile;                            
                    }
                    else
                    {
                        // Second time, update.
                        var newScoreSaberData = (www.downloadHandler as CacheableDownloadHandlerScoreSaberData).ScoreSaberDataFile;
                        foreach (var pair in newScoreSaberData.SongHashToScoreSaberData)
                        {
                            if (ScoreSaberDatabaseDownloader.ScoreSaberDataFile.SongHashToScoreSaberData.ContainsKey(pair.Key))
                            {
                                foreach (var diff in pair.Value.diffs)
                                {
                                    var index = ScoreSaberDatabaseDownloader.ScoreSaberDataFile.SongHashToScoreSaberData[pair.Key].diffs.FindIndex(x => x.diff == diff.diff);
                                    if (index < 0)
                                    {
                                        ScoreSaberDatabaseDownloader.ScoreSaberDataFile.SongHashToScoreSaberData[pair.Key].diffs.Add(diff);
                                    }
                                }
                            }
                        }
                    }
                    Logger.Info("Success downloading ScoreSaber data!");
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
