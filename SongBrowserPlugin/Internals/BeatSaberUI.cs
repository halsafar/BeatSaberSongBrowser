using BeatSaberMarkupLanguage.Components;
using HMUI;
using IPA.Utilities;
using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VRUIControls;
using Image = UnityEngine.UI.Image;
using Logger = SongBrowser.Logging.Logger;

namespace SongBrowser.Internals
{
    public static class BeatSaberUI
    {
        private static PhysicsRaycasterWithCache _physicsRaycaster;
        public static PhysicsRaycasterWithCache PhysicsRaycasterWithCache
        {
            get
            {
                if (_physicsRaycaster == null)
                    _physicsRaycaster = Resources.FindObjectsOfTypeAll<MainMenuViewController>().First().GetComponent<VRGraphicRaycaster>().GetField<PhysicsRaycasterWithCache, VRGraphicRaycaster>("_physicsRaycaster");
                return _physicsRaycaster;
            }
        }

        /// <summary>
        /// Creates a ViewController of type T, and marks it to not be destroyed.
        /// </summary>
        /// <typeparam name="T">The variation of ViewController you want to create.</typeparam>
        /// <returns>The newly created ViewController of type T.</returns>
        public static T CreateViewController<T>(string name) where T : ViewController
        {
            T vc = new GameObject(typeof(T).Name, typeof(VRGraphicRaycaster), typeof(CanvasGroup), typeof(T)).GetComponent<T>();
            vc.GetComponent<VRGraphicRaycaster>().SetField("_physicsRaycaster", PhysicsRaycasterWithCache);

            vc.rectTransform.anchorMin = new Vector2(0f, 0f);
            vc.rectTransform.anchorMax = new Vector2(1f, 1f);
            vc.rectTransform.sizeDelta = new Vector2(0f, 0f);
            vc.rectTransform.anchoredPosition = new Vector2(0f, 0f);
            vc.gameObject.SetActive(false);
            vc.name = name;
            return vc;
        }

        public static T CreateCurvedViewController<T>(string name, float curveRadius) where T : ViewController
        {
            T vc = new GameObject(typeof(T).Name, typeof(VRGraphicRaycaster), typeof(CurvedCanvasSettings), typeof(CanvasGroup), typeof(T)).GetComponent<T>();
            vc.GetComponent<VRGraphicRaycaster>().SetField("_physicsRaycaster", PhysicsRaycasterWithCache);

            vc.GetComponent<CurvedCanvasSettings>().SetRadius(curveRadius);

            vc.rectTransform.anchorMin = new Vector2(0f, 0f);
            vc.rectTransform.anchorMax = new Vector2(1f, 1f);
            vc.rectTransform.sizeDelta = new Vector2(0f, 0f);
            vc.rectTransform.anchoredPosition = new Vector2(0f, 0f);
            vc.gameObject.SetActive(false);
            return vc;
        }

        /// <summary>
        /// Create Base button
        /// </summary>
        /// <param name="name"></param>
        /// <param name="parent"></param>
        /// <param name="buttonTemplate"></param>
        /// <returns></returns>
        public static Button CreateBaseButton(String name, RectTransform parent, String buttonTemplate)
        {
            Button btn = UnityEngine.Object.Instantiate(Resources.FindObjectsOfTypeAll<Button>().Last(x => (x.name == buttonTemplate)), parent, false);
            btn.name = name;
            btn.interactable = true;
            return btn;
        }

        /// <summary>
        /// Create basic icon button.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="parent"></param>
        /// <param name="buttonTemplate"></param>
        /// <returns></returns>
        public static Button CreateIconButton(String name, RectTransform parent, String buttonTemplate, Sprite icon, String hint)
        {
            Button btn = CreateBaseButton(name, parent, buttonTemplate);

            BeatSaberUI.SetHoverHint(btn.transform as RectTransform, $"{name}_hoverHintText", hint);
            btn.gameObject.AddComponent<ExternalComponents>().components.Add(btn.GetComponentsInChildren<LayoutGroup>().First(x => x.name == "Content"));

            Transform contentTransform = btn.transform.Find("Content");
            GameObject.Destroy(contentTransform.Find("Text").gameObject);
            Image iconImage = new GameObject("Icon").AddComponent<ImageView>();
            iconImage.material = BeatSaberMarkupLanguage.Utilities.ImageResources.NoGlowMat;
            iconImage.rectTransform.SetParent(contentTransform, false);
            iconImage.rectTransform.sizeDelta = new Vector2(10f, 10f);
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;
            if (iconImage != null)
            {
                ButtonIconImage btnIcon = btn.gameObject.AddComponent<ButtonIconImage>();
                btnIcon.image = iconImage;
            }

            GameObject.Destroy(btn.transform.Find("Content").GetComponent<LayoutElement>());
            btn.GetComponentsInChildren<RectTransform>().First(x => x.name == "Underline").gameObject.SetActive(false);

            ContentSizeFitter buttonSizeFitter = btn.gameObject.AddComponent<ContentSizeFitter>();
            buttonSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            buttonSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            btn.onClick.RemoveAllListeners();

            return btn;
        }

