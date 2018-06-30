using UnityEngine;
using System.Linq;
using UnityEngine.UI;
using TMPro;

namespace SongBrowserPlugin
{
    public static class UIBuilder
    {
        static public Button CreateUIButton(RectTransform parent, string buttonTemplate, Button buttonInstance)
        {
            if (buttonInstance == null)
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

        static public void SetButtonText(ref Button button, string text)
        {
            if (button.GetComponentInChildren<TextMeshProUGUI>() != null)
            {

                button.GetComponentInChildren<TextMeshProUGUI>().text = text;
            }

        }

        static public void SetButtonTextSize(ref Button button, float fontSize)
        {
            if (button.GetComponentInChildren<TextMeshProUGUI>() != null)
            {
                button.GetComponentInChildren<TextMeshProUGUI>().fontSize = fontSize;
            }


        }

        static public void SetButtonIcon(ref Button button, Sprite icon)
        {
            if (button.GetComponentsInChildren<UnityEngine.UI.Image>().Count() > 1)
            {
                button.GetComponentsInChildren<UnityEngine.UI.Image>()[1].sprite = icon;
            }            
        }

        static public void SetButtonIconEnabled(ref Button button, bool enabled)
        {
            if (button.GetComponentsInChildren<UnityEngine.UI.Image>().Count() > 1)
            {
                button.GetComponentsInChildren<UnityEngine.UI.Image>()[1].enabled = enabled;
            }
        }

        static public void SetButtonBackground(ref Button button, Sprite background)
        {
            if (button.GetComponentsInChildren<Image>().Any())
            {
                button.GetComponentsInChildren<UnityEngine.UI.Image>()[0].sprite = background;
            }

        }

        static public void SetButtonBorder(ref Button button, Color color)
        {
            if (button.GetComponentsInChildren<Image>().Any())
            {
                button.GetComponentsInChildren<UnityEngine.UI.Image>()[0].color = color;
            }
        }
    }
}
