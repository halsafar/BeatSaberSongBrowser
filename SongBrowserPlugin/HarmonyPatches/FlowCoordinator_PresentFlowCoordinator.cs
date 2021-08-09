
using HarmonyLib;
using HMUI;
using System;
using Logger = SongBrowser.Logging.Logger;

namespace SongBrowser.HarmonyPatches
{
    [HarmonyPatch(typeof(FlowCoordinator))]
    [HarmonyPatch("PresentFlowCoordinator", MethodType.Normal)]
    class FlowCoordinator_PresentFlowCoordinator
    {
#pragma warning disable IDE0051 // Remove unused private members
        static void Prefix(FlowCoordinator flowCoordinator, Action finishedCallback = null, ViewController.AnimationDirection animationDirection = ViewController.AnimationDirection.Horizontal, bool immediately = false, bool replaceTopViewController = false)
#pragma warning restore IDE0051 // Remove unused private members
        {
            var flowType = flowCoordinator.GetType();
            if (flowType == typeof(SoloFreePlayFlowCoordinator))
            {
                Logger.Info("Initializing SongBrowser for Single Player Mode");
                SongBrowser.SongBrowserApplication.Instance.HandleSoloModeSelection();
            }
            else if (flowType == typeof(MultiplayerLevelSelectionFlowCoordinator))
            {
                Logger.Info("Initializing SongBrowser for Multiplayer Mode");
                SongBrowser.SongBrowserApplication.Instance.HandleMultiplayerModeSelection();
            }
            else if (flowType == typeof(PartyFreePlayFlowCoordinator))
            {
                Logger.Info("Initializing SongBrowser for Party Mode");
                SongBrowser.SongBrowserApplication.Instance.HandlePartyModeSelection();
            }
            else if (flowType == typeof(CampaignFlowCoordinator))
            {
                Logger.Info("Initializing SongBrowser for Multiplayer Mode");
                SongBrowser.SongBrowserApplication.Instance.HandleCampaignModeSelection();
            }

            return;
        }
    }
}
