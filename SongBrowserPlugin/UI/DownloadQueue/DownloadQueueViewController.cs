using CustomUI.BeatSaber;
using CustomUI.Utilities;
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
using Logger = SongBrowserPlugin.Logging.Logger;

// Modified From: https://github.com/andruzzzhka/BeatSaverDownloader
// - Adding queue count
namespace SongBrowserPlugin.UI.DownloadQueue
{
    public class DownloadQueueViewController : VRUIViewController, TableView.IDataSource
    {
        public event Action allSongsDownloaded;

        public List<Song> queuedSongs = new List<Song>();

        TextMeshProUGUI _titleText;

        Button _abortButton;
        TableView _queuedSongsTableView;
        LevelListTableCell _songListTableCellInstance;
        private Button _pageUpButton;
        private Button _pageDownButton;

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            if (firstActivation && type == ActivationType.AddedToHierarchy)
            {
                Downloader.Instance.songDownloaded += SongDownloaded;

                _songListTableCellInstance = Resources.FindObjectsOfTypeAll<LevelListTableCell>().First(x => (x.name == "LevelListTableCell"));

                _titleText = BeatSaberUI.CreateText(rectTransform, "DOWNLOAD QUEUE", new Vector2(0f, 36f));
                _titleText.alignment = TextAlignmentOptions.Top;
                _titleText.fontSize = 7;

                _pageUpButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageUpButton")), rectTransform, false);
                (_pageUpButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 1f);
                (_pageUpButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 1f);
                (_pageUpButton.transform as RectTransform).anchoredPosition = new Vector2(0f, -14f);
                (_pageUpButton.transform as RectTransform).sizeDelta = new Vector2(40f, 10f);
                _pageUpButton.interactable = true;
                _pageUpButton.onClick.AddListener(delegate ()
                {
                    _queuedSongsTableView.PageScrollUp();
                });

                _pageDownButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "PageDownButton")), rectTransform, false);
                (_pageDownButton.transform as RectTransform).anchorMin = new Vector2(0.5f, 0f);
                (_pageDownButton.transform as RectTransform).anchorMax = new Vector2(0.5f, 0f);
                (_pageDownButton.transform as RectTransform).anchoredPosition = new Vector2(0f, 8f);
                (_pageDownButton.transform as RectTransform).sizeDelta = new Vector2(40f, 10f);
                _pageDownButton.interactable = true;
                _pageDownButton.onClick.AddListener(delegate ()
                {
                    _queuedSongsTableView.PageScrollDown();
                });

                _queuedSongsTableView = new GameObject().AddComponent<TableView>();
                _queuedSongsTableView.transform.SetParent(rectTransform, false);

                _queuedSongsTableView.SetPrivateField("_isInitialized", false);
                _queuedSongsTableView.SetPrivateField("_preallocatedCells", new TableView.CellsGroup[0]);
                _queuedSongsTableView.Init();

                RectMask2D viewportMask = Instantiate(Resources.FindObjectsOfTypeAll<RectMask2D>().First(), _queuedSongsTableView.transform, false);
                viewportMask.transform.DetachChildren();
                _queuedSongsTableView.GetComponentsInChildren<RectTransform>().First(x => x.name == "Content").transform.SetParent(viewportMask.rectTransform, false);

                (_queuedSongsTableView.transform as RectTransform).anchorMin = new Vector2(0.3f, 0.5f);
                (_queuedSongsTableView.transform as RectTransform).anchorMax = new Vector2(0.7f, 0.5f);
                (_queuedSongsTableView.transform as RectTransform).sizeDelta = new Vector2(0f, 60f);
                (_queuedSongsTableView.transform as RectTransform).anchoredPosition = new Vector3(0f, -3f);

                ReflectionUtil.SetPrivateField(_queuedSongsTableView, "_pageUpButton", _pageUpButton);
                ReflectionUtil.SetPrivateField(_queuedSongsTableView, "_pageDownButton", _pageDownButton);

                _queuedSongsTableView.dataSource = this;

                _abortButton = BeatSaberUI.CreateUIButton(rectTransform, "CreditsButton", new Vector2(36f, -30f), new Vector2(20f, 10f), AbortDownloads, "Abort All");
                _abortButton.ToggleWordWrapping(false);

                Downloader.Instance.songDownloaded += (Song song) => { Refresh(); };
            }
        }

        public void AbortDownloads()
        {
            Logger.Info("Cancelling downloads...");
            foreach (Song song in queuedSongs.Where(x => x.songQueueState == SongQueueState.Downloading || x.songQueueState == SongQueueState.Queued))
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
            queuedSongs.Add(song);
            song.songQueueState = SongQueueState.Queued;
            if (startDownload && queuedSongs.Count(x => x.songQueueState == SongQueueState.Downloading) < PluginConfig.MaxSimultaneousDownloads)
            {
                DownloadSong(song);
            }
            Refresh();
        }

        public void DownloadAllSongsFromQueue()
        {
            Logger.Info("Downloading all songs from queue...");

            for (int i = 0; i < Math.Min(PluginConfig.MaxSimultaneousDownloads, queuedSongs.Count); i++)
            {
                DownloadSong(queuedSongs[i]);
            }
        }

        IEnumerator DownloadSong(Song song)
        {
            yield return Downloader.Instance.DownloadSongCoroutine(song);
            Refresh();
        }

        private void SongDownloaded(Song obj)
        {
            Refresh();
            if (queuedSongs.Count(x => x.songQueueState == SongQueueState.Downloading) < PluginConfig.MaxSimultaneousDownloads && queuedSongs.Any(x => x.songQueueState == SongQueueState.Queued))
            {
                StartCoroutine(DownloadSong(queuedSongs.First(x => x.songQueueState == SongQueueState.Queued)));
            }
        }

        public void Refresh()
        {
            int removed = queuedSongs.RemoveAll(x => x.songQueueState == SongQueueState.Downloaded || x.songQueueState == SongQueueState.Error);

            Logger.Info($"Removed {removed} songs from queue");

            _queuedSongsTableView.ReloadData();
            _queuedSongsTableView.ScrollToRow(0, true);

            if (queuedSongs.Count(x => x.songQueueState == SongQueueState.Downloading || x.songQueueState == SongQueueState.Queued) == 0)
            {
                Logger.Info("All songs downloaded!");
                allSongsDownloaded?.Invoke();
            }

            if (queuedSongs.Count(x => x.songQueueState == SongQueueState.Downloading) < PluginConfig.MaxSimultaneousDownloads && queuedSongs.Any(x => x.songQueueState == SongQueueState.Queued))
            {
                StartCoroutine(DownloadSong(queuedSongs.First(x => x.songQueueState == SongQueueState.Queued)));
            }
        }

        public float RowHeight()
        {
            return 10f;
        }

        public int NumberOfRows()
        {
            return queuedSongs.Count;
        }

        public TableCell CellForRow(int row)
        {
            LevelListTableCell _tableCell = Instantiate(_songListTableCellInstance);

            DownloadQueueTableCell _queueCell = _tableCell.gameObject.AddComponent<DownloadQueueTableCell>();

            _queueCell.Init(queuedSongs[row]);

            return _queueCell;
        }
    }
}
