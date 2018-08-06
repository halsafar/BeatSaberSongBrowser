using SongBrowserPlugin.DataAccess;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using VRUI;

namespace SongBrowserPlugin.UI
{
    public class PlaylistSelectionListViewController : VRUI.VRUIViewController
    {
        public const String Name = "PlaylistSelectionListViewController";

        private Logger _log = new Logger(Name);

        private PlaylistTableView _tableView;

        PlaylistsReader _playlistsReader;        

        public Action<PlaylistSelectionListViewController> didSelectPlaylistEvent;

        public Playlist SelectedPlaylist { get; private set; }

        protected override void DidActivate(bool firstActivation, VRUIViewController.ActivationType activationType)
        {
            _log.Debug("DidActivate()");

            if (_playlistsReader == null)
            { 
                String playlistPath = Path.Combine(Environment.CurrentDirectory, "Playlists");
                _playlistsReader = new PlaylistsReader(playlistPath);
                _playlistsReader.UpdatePlaylists();
                _log.Debug("Reader {0} playlists!", _playlistsReader.Playlists.Count);
            }

            base.DidActivate(firstActivation, activationType);

            if (_tableView == null)
            {
                _tableView = new GameObject(name).AddComponent<PlaylistTableView>();
                _tableView.Init(rectTransform, _playlistsReader);

                _tableView.didSelectPlaylistEvent += HandlePlaylistListTableViewDidSelectRow;
            }
        }

        public virtual void HandlePlaylistListTableViewDidSelectRow(PlaylistTableView tableView, int row)
        {
            this.SelectedPlaylist = _playlistsReader.Playlists[row];
            if (this.didSelectPlaylistEvent != null)
            {
                this.didSelectPlaylistEvent(this);
            }
        }
    }
}
