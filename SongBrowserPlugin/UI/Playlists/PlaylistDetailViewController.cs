using CustomUI.BeatSaber;
using SongBrowserPlugin.DataAccess;
using SongLoaderPlugin;
using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRUI;
using Logger = SongBrowserPlugin.Logging.Logger;
using Sprites = SongBrowserPlugin.UI.Base64Sprites;

namespace SongBrowserPlugin.UI
{
    class PlaylistDetailViewController : VRUIViewController
    {

        public event Action<Playlist> downloadButtonPressed;
        public event Action<Playlist> selectButtonPressed;

        private Playlist _currentPlaylist;

        private TextMeshProUGUI songNameText;

        private Button _downloadButton;
        private Button _selectButton;
        private string _selectButtonText = "Select";

        private TextMeshProUGUI authorText;
        private TextMeshProUGUI totalSongsText;
        private TextMeshProUGUI downloadedSongsText;

        private StandardLevelDetailView _levelDetails;

        public bool addDownloadButton = true;
        private Image coverImage;

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            if (firstActivation && type == ActivationType.AddedToHierarchy)
            {
                gameObject.SetActive(true);
                rectTransform.sizeDelta = new Vector2(60f, 0f);

                _levelDetails = GetComponentsInChildren<StandardLevelDetailView>(true).First(x => x.name == "LevelDetail");
                _levelDetails.gameObject.SetActive(true);

                RemoveCustomUIElements(rectTransform);

                Destroy(GetComponentsInChildren<LevelParamsPanel>().First(x => x.name == "LevelParamsPanel").gameObject);

                RectTransform yourStats = GetComponentsInChildren<RectTransform>(true).First(x => x.name == "Stats");
                yourStats.gameObject.SetActive(true);

                //RectTransform buttonsRect = GetComponentsInChildren<RectTransform>().First(x => x.name == "PlayButtons");
                //buttonsRect.anchoredPosition = new Vector2(0f, 6f);

                TextMeshProUGUI[] _textComponents = GetComponentsInChildren<TextMeshProUGUI>();

                try
                {
                    songNameText = _textComponents.First(x => x.name == "SongNameText");
                    _textComponents.First(x => x.name == "Title").text = "Playlist";
                    songNameText.enableWordWrapping = true;

                    _textComponents.First(x => x.name == "Title" && x.transform.parent.name == "MaxCombo").text = "Author";
                    authorText = _textComponents.First(x => x.name == "Value" && x.transform.parent.name == "MaxCombo");
                    authorText.rectTransform.sizeDelta = new Vector2(24f, 0f);

                    _textComponents.First(x => x.name == "Title" && x.transform.parent.name == "Highscore").text = "Total songs";
                    totalSongsText = _textComponents.First(x => x.name == "Value" && x.transform.parent.name == "Highscore");

                    _textComponents.First(x => x.name == "Title" && x.transform.parent.name == "MaxRank").text = "Downloaded";
                    downloadedSongsText = _textComponents.First(x => x.name == "Value" && x.transform.parent.name == "MaxRank");
                }
                catch (Exception e)
                {
                    Logger.Exception("Unable to convert detail view controller! Exception:  " + e);
                }

                _selectButton = _levelDetails.playButton;
                _selectButton.SetButtonText(_selectButtonText);
                _selectButton.ToggleWordWrapping(false);
                _selectButton.onClick.RemoveAllListeners();
                _selectButton.onClick.AddListener(() => { selectButtonPressed?.Invoke(_currentPlaylist); });

                if (addDownloadButton)
                {
                    _downloadButton = _levelDetails.practiceButton;
                    _downloadButton.SetButtonIcon(Sprites.DownloadIcon);
                    _downloadButton.onClick.RemoveAllListeners();
                    _downloadButton.onClick.AddListener(() => { downloadButtonPressed?.Invoke(_currentPlaylist); });
                }
                else
                {
                    Destroy(_levelDetails.practiceButton.gameObject);
                }

                coverImage = _levelDetails.GetPrivateField<Image>("_coverImage");
            }
        }

        public void SetDownloadState(bool downloaded)
        {
            _downloadButton.interactable = !downloaded;
        }

        public void SetSelectButtonState(bool enabled)
        {
            _selectButton.interactable = enabled;
        }

        public void SetSelectButtonText(string text)
        {
            _selectButtonText = text;
            if (_selectButton != null)
            {
                _selectButton.SetButtonText(_selectButtonText);
            }
        }

        public void SetContent(Playlist newPlaylist)
        {
            _currentPlaylist = newPlaylist;

            songNameText.text = newPlaylist.playlistTitle;

            authorText.text = newPlaylist.playlistAuthor;

            coverImage.sprite = _currentPlaylist.icon;

            if (newPlaylist.songs.Count > 0)
            {
                totalSongsText.text = newPlaylist.songs.Count.ToString();
                downloadedSongsText.text = newPlaylist.songs.Where(x => x.level != null).Count().ToString();
                SetDownloadState(newPlaylist.songs.All(x => x.level != null));
            }
            else
            {
                totalSongsText.text = newPlaylist.playlistSongCount.ToString();
                downloadedSongsText.text = "??";
            }
        }

        void RemoveCustomUIElements(Transform parent)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);

                if (child.name.StartsWith("CustomUI"))
                {
                    Destroy(child.gameObject);
                }
                if (child.childCount > 0)
                {
                    RemoveCustomUIElements(child);
                }
            }
        }
    }
}
