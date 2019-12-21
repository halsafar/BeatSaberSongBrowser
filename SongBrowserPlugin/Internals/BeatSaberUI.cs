using BS_Utils.Utilities;
using HMUI;
using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;
using Logger = SongBrowser.Logging.Logger;


namespace SongBrowser.Internals
{
    public static class BeatSaberUI
    {
        private static Button _backButtonInstance;

        /// <summary>
        /// Creates a ViewController of type T, and marks it to not be destroyed.
        /// </summary>
        /// <typeparam name="T">The variation of ViewController you want to create.</typeparam>
        /// <returns>The newly created ViewController of type T.</returns>
        public static T CreateViewController<T>(string name="CustomViewController") where T : ViewController
        {
            T vc = new GameObject(name).AddComponent<T>();
            UnityEngine.GameObject.DontDestroyOnLoad(vc.gameObject);

            vc.rectTransform.anchorMin = new Vector2(0f, 0f);
            vc.rectTransform.anchorMax = new Vector2(1f, 1f);
            vc.rectTransform.sizeDelta = new Vector2(0f, 0f);
            vc.rectTransform.anchoredPosition = new Vector2(0f, 0f);

            return vc;
        }

        /// <summary>
        /// Clone a Unity Button into a Button we control.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="buttonTemplate"></param>
        /// <param name="buttonInstance"></param>
        /// <returns></returns>
        static public Button CreateUIButton(RectTransform parent, Button buttonTemplate)
        {
            Button btn = UnityEngine.Object.Instantiate(buttonTemplate, parent, false);
            UnityEngine.Object.DestroyImmediate(btn.GetComponent<SignalOnUIButtonClick>());
            btn.onClick = new Button.ButtonClickedEvent();
            btn.name = "CustomUIButton";

            return btn;
        }

        /// <summary>
        /// Create an icon button, simple.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="buttonTemplate"></param>
        /// <param name="iconSprite"></param>
        /// <returns></returns>
        public static Button CreateIconButton(RectTransform parent, Button buttonTemplate, Sprite iconSprite)
        {
            Button newButton = BeatSaberUI.CreateUIButton(parent, buttonTemplate);
            newButton.interactable = true;

            RectTransform textRect = newButton.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(c => c.name == "Text");
            if (textRect != null)
            {
                UnityEngine.Object.Destroy(textRect.gameObject);
            }

            newButton.SetButtonIcon(iconSprite);
            newButton.onClick.RemoveAllListeners();

            return newButton;
        }

        /// <summary>
        /// Creates a copy of a template button and returns it.
        /// </summary>
        /// <param name="parent">The transform to parent the button to.</param>
        /// <param name="buttonTemplate">The name of the button to make a copy of. Example: "QuitButton", "PlayButton", etc.</param>
        /// <param name="anchoredPosition">The position the button should be anchored to.</param>
        /// <param name="sizeDelta">The size of the buttons RectTransform.</param>
        /// <param name="onClick">Callback for when the button is pressed.</param>
        /// <param name="buttonText">The text that should be shown on the button.</param>
        /// <param name="icon">The icon that should be shown on the button.</param>
        /// <returns>The newly created button.</returns>
        public static Button CreateUIButton(RectTransform parent, string buttonTemplate, Vector2 anchoredPosition, Vector2 sizeDelta, UnityAction onClick = null, string buttonText = "BUTTON", Sprite icon = null)
        {
            Button btn = UnityEngine.Object.Instantiate(Resources.FindObjectsOfTypeAll<Button>().Last(x => (x.name == buttonTemplate)), parent, false);
            btn.onClick = new Button.ButtonClickedEvent();
            if (onClick != null)
                btn.onClick.AddListener(onClick);
            btn.name = "CustomUIButton";

            (btn.transform as RectTransform).anchorMin = new Vector2(0.5f, 0.5f);
            (btn.transform as RectTransform).anchorMax = new Vector2(0.5f, 0.5f);
            (btn.transform as RectTransform).anchoredPosition = anchoredPosition;
            (btn.transform as RectTransform).sizeDelta = sizeDelta;

            btn.SetButtonText(buttonText);
            if (icon != null)
                btn.SetButtonIcon(icon);

            return btn;
        }

