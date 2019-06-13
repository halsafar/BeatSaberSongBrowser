using HMUI;
using SongBrowser.DataAccess;
using SongBrowser.Internals;
using SongCore.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRUI;
using Logger = SongBrowser.Logging.Logger;

namespace SongBrowser.UI
{
    class PlaylistListViewController : VRUIViewController, TableView.IDataSource
    {
        public event Action<Playlist> didSelectRow;

        public List<Playlist> playlistList = new List<Playlist>();

        public bool highlightDownloadedPlaylists = false;

        private Button _pageUpButton;
        private Button _pageDownButton;

        private TableView _songsTableView;
        private LevelListTableCell _songListTableCellInstance;

        private int _lastSelectedRow;

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            if (firstActivation && type == ActivationType.AddedToHierarchy)
            {
                rectTransform.anchorMin = new Vector2(0.5f, 0f);
                rectTransform.anchorMax = new Vector2(0.5f, 1f);
                rectTransform.sizeDelta = new Vector2(75f, 0f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);

                _pageUpButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().Last(x => (x.name == "PageUpButton")), rectTransform, false);
                (_pageUpButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 1f);
                (_pageUpButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 1f);
                (_pageUpButton.transform as RectTransform).anchoredPosition = new Vector2(0f, -9f);
                (_pageUpButton.transform as RectTransform).sizeDelta = new Vector2(40f, 10f);
                _pageUpButton.interactable = true;
                _pageUpButton.onClick.AddListener(delegate ()
                {
                    _songsTableView.PageScrollUp();
                });

                _pageDownButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageDownButton")), rectTransform, false);
                (_pageDownButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 0f);
                (_pageDownButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 0f);
                (_pageDownButton.transform as RectTransform).anchoredPosition = new Vector2(0f, 9f);
                (_pageDownButton.transform as RectTransform).sizeDelta = new Vector2(40f, 10f);
                _pageDownButton.interactable = true;
                _pageDownButton.onClick.AddListener(delegate ()
                {
                    _songsTableView.PageScrollDown();
                });

                _songListTableCellInstance = Resources.FindObjectsOfTypeAll<LevelListTableCell>().First(x => (x.name == "LevelListTableCell"));

                RectTransform container = new GameObject("CustomListContainer", typeof(RectTransform)).transform as RectTransform;
                container.SetParent(rectTransform, false);
                container.anchorMin = new Vector2(0f, 0.5f);
                container.anchorMax = new Vector2(1f, 0.5f);
                container.sizeDelta = new Vector2(0f, 0f);
                container.anchoredPosition = new Vector2(0f, 0f);

                var gameObject = new GameObject("CustomTableView", typeof(RectTransform));
                gameObject.SetActive(false);
                _songsTableView = gameObject.AddComponent<TableView>();
                _songsTableView.gameObject.AddComponent<RectMask2D>();
                _songsTableView.transform.SetParent(container, false);
                _songsTableView.SetPrivateField("_isInitialized", false);
                _songsTableView.SetPrivateField("_preallocatedCells", new TableView.CellsGroup[0]);
                _songsTableView.Init();
                gameObject.SetActive(true);

                (_songsTableView.transform as RectTransform).anchorMin = new Vector2(0f, 0f);
                (_songsTableView.transform as RectTransform).anchorMax = new Vector2(1f, 1f);
                (_songsTableView.transform as RectTransform).sizeDelta = new Vector2(0f, 60f);
                (_songsTableView.transform as RectTransform).anchoredPosition = new Vector2(0f, 0f);

                _songsTableView.dataSource = this;
                _songsTableView.ScrollToCellWithIdx(0, TableView.ScrollPositionType.Beginning, false);
                _lastSelectedRow = -1;
                _songsTableView.didSelectCellWithIdxEvent += _songsTableView_DidSelectRowEvent;
            }
            else
            {
                _songsTableView.ReloadData();
                _songsTableView.ScrollToCellWithIdx(0, TableView.ScrollPositionType.Beginning, false);
                _lastSelectedRow = -1;
            }
        }

        internal void Refresh()
        {
            _songsTableView.RefreshTable();
        }

        protected override void DidDeactivate(DeactivationType type)
        {
            _lastSelectedRow = -1;
        }

        public void SetContent(List<Playlist> playlists)
        {
            if (playlists == null && playlistList != null)
                playlistList.Clear();
            else
                playlistList = new List<Playlist>(playlists);

            if (_songsTableView != null)
            {
                _songsTableView.ReloadData();
                _songsTableView.ScrollToCellWithIdx(0, TableView.ScrollPositionType.Center, false);
            }
        }

        private void _songsTableView_DidSelectRowEvent(TableView sender, int row)
        {
            _lastSelectedRow = row;
            didSelectRow?.Invoke(playlistList[row]);
        }

        public float CellSize()
        {
            return 10f;
        }

        public int NumberOfCells()
        {
            return playlistList.Count;
        }

        public TableCell CellForIdx(int row)
        {
            LevelListTableCell _tableCell = Instantiate(_songListTableCellInstance);

            _tableCell.reuseIdentifier = "PlaylistTableCell";
            var songNameText = _tableCell.GetPrivateField<TextMeshProUGUI>("_songNameText");
            songNameText.text = playlistList[row].playlistTitle;
            songNameText.overflowMode = TextOverflowModes.Overflow;
            _tableCell.GetPrivateField<TextMeshProUGUI>("_authorText").text = playlistList[row].playlistAuthor;
            if (playlistList[row].icon != null)
            {
                _tableCell.GetPrivateField<RawImage>("_coverRawImage").texture = playlistList[row].icon.texture;
            }
            _tableCell.SetPrivateField("_beatmapCharacteristicAlphas", new float[0]);
            _tableCell.SetPrivateField("_beatmapCharacteristicImages", new UnityEngine.UI.Image[0]);
            _tableCell.SetPrivateField("_bought", true);

            foreach (var icon in _tableCell.GetComponentsInChildren<UnityEngine.UI.Image>().Where(x => x.name.StartsWith("LevelTypeIcon")))
            {
                Destroy(icon.gameObject);
            }

            if (highlightDownloadedPlaylists)
            {
                if (PlaylistsCollection.loadedPlaylists.Any(x => x.PlaylistEqual(playlistList[row])))
                {
                    foreach (UnityEngine.UI.Image img in _tableCell.GetComponentsInChildren<UnityEngine.UI.Image>())
                    {
                        img.color = new Color(1f, 1f, 1f, 0.2f);
                    }
                    foreach (TextMeshProUGUI text in _tableCell.GetComponentsInChildren<TextMeshProUGUI>())
                    {
                        text.faceColor = new Color(1f, 1f, 1f, 0.2f);
                    }
                }
            }

            return _tableCell;
        }
    }
}
