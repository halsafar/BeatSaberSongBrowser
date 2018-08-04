using SongBrowserPlugin.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using VRUI;

namespace SongBrowserPlugin.UI
{
    public class PlaylistFlowCoordinator : FlowCoordinator
    {
        public const String Name = "PlaylistFlowCoordinator";
        private Logger _log = new Logger(Name);

        private PlaylistSelectionMasterViewController _playlistNavigationController;
        private PlaylistSelectionListViewController _playlistViewController;
        private PlaylistDetailViewController _playlistDetailViewController;

        private bool _initialized;

        public Action<Playlist> didSelectPlaylist;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parentViewController"></param>
        /// <param name="levels"></param>
        /// <param name="gameplayMode"></param>
        public virtual void Present(VRUIViewController parentViewController)
        {
            if (!this._initialized)
            {
                _playlistNavigationController = UIBuilder.CreateViewController<PlaylistSelectionMasterViewController>("PlaylistSelectionMasterViewController");
                _playlistViewController = UIBuilder.CreateViewController<PlaylistSelectionListViewController>("PlaylistSelectionListViewController");
                _playlistDetailViewController = UIBuilder.CreateViewController<PlaylistDetailViewController>("PlaylistDetailViewController");

                this._playlistViewController.didSelectPlaylistEvent += HandlePlaylistListDidSelectPlaylist;
                this._playlistDetailViewController.didPressPlayPlaylist += HandleDidPlayPlaylist;

                this._initialized = true;
            }

            _playlistViewController.rectTransform.anchorMin = new Vector2(0.3f, 0f);
            _playlistViewController.rectTransform.anchorMax = new Vector2(0.7f, 1f);

            _playlistDetailViewController.rectTransform.anchorMin = new Vector2(0.3f, 0f);
            _playlistDetailViewController.rectTransform.anchorMax = new Vector2(0.7f, 1f);

            parentViewController.PresentModalViewController(this._playlistNavigationController, null, parentViewController.isRebuildingHierarchy);
            this._playlistNavigationController.PushViewController(this._playlistViewController, parentViewController.isRebuildingHierarchy);            
        }

        /// <summary>
        /// 
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
            }
        }

        public void HandleDidPlayPlaylist(Playlist p)
        {
            this._playlistNavigationController.DismissModalViewController(delegate ()
            {
                didSelectPlaylist.Invoke(p);
            });
        }

        public void LateUpdate()
        {
            // accept
            if (Input.GetKeyDown(KeyCode.Return))
            {
                HandleDidPlayPlaylist(this._playlistViewController.SelectedPlaylist);
            }
        }
    }


}
