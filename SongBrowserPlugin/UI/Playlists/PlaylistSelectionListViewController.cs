using SongBrowserPlugin.DataAccess;
using System;
using System.IO;
using UnityEngine;
using VRUI;

namespace SongBrowserPlugin.UI
{
    public class PlaylistSelectionListViewController : VRUI.VRUIViewController
    {
        public const String Name = "PlaylistSelectionListViewController";

        private Logger _log = new Logger(Name);

        private PlaylistTableView _playlistTableView;

        private PlaylistsReader _playlistsReader;        

        public Action<PlaylistSelectionListViewController> didSelectPlaylistRowEvent;

        public Playlist SelectedPlaylist { get; private set; }

        /// <summary>
        /// Instantiate the playlist table view.
        /// </summary>
        /// <param name="firstActivation"></param>
        /// <param name="activationType"></param>
        protected override void DidActivate(bool firstActivation, VRUIViewController.ActivationType activationType)
        {
            _log.Debug("DidActivate()");

            if (_playlistsReader == null)
            { 
                String playlistPath = Path.Combine(Environment.CurrentDirectory, "Playlists");
                _playlistsReader = new PlaylistsReader(playlistPath);
                _playlistsReader.UpdatePlaylists();
                _log.Debug("Reader found {0} playlists!", _playlistsReader.Playlists.Count);
            }

            base.DidActivate(firstActivation, activationType);

            if (_playlistTableView == null)
            {
                _playlistTableView = new GameObject(name).AddComponent<PlaylistTableView>();
                _playlistTableView.Init(rectTransform, _playlistsReader);

                _playlistTableView.didSelectPlaylistEvent += HandlePlaylistListTableViewDidSelectRow;
            }
        }

        /// <summary>
        /// Deactivate - Destroy!
        /// </summary>
        /// <param name="deactivationType"></param>
        protected override void DidDeactivate(VRUIViewController.DeactivationType deactivationType)
        {
            _log.Debug("DidDeactivate()");
            this._playlistTableView.gameObject.SetActive(false);
            Destroy(this._playlistTableView);
            base.DidDeactivate(deactivationType);
        }

        /// <summary>
        /// Did select a playlist row.
        /// </summary>
        /// <param name="tableView"></param>
        /// <param name="row"></param>
        public virtual void HandlePlaylistListTableViewDidSelectRow(PlaylistTableView tableView, int row)
        {
            this.SelectedPlaylist = _playlistsReader.Playlists[row];
            if (this.didSelectPlaylistRowEvent != null)
            {
                this.didSelectPlaylistRowEvent(this);
            }
        }
    }
}
