using SongBrowserPlugin.DataAccess;
using System;
using System.Reflection;
using UnityEngine;
using VRUI;

namespace SongBrowserPlugin.UI
{
    public class PlaylistFlowCoordinator : FlowCoordinator
    {
        public const String Name = "PlaylistFlowCoordinator";
        private Logger _log = new Logger(Name);

        private PlaylistSelectionNavigationController _playlistNavigationController;
        private PlaylistSelectionListViewController _playlistListViewController;
        private PlaylistDetailViewController _playlistDetailViewController;

        private bool _initialized;

        public Action<Playlist> didSelectPlaylist;

        public virtual void OnDestroy()
        {
            _log.Trace("OnDestroy()");
        }

        /// <summary>
        /// 
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

                // Set parent view controllers appropriately.
                _playlistNavigationController.GetType().GetField("_parentViewController", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(_playlistNavigationController, parentViewController);
                _playlistListViewController.GetType().GetField("_parentViewController", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(_playlistListViewController, _playlistNavigationController);
                _playlistDetailViewController.GetType().GetField("_parentViewController", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).SetValue(_playlistDetailViewController, _playlistListViewController);

                this._playlistListViewController.didSelectPlaylistRowEvent += HandlePlaylistListDidSelectPlaylist;
                this._playlistDetailViewController.didPressPlayPlaylist += HandleDidPlayPlaylist;
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
        
        public void LateUpdate()
        {
            // accept
            if (Input.GetKeyDown(KeyCode.Return))
            {
                HandleDidPlayPlaylist(this._playlistListViewController.SelectedPlaylist);
            }
        }
    }
}
