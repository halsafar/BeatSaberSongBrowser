using HMUI;
using SongBrowserPlugin.DataAccess;
using SongBrowserPlugin.Internals;
using SongLoaderPlugin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRUI;

namespace SongBrowserPlugin.UI
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
                rectTransform.anchorMin = new Vector2(0.3f, 0f);
                rectTransform.anchorMax = new Vector2(0.7f, 1f);

                _pageUpButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageUpButton")), rectTransform, false);
                (_pageUpButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 1f);
                (_pageUpButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 1f);
                (_pageUpButton.transform as RectTransform).anchoredPosition = new Vector2(0f, -14f);
                (_pageUpButton.transform as RectTransform).sizeDelta = new Vector2(40f, 10f);
                _pageUpButton.interactable = true;
                _pageUpButton.onClick.AddListener(delegate ()
                {
                    _songsTableView.PageScrollUp();
                });

                _pageDownButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageDownButton")), rectTransform, false);
                (_pageDownButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 0f);
                (_pageDownButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 0f);
                (_pageDownButton.transform as RectTransform).anchoredPosition = new Vector2(0f, 8f);
                (_pageDownButton.transform as RectTransform).sizeDelta = new Vector2(40f, 10f);
                _pageDownButton.interactable = true;
                _pageDownButton.onClick.AddListener(delegate ()
                {
                    _songsTableView.PageScrollDown();
                });

                _songListTableCellInstance = Resources.FindObjectsOfTypeAll<LevelListTableCell>().First(x => (x.name == "LevelListTableCell"));

                RectTransform container = new GameObject("Content", typeof(RectTransform)).transform as RectTransform;
                container.SetParent(rectTransform, false);
                container.anchorMin = new Vector2(0f, 0.5f);
                container.anchorMax = new Vector2(1f, 0.5f);
                container.sizeDelta = new Vector2(0f, 60f);
                container.anchoredPosition = new Vector2(0f, -3f);

                _songsTableView = new GameObject("CustomTableView").AddComponent<TableView>();
                _songsTableView.gameObject.AddComponent<RectMask2D>();
                _songsTableView.transform.SetParent(container, false);

                _songsTableView.SetPrivateField("_isInitialized", false);
                _songsTableView.SetPrivateField("_preallocatedCells", new TableView.CellsGroup[0]);
                _songsTableView.Init();

                (_songsTableView.transform as RectTransform).anchorMin = new Vector2(0f, 0f);
                (_songsTableView.transform as RectTransform).anchorMax = new Vector2(1f, 1f);
                (_songsTableView.transform as RectTransform).sizeDelta = new Vector2(0f, 0f);
                (_songsTableView.transform as RectTransform).anchoredPosition = new Vector2(0f, 0f);

                _songsTableView.SetPrivateField("_pageUpButton", _pageUpButton);
                _songsTableView.SetPrivateField("_pageDownButton", _pageDownButton);

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
            _tableCell.GetPrivateField<TextMeshProUGUI>("_songNameText").text = playlistList[row].playlistTitle;
            _tableCell.GetPrivateField<TextMeshProUGUI>("_authorText").text = playlistList[row].playlistAuthor;
            _tableCell.GetPrivateField<UnityEngine.UI.Image>("_coverImage").sprite = playlistList[row].icon;

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

#if DEBUG
        private void LateUpdate()
        {
            CheckDebugUserInput();
        }

        private void CheckDebugUserInput()
        {
            bool isShiftKeyDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (Input.GetKeyDown(KeyCode.N) && isShiftKeyDown)
            {
                _songsTableView.PageScrollUp();
            }
            else if (Input.GetKeyDown(KeyCode.N))
            {
                _lastSelectedRow = (_lastSelectedRow - 1) % _songsTableView.dataSource.NumberOfCells();
                if (_lastSelectedRow < 0)
                {
                    _lastSelectedRow = _songsTableView.dataSource.NumberOfCells() - 1;
                }
                _songsTableView.ScrollToCellWithIdx(_lastSelectedRow, TableView.ScrollPositionType.Beginning, false);
                this._songsTableView_DidSelectRowEvent(_songsTableView, _lastSelectedRow);
            }

            if (Input.GetKeyDown(KeyCode.M) && isShiftKeyDown)
            {
                _songsTableView.PageScrollDown();
            }
            else if (Input.GetKeyDown(KeyCode.M))
            {
                _lastSelectedRow = (_lastSelectedRow + 1) % _songsTableView.dataSource.NumberOfCells();
                _songsTableView.ScrollToCellWithIdx(_lastSelectedRow, TableView.ScrollPositionType.End, false);
                this._songsTableView_DidSelectRowEvent(_songsTableView, _lastSelectedRow);
            }
        }
#endif
    }
}
