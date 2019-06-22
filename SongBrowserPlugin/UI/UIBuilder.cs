﻿using SongBrowser.DataAccess;
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

            UIBuilder.SetButtonText(newButton, buttonText);
            UIBuilder.SetButtonIconEnabled(newButton, false);
            UIBuilder.SetButtonTextSize(newButton, fontSize);

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
        public static SongSortButton CreateSortButton(RectTransform parent, Button buttonTemplate, Sprite iconSprite, Sprite borderSprite, string buttonText, float fontSize, float x, float y, float w, float h, SongSortMode sortMode, System.Action<SongSortMode> onClickEvent)
        {
            SongSortButton sortButton = new SongSortButton();
            Button newButton = UIBuilder.CreateUIButton(parent, buttonTemplate);

            newButton.interactable = true;
            (newButton.transform as RectTransform).anchoredPosition = new Vector2(x, y);
            (newButton.transform as RectTransform).sizeDelta = new Vector2(w, h);

            UIBuilder.SetButtonText(newButton, buttonText);
            UIBuilder.SetButtonIconEnabled(newButton, false);
            UIBuilder.SetButtonIcon(newButton, iconSprite);
            UIBuilder.SetButtonTextSize(newButton, fontSize);

            Image stroke = newButton.GetComponentsInChildren<Image>().First(btn => btn.name == "Stroke");
            stroke.sprite = borderSprite;
            //stroke.rectTransform.localScale = new Vector2(0.9f, 0.9f);

            newButton.GetComponentsInChildren<HorizontalLayoutGroup>().First(btn => btn.name == "Content").padding = new RectOffset(2, 2, 2, 2);

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

            UIBuilder.SetButtonIcon(newButton, iconSprite);

            return newButton;
        }

        /// <summary>
        /// Adjust a Button text.
        /// </summary>
        /// <param name="button"></param>
        /// <param name="text"></param>
        static public void SetButtonText(Button button, string text)
        {
            Polyglot.LocalizedTextMeshProUGUI localizer = button.GetComponentInChildren<Polyglot.LocalizedTextMeshProUGUI>();
            if (localizer != null)
            {
                GameObject.Destroy(localizer);
            }

            TextMeshProUGUI txt = button.GetComponentsInChildren<TextMeshProUGUI>().FirstOrDefault(x => x.name == "Text");
            if (txt != null)
            {
                txt.text = text;
                txt.verticalMapping = TextureMappingOptions.Line;
            }
        }

        /// <summary>
        /// Adjust button text size.
        /// </summary>
        /// <param name="button"></param>
        /// <param name="fontSize"></param>
        static public void SetButtonTextSize(Button button, float fontSize)
        {
            TextMeshProUGUI txt = button.GetComponentsInChildren<TextMeshProUGUI>().FirstOrDefault(x => x.name == "Text");
            if (txt != null)
            {
                txt.fontSize = fontSize;                
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
        /// Set a button icon.
        /// </summary>
        /// <param name="button"></param>
        /// <param name="icon"></param>
        static public void SetButtonIcon(Button button, Sprite icon)
        {
            if (button.GetComponentsInChildren<UnityEngine.UI.Image>().Count() > 1)
            {
                Image img = button.GetComponentsInChildren<Image>().FirstOrDefault(x => x.name == "Icon");
                if (img != null)
                {
                    img.sprite = icon;
                }
            }            
        }

        /// <summary>
        /// Disable a button icon.
        /// </summary>
        /// <param name="button"></param>
        /// <param name="enabled"></param>
        static public void SetButtonIconEnabled(Button button, bool enabled)
        {
            Image img = button.GetComponentsInChildren<Image>(true).FirstOrDefault(x => x.name == "Icon");
            if (img != null)
            {
                img.enabled = enabled;
                UnityEngine.Object.DestroyImmediate(img.gameObject);
            }
        }

        /// <summary>
        /// Adjust button background color.
        /// </summary>
        /// <param name="button"></param>
        /// <param name="background"></param>
        static public void SetButtonBackground(Button button, Sprite background)
        {
            Image img = button.GetComponentsInChildren<Image>().FirstOrDefault(x => x.name == "BG");
            if (img != null)
            {
                img.sprite = background;
            }
            else
            {
                Logger.Debug("NULL BG");
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
        /// 
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
