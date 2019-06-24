using CustomUI.BeatSaber;
using SongBrowser.DataAccess;
using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRUI;
using Image = UnityEngine.UI.Image;
using Logger = SongBrowser.Logging.Logger;


namespace SongBrowser.UI
{
    public static class UIBuilder
    {
        /// <summary>
        /// Create an empty BeatSaber VRUI view controller.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public static T CreateViewController<T>(string name) where T : VRUIViewController
        {
            T vc = new GameObject(name).AddComponent<T>();

            vc.rectTransform.anchorMin = new Vector2(0f, 0f);
            vc.rectTransform.anchorMax = new Vector2(1f, 1f);
            vc.rectTransform.sizeDelta = new Vector2(0f, 0f);
            vc.rectTransform.anchoredPosition = new Vector2(0f, 0f);

            return vc;
        }

        /// <summary>
        /// Create empty FlowCoordinator
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public static T CreateFlowCoordinator<T>(string name) where T : FlowCoordinator
        {
            T vc = new GameObject(name).AddComponent<T>();
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
            Button newButton = UIBuilder.CreateUIButton(parent, buttonTemplate);
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
