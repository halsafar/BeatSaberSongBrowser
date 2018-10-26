using SongBrowserPlugin.DataAccess;
using SongBrowserPlugin.UI.DownloadQueue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRUI;

namespace SongBrowserPlugin.UI
{
    class PlaylistDetailViewController : VRUIViewController
    {
        private Playlist _selectedPlaylist;

        private TextMeshProUGUI _playlistTitleText;
        private TextMeshProUGUI _playlistAuthorText;
        private TextMeshProUGUI _playlistNumberOfSongs;

        private Button _selectButton;
        Button _downloadButton;

        public Action<Playlist> didPressPlayPlaylist;
        public event Action didPressDownloadPlaylist;
       
        /// <summary>
        /// Override the left/right screen selector.
        /// Put the Download Queue into the left screen.
        /// </summary>
        /// <param name="leftScreenViewController"></param>
        /// <param name="rightScreenViewController"></param>
        protected override void LeftAndRightScreenViewControllers(out VRUIViewController leftScreenViewController, out VRUIViewController rightScreenViewController)
        {
            PlaylistFlowCoordinator playlistFlowCoordinator = Resources.FindObjectsOfTypeAll<PlaylistFlowCoordinator>().First();
            leftScreenViewController = playlistFlowCoordinator.DownloadQueueViewController;
            rightScreenViewController = null;
        }

        /// <summary>
        /// Initialize the UI Elements.
        /// </summary>
        /// <param name="playlist"></param>
        public void Init(Playlist playlist)
        {
            _playlistTitleText = UIBuilder.CreateText(this.transform as RectTransform,
                playlist.playlistTitle,
                new Vector2(0, -20),
                new Vector2(60f, 10f)
            );
            _playlistTitleText.alignment = TextAlignmentOptions.Center;

            _playlistAuthorText = UIBuilder.CreateText(this.transform as RectTransform,
                playlist.playlistAuthor,
                new Vector2(0, -30),
                new Vector2(60f, 10f)
            );
            _playlistAuthorText.alignment = TextAlignmentOptions.Center;

            _playlistNumberOfSongs = UIBuilder.CreateText(this.transform as RectTransform,
                playlist.songs.Count.ToString(),
                new Vector2(0, -40),
                new Vector2(60f, 10f)
            );
            _playlistNumberOfSongs.alignment = TextAlignmentOptions.Center;

            Button buttonTemplate = Resources.FindObjectsOfTypeAll<Button>().FirstOrDefault(x => x.name == "PlayButton");
            _selectButton = UIBuilder.CreateButton(this.transform as RectTransform, buttonTemplate, "Select", 3, 0, 3.5f, 25, 6);
            _selectButton.onClick.AddListener(delegate ()
            {
                didPressPlayPlaylist.Invoke(_selectedPlaylist);
            });

            _downloadButton = UIBuilder.CreateUIButton((RectTransform)_selectButton.transform.parent, "PlayButton");
            (_downloadButton.transform as RectTransform).sizeDelta = new Vector2(30f, 10f);
            (_downloadButton.transform as RectTransform).anchoredPosition = new Vector2(2f, 24f);
            UIBuilder.SetButtonText(ref _downloadButton, "Download");
            _downloadButton.onClick.AddListener(delegate () { didPressDownloadPlaylist?.Invoke(); });

            SetContent(playlist);
        }

        /// <summary>
        /// Set the content.
        /// </summary>
        /// <param name="p"></param>
        public virtual void SetContent(Playlist p)
        {
            _selectedPlaylist = p;
            _playlistTitleText.text = _selectedPlaylist.playlistTitle;
            _playlistAuthorText.text = _selectedPlaylist.playlistAuthor;
            _playlistNumberOfSongs.text = "Song Count: " + _selectedPlaylist.songs.Count.ToString();
        }

        /// <summary>
        /// Disable / Enable the select and play buttons.
        /// </summary>
        /// <param name="enableSelect"></param>
        /// <param name="enableDownload"></param>
        public void UpdateButtons(bool enableSelect, bool enableDownload)
        {
            _selectButton.interactable = enableSelect;
            _downloadButton.interactable = enableDownload;
        }
    }
}
