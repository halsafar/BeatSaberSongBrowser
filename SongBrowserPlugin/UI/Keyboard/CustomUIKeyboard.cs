using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace SongBrowserPlugin.UI
{
    // https://github.com/andruzzzhka/BeatSaverDownloader/blob/master/BeatSaverDownloader/PluginUI/UIElements/CustomUIKeyboard.cs
    class CustomUIKeyboard : UIKeyboard
    {
        public void DeleteButtonWasPressed()
        {
            
        }
        /*public override void Awake()
        {
            UIKeyboard original = GetComponent<UIKeyboard>();

            System.Reflection.FieldInfo[] fields = original.GetType().GetFields(System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            foreach (System.Reflection.FieldInfo field in fields)
            {
                field.SetValue(this, field.GetValue(original));
            }

            Destroy(original);

        }

        public override void Start()
        {
            name = "CustomUIKeyboard";

            (transform as RectTransform).anchoredPosition -= new Vector2(0f, 5f);

            string[] array = new string[]
            {
                "q",
                "w",
                "e",
                "r",
                "t",
                "y",
                "u",
                "i",
                "o",
                "p",
                "a",
                "s",
                "d",
                "f",
                "g",
                "h",
                "j",
                "k",
                "l",
                "z",
                "x",
                "c",
                "v",
                "b",
                "n",
                "m",
                "<-",
                "space"
            };


            for (int i = 0; i < array.Length; i++)
            {
                TextMeshProButton textButton = Instantiate(_keyButtonPrefab);
                textButton.text.text = array[i];
                if (i < array.Length - 2)
                {
                    string key = array[i];
                    textButton.button.onClick.AddListener(delegate ()
                    {
                        KeyButtonWasPressed(key);
                        this.textKeyWasPressedEvent.Invoke();
                    });
                }
                else if (i == array.Length - 2)
                {
                    textButton.button.onClick.AddListener(delegate ()
                    {
                        DeleteButtonWasPressed();
                    });
                }
                else
                {
                    textButton.button.onClick.AddListener(delegate ()
                    {
                        SpaceButtonWasPressed();
                    });
                }
                RectTransform buttonRect = textButton.GetComponent<RectTransform>();
                RectTransform component2 = transform.GetChild(i).gameObject.GetComponent<RectTransform>();
                buttonRect.SetParent(component2, false);
                buttonRect.localPosition = Vector2.zero;
                buttonRect.localScale = Vector3.one;
                buttonRect.anchoredPosition = Vector2.zero;
                buttonRect.anchorMin = Vector2.zero;
                buttonRect.anchorMax = Vector3.one;
                buttonRect.offsetMin = Vector2.zero;
                buttonRect.offsetMax = Vector2.zero;
            }


            for (int i = 1; i <= 10; i++)
            {
                TextMeshProButton textButton = Instantiate(_keyButtonPrefab);
                textButton.text.text = i.ToString().Last().ToString();

                string key = i.ToString().Last().ToString();
                textButton.button.onClick.AddListener(delegate ()
                {
                    KeyButtonWasPressed(key);
                });

                RectTransform buttonRect = textButton.GetComponent<RectTransform>();
                RectTransform component2 = transform.GetChild(i - 1).gameObject.GetComponent<RectTransform>();

                RectTransform buttonHolder = Instantiate(component2, component2.parent, false);
                Destroy(buttonHolder.GetComponentInChildren<Button>().gameObject);

                buttonHolder.anchoredPosition -= new Vector2(0f, -10.5f);

                buttonRect.SetParent(buttonHolder, false);

                buttonRect.localPosition = Vector2.zero;
                buttonRect.localScale = Vector3.one;
                buttonRect.anchoredPosition = Vector2.zero;
                buttonRect.anchorMin = Vector2.zero;
                buttonRect.anchorMax = Vector3.one;
                buttonRect.offsetMin = Vector2.zero;
                buttonRect.offsetMax = Vector2.zero;
            }

        }*/
    }
}
