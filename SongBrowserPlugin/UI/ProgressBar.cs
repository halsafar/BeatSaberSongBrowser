using HMUI;
using SongLoaderPlugin;
using SongLoaderPlugin.OverrideClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SongBrowserPlugin.UI
{
    // Modified version of:
    // https://raw.githubusercontent.com/xyonico/BeatSaberSongLoader/master/SongLoaderPlugin/ProgressBar.cs
    // - Adjusted position
    // - Repurposed.
    public class ProgressBar : MonoBehaviour
    {
        private Canvas _canvas;
        private TMP_Text _creditText;
        private TMP_Text _headerText;
        private Image _loadingBackg;
        private Image _loadingBar;

        private static readonly Vector3 Position = new Vector3(0, 0f, 2.5f);
        private static readonly Vector3 Rotation = new Vector3(0, 0, 0);
        private static readonly Vector3 Scale = new Vector3(0.01f, 0.01f, 0.01f);

        private static readonly Vector2 CanvasSize = new Vector2(200, 50);

        private static readonly Vector2 CreditPosition = new Vector2(50, 22);
        private const string CreditText = "Song Browser Plugin <size=75%>by Halsafar</size>";
        private const float CreditFontSize = 9f;
        private static readonly Vector2 HeaderPosition = new Vector2(50, 15);
        private static readonly Vector2 HeaderSize = new Vector2(200, 20);
        private const string HeaderText = "Processing songs...";
        private const float HeaderFontSize = 12f;

        private static readonly Vector2 LoadingBarSize = new Vector2(100, 10);
        private static readonly Color BackgroundColor = new Color(0, 0, 0, 0.2f);

        private bool _showingMessage;

        public static ProgressBar Create()
        {
            return new GameObject("Progress Bar").AddComponent<ProgressBar>();
        }

        public void ShowMessage(string message, float time)
        {
            _showingMessage = true;
            _headerText.text = message;
            _loadingBar.enabled = false;
            _loadingBackg.enabled = false;
            _canvas.enabled = true;
            StartCoroutine(DisableCanvasRoutine(time));
        }

        public void ShowMessage(string message)
        {
            _showingMessage = true;
            _headerText.text = message;
            _loadingBar.enabled = false;
            _loadingBackg.enabled = false;
            _canvas.enabled = true;
        }

        private void OnEnable()
        {
            SceneManager.activeSceneChanged += SceneManagerOnActiveSceneChanged;
            SongLoader.SongsLoadedEvent += SongLoaderOnSongsLoadedEvent;
            SongBrowserModel.didFinishProcessingSongs += SongBrowserFinishedProcessingSongs;
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= SceneManagerOnActiveSceneChanged;
            SongLoader.SongsLoadedEvent -= SongLoaderOnSongsLoadedEvent;
            SongBrowserModel.didFinishProcessingSongs -= SongBrowserFinishedProcessingSongs;
        }

        private void SceneManagerOnActiveSceneChanged(Scene arg0, Scene arg1)
        {
            if (arg1.buildIndex == 1)
            {
                if (_showingMessage)
                {
                    _canvas.enabled = true;
                }
            }
            else
            {
                _canvas.enabled = false;
            }
        }

        private void SongLoaderOnSongsLoadedEvent(SongLoader arg1, List<CustomLevel> arg2)
        {
            _showingMessage = false;
            _headerText.text = HeaderText;
            _loadingBar.enabled = true;
            _loadingBackg.enabled = true;
            _canvas.enabled = true;
        }

        private void SongBrowserFinishedProcessingSongs(List<CustomLevel> arg2)
        {
            _showingMessage = false;
            _headerText.text = arg2.Count + " songs processed.";
            _loadingBar.enabled = false;
            _loadingBackg.enabled = false;
            StartCoroutine(DisableCanvasRoutine(5f));
        }

        private IEnumerator DisableCanvasRoutine(float time)
        {
            yield return new WaitForSecondsRealtime(time);
            _canvas.enabled = false;
            _showingMessage = false;
        }

        private void Awake()
        {
            gameObject.transform.position = Position;
            gameObject.transform.eulerAngles = Rotation;
            gameObject.transform.localScale = Scale;

            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.enabled = false;
            var rectTransform = _canvas.transform as RectTransform;
            rectTransform.sizeDelta = CanvasSize;

            _creditText = new GameObject("Credit").AddComponent<TextMeshProUGUI>();
            rectTransform = _creditText.transform as RectTransform;
            rectTransform.SetParent(_canvas.transform, false);
            rectTransform.anchoredPosition = CreditPosition;
            rectTransform.sizeDelta = HeaderSize;
            _creditText.text = CreditText;
            _creditText.fontSize = CreditFontSize;

            _headerText = new GameObject("Header").AddComponent<TextMeshProUGUI>();
            rectTransform = _headerText.transform as RectTransform;
            rectTransform.SetParent(_canvas.transform, false);
            rectTransform.anchoredPosition = HeaderPosition;
            rectTransform.sizeDelta = HeaderSize;
            _headerText.text = HeaderText;
            _headerText.fontSize = HeaderFontSize;

            _loadingBackg = new GameObject("Background").AddComponent<Image>();
            rectTransform = _loadingBackg.transform as RectTransform;
            rectTransform.SetParent(_canvas.transform, false);
            rectTransform.sizeDelta = LoadingBarSize;
            _loadingBackg.color = BackgroundColor;

            _loadingBar = new GameObject("Loading Bar").AddComponent<Image>();
            rectTransform = _loadingBar.transform as RectTransform;
            rectTransform.SetParent(_canvas.transform, false);
            rectTransform.sizeDelta = LoadingBarSize;
            var tex = Texture2D.whiteTexture;
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f, 100, 1);
            _loadingBar.sprite = sprite;
            _loadingBar.type = Image.Type.Filled;
            _loadingBar.fillMethod = Image.FillMethod.Horizontal;
            _loadingBar.color = new Color(1, 1, 1, 0.5f);

            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (!_canvas.enabled) return;

        }
    }
}
