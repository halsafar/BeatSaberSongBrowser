using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using TMPro;

namespace SongBrowserPlugin
{
    public static class UIBuilder
    {
        static public Button CreateUIButton(RectTransform parent, string buttonTemplate, Button _buttonInstance)
        {
            if (_buttonInstance == null)
            {
                return null;
            }

            Button btn = UnityEngine.Object.Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == buttonTemplate)), parent, false);
            UnityEngine.Object.DestroyImmediate(btn.GetComponent<GameEventOnUIButtonClick>());
            btn.onClick = new Button.ButtonClickedEvent();

            return btn;
        }

        static public TextMeshProUGUI CreateText(RectTransform parent, string text, Vector2 position)
        {
            TextMeshProUGUI textMesh = new GameObject("TextMeshProUGUI_GO").AddComponent<TextMeshProUGUI>();
            textMesh.rectTransform.SetParent(parent, false);
            textMesh.text = text;
            textMesh.fontSize = 4;
            textMesh.color = Color.white;
            textMesh.font = Resources.Load<TMP_FontAsset>("Teko-Medium SDF No Glow");
            textMesh.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            textMesh.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            textMesh.rectTransform.sizeDelta = new Vector2(60f, 10f);
            textMesh.rectTransform.anchoredPosition = position;

            return textMesh;
        }

        static public void SetButtonText(ref Button _button, string _text)
        {
            if (_button.GetComponentInChildren<TextMeshProUGUI>() != null)
            {

                _button.GetComponentInChildren<TextMeshProUGUI>().text = _text;
            }

        }

        static public void SetButtonTextSize(ref Button _button, float _fontSize)
        {
            if (_button.GetComponentInChildren<TextMeshProUGUI>() != null)
            {
                _button.GetComponentInChildren<TextMeshProUGUI>().fontSize = _fontSize;
            }


        }

        static public void SetButtonIcon(ref Button _button, Sprite _icon)
        {
            if (_button.GetComponentsInChildren<UnityEngine.UI.Image>().Count() > 1)
            {
                _button.GetComponentsInChildren<UnityEngine.UI.Image>()[1].sprite = _icon;
            }            
        }

        static public void SetButtonIconEnabled(ref Button _button, bool enabled)
        {
            if (_button.GetComponentsInChildren<UnityEngine.UI.Image>().Count() > 1)
            {
                _button.GetComponentsInChildren<UnityEngine.UI.Image>()[1].enabled = enabled;
            }
        }

        static public void SetButtonBackground(ref Button _button, Sprite _background)
        {
            if (_button.GetComponentsInChildren<Image>().Any())
            {
                _button.GetComponentsInChildren<UnityEngine.UI.Image>()[0].sprite = _background;
            }

        }

        static public void SetButtonBorder(ref Button _button, Color color)
        {
            if (_button.GetComponentsInChildren<Image>().Any())
            {
                _button.GetComponentsInChildren<UnityEngine.UI.Image>()[0].color = color;
            }
        }
    }
}
