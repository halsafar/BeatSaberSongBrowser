using CustomUI.BeatSaber;
using SimpleJSON;
using SongBrowserPlugin.DataAccess;
using SongBrowserPlugin.DataAccess.BeatSaverApi;
using SongBrowserPlugin.UI.DownloadQueue;
using SongLoaderPlugin;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;
using VRUI;
using Logger = SongBrowserPlugin.Logging.Logger;

namespace SongBrowserPlugin.UI
{
    public class PlaylistFlowCoordinator : FlowCoordinator
    {
        private BackButtonNavigationController _playlistNavigationController;
        private PlaylistListViewController _playlistListViewController;
        private PlaylistDetailViewController _playlistDetailViewController;

        public DownloadQueueViewController DownloadQueueViewController;

        public FlowCoordinator ParentFlowCoordinator;

        public PlaylistsReader _playlistsReader;

        private bool _downloadingPlaylist;

        private Playlist _lastPlaylist;

        /// <summary>
        /// User pressed "select" on the playlist.
        /// </summary>
        public Action<Playlist> didFinishEvent;

        /// <summary>
        /// Destroy.
        /// </summary>
        public virtual void OnDestroy()
        {
            Logger.Trace("OnDestroy()");
        }

        /// <summary>
        /// 
        /// </summary>
        public void Awake()
        {
            _playlistNavigationController = BeatSaberUI.CreateViewController<BackButtonNavigationController>();

            GameObject _playlistDetailGameObject = Instantiate(Resources.FindObjectsOfTypeAll<StandardLevelDetailViewController>().First(), _playlistNavigationController.rectTransform, false).gameObject;
            _playlistDetailViewController = _playlistDetailGameObject.AddComponent<PlaylistDetailViewController>();
            Destroy(_playlistDetailGameObject.GetComponent<StandardLevelDetailViewController>());
            _playlistDetailViewController.name = "PlaylistDetailViewController";
        }

