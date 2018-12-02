﻿using CustomUI.BeatSaber;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRUI;

namespace SongBrowserPlugin.UI
{
    // https://github.com/andruzzzhka/BeatSaverDownloader/blob/master/BeatSaverDownloader/PluginUI/ViewControllers/SearchKeyboardViewController.cs
    class SearchKeyboardViewController : VRUIViewController
    {
        GameObject _searchKeyboardGO;

        CustomUIKeyboard _searchKeyboard;

        TextMeshProUGUI _inputText;
        public string _inputString = "";

        public event Action<string> searchButtonPressed;
        public event Action backButtonPressed;

        protected override void DidActivate(bool firstActivation, ActivationType type)
        {
            if (type == ActivationType.AddedToHierarchy && firstActivation)
            {
                _searchKeyboardGO = Instantiate(Resources.FindObjectsOfTypeAll<UIKeyboard>().First(x => x.name != "CustomUIKeyboard"), rectTransform, false).gameObject;

                Destroy(_searchKeyboardGO.GetComponent<UIKeyboard>());
                _searchKeyboard = _searchKeyboardGO.AddComponent<CustomUIKeyboard>();

                _searchKeyboard.textKeyWasPressedEvent += delegate (char input) { _inputString += input; UpdateInputText(); };
                _searchKeyboard.deleteButtonWasPressedEvent += delegate () { _inputString = _inputString.Substring(0, _inputString.Length - 1); UpdateInputText(); };
                _searchKeyboard.cancelButtonWasPressedEvent += () => { backButtonPressed?.Invoke(); };
                _searchKeyboard.okButtonWasPressedEvent += () => { searchButtonPressed?.Invoke(_inputString); };

                //_inputText = BeatSaberUI.CreateText(rectTransform, "Search...", new Vector2(0f, 22f));
                _inputText = BeatSaberUI.CreateText(rectTransform, "Search...", new Vector2(0f, 22f));
                _inputText.alignment = TextAlignmentOptions.Center;
                _inputText.fontSize = 6f;

            }
            else
            {
                _inputString = "";
                UpdateInputText();
            }

        }

        void UpdateInputText()
        {
            if (_inputText != null)
            {
                _inputText.text = _inputString.ToUpper();
                if (string.IsNullOrEmpty(_inputString))
                {
                    _searchKeyboard.OkButtonInteractivity = false;
                }
                else
                {
                    _searchKeyboard.OkButtonInteractivity = true;
                }
            }
        }

        void ClearInput()
        {
            _inputString = "";
        }

        void Back()
        {
            _inputString = "";
            backButtonPressed?.Invoke();
            //DismissViewControllerCoroutine(null, false);
        }

        /// <summary>
        /// Emulate keyboard support.
        /// </summary>
        private void LateUpdate()
        {
            if (!this.isInViewControllerHierarchy) return;

            if (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return))
            {
                _searchKeyboard.OkButtonWasPressed();
            }
            else if (Input.GetKeyDown(KeyCode.Backspace))
            {
                this._searchKeyboard.DeleteButtonWasPressed();
            }
            else if (Input.GetKeyDown(KeyCode.Space))
            {
                this._searchKeyboard.KeyPressed(' ');
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                this.Back();
            }

            IEnumerable<KeyCode> keycodeIterator = Enum.GetValues(typeof(KeyCode)).Cast<KeyCode>();
            foreach (KeyCode keycode in keycodeIterator)
            {
                if (!((keycode >= KeyCode.A && keycode <= KeyCode.Z) || (keycode >= KeyCode.Alpha0 && keycode <= KeyCode.Alpha9))) continue;
                if (Input.GetKeyDown(keycode))
                {
                    this._searchKeyboard.KeyPressed(keycode.ToString().ToCharArray()[0]);
                }
            }      
        }
    }
}
