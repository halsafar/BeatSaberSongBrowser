using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SongCore.Utilities;


namespace SongBrowser.UI
{
    /// <summary>
    /// Taken from SongCore, modified
    /// </summary>
    public class ProgressBar : MonoBehaviour
    {
        private Canvas _canvas;
        private TMP_Text _authorNameText;
        private TMP_Text _pluginNameText;
        private TMP_Text _headerText;
        internal Image _loadingBackg;

        private static readonly Vector3 Position = new Vector3(0, -0.85f, 2.5f);
        private static readonly Vector3 Rotation = new Vector3(0, 0, 0);
        private static readonly Vector3 Scale = new Vector3(0.01f, 0.01f, 0.01f);

        private static readonly Vector2 CanvasSize = new Vector2(200, 50);

        private const string AuthorNameText = "Halsafar";
        private const float AuthorNameFontSize = 7f;
        private static readonly Vector2 AuthorNamePosition = new Vector2(10, 31);

        private const string PluginNameText = "Song Browser - v<size=100%>" + Plugin.VERSION_NUMBER + "</size>";
        private const float PluginNameFontSize = 9f;
        private static readonly Vector2 PluginNamePosition = new Vector2(10, 23);

        private static readonly Vector2 HeaderPosition = new Vector2(10, 15);
        private static readonly Vector2 HeaderSize = new Vector2(100, 20);
        private const string HeaderText = "Processing songs...";
        private const float HeaderFontSize = 15f;

        private static readonly Vector2 LoadingBarSize = new Vector2(100, 10);
        private static readonly Color BackgroundColor = new Color(0, 0, 0, 0.2f);

        private bool _showingMessage;

        public static ProgressBar Create()
        {
            return new GameObject("Progress Bar").AddComponent<ProgressBar>();
        }

        public void ShowMessage(string message, float time)
        {
            StopAllCoroutines();
            _showingMessage = true;
            _headerText.text = message;
            _loadingBackg.enabled = false;
            _canvas.enabled = true;
            StartCoroutine(DisableCanvasRoutine(time));
        }

        public void ShowMessage(string message)
        {
            StopAllCoroutines();
            _showingMessage = true;
            _headerText.text = message;
            _loadingBackg.enabled = false;
            _canvas.enabled = true;
        }

        private void OnEnable()
        {
            SceneManager.activeSceneChanged += SceneManagerOnActiveSceneChanged;
            SongBrowserModel.didFinishProcessingSongs += SongBrowserFinishedProcessingSongs;
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= SceneManagerOnActiveSceneChanged;
            SongBrowserModel.didFinishProcessingSongs -= SongBrowserFinishedProcessingSongs;
        }

        private void SceneManagerOnActiveSceneChanged(Scene oldScene, Scene newScene)
        {
            if (newScene.name == "MenuCore")
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

        private void SongBrowserFinishedProcessingSongs(Dictionary<string, CustomPreviewBeatmapLevel> arg2)
        {
            StopAllCoroutines();
            _showingMessage = false;
            _headerText.text = arg2.Count + " songs processed";
            _loadingBackg.enabled = false;
            StartCoroutine(DisableCanvasRoutine(8f));
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

            _authorNameText = Utils.CreateText(_canvas.transform as RectTransform, AuthorNameText, AuthorNamePosition);
            rectTransform = _authorNameText.transform as RectTransform;
            rectTransform.SetParent(_canvas.transform, false);
            rectTransform.anchoredPosition = AuthorNamePosition;
            rectTransform.sizeDelta = HeaderSize;
            _authorNameText.text = AuthorNameText;
            _authorNameText.fontSize = AuthorNameFontSize;

            _pluginNameText = Utils.CreateText(_canvas.transform as RectTransform, PluginNameText, PluginNamePosition);
            rectTransform = _pluginNameText.transform as RectTransform;
            rectTransform.SetParent(_canvas.transform, false);
            rectTransform.sizeDelta = HeaderSize;
            rectTransform.anchoredPosition = PluginNamePosition;
            _pluginNameText.text = PluginNameText;
            _pluginNameText.fontSize = PluginNameFontSize;

            _headerText = Utils.CreateText(_canvas.transform as RectTransform, HeaderText, HeaderPosition);
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

            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (!_canvas.enabled) return;
        }
    }
}
