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

namespace SongBrowserPlugin.UI
{
    public class PlaylistFlowCoordinator : FlowCoordinator
    {
        public static string beatsaverURL = "https://beatsaver.com";

        public const String Name = "PlaylistFlowCoordinator";
        private Logger _log = new Logger(Name);

        private PlaylistSelectionNavigationController _playlistNavigationController;
        private PlaylistSelectionListViewController _playlistListViewController;
        private PlaylistDetailViewController _playlistDetailViewController;

        public DownloadQueueViewController DownloadQueueViewController;

        private bool _initialized;

        private bool _downloadingPlaylist;

        private Song _lastRequestedSong;

        /// <summary>
        /// User pressed "select" on the playlist.
        /// </summary>
        public Action<Playlist> didSelectPlaylist;

        /// <summary>
        /// Destroy.
        /// </summary>
        public virtual void OnDestroy()
        {
            _log.Trace("OnDestroy()");
        }

        /// <summary>
        /// Present the playlist selector flow.
        /// </summary>
        /// <param name="parentViewController"></param>
        /// <param name="levels"></param>
        /// <param name="gameplayMode"></param>
        public virtual void Present(VRUIViewController parentViewController)
        {
            _log.Trace("Presenting Playlist Selector! - initialized: {0}", this._initialized);
            if (!this._initialized)
            {
                _playlistNavigationController = UIBuilder.CreateViewController<PlaylistSelectionNavigationController>("PlaylistSelectionMasterViewController");
                _playlistListViewController = UIBuilder.CreateViewController<PlaylistSelectionListViewController>("PlaylistSelectionListViewController");
                _playlistDetailViewController = UIBuilder.CreateViewController<PlaylistDetailViewController>("PlaylistDetailViewController");

                this.DownloadQueueViewController = UIBuilder.CreateViewController<DownloadQueueViewController>("DownloadQueueViewController");

                // Set parent view controllers appropriately.
                _playlistNavigationController.GetType().GetField("_parentViewController", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(_playlistNavigationController, parentViewController);
                _playlistListViewController.GetType().GetField("_parentViewController", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(_playlistListViewController, _playlistNavigationController);
                _playlistDetailViewController.GetType().GetField("_parentViewController", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(_playlistDetailViewController, _playlistListViewController);

                this._playlistListViewController.didSelectPlaylistRowEvent += HandlePlaylistListDidSelectPlaylist;

                this._playlistDetailViewController.didPressPlayPlaylist += HandleDidPlayPlaylist;
                this._playlistDetailViewController.didPressDownloadPlaylist += HandleDownloadPressed;

                this._playlistNavigationController.didDismissEvent += HandleDidFinish;

                _playlistListViewController.rectTransform.anchorMin = new Vector2(0.3f, 0f);
                _playlistListViewController.rectTransform.anchorMax = new Vector2(0.7f, 1f);

                _playlistDetailViewController.rectTransform.anchorMin = new Vector2(0.3f, 0f);
                _playlistDetailViewController.rectTransform.anchorMax = new Vector2(0.7f, 1f);

                parentViewController.PresentModalViewController(this._playlistNavigationController, null, parentViewController.isRebuildingHierarchy);
                this._playlistNavigationController.PushViewController(this._playlistListViewController, parentViewController.isRebuildingHierarchy);

                this._initialized = true;
            }                        
        }

        /// <summary>
        /// Update the playlist detail view when a row is selected.
        /// </summary>
        /// <param name="songListViewController"></param>
        public virtual void HandlePlaylistListDidSelectPlaylist(PlaylistSelectionListViewController playlistListViewController)
        {
            _log.Debug("Selected Playlist: {0}", playlistListViewController.SelectedPlaylist.playlistTitle);
            if (!this._playlistDetailViewController.isInViewControllerHierarchy)
            {
                this._playlistDetailViewController.Init(playlistListViewController.SelectedPlaylist);
                this._playlistNavigationController.PushViewController(this._playlistDetailViewController, playlistListViewController.isRebuildingHierarchy);
            }
            else
            {
                this._playlistDetailViewController.SetContent(playlistListViewController.SelectedPlaylist);
                this._playlistDetailViewController.UpdateButtons(!_downloadingPlaylist, !_downloadingPlaylist);
            }
        }

        /// <summary>
        /// Playlist was selected, dismiss view and inform song browser.
        /// </summary>
        /// <param name="p"></param>
        public void HandleDidPlayPlaylist(Playlist p)
        {
            try
            {
                _log.Debug("Playlist selector selected playlist...");
                this._playlistNavigationController.DismissModalViewController(delegate ()                
                {
                    didSelectPlaylist.Invoke(p);
                }, true);
            }
            catch (Exception e)
            {
                _log.Exception("", e);
            }
        }

        /// <summary>
        /// Playlist was dismissed, inform song browser (pass in null).
        /// </summary>
        public void HandleDidFinish()
        {
            try
            {
                if (!this.DownloadQueueViewController._queuedSongs.Any(x => x.songQueueState == SongQueueState.Queued || x.songQueueState == SongQueueState.Downloading) && !_downloadingPlaylist)
                {
                    _log.Debug("Aborting downloads...");
                    this.DownloadQueueViewController.AbortDownloads();
                }

                _log.Debug("Playlist selector dismissed...");
                this._playlistNavigationController.DismissModalViewController(delegate ()
                {
                    didSelectPlaylist.Invoke(null);
                }, true);
            }
            catch (Exception e)
            {
                _log.Exception("", e);
            }
        }

        /// <summary>
        /// Filter songs that we don't have data for.
        /// </summary>
        /// <param name="songs"></param>
        /// <param name="playlist"></param>
        /// <returns></returns>
        private void FilterSongsForPlaylist(Playlist playlist)
        {
            if (!playlist.songs.All(x => x.Level != null))
            {
                playlist.songs.ForEach(x =>
                {
                    if (x.Level == null)
                    {
                        x.Level = SongLoader.CustomLevels.FirstOrDefault(y => y.customSongInfo.path.Contains(x.Key) && Directory.Exists(y.customSongInfo.path));
                    }
                });
            }
        }

        /// <summary>
        /// Download playlist button pressed.
        /// </summary>
        private void HandleDownloadPressed()
        {
            if (!_downloadingPlaylist)
            {
                StartCoroutine(DownloadPlaylist(this._playlistListViewController.SelectedPlaylist));
            }
            else
            {
                _log.Info("Already downloading playlist!");
            }
        }

        /// <summary>
        /// Download Playlist.
        /// </summary>
        /// <param name="playlist"></param>
        /// <returns></returns>
        public IEnumerator DownloadPlaylist(Playlist playlist)
        {
            Playlist selectedPlaylist = this._playlistListViewController.SelectedPlaylist;
            this.FilterSongsForPlaylist(selectedPlaylist);
            List<PlaylistSong> playlistSongsToDownload = selectedPlaylist.songs.Where(x => x.Level == null).ToList();

            List<Song> beatSaverSongs = new List<Song>();

            DownloadQueueViewController.AbortDownloads();
            _downloadingPlaylist = true;
            _playlistDetailViewController.UpdateButtons(!_downloadingPlaylist, !_downloadingPlaylist);

            foreach (var item in playlistSongsToDownload)
            {
                _log.Debug("Obtaining hash and url for " + item.Key + ": " + item.SongName);
                yield return GetSongByPlaylistSong(item);

                _log.Debug("Song is null: " + (_lastRequestedSong == null) + "\n Level is downloaded: " + (SongLoader.CustomLevels.Any(x => x.levelID.Substring(0, 32) == _lastRequestedSong.hash.ToUpper())));

                if (_lastRequestedSong != null && !SongLoader.CustomLevels.Any(x => x.levelID.Substring(0, 32) == _lastRequestedSong.hash.ToUpper()))
                {
                    _log.Debug(item.Key + ": " + item.SongName + "  -  " + _lastRequestedSong.hash);
                    beatSaverSongs.Add(_lastRequestedSong);
                    DownloadQueueViewController.EnqueueSong(_lastRequestedSong, false);
                }
            }

            _log.Info($"Need to download {beatSaverSongs.Count(x => x.songQueueState == SongQueueState.Queued)} songs:");

            if (!beatSaverSongs.Any(x => x.songQueueState == SongQueueState.Queued))
            {
                _downloadingPlaylist = false;
                _playlistDetailViewController.UpdateButtons(!_downloadingPlaylist, !_downloadingPlaylist);
            }

            foreach (var item in beatSaverSongs.Where(x => x.songQueueState == SongQueueState.Queued))
            {
                _log.Debug(item.songName);
            }

            DownloadQueueViewController.allSongsDownloaded -= AllSongsDownloaded;
            DownloadQueueViewController.allSongsDownloaded += AllSongsDownloaded;

            DownloadQueueViewController.DownloadAllSongsFromQueue();
        }

        /// <summary>
        /// Songs finished downloading.
        /// </summary>
        private void AllSongsDownloaded()
        {
            SongLoader.Instance.RefreshSongs(false);

            this.FilterSongsForPlaylist(this._playlistListViewController.SelectedPlaylist);

            _downloadingPlaylist = false;
            _playlistDetailViewController.UpdateButtons(!_downloadingPlaylist, !_downloadingPlaylist);
        }

        /// <summary>
        /// Fetch the song info from beat saver.
        /// </summary>
        /// <param name="song"></param>
        /// <returns></returns>
        public IEnumerator GetSongByPlaylistSong(PlaylistSong song)
        {
            UnityWebRequest wwwId = null;
            try
            {
                wwwId = UnityWebRequest.Get($"{PlaylistFlowCoordinator.beatsaverURL}/api/songs/detail/" + song.Key);
                wwwId.timeout = 10;
            }
            catch
            {
                _lastRequestedSong = new Song() { songName = song.SongName, songQueueState = SongQueueState.Error, downloadingProgress = 1f, hash = "" };

                yield break;
            }

            yield return wwwId.SendWebRequest();

            if (wwwId.isNetworkError || wwwId.isHttpError)
            {
                _log.Error(wwwId.error);
                _log.Error($"Song {song.SongName} doesn't exist on BeatSaver!");
                _lastRequestedSong = new Song() { songName = song.SongName, songQueueState = SongQueueState.Error, downloadingProgress = 1f, hash = "" };
            }
            else
            {
                JSONNode node = JSON.Parse(wwwId.downloadHandler.text);
                Song _tempSong = new Song(node["song"]);

                _lastRequestedSong = _tempSong;
            }
        }

        public void LateUpdate()
        {
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.Return))
            {
                HandleDownloadPressed();
            }
            else if (Input.GetKeyDown(KeyCode.Return))
            {
                HandleDidPlayPlaylist(this._playlistListViewController.SelectedPlaylist);
            }
        }
    }
}
