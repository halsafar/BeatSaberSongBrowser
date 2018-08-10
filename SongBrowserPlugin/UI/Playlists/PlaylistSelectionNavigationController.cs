using System;
using UnityEngine;
using UnityEngine.UI;
using VRUI;

namespace SongBrowserPlugin.UI
{
    class PlaylistSelectionNavigationController : VRUINavigationController
    {
        public const String Name = "PlaylistSelectionMasterViewController";

        public Action didDismissEvent;

        private Button _dismissButton;        

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
                _dismissButton = UIBuilder.CreateBackButton(this.rectTransform);
                _dismissButton.onClick.AddListener(HandleDismissButton);               
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private void HandleDismissButton()
        {
            try
            {
                _log.Debug("Dismissing...");
                didDismissEvent.Invoke();
            }
            catch (Exception e)
            {
                _log.Exception("HandleDismissButton Exception: ", e);
            }
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
