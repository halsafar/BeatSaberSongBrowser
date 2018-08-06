using SongBrowserPlugin.DataAccess;
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

        public Action<Playlist> didPressPlayPlaylist;

        public void Init(Playlist playlist)
        {
            _playlistTitleText = UIBuilder.CreateText(this.transform as RectTransform,
                playlist.playlistTitle,
                new Vector2(0, -20)                
            );
            _playlistTitleText.alignment = TextAlignmentOptions.Center;

            _playlistAuthorText = UIBuilder.CreateText(this.transform as RectTransform,
                playlist.playlistAuthor,
                new Vector2(0, -30)
            );
            _playlistAuthorText.alignment = TextAlignmentOptions.Center;

            _playlistNumberOfSongs = UIBuilder.CreateText(this.transform as RectTransform,
                playlist.songs.Count.ToString(),
                new Vector2(0, -40)
            );
            _playlistNumberOfSongs.alignment = TextAlignmentOptions.Center;

            Button buttonTemplate = Resources.FindObjectsOfTypeAll<Button>().FirstOrDefault(x => x.name == "PlayButton");
            _selectButton = UIBuilder.CreateButton(this.transform as RectTransform, buttonTemplate, "Select", 3, 0, 3.5f, 25, 6);
            _selectButton.onClick.AddListener(delegate ()
            {
                didPressPlayPlaylist.Invoke(_selectedPlaylist);
            });

            SetContent(playlist);
        }

        public virtual void SetContent(Playlist p)
        {
            _selectedPlaylist = p;
            _playlistTitleText.text = _selectedPlaylist.playlistTitle;
            _playlistAuthorText.text = _selectedPlaylist.playlistAuthor;
            _playlistNumberOfSongs.text = "Song Count: " + _selectedPlaylist.songs.Count.ToString();
        }
    }
}
