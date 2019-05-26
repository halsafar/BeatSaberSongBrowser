using SongBrowser.DataAccess;
using SongBrowser.DataAccess.BeatSaverApi;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;
using Logger = SongBrowser.Logging.Logger;

// From: https://github.com/andruzzzhka/BeatSaverDownloader
namespace SongBrowser.UI.DownloadQueue
{
    class DownloadQueueTableCell : LevelListTableCell
    {
        Song song;

        protected override void Awake()
        {
            base.Awake();
        }

        public void Init(Song _song)
        {
            Destroy(GetComponent<LevelListTableCell>());

            reuseIdentifier = "DownloadCell";

            song = _song;

            _authorText = GetComponentsInChildren<TextMeshProUGUI>().First(x => x.name == "Author");
            _songNameText = GetComponentsInChildren<TextMeshProUGUI>().First(x => x.name == "SongName");
            _coverRawImage = GetComponentsInChildren<UnityEngine.UI.RawImage>().First(x => x.name == "CoverImage");
            _bgImage = GetComponentsInChildren<UnityEngine.UI.Image>().First(x => x.name == "BG");
            _highlightImage = GetComponentsInChildren<UnityEngine.UI.Image>().First(x => x.name == "Highlight");
            _beatmapCharacteristicAlphas = new float[0];
            _beatmapCharacteristicImages = new UnityEngine.UI.Image[0];
            _bought = true;

            foreach (var icon in GetComponentsInChildren<UnityEngine.UI.Image>().Where(x => x.name.StartsWith("LevelTypeIcon")))
            {
                Destroy(icon.gameObject);
            }

            _songNameText.text = string.Format("{0}\n<size=80%>{1}</size>", song.songName, song.songSubName);
            _authorText.text = song.authorName;
            StartCoroutine(LoadScripts.LoadSpriteCoroutine(song.coverUrl, (cover) => { _coverRawImage.texture = cover.texture; }));

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
