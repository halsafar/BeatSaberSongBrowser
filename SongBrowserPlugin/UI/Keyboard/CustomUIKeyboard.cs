using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Logger = SongBrowser.Logging.Logger;

namespace SongBrowser.UI
{
    // https://github.com/andruzzzhka/BeatSaverDownloader/blob/master/BeatSaverDownloader/PluginUI/UIElements/CustomUIKeyboard.cs
    class CustomUIKeyboard : MonoBehaviour
    {
        public event Action<char> textKeyWasPressedEvent;
        public event Action deleteButtonWasPressedEvent;
        public event Action cancelButtonWasPressedEvent;
        public event Action okButtonWasPressedEvent;

        public bool HideCancelButton { get { return hideCancelButton; } set { hideCancelButton = value; _cancelButton.gameObject.SetActive(!value); } }
        public bool OkButtonInteractivity { get { return okButtonInteractivity; } set { okButtonInteractivity = value; _okButton.interactable = value; } }

        private bool okButtonInteractivity;
        private bool hideCancelButton;

        TextMeshProButton _keyButtonPrefab;
        Button _cancelButton;
        Button _okButton;


        public void Awake()
        {
            _keyButtonPrefab = Resources.FindObjectsOfTypeAll<TextMeshProButton>().First(x => x.name == "KeyboardButton");

            Logger.Log("Found keyboard button!");

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
                "space",
                "OK",
                "Cancel"
            };

            for (int i = 0; i < array.Length; i++)
            {
                RectTransform parent = transform.GetChild(i) as RectTransform;
                //TextMeshProButton textMeshProButton = Instantiate(_keyButtonPrefab, parent);
                TextMeshProButton textMeshProButton = parent.GetComponentInChildren<TextMeshProButton>();
                textMeshProButton.text.text = array[i];
                RectTransform rectTransform = textMeshProButton.transform as RectTransform;
                rectTransform.localPosition = Vector2.zero;
                rectTransform.localScale = Vector3.one;
                rectTransform.anchoredPosition = Vector2.zero;
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector3.one;
                rectTransform.offsetMin = Vector2.zero;
                rectTransform.offsetMax = Vector2.zero;
                Navigation navigation = textMeshProButton.button.navigation;
                navigation.mode = Navigation.Mode.None;
                textMeshProButton.button.navigation = navigation;
                textMeshProButton.button.onClick.RemoveAllListeners();
                if (i < array.Length - 4)
                {
                    string key = array[i];
                    textMeshProButton.button.onClick.AddListener(delegate ()
                    {
                        textKeyWasPressedEvent?.Invoke(key[0]);
                    });
                }
                else if (i == array.Length - 4)
                {
                    textMeshProButton.button.onClick.AddListener(delegate ()
                    {
                        deleteButtonWasPressedEvent?.Invoke();
                    });
                }
                else if (i == array.Length - 1)
                {
                    (textMeshProButton.transform as RectTransform).sizeDelta = new Vector2(7f, 1.5f);
                    _cancelButton = textMeshProButton.button;
                    _cancelButton.gameObject.SetActive(!HideCancelButton);
                    textMeshProButton.button.onClick.AddListener(delegate ()
                    {
                        cancelButtonWasPressedEvent?.Invoke();
                    });
                }
                else if (i == array.Length - 2)
                {
                    _okButton = textMeshProButton.button;
                    _okButton.interactable = OkButtonInteractivity;
                    textMeshProButton.button.onClick.AddListener(delegate ()
                    {
                        okButtonWasPressedEvent?.Invoke();
                    });
                }
                else
                {
                    textMeshProButton.button.onClick.AddListener(delegate ()
                    {
                        textKeyWasPressedEvent?.Invoke(' ');
                    });
                }
            }

            name = "CustomUIKeyboard";

            (transform as RectTransform).anchoredPosition -= new Vector2(0f, 0f);

            for (int i = 1; i <= 10; i++)
            {
                TextMeshProButton textButton = Instantiate(_keyButtonPrefab);
                textButton.text.text = (i % 10).ToString();

                string key = (i % 10).ToString();
                textButton.button.onClick.AddListener(delegate ()
                {
                    textKeyWasPressedEvent?.Invoke(key[0]);
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

        }

        public void KeyPressed(char key)
        {
            textKeyWasPressedEvent.Invoke(key);
        }

        public void DeleteButtonWasPressed()
        {
            deleteButtonWasPressedEvent.Invoke();
        }

        public void OkButtonWasPressed()
        {
            okButtonWasPressedEvent.Invoke();
        }
    }
}
