using SongBrowserPlugin.DataAccess.BeatSaverApi;
using SongBrowserPlugin.UI;
using SongLoaderPlugin;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

namespace SongBrowserPlugin
{
    class Downloader : MonoBehaviour
    {
        private Logger _log = new Logger("Downloader");

        public static Downloader Instance;

        private StandardLevelDetailViewController _levelDetailViewController;

        public Action<Song> downloadStarted;
        public Action<Song> downloadFinished;

        /// <summary>
        /// Load this.
        /// </summary>
        internal static void OnLoad()
        {
            if (Instance != null)
            {
                return;
            }

            new GameObject("SongBrowserDownloader").AddComponent<Downloader>();            
        }

        /// <summary>
        /// Downloader has awoken.
        /// </summary>
        private void Awake()
        {
            _log.Trace("Awake()");

            Instance = this;
        }

        /// <summary>
        /// Acquire any UI elements from Beat saber that we need.  Wait for the song list to be loaded.
        /// </summary>
        public void Start()
        {
            _log.Trace("Start()");

            StandardLevelSelectionFlowCoordinator levelSelectionFlowCoordinator = Resources.FindObjectsOfTypeAll<StandardLevelSelectionFlowCoordinator>().First();
            _levelDetailViewController = levelSelectionFlowCoordinator.GetPrivateField<StandardLevelDetailViewController>("_levelDetailViewController");            
        }

        /// <summary>
        /// Handle downloading a song.  
        /// Ported from: https://github.com/andruzzzhka/BeatSaverDownloader/blob/master/BeatSaverDownloader/PluginUI/PluginUI.cs
        /// </summary>
        /// <param name="songInfo"></param>
        /// <returns></returns>
        public IEnumerator DownloadSongCoroutine(Song songInfo)
        {
            songInfo.songQueueState = SongQueueState.Downloading;

            downloadStarted?.Invoke(songInfo);

            UnityWebRequest www;
            bool timeout = false;
            float time = 0f;
            UnityWebRequestAsyncOperation asyncRequest;

            try
            {
                www = UnityWebRequest.Get(songInfo.downloadUrl);

                asyncRequest = www.SendWebRequest();
            }
            catch
            {
                songInfo.songQueueState = SongQueueState.Error;
                songInfo.downloadingProgress = 1f;

                yield break;
            }

            while ((!asyncRequest.isDone || songInfo.downloadingProgress != 1f) && songInfo.songQueueState != SongQueueState.Error)
            {
                yield return null;

                time += Time.deltaTime;

                if ((time >= 15f && asyncRequest.progress == 0f) || songInfo.songQueueState == SongQueueState.Error)
                {
                    www.Abort();
                    timeout = true;
                }

                songInfo.downloadingProgress = asyncRequest.progress;
            }
        
            if (www.isNetworkError || www.isHttpError || timeout || songInfo.songQueueState == SongQueueState.Error)
            {
                if (timeout)
                {
                    songInfo.songQueueState = SongQueueState.Error;
                    TextMeshProUGUI _errorText = UIBuilder.CreateText(_levelDetailViewController.rectTransform, "Request timeout", new Vector2(18f, -64f), new Vector2(60f, 10f));
                    Destroy(_errorText.gameObject, 2f);
                }
                else
                {
                    songInfo.songQueueState = SongQueueState.Error;
                    _log.Error($"Downloading error: {www.error}");
                    TextMeshProUGUI _errorText = UIBuilder.CreateText(_levelDetailViewController.rectTransform, www.error, new Vector2(18f, -64f), new Vector2(60f, 10f));
                    Destroy(_errorText.gameObject, 2f);
                }
            }
            else
            {

                _log.Debug("Received response from BeatSaver.com...");

                string zipPath = "";
                string docPath = "";
                string customSongsPath = "";

                byte[] data = www.downloadHandler.data;

                try
                {

                    docPath = Application.dataPath;
                    docPath = docPath.Substring(0, docPath.Length - 5);
                    docPath = docPath.Substring(0, docPath.LastIndexOf("/"));
                    customSongsPath = docPath + "/CustomSongs/" + songInfo.id + "/";
                    zipPath = customSongsPath + songInfo.id + ".zip";
                    if (!Directory.Exists(customSongsPath))
                    {
                        Directory.CreateDirectory(customSongsPath);
                    }
                    File.WriteAllBytes(zipPath, data);
                    _log.Debug("Downloaded zip file!");
                }
                catch (Exception e)
                {
                    _log.Exception("EXCEPTION: ", e);
                    songInfo.songQueueState = SongQueueState.Error;
                    yield break;
                }

                _log.Debug("Extracting...");

                try
                {
                    ZipFile.ExtractToDirectory(zipPath, customSongsPath);
                }
                catch (Exception e)
                {
                    _log.Exception("Can't extract ZIP! Exception: ", e);
                }

                songInfo.path = Directory.GetDirectories(customSongsPath).FirstOrDefault();

                if (string.IsNullOrEmpty(songInfo.path))
                {
                    songInfo.path = customSongsPath;
                }

                try
                {
                    File.Delete(zipPath);
                }
                catch (IOException e)
                {
                    _log.Warning($"Can't delete zip! Exception: {e}");
                }

                songInfo.songQueueState = SongQueueState.Downloaded;

                _log.Debug("Downloaded!");

                downloadFinished?.Invoke(songInfo);
            }
        }
    }
}
