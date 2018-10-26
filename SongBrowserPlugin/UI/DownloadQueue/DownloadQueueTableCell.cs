using SongBrowserPlugin.DataAccess;
using SongBrowserPlugin.DataAccess.BeatSaverApi;
using System.Reflection;
using UnityEngine;

// From: https://github.com/andruzzzhka/BeatSaverDownloader
namespace SongBrowserPlugin.UI.DownloadQueue
{
    class DownloadQueueTableCell : StandardLevelListTableCell
    {
        Song song;


        protected override void Awake()
        {
            base.Awake();
        }

        public void Init(Song _song)
        {
            StandardLevelListTableCell cell = GetComponent<StandardLevelListTableCell>();

            foreach (FieldInfo info in cell.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic))
            {
                info.SetValue(this, info.GetValue(cell));
            }

            Destroy(cell);

            reuseIdentifier = "DownloadCell";

            song = _song;

            songName = string.Format("{0}\n<size=80%>{1}</size>", song.songName, song.songSubName);
            author = song.authorName;
            StartCoroutine(LoadScripts.LoadSprite(song.coverUrl, this));

            _bgImage.enabled = true;
            _bgImage.sprite = Sprite.Create((new Texture2D(1, 1)), new Rect(0, 0, 1, 1), Vector2.one / 2f);
            _bgImage.type = UnityEngine.UI.Image.Type.Filled;
            _bgImage.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;

            switch (song.songQueueState)
            {
                case SongQueueState.Queued:
                case SongQueueState.Downloading:
                    {
                        _bgImage.color = new Color(1f, 1f, 1f, 0.35f);
                        _bgImage.fillAmount = song.downloadingProgress;
                    }
                    break;
                case SongQueueState.Available:
                case SongQueueState.Downloaded:
                    {
                        _bgImage.color = new Color(1f, 1f, 1f, 0.35f);
                        _bgImage.fillAmount = 1f;
                    }
                    break;
                case SongQueueState.Error:
                    {
                        _bgImage.color = new Color(1f, 0f, 0f, 0.35f);
                        _bgImage.fillAmount = 1f;
                    }
                    break;
            }
        }

        public void Update()
        {

            _bgImage.enabled = true;
            switch (song.songQueueState)
            {
                case SongQueueState.Queued:
                case SongQueueState.Downloading:
                    {
                        _bgImage.color = new Color(1f, 1f, 1f, 0.35f);
                        _bgImage.fillAmount = song.downloadingProgress;
                    }
                    break;
                case SongQueueState.Available:
                case SongQueueState.Downloaded:
                    {
                        _bgImage.color = new Color(1f, 1f, 1f, 0.35f);
                        _bgImage.fillAmount = 1f;
                    }
                    break;
                case SongQueueState.Error:
                    {
                        _bgImage.color = new Color(1f, 0f, 0f, 0.35f);
                        _bgImage.fillAmount = 1f;
                    }
                    break;
            }
        }

    }
}