        /// <summary>
        /// Creates a copy of a template button and returns it.
        /// </summary>
        /// <param name="parent">The transform to parent the button to.</param>
        /// <param name="buttonTemplate">The name of the button to make a copy of. Example: "QuitButton", "PlayButton", etc.</param>
        /// <param name="anchoredPosition">The position the button should be anchored to.</param>
        /// <param name="onClick">Callback for when the button is pressed.</param>
        /// <param name="buttonText">The text that should be shown on the button.</param>
        /// <param name="icon">The icon that should be shown on the button.</param>
        /// <returns>The newly created button.</returns>
        public static Button CreateUIButton(RectTransform parent, string buttonTemplate, Vector2 anchoredPosition, UnityAction onClick = null, string buttonText = "BUTTON", Sprite icon = null)
        {
            Button btn = UnityEngine.Object.Instantiate(Resources.FindObjectsOfTypeAll<Button>().Last(x => (x.name == buttonTemplate)), parent, false);
            btn.onClick = new Button.ButtonClickedEvent();
            if (onClick != null)
                btn.onClick.AddListener(onClick);
            btn.name = "CustomUIButton";

            (btn.transform as RectTransform).anchorMin = new Vector2(0.5f, 0.5f);
            (btn.transform as RectTransform).anchorMax = new Vector2(0.5f, 0.5f);
            (btn.transform as RectTransform).anchoredPosition = anchoredPosition;

            btn.SetButtonText(buttonText);
            if (icon != null)
                btn.SetButtonIcon(icon);

            return btn;
        }


        /// <summary>
        /// Creates a copy of a template button and returns it.
        /// </summary>
        /// <param name="parent">The transform to parent the button to.</param>
        /// <param name="buttonTemplate">The name of the button to make a copy of. Example: "QuitButton", "PlayButton", etc.</param>
        /// <param name="onClick">Callback for when the button is pressed.</param>
        /// <param name="buttonText">The text that should be shown on the button.</param>
        /// <param name="icon">The icon that should be shown on the button.</param>
        /// <returns>The newly created button.</returns>
        public static Button CreateUIButton(RectTransform parent, string buttonTemplate, UnityAction onClick = null, string buttonText = "BUTTON", Sprite icon = null)
        {
            Button btn = UnityEngine.Object.Instantiate(Resources.FindObjectsOfTypeAll<Button>().Last(x => (x.name == buttonTemplate)), parent, false);
            btn.onClick = new Button.ButtonClickedEvent();
            if (onClick != null)
                btn.onClick.AddListener(onClick);
            btn.name = "CustomUIButton";

            (btn.transform as RectTransform).anchorMin = new Vector2(0.5f, 0.5f);
            (btn.transform as RectTransform).anchorMax = new Vector2(0.5f, 0.5f);
            btn.SetButtonText(buttonText);
            if (icon != null)
                btn.SetButtonIcon(icon);
            return btn;
        }

        /// <summary>
        /// Creates a copy of a back button.
        /// </summary>
        /// <param name="parent">The transform to parent the new button to.</param>
        /// <param name="onClick">Callback for when the button is pressed.</param>
        /// <returns>The newly created back button.</returns>
        public static Button CreateBackButton(RectTransform parent, UnityAction onClick = null)
        {
            if (_backButtonInstance == null)
            {
                try
                {
                    _backButtonInstance = Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "BackArrowButton"));
                }
                catch
                {
                    return null;
                }
            }

            Button btn = UnityEngine.GameObject.Instantiate(_backButtonInstance, parent, false);
            btn.onClick = new Button.ButtonClickedEvent();
            if (onClick != null)
                btn.onClick.AddListener(onClick);
            btn.name = "CustomUIButton";

