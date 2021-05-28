using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Image = UnityEngine.UI.Image;

namespace SongBrowser.Internals
{
    public static class BeatSaberUIExtensions
    {
        #region Button Extensions
        public static void SetButtonText(this Button _button, string _text)
        {
            HMUI.CurvedTextMeshPro textMesh = _button.GetComponentInChildren<HMUI.CurvedTextMeshPro>();
            if (textMesh != null)
            {
                textMesh.SetText(_text);
            }
        }

        public static void SetButtonTextSize(this Button _button, float _fontSize)
        {
            var txtMesh = _button.GetComponentInChildren<HMUI.CurvedTextMeshPro>();
            if (txtMesh != null)
            {
                txtMesh.fontSize = _fontSize;
            }
        }

        public static void ToggleWordWrapping(this Button _button, bool enableWordWrapping)
        {
            var txtMesh = _button.GetComponentInChildren<HMUI.CurvedTextMeshPro>();
            if (txtMesh != null)
            {
                txtMesh.enableWordWrapping = enableWordWrapping;
            }
        }

        public static void SetButtonBackgroundActive(this Button parent, bool active)
        {
            HMUI.ImageView img = parent.GetComponentsInChildren<HMUI.ImageView>().Last(x => x.name == "BG");
            if (img != null)
            {
                img.gameObject.SetActive(active);
            }
        }

        public static void SetButtonUnderlineColor(this Button parent, Color color)
        {
            HMUI.ImageView img = parent.GetComponentsInChildren<HMUI.ImageView>().FirstOrDefault(x => x.name == "Underline");
            if (img != null)
            {
                img.color = color;
            }
        }

        public static void SetButtonBorder(this Button button, Color color)
        {
            HMUI.ImageView img = button.GetComponentsInChildren<HMUI.ImageView>().FirstOrDefault(x => x.name == "Border");
            if (img != null)
            {
                img.color0 = color;
                img.color1 = color;
                img.color = color;
                img.fillMethod = Image.FillMethod.Horizontal;
                img.SetAllDirty();
            }
        }
        #endregion

        #region ViewController Extensions
        public static Button CreateUIButton(this HMUI.ViewController parent, string name, string buttonTemplate, Vector2 anchoredPosition, Vector2 sizeDelta, UnityAction onClick = null, string buttonText = "BUTTON")
        {
            Button btn = BeatSaberUI.CreateUIButton(name, parent.rectTransform, buttonTemplate, anchoredPosition, sizeDelta, onClick, buttonText);
            return btn;
        }
        public static Button CreateIconButton(this HMUI.ViewController parent, string name, string buttonTemplate, Vector2 anchoredPosition, Vector2 sizeDelta, UnityAction onClick, Sprite icon, string hint)
        {
            Button btn = BeatSaberUI.CreateIconButton(name, parent.rectTransform, buttonTemplate, anchoredPosition, sizeDelta, onClick, icon, hint);
            return btn;
        }
        #endregion
    }
}