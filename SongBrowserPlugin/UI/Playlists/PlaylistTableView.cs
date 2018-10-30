using HMUI;
using SongBrowserPlugin.DataAccess;
using SongLoaderPlugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace SongBrowserPlugin.UI
{
    public class PlaylistTableView : MonoBehaviour, TableView.IDataSource
    {
        public const String Name = "PlaylistTableView";

        private Logger _log = new Logger(Name);

        private StandardLevelListTableCell _cellInstanceTemplate;

        protected TableView _tableView;

        private RectTransform _rect;

        private PlaylistsReader _reader;

        private int _selectedRow;

        public event Action<PlaylistTableView, int> didSelectPlaylistEvent;

        [SerializeField]
        protected float _cellHeight = 12f;

        /// <summary>
        /// Constructor.
        /// </summary>
        public PlaylistTableView()
        {
            
        }

        public virtual void OnDestroy()
        {
            _log.Trace("OnDestroy()");
            Destroy(this._tableView);
        }

        /// <summary>
        /// Setup the tableview.
        /// </summary>
        /// <param name="parent"></param>
        public void Init(RectTransform parent, PlaylistsReader reader)
        {
            _rect = parent;
            _reader = reader;

            try
            {
                _cellInstanceTemplate = Resources.FindObjectsOfTypeAll<StandardLevelListTableCell>().First(x => (x.name == "StandardLevelListTableCell"));

                if (_tableView == null)
                {
                    _tableView = new GameObject().AddComponent<TableView>();
                    _tableView.Awake();
                    _tableView.transform.SetParent(parent, false);

                    Mask viewportMask = Instantiate(Resources.FindObjectsOfTypeAll<Mask>().First(), _tableView.transform, false);
                    viewportMask.transform.DetachChildren();
                    _tableView.GetComponentsInChildren<RectTransform>().First(x => x.name == "Content").transform.SetParent(viewportMask.rectTransform, false);

                    (_tableView.transform as RectTransform).anchorMin = new Vector2(0f, 0.5f);
                    (_tableView.transform as RectTransform).anchorMax = new Vector2(1f, 0.5f);
                    (_tableView.transform as RectTransform).sizeDelta = new Vector2(0f, 60f);
                    (_tableView.transform as RectTransform).position = new Vector3(0f, 0f, 2.4f);
                    (_tableView.transform as RectTransform).anchoredPosition = new Vector3(0f, -3f);

                    Button pageUpButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageUpButton")), parent, false);
                    Button pageDownButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageDownButton")), parent, false);

                    (pageUpButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 1f);
                    (pageUpButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 1f);
                    (pageUpButton.transform as RectTransform).anchoredPosition = new Vector2(0f, -14f);
                    pageUpButton.interactable = true;
                    pageUpButton.onClick.AddListener(delegate ()
                    {
                        _tableView.PageScrollUp();
                    });

                    (pageDownButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 0f);
                    (pageDownButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 0f);
                    (pageDownButton.transform as RectTransform).anchoredPosition = new Vector2(0f, 8f);
                    pageDownButton.interactable = true;
                    pageDownButton.onClick.AddListener(delegate ()
                    {
                        _tableView.PageScrollDown();
                    });

                    _tableView.SetPrivateField("_pageUpButton", pageUpButton);
                    _tableView.SetPrivateField("_pageDownButton", pageDownButton);
                }

                this._tableView.didSelectRowEvent += this.HandleDidSelectRowEvent;
                this._tableView.dataSource = this;

                _log.Debug("Initialized PlaylistTableView");
            }
            catch (Exception e)
            {
                _log.Exception("Exception initializing playlist table view: ", e);
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tableView"></param>
        /// <param name="row"></param>
        public virtual void HandleDidSelectRowEvent(TableView tableView, int row)
        {
            _log.Debug("HandleDidSelectRowEvent - Row: {0}", row);
            if (this.didSelectPlaylistEvent != null)
            {
                this.didSelectPlaylistEvent(this, row);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public float RowHeight()
        {
            return this._cellHeight;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public int NumberOfRows()
        {
            if (this._reader == null)
            {
                return 0;
            }
            return this._reader.Playlists.Count;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public TableCell CellForRow(int row)
        {
            _log.Debug("CellForRow({0})", row);
            try
            {
                Playlist p = _reader.Playlists[row];
                StandardLevelListTableCell tableCell = Instantiate(_cellInstanceTemplate, this._tableView.transform, false);
                tableCell.coverImage = Base64Sprites.Base64ToSprite(p.image);
                tableCell.songName = p.playlistTitle;
                tableCell.author = p.playlistAuthor;

                return tableCell;
            }
            catch (Exception e)
            {
                _log.Exception("Exception getting cell for row", e);
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        private void CheckDebugUserInput()
        {
            if (Input.GetKeyDown(KeyCode.N) && Input.GetKey(KeyCode.LeftShift))
            {
                _tableView.PageScrollUp();
            }
            else if (Input.GetKeyDown(KeyCode.N))
            {
                _selectedRow = (_selectedRow - 1) % this._reader.Playlists.Count;
                if (_selectedRow < 0)
                {
                    _selectedRow = this._reader.Playlists.Count-1;
                }
                _tableView.ScrollToRow(_selectedRow, true);
                this.HandleDidSelectRowEvent(_tableView, _selectedRow);
            }

            if (Input.GetKeyDown(KeyCode.M) && Input.GetKey(KeyCode.LeftShift))
            {
                _tableView.PageScrollDown();
            }
            else if (Input.GetKeyDown(KeyCode.M))
            {
                _selectedRow = (_selectedRow + 1) % this._reader.Playlists.Count;
                _tableView.ScrollToRow(_selectedRow, true);
                this.HandleDidSelectRowEvent(_tableView, _selectedRow);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void Update()
        {
            CheckDebugUserInput();
        }
    }
}