            return btn;
        }

        /// <summary>
        /// Creates a TextMeshProUGUI component.
        /// </summary>
        /// <param name="parent">Thet ransform to parent the new TextMeshProUGUI component to.</param>
        /// <param name="text">The text to be displayed.</param>
        /// <param name="anchoredPosition">The position the button should be anchored to.</param>
        /// <returns>The newly created TextMeshProUGUI component.</returns>
        public static TextMeshProUGUI CreateText(RectTransform parent, string text, Vector2 anchoredPosition)
        {
            return CreateText(parent, text, anchoredPosition, new Vector2(60f, 10f));
        }

        /// <summary>
        /// Creates a TextMeshProUGUI component.
        /// </summary>
        /// <param name="parent">Thet transform to parent the new TextMeshProUGUI component to.</param>
        /// <param name="text">The text to be displayed.</param>
        /// <param name="anchoredPosition">The position the text component should be anchored to.</param>
        /// <param name="sizeDelta">The size of the text components RectTransform.</param>
        /// <returns>The newly created TextMeshProUGUI component.</returns>
        public static TextMeshProUGUI CreateText(RectTransform parent, string text, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            GameObject gameObj = new GameObject("CustomUIText");
            gameObj.SetActive(false);

            TextMeshProUGUI textMesh = gameObj.AddComponent<TextMeshProUGUI>();
            textMesh.font = UnityEngine.GameObject.Instantiate(Resources.FindObjectsOfTypeAll<TMP_FontAsset>().First(t => t.name == "Teko-Medium SDF No Glow"));
            textMesh.rectTransform.SetParent(parent, false);
            textMesh.text = text;
            textMesh.fontSize = 4;
            textMesh.color = Color.white;

            textMesh.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            textMesh.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            textMesh.rectTransform.sizeDelta = sizeDelta;
            textMesh.rectTransform.anchoredPosition = anchoredPosition;

            gameObj.SetActive(true);
            return textMesh;
        }

        /// <summary>
        /// Replace existing HoverHint on stat panel icons.
        /// </summary>
        /// <param name="button"></param>
        /// <param name="name"></param>
        /// <param name="text"></param>
        public static void SetHoverHint(RectTransform button, string name, string text)
        {
            HoverHintController hoverHintController = Resources.FindObjectsOfTypeAll<HoverHintController>().First();
            DestroyHoverHint(button);
            var newHoverHint = button.gameObject.AddComponent<HoverHint>();
            newHoverHint.SetPrivateField("_hoverHintController", hoverHintController);
            newHoverHint.text = text;
            newHoverHint.name = name;
        }

        /// <summary>
        /// Safely destroy existing hoverhint.
        /// </summary>
        /// <param name="button"></param>
        public static void DestroyHoverHint(RectTransform button)
        {
            HoverHint currentHoverHint = button.GetComponentsInChildren<HMUI.HoverHint>().First();
            if (currentHoverHint != null)
            {
                UnityEngine.GameObject.DestroyImmediate(currentHoverHint);
            }
        }

        /// <summary>
        /// Adjust button text size.
        /// </summary>
        /// <param name="button"></param>
        /// <param name="fontSize"></param>
        static public void SetButtonTextColor(Button button, Color color)
        {
            TextMeshProUGUI txt = button.GetComponentsInChildren<TextMeshProUGUI>().FirstOrDefault(x => x.name == "Text");
            if (txt != null)
            {
                txt.color = color;
            }
        }

        /// <summary>
        /// Adjust button border.
        /// </summary>
        /// <param name="button"></param>
        /// <param name="color"></param>
        static public void SetButtonBorder(Button button, Color color)
        {
            Image img = button.GetComponentsInChildren<Image>().FirstOrDefault(x => x.name == "Stroke");
            if (img != null)
            {
                img.color = color;
            }
        }

        /// <summary>
        /// Adjust button border.
        /// </summary>
        /// <param name="button"></param>
        /// <param name="color"></param>
        static public void SetButtonBorderActive(Button button, bool active)
        {
            Image img = button.GetComponentsInChildren<Image>().FirstOrDefault(x => x.name == "Stroke");
            if (img != null)
            {
                img.gameObject.SetActive(active);
            }
        }

        /// <summary>
        /// Find and adjust a stat panel item text fields.
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="text"></param>
        static public void SetStatButtonText(RectTransform rect, String text)
        {
            TextMeshProUGUI txt = rect.GetComponentsInChildren<TextMeshProUGUI>().FirstOrDefault(x => x.name == "ValueText");
            if (txt != null)
            {
                txt.text = text;
            }
        }

        /// <summary>
        /// Find and adjust a stat panel item icon.
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="icon"></param>
        static public void SetStatButtonIcon(RectTransform rect, Sprite icon)
        {
            Image img = rect.GetComponentsInChildren<Image>().FirstOrDefault(x => x.name == "Icon");
            if (img != null)
            {
                img.sprite = icon;
                img.color = Color.white;
            }
        }
    }
}
