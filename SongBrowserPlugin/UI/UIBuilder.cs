using HMUI;
using SongBrowserPlugin.DataAccess;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VRUI;
using Image = UnityEngine.UI.Image;


namespace SongBrowserPlugin.UI
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
        /// Helper, create a UI button from template name.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="buttonTemplateName"></param>
        /// <returns></returns>
        static public Button CreateUIButton(RectTransform parent, String buttonTemplateName)
        {
            Button b = Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == buttonTemplateName));
            return CreateUIButton(parent, b);
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
        /// Very generic helper create button method.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="buttonTemplate"></param>
        /// <param name="buttonText"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        static public Button CreateButton(RectTransform parent, Button buttonTemplate, String buttonText, float fontSize, float x, float y, float w, float h)
        {
            Button newButton = UIBuilder.CreateUIButton(parent, buttonTemplate);

            newButton.interactable = true;
            (newButton.transform as RectTransform).anchoredPosition = new Vector2(x, y);
            (newButton.transform as RectTransform).sizeDelta = new Vector2(w, h);

            UIBuilder.SetButtonText(ref newButton, buttonText);
            UIBuilder.SetButtonIconEnabled(ref newButton, false);
            UIBuilder.SetButtonTextSize(ref newButton, fontSize);

            return newButton;
        }

        /// <summary>
        /// Generic create sort button.
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="templateButtonName"></param>
        /// <param name="buttonText"></param>
        /// <param name="iconName"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="action"></param>
        public static SongSortButton CreateSortButton(RectTransform parent, Button buttonTemplate, Sprite iconSprite, string buttonText, float fontSize, float x, float y, float w, float h, SongSortMode sortMode, System.Action<SongSortMode> onClickEvent)
        {
            SongSortButton sortButton = new SongSortButton();
            Button newButton = UIBuilder.CreateUIButton(parent, buttonTemplate);

            newButton.interactable = true;
            (newButton.transform as RectTransform).anchoredPosition = new Vector2(x, y);
            (newButton.transform as RectTransform).sizeDelta = new Vector2(w, h);

            UIBuilder.SetButtonText(ref newButton, buttonText);
            UIBuilder.SetButtonIconEnabled(ref newButton, false);
            UIBuilder.SetButtonIcon(ref newButton, iconSprite);
            UIBuilder.SetButtonTextSize(ref newButton, fontSize);

            newButton.onClick.RemoveAllListeners();
            newButton.onClick.AddListener(delegate ()
            {
                onClickEvent(sortMode);
            });
            
            sortButton.Button = newButton;
            sortButton.SortMode = sortMode;

            return sortButton;
        }

        /// <summary>
        /// Create a page up/down button.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="buttonTemplate"></param>
        /// <param name="iconSprite"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <param name="iconWidth"></param>
        /// <param name="iconHeight"></param>
        /// <param name="iconRotation"></param>
        /// <returns></returns>
        public static Button CreateIconButton(RectTransform parent, Button buttonTemplate, Sprite iconSprite, Vector2 pos, Vector2 size, Vector2 iconPos, Vector2 iconSize, Vector2 iconScale, float iconRotation)
        {
            Button newButton = UIBuilder.CreateUIButton(parent, buttonTemplate);

            newButton.interactable = true;
            (newButton.transform as RectTransform).anchoredPosition = new Vector2(pos.x, pos.y);
            (newButton.transform as RectTransform).sizeDelta = new Vector2(size.x, size.y);

            RectTransform iconTransform = newButton.GetComponentsInChildren<RectTransform>(true).First(c => c.name == "Icon");
            iconTransform.gameObject.SetActive(true);

            HorizontalLayoutGroup hgroup = iconTransform.parent.GetComponent<HorizontalLayoutGroup>();
            UnityEngine.Object.Destroy(hgroup);

            iconTransform.anchoredPosition = new Vector2(iconPos.x, iconPos.y);
            iconTransform.sizeDelta = new Vector2(iconSize.x, iconSize.y);
            iconTransform.localScale = new Vector2(iconScale.x, iconScale.y);            
            iconTransform.Rotate(0, 0, iconRotation);

            UnityEngine.Object.Destroy(newButton.GetComponentsInChildren<RectTransform>(true).First(c => c.name == "Text").gameObject);

            UIBuilder.SetButtonBorder(ref newButton, Color.clear);
            UIBuilder.SetButtonIcon(ref newButton, iconSprite);

            return newButton;
        }

        /// <summary>
        /// Create a beat saber dismiss button.
        /// </summary>
        /// <param name="parent"></param>
        /// <returns></returns>
        public static Button CreateBackButton(RectTransform parent)
        {
            Button dismissButton = CreateUIButton(parent, "BackArrowButton");
            UnityEngine.Object.DestroyImmediate(dismissButton.GetComponent<SignalOnUIButtonClick>());
            dismissButton.onClick = new Button.ButtonClickedEvent();
            dismissButton.name = "CustomUIButton";

            return dismissButton;
        }

        /// <summary>
        /// Generate TextMesh.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="text"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        static public TextMeshProUGUI CreateText(RectTransform parent, string text, Vector2 position, Vector2 width)
        {
            TextMeshProUGUI textMesh = new GameObject("TextMeshProUGUI_GO").AddComponent<TextMeshProUGUI>();
            textMesh.rectTransform.SetParent(parent, false);
            textMesh.text = text;
            textMesh.fontSize = 4;
            textMesh.color = Color.white;
            textMesh.font = Resources.Load<TMP_FontAsset>("Teko-Medium SDF No Glow");
            textMesh.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            textMesh.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            //textMesh.rectTransform.sizeDelta = size;
            textMesh.rectTransform.sizeDelta = width;
            textMesh.rectTransform.anchoredPosition = position;

            return textMesh;
        }

        /// <summary>
        /// Adjust a Button text.
        /// </summary>
        /// <param name="button"></param>
        /// <param name="text"></param>
        static public void SetButtonText(ref Button button, string text)
        {
            if (button.GetComponentInChildren<TextMeshProUGUI>() != null)
            {
                button.GetComponentInChildren<TextMeshProUGUI>().text = text;
            }
        }

        /// <summary>
        /// Adjust button text size.
        /// </summary>
        /// <param name="button"></param>
        /// <param name="fontSize"></param>
        static public void SetButtonTextSize(ref Button button, float fontSize)
        {
            if (button.GetComponentInChildren<TextMeshProUGUI>() != null)
            {
                button.GetComponentInChildren<TextMeshProUGUI>().fontSize = fontSize;
            }
        }

        /// <summary>
        /// Set a button icon.
        /// </summary>
        /// <param name="button"></param>
        /// <param name="icon"></param>
        static public void SetButtonIcon(ref Button button, Sprite icon)
        {
            if (button.GetComponentsInChildren<UnityEngine.UI.Image>().Count() > 1)
            {
                button.GetComponentsInChildren<Image>().First(x => x.name == "Icon").sprite = icon;                
            }            
        }

        /// <summary>
        /// Disable a button icon.
        /// </summary>
        /// <param name="button"></param>
        /// <param name="enabled"></param>
        static public void SetButtonIconEnabled(ref Button button, bool enabled)
        {
            if (button.GetComponentsInChildren<UnityEngine.UI.Image>().Count() > 1)
            {
                button.GetComponentsInChildren<UnityEngine.UI.Image>()[1].enabled = enabled;
            }
        }

        /// <summary>
        /// Adjust button background color.
        /// </summary>
        /// <param name="button"></param>
        /// <param name="background"></param>
        static public void SetButtonBackground(ref Button button, Sprite background)
        {
            if (button.GetComponentsInChildren<Image>().Any())
            {
                button.GetComponentsInChildren<UnityEngine.UI.Image>()[0].sprite = background;
            }

        }

        /// <summary>
        /// Adjust button border.
        /// </summary>
        /// <param name="button"></param>
        /// <param name="color"></param>
        static public void SetButtonBorder(ref Button button, Color color)
        {
            if (button.GetComponentsInChildren<Image>().Any())
            {
                button.GetComponentsInChildren<UnityEngine.UI.Image>()[0].color = color;
            }
        }
    }
}
