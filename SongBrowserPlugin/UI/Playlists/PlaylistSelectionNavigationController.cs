using HMUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VRUI;

namespace SongBrowserPlugin.UI
{
    class PlaylistSelectionMasterViewController : VRUINavigationController
    {
        private Button _dismissButton;
        public const String Name = "PlaylistSelectionMasterViewController";
        private Logger _log = new Logger(Name);

        /// <summary>
        /// Override DidActivate to inject our UI elements.
        /// </summary>
        protected override void DidActivate(bool firstActivation, VRUIViewController.ActivationType activationType)
        {
            _log.Debug("DidActivate()");
            base.DidActivate(firstActivation, activationType);

            if (firstActivation)
            {

            }

            if (activationType == VRUIViewController.ActivationType.AddedToHierarchy)
            {
                _log.Debug("Adding Dismiss Button");
                _dismissButton = Instantiate(Resources.FindObjectsOfTypeAll<Button>().First(x => (x.name == "BackArrowButton")), this.rectTransform, false);
                _dismissButton.onClick.RemoveAllListeners();
                _dismissButton.onClick.AddListener(HandleDismissButton);               
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void HandleDismissButton()
        {
            _log.Debug("Dismissing!");
            this.DismissModalViewController(null, false);
        }

        /// <summary>
        /// 
        /// </summary>
        private void CheckDebugUserInput()
        {
            // leave
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _dismissButton.onClick.Invoke();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void LateUpdate()
        {
            if (!this.isActiveAndEnabled) return;
            CheckDebugUserInput();
        }
    }
}
