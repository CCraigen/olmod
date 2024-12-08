using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Overload;
using HarmonyLib;

namespace GameMod
{
    /// <summary>
    /// Any private methods that would normally need a Reflection invocation can be added
    /// here for common reference UNLESS they have also been patched.
    /// 
    /// -- naming ex. "Server.PrivateMethod()" is now "OL_Server.PrivateMethod()".
    /// </summary>

    // ====================
    // Overload.Client
    // ====================
    [HarmonyPatch]
    public static class OL_Client
    {
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Client), "GetPlayerFromNetId")]
        public static Player GetPlayerFromNetId(NetworkInstanceId net_id)
        {
            throw new NotImplementedException("GetPlayerFromNetId stub");
        }
    }

    // ====================
    // Overload.Server
    // ====================
    [HarmonyPatch]
    public static class OL_Server
    {
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Server), "QueueNewInputsForProcessingOnServer")]
        public static void QueueNewInputsForProcessingOnServer(Player player, PlayerInputMessage msg)
        {
            throw new NotImplementedException("QueueNewInputsForProcessingOnServer stub");
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(Server), "SendJustPressedOrJustReleasedMessage")]
        public static void SendJustPressedOrJustReleasedMessage(Player player, CCInput button)
        {
            throw new NotImplementedException("SendJustPressedOrJustReleasedMessage stub");
        }
    }

    /*
    // ====================
    // Overload.UIElement
    // ====================
    [HarmonyPatch]
    public static class OL_UIElement
    {
        [HarmonyReversePatch]
        [HarmonyPatch(typeof(UIElement), "DrawScoreHeader")]
        public static void DrawScoreHeader(UIElement instance, Vector2 pos, float col1, float col2, float col3, float col4, float col5, bool score = false)
        {
            {
                throw new NotImplementedException("DrawScoreHeader stub");
            }
        }

        [HarmonyReversePatch]
        [HarmonyPatch(typeof(UIElement), "DrawScoresForTeam")]
        public static int DrawScoresForTeam(UIElement instance, MpTeam team, Vector2 pos, float col1, float col2, float col3, float col4, float col5)
        {
            {
                throw new NotImplementedException("DrawScoresForTeam stub");
            }
        }
    }
    */
}