        /// <summary>
        /// Create an icon button, simple.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="buttonTemplate"></param>
        /// <param name="iconSprite"></param>
        /// <returns></returns>
        public static Button CreateIconButton(String name, RectTransform parent, String buttonTemplate, Vector2 anchoredPosition, Vector2 sizeDelta, UnityAction onClick, Sprite icon, String hint)
        {
            Logger.Debug("CreateIconButton({0}, {1}, {2}, {3}, {4}", name, parent, buttonTemplate, anchoredPosition, sizeDelta);
            Button btn = CreateIconButton(name, parent, buttonTemplate, icon, hint);

            (btn.transform as RectTransform).anchorMin = new Vector2(0.5f, 0.5f);
            (btn.transform as RectTransform).anchorMax = new Vector2(0.5f, 0.5f);
            (btn.transform as RectTransform).anchoredPosition = anchoredPosition;
            (btn.transform as RectTransform).sizeDelta = sizeDelta;

            btn.onClick.RemoveAllListeners();
            if (onClick != null)
                btn.onClick.AddListener(onClick);

            return btn;
        }

        public static Button CreatePageButton(String name, RectTransform parent, String buttonTemplate, Vector2 anchoredPosition, Vector2 sizeDelta, UnityAction onClick, Sprite icon)
        {
            Logger.Debug("CreatePageButton({0}, {1}, {2}, {3}, {4}", name, parent, buttonTemplate, anchoredPosition, sizeDelta);
            Button btn = CreateBaseButton(name, parent, buttonTemplate);

            (btn.transform as RectTransform).anchorMin = new Vector2(0.5f, 0.5f);
            (btn.transform as RectTransform).anchorMax = new Vector2(0.5f, 0.5f);
            (btn.transform as RectTransform).anchoredPosition = anchoredPosition;
            (btn.transform as RectTransform).sizeDelta = sizeDelta;
            (btn.transform as RectTransform).pivot = new Vector2(0.5f, 0.5f);

            ButtonIconImage btnIcon = btn.gameObject.AddComponent<ButtonIconImage>();
            btnIcon.image = btn.gameObject.GetComponentsInChildren<Image>(true).Where(x => x.gameObject.name == "Icon").FirstOrDefault();
            btnIcon.image.sprite = icon;

            btn.onClick.RemoveAllListeners();
            if (onClick != null)
                btn.onClick.AddListener(onClick);

            return btn;
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
        public static Button CreateUIButton(String name, RectTransform parent, string buttonTemplate, Vector2 anchoredPosition, Vector2 sizeDelta, UnityAction onClick = null, string buttonText = "BUTTON")
        {
            Logger.Debug("CreateUIButton({0}, {1}, {2}, {3}, {4}", name, parent, buttonTemplate, anchoredPosition, sizeDelta);
            Button btn = CreateBaseButton(name, parent, buttonTemplate);
            btn.gameObject.SetActive(true);

            Polyglot.LocalizedTextMeshProUGUI localizer = btn.GetComponentInChildren<Polyglot.LocalizedTextMeshProUGUI>();
            if (localizer != null)
            {
                GameObject.Destroy(localizer);
            }
            BeatSaberMarkupLanguage.Components.ExternalComponents externalComponents = btn.gameObject.AddComponent<BeatSaberMarkupLanguage.Components.ExternalComponents>();
            TextMeshProUGUI textMesh = btn.GetComponentInChildren<TextMeshProUGUI>();
            textMesh.richText = true;
            externalComponents.components.Add(textMesh);

            var contentTransform = btn.transform.Find("Content").GetComponent<LayoutElement>();
            if (contentTransform != null)
            {
                GameObject.Destroy(contentTransform);
            }

            ContentSizeFitter buttonSizeFitter = btn.gameObject.AddComponent<ContentSizeFitter>();
            buttonSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            buttonSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            LayoutGroup stackLayoutGroup = btn.GetComponentInChildren<LayoutGroup>();
            if (stackLayoutGroup != null)
            {
                externalComponents.components.Add(stackLayoutGroup);
            }

            btn.onClick.RemoveAllListeners();
            if (onClick != null)
            {
                btn.onClick.AddListener(onClick);
            }

            (btn.transform as RectTransform).anchorMin = new Vector2(0.5f, 0.5f);
            (btn.transform as RectTransform).anchorMax = new Vector2(0.5f, 0.5f);
            (btn.transform as RectTransform).anchoredPosition = anchoredPosition;
            (btn.transform as RectTransform).sizeDelta = sizeDelta;

            btn.SetButtonText(buttonText);

            return btn;
        }

        public static RectTransform CreateStatIcon(string name, RectTransform template, Transform parent, Sprite icon, String hoverHintText = null)
        {
            RectTransform statIcon = UnityEngine.Object.Instantiate(template, parent, false);
            statIcon.name = name;
            (statIcon.transform as RectTransform).Translate(0, -0.1f, 0);
            BeatSaberUI.SetStatButtonIcon(statIcon, icon);
            BeatSaberUI.DestroyHoverHint(statIcon);

            if (!String.IsNullOrEmpty(hoverHintText))
            {
                BeatSaberUI.SetHoverHint(statIcon, $"{name}_hoverHintText", hoverHintText);
            }

            return statIcon;
        }

        public static TextMeshProUGUI CreateText(RectTransform parent, string text, float fontSize, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            GameObject gameObj = new GameObject("CustomUIText");
            gameObj.SetActive(false);

            TextMeshProUGUI textMesh = gameObj.AddComponent<CurvedTextMeshPro>();
            textMesh.font = UnityEngine.Object.Instantiate(Resources.FindObjectsOfTypeAll<TMP_FontAsset>().First(t => t.name == "Teko-Medium SDF No Glow"));
            textMesh.rectTransform.SetParent(parent, false);
            textMesh.text = text;
            textMesh.fontSize = fontSize;
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
            HoverHint hover = button.gameObject.AddComponent<HoverHint>();
            hover.text = text;
            hover.name = name;
            hover.SetField("_hoverHintController", Resources.FindObjectsOfTypeAll<HoverHintController>().First());
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
