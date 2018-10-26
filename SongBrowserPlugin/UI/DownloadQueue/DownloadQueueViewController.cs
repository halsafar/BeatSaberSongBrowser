using HMUI;
using SongBrowserPlugin.DataAccess.BeatSaverApi;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRUI;


// From: https://github.com/andruzzzhka/BeatSaverDownloader
namespace SongBrowserPlugin.UI.DownloadQueue
{
    public class DownloadQueueViewController : VRUIViewController, TableView.IDataSource
    {
        private Logger _log = new Logger("DownloadQueueViewController");

        public Action allSongsDownloaded;
        public List<Song> _queuedSongs = new List<Song>();

        TextMeshProUGUI _titleText;

        Button _abortButton;
        TableView _queuedSongsTableView;
        StandardLevelListTableCell _songListTableCellInstance;

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {

            _songListTableCellInstance = Resources.FindObjectsOfTypeAll<StandardLevelListTableCell>().First(x => (x.name == "StandardLevelListTableCell"));

            if (_titleText == null)
            {
                _titleText = UIBuilder.CreateText(rectTransform, "DOWNLOAD QUEUE", new Vector2(0f, -6f), new Vector2(60f, 10f));
                _titleText.alignment = TextAlignmentOptions.Top;
                _titleText.fontSize = 8;
            }

            if (_queuedSongsTableView == null)
            {
                _queuedSongsTableView = new GameObject().AddComponent<TableView>();

                _queuedSongsTableView.transform.SetParent(rectTransform, false);

                _queuedSongsTableView.dataSource = this;

                (_queuedSongsTableView.transform as RectTransform).anchorMin = new Vector2(0.3f, 0.5f);
                (_queuedSongsTableView.transform as RectTransform).anchorMax = new Vector2(0.7f, 0.5f);
                (_queuedSongsTableView.transform as RectTransform).sizeDelta = new Vector2(0f, 60f);
                (_queuedSongsTableView.transform as RectTransform).anchoredPosition = new Vector3(0f, -3f);
            }

            if (_abortButton == null)
            {
                _abortButton = UIBuilder.CreateUIButton(rectTransform, "SettingsButton");
                UIBuilder.SetButtonText(ref _abortButton, "Abort All");

                (_abortButton.transform as RectTransform).sizeDelta = new Vector2(30f, 10f);
                (_abortButton.transform as RectTransform).anchoredPosition = new Vector2(-4f, 6f);

                _abortButton.onClick.AddListener(delegate () {
                    AbortDownloads();
                });
            }

            AbortDownloads();
        }

        public void AbortDownloads()
        {
            _log.Info("Cancelling downloads...");
            foreach (Song song in _queuedSongs.Where(x => x.songQueueState == SongQueueState.Downloading || x.songQueueState == SongQueueState.Queued))
            {
                song.songQueueState = SongQueueState.Error;
                song.downloadingProgress = 1f;
            }
            Refresh();
            allSongsDownloaded?.Invoke();
        }

        protected override void DidDeactivate(DeactivationType type)
        {

        }

        public void EnqueueSong(Song song, bool startDownload = true)
        {
            _queuedSongs.Add(song);
            song.songQueueState = SongQueueState.Queued;

            Refresh();

            if (startDownload)
                StartCoroutine(DownloadSong(song));
        }

        public void DownloadAllSongsFromQueue()
        {
            _log.Info("Downloading all songs from queue...");

            for (int i = 0; i < Math.Min(_queuedSongs.Count(x => x.songQueueState == SongQueueState.Queued), 4); i++)
            {
                StartCoroutine(DownloadSong(_queuedSongs[i]));
            }
        }

        IEnumerator DownloadSong(Song song)
        {
            yield return Downloader.Instance.DownloadSongCoroutine(song);

            _queuedSongs.Remove(song);
            song.songQueueState = SongQueueState.Available;
            Refresh();

            if (!_queuedSongs.Any(x => x.songQueueState == SongQueueState.Downloading || x.songQueueState == SongQueueState.Queued))
            {
                allSongsDownloaded?.Invoke();
            }
            else
            {
                if (_queuedSongs.Any(x => x.songQueueState == SongQueueState.Queued))
                {
                    StartCoroutine(DownloadSong(_queuedSongs.First(x => x.songQueueState == SongQueueState.Queued)));
                }
            }
        }

        public void Refresh()
        {
            int removed = _queuedSongs.RemoveAll(x => x.songQueueState != SongQueueState.Downloading && x.songQueueState != SongQueueState.Queued);

            _log.Debug($"Removed {removed} songs from queue");

            _queuedSongsTableView.ReloadData();
            _queuedSongsTableView.ScrollToRow(0, true);
        }

        public float RowHeight()
        {
            return 10f;
        }

        public int NumberOfRows()
        {
            return _queuedSongs.Count;
        }

        public TableCell CellForRow(int row)
        {
            StandardLevelListTableCell _tableCell = Instantiate(_songListTableCellInstance);

            DownloadQueueTableCell _queueCell = _tableCell.gameObject.AddComponent<DownloadQueueTableCell>();

            _queueCell.Init(_queuedSongs[row]);

            return _queueCell;
        }
    }
}
