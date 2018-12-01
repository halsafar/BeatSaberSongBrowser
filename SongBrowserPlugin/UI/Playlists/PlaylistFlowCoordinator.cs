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

namespace SongBrowserPlugin.UI
{
    public class PlaylistFlowCoordinator : FlowCoordinator
    {
        public static string beatsaverURL = "https://beatsaver.com";

        private Logger _log = new Logger("PlaylistFlowCoordinator");

        private BackButtonNavigationController _playlistNavigationController;
        private PlaylistListViewController _playlistListViewController;
        private PlaylistDetailViewController _playlistDetailViewController;

        public DownloadQueueViewController DownloadQueueViewController;

        public FlowCoordinator ParentFlowCoordinator;

        public PlaylistsReader _playlistsReader;

        private bool _downloadingPlaylist;

        private Song _lastRequestedSong;

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
            _log.Trace("OnDestroy()");
        }

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
            _log.Trace("Presenting Playlist Selector! - firstActivation: {0}", firstActivation);
            try
            {
                if (firstActivation && activationType == ActivationType.AddedToHierarchy)
                {
                    if (_playlistsReader == null)
                    {
                        _playlistsReader = new PlaylistsReader();
                        _playlistsReader.UpdatePlaylists();
                        _log.Debug("Reader found {0} playlists!", _playlistsReader.Playlists.Count);
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
                _log.Exception("Exception displaying playlist selector", e);
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
            _log.Debug("Selected Playlist: {0}", playlist.Title);

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
                if (this.DownloadQueueViewController.queuedSongs.Any(x => x.songQueueState == SongQueueState.Queued || x.songQueueState == SongQueueState.Downloading))
                {
                    _log.Debug("Aborting downloads...");
                    this.DownloadQueueViewController.AbortDownloads();
                }

                _log.Debug("Playlist selector dismissed...");
                this._playlistNavigationController.DismissViewControllerCoroutine(delegate ()
                {
                    didFinishEvent.Invoke(null);
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
            if (!playlist.Songs.All(x => x.Level != null))
            {
                playlist.Songs.ForEach(x =>
                {
                    if (x.Level == null)
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
            this.FilterSongsForPlaylist(playlist);
            List<PlaylistSong> playlistSongsToDownload = playlist.Songs.Where(x => x.Level == null).ToList();

            List<Song> beatSaverSongs = new List<Song>();

            DownloadQueueViewController.AbortDownloads();
            _downloadingPlaylist = true;
            _playlistDetailViewController.UpdateButtons(!_downloadingPlaylist, !_downloadingPlaylist);

            foreach (var item in playlistSongsToDownload)
            {
                if (String.IsNullOrEmpty(playlist.CustomArchiveUrl))
                {
                    _log.Debug("Obtaining hash and url for " + item.Key + ": " + item.SongName);
                    yield return GetSongByPlaylistSong(playlist, item);

                    _log.Debug("Song is null: " + (_lastRequestedSong == null) + "\n Level is downloaded: " + (SongLoader.CustomLevels.Any(x => x.levelID.Substring(0, 32) == _lastRequestedSong.hash.ToUpper())));
                }
                else
                {
                    // stamp archive url
                    String archiveUrl = playlist.CustomArchiveUrl.Replace("[KEY]", item.Key);

                    // Create fake song with what we know...
                    // TODO - update this if we ever know more...
                    _lastRequestedSong = new Song()
                    {
                        songName = item.SongName,
                        downloadingProgress = 0f,
                        hash = "",
                        downloadUrl = archiveUrl
                    };
                }

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

            DownloadQueueViewController.allSongsDownloaded -= HandleAllSongsDownloaded;
            DownloadQueueViewController.allSongsDownloaded += HandleAllSongsDownloaded;

            DownloadQueueViewController.DownloadAllSongsFromQueue();
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
        /// Fetch the song info from beat saver.
        /// </summary>
        /// <param name="song"></param>
        /// <returns></returns>
        public IEnumerator GetSongByPlaylistSong(Playlist playlist, PlaylistSong song)
        {
            UnityWebRequest wwwId = null;
            try
            {
                wwwId = UnityWebRequest.Get($"{PlaylistFlowCoordinator.beatsaverURL}/api/songs/detail/" + song.Key);
                string url = PlaylistFlowCoordinator.beatsaverURL + $"/api/songs/detail/" + song.Key;
                if (!string.IsNullOrEmpty(playlist.CustomDetailUrl))
                {
                    url = playlist.CustomDetailUrl + song.Key;
                }
                wwwId = UnityWebRequest.Get(url);
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
                _log.Error($"Song {song.Key}({song.SongName}) doesn't exist!");
                _lastRequestedSong = new Song() { songName = song.SongName, songQueueState = SongQueueState.Error, downloadingProgress = 1f, hash = "" };
            }
            else
            {
                JSONNode node = JSON.Parse(wwwId.downloadHandler.text);
                Song _tempSong = new Song(node["song"]);

                _lastRequestedSong = _tempSong;
            }
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
                HandleSelectRow(_lastPlaylist);
            }
        }
    }
}