        /// <summary>
        /// Present the playlist selector flow.
        /// </summary>
        /// <param name="parentViewController"></param>
        /// <param name="levels"></param>
        /// <param name="gameplayMode"></param>
        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            Logger.Trace("Presenting Playlist Selector! - firstActivation: {0}", firstActivation);
            try
            {
                if (firstActivation && activationType == ActivationType.AddedToHierarchy)
                {
                    if (_playlistsReader == null)
                    {
                        _playlistsReader = new PlaylistsReader();
                        _playlistsReader.UpdatePlaylists();
                        Logger.Debug("Reader found {0} playlists!", _playlistsReader.Playlists.Count);
                    }

                    title = "Playlists";

                    _playlistNavigationController.didFinishEvent += HandleDidFinish;

                    _playlistListViewController = BeatSaberUI.CreateViewController<PlaylistListViewController>();
                    _playlistListViewController.didSelectRow += HandleSelectRow;

                    _playlistDetailViewController.downloadButtonPressed += HandleDownloadPressed;
                    _playlistDetailViewController.selectButtonPressed += HandleDidSelectPlaylist;

                    DownloadQueueViewController = BeatSaberUI.CreateViewController<DownloadQueueViewController>();

                    SetViewControllersToNavigationConctroller(_playlistNavigationController, new VRUIViewController[]
                    {
                        _playlistListViewController
                    });

                    ProvideInitialViewControllers(_playlistNavigationController, DownloadQueueViewController, null);
                }

                _downloadingPlaylist = false;
                _playlistListViewController.SetContent(_playlistsReader.Playlists, _lastPlaylist);

                DownloadQueueViewController.allSongsDownloaded += HandleAllSongsDownloaded;
            }
            catch (Exception e)
            {
                Logger.Exception("Exception displaying playlist selector", e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="deactivationType"></param>
        protected override void DidDeactivate(DeactivationType deactivationType)
        {
            DownloadQueueViewController.allSongsDownloaded -= HandleAllSongsDownloaded;
        }

        /// <summary>
        /// Update the playlist detail view when a row is selected.
        /// </summary>
        /// <param name="songListViewController"></param>
        public virtual void HandleSelectRow(Playlist playlist)
        {
            Logger.Debug("Selected Playlist: {0}", playlist.Title);

            //int missingCount = CountMissingSongs(playlist);

            if (!_playlistDetailViewController.isInViewControllerHierarchy)
            {
                PushViewControllerToNavigationController(_playlistNavigationController, _playlistDetailViewController);
            }

            _lastPlaylist = playlist;
            _playlistDetailViewController.SetContent(playlist);

            this._playlistDetailViewController.UpdateButtons(!_downloadingPlaylist, !_downloadingPlaylist);
        }

        /// <summary>
        /// Playlist was selected, dismiss view and inform song browser.
        /// </summary>
        /// <param name="p"></param>
        public void HandleDidSelectPlaylist(Playlist p)
        {
            try
            {
                if (!DownloadQueueViewController.queuedSongs.Any(x => x.songQueueState == SongQueueState.Downloading || x.songQueueState == SongQueueState.Queued))
                {
                    DownloadQueueViewController.AbortDownloads();

                    ParentFlowCoordinator.InvokePrivateMethod("DismissFlowCoordinator", new object[] { this, null, false });
                    didFinishEvent?.Invoke(p);
                }
            }
            catch (Exception e)
            {
                Logger.Exception("", e);
            }
        }

        /// <summary>
        /// Playlist was dismissed, inform song browser (pass in null).
        /// </summary>
        public void HandleDidFinish()
        {
            try
            {
                if (this.DownloadQueueViewController.queuedSongs.Any(x => x.songQueueState == SongQueueState.Queued || x.songQueueState == SongQueueState.Downloading))
                {
                    Logger.Debug("Aborting downloads...");
                    this.DownloadQueueViewController.AbortDownloads();
                }

                Logger.Debug("Playlist selector dismissed...");
                this._playlistNavigationController.DismissViewControllerCoroutine(delegate ()
                {
                    didFinishEvent.Invoke(null);
                }, true);
            }
            catch (Exception e)
            {
                Logger.Exception("", e);
            }
        }

        /// <summary>
        /// Filter songs that we don't have data for.
        /// </summary>
        /// <param name="songs"></param>
        /// <param name="playlist"></param>
        /// <returns></returns>
        private void FilterSongsForPlaylist(Playlist playlist, bool matchAll = false)
        {
            if (!playlist.Songs.All(x => x.Level != null) || matchAll)
            {
                playlist.Songs.ForEach(x =>
                {
                    if (x.Level == null || matchAll)
                    {
                        x.Level = SongLoader.CustomLevels.FirstOrDefault(y => (y.customSongInfo.path.Contains(x.Key) && Directory.Exists(y.customSongInfo.path)) || (string.IsNullOrEmpty(x.LevelId) ? false : y.levelID.StartsWith(x.LevelId)));
                    }
                });
            }
        }

        /// <summary>
        /// Count missing songs for display.
        /// </summary>
        /// <param name="playlist"></param>
        /// <returns></returns>
        private int CountMissingSongs(Playlist playlist)
        {
            return playlist.Songs.Count - playlist.Songs.Count(x => SongLoader.CustomLevels.Any(y => y.customSongInfo.path.Contains(x.Key)));
        }

        /// <summary>
        /// Download playlist button pressed.
        /// </summary>
        private void HandleDownloadPressed(Playlist playlist)
        {
            if (!_downloadingPlaylist)
            {
                StartCoroutine(DownloadPlaylist(playlist));
            }
            else
            {
                Logger.Info("Already downloading playlist!");
            }
        }

        /// <summary>
        /// Download Playlist.
        /// </summary>
        /// <param name="playlist"></param>
        /// <returns></returns>
        public IEnumerator DownloadPlaylist(Playlist playlist)
        {
            this.FilterSongsForPlaylist(playlist, true);
            List<PlaylistSong> playlistSongsToDownload = playlist.Songs.Where(x => x.Level == null).ToList();

            List<PlaylistSong> needToDownload = playlist.Songs.Where(x => x.Level == null).ToList();
            Logger.Info($"Need to download {needToDownload.Count} songs");

            _downloadingPlaylist = true;
            foreach (var item in needToDownload)
            {
                Song beatSaverSong = null;

                if (String.IsNullOrEmpty(playlist.CustomArchiveUrl))
                {
                    Logger.Info("Obtaining hash and url for " + item.Key + ": " + item.SongName);
                    yield return GetInfoForSong(playlist, item, (Song song) => { beatSaverSong = song; });
                }
                else
                {
                    string archiveUrl = playlist.CustomArchiveUrl.Replace("[KEY]", item.Key);

                    beatSaverSong = new Song()
                    {
                        songName = item.SongName,
                        id = item.Key,
                        downloadingProgress = 0f,
                        hash = item.LevelId,
                        downloadUrl = archiveUrl
                    };
                }

                if (beatSaverSong != null && !SongLoader.CustomLevels.Any(x => x.levelID.Substring(0, 32) == beatSaverSong.hash.ToUpper()))
                {
                    DownloadQueueViewController.EnqueueSong(beatSaverSong, true);
                }
            }
            _downloadingPlaylist = false;
        }

        /// <summary>
        /// Request song info via BeatSaber (or custom) api call.
        /// </summary>
        /// <param name="playlist"></param>
        /// <param name="song"></param>
        /// <param name="songCallback"></param>
        /// <returns></returns>
        public IEnumerator GetInfoForSong(Playlist playlist, PlaylistSong song, Action<Song> songCallback)
        {
            string url = $"{PluginConfig.BeatsaverURL}/api/songs/detail/{song.Key}";
            if (!string.IsNullOrEmpty(playlist.CustomDetailUrl))
            {
                url = playlist.CustomDetailUrl + song.Key;
            }

            Logger.Debug("Attempting to connect to: {0}", url);
            UnityWebRequest www = UnityWebRequest.Get(url);
            www.timeout = 15;
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Logger.Error($"Unable to connect to {PluginConfig.BeatsaverURL}! " + (www.isNetworkError ? $"Network error: {www.error}" : (www.isHttpError ? $"HTTP error: {www.error}" : "Unknown error")));
            }
            else
            {
                try
                {
                    JSONNode node = JSON.Parse(www.downloadHandler.text);
                    songCallback?.Invoke(new Song(node["song"]));
                }
                catch (Exception e)
                {
                    Logger.Exception("Unable to parse response! Exception: ", e);
                }
            }
        }

        /// <summary>
        /// Songs finished downloading.
        /// </summary>
        private void HandleAllSongsDownloaded()
        {
            SongLoader.Instance.RefreshSongs(false);

            this.FilterSongsForPlaylist(_lastPlaylist);

            _downloadingPlaylist = false;
            _playlistDetailViewController.UpdateButtons(!_downloadingPlaylist, !_downloadingPlaylist);
        }

        /// <summary>
        /// Useful playlist navigation.
        /// Shift+Enter downloads.
        /// Enter selects.
        /// </summary>
        public void LateUpdate()
        {
            bool isShiftKeyDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (isShiftKeyDown && Input.GetKeyDown(KeyCode.Return))
            {
                HandleDownloadPressed(_lastPlaylist);
            }
            else if (Input.GetKeyDown(KeyCode.Return))
            {
                HandleDidSelectPlaylist(_lastPlaylist);
            }
        }
    }
}
