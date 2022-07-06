
using HarmonyLib;
using HMUI;
using System;

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
                Plugin.Log.Info("Initializing SongBrowser for Single Player Mode");
                SongBrowser.SongBrowserApplication.Instance.HandleSoloModeSelection();
            }
            else if (flowType == typeof(MultiplayerLevelSelectionFlowCoordinator))
            {
                Plugin.Log.Info("Initializing SongBrowser for Multiplayer Mode");
                SongBrowser.SongBrowserApplication.Instance.HandleMultiplayerModeSelection();
            }
            else if (flowType == typeof(PartyFreePlayFlowCoordinator))
            {
                Plugin.Log.Info("Initializing SongBrowser for Party Mode");
                SongBrowser.SongBrowserApplication.Instance.HandlePartyModeSelection();
            }
            else if (flowType == typeof(CampaignFlowCoordinator))
            {
                Plugin.Log.Info("Initializing SongBrowser for Multiplayer Mode");
                SongBrowser.SongBrowserApplication.Instance.HandleCampaignModeSelection();
            }

            return;
        }
    }
}
