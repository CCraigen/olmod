using HarmonyLib;
using Overload;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;

namespace GameMod
{
    /// <summary>
    /// Does a better job of initializing playership state at spawn, resetting the flak/cyclone fire counter, the thunderbolt power level, and clearing the boost overheat.
    /// </summary>
    [HarmonyPatch(typeof(Player), "RestorePlayerShipDataAfterRespawn")]
    class MPSpawnInitialization
    {
        private static FieldInfo _PlayerShip_flak_fire_count_Field = typeof(PlayerShip).GetField("flak_fire_count", BindingFlags.NonPublic | BindingFlags.Instance);

        static void Prefix(Player __instance)
        {
            _PlayerShip_flak_fire_count_Field.SetValue(__instance.c_player_ship, 0);
            __instance.c_player_ship.m_thunder_power = 0;
            __instance.c_player_ship.m_boost_heat = 0;
            __instance.c_player_ship.m_boost_overheat_timer = 0f;
        }
    }

    // moves the colliders during the same frame as spawn, rather than the frame after as they currently do
    [HarmonyPatch(typeof(PlayerShip), "DoSpawnEffects")]
    class MPSpawnInitialization_ColliderSpawnSync_DoSpawnEffects
    {
        static void Postfix(PlayerShip __instance, Vector3 pos, Quaternion rot)
        {
            if (GameplayManager.IsMultiplayer)
            {
                __instance.c_mesh_collider_trans.localPosition = pos;
                __instance.c_mesh_collider_trans.localRotation = rot;
            }
        }
    }

    // moves the colliders during the same frame as spawn, rather than the frame after as they currently do
    [HarmonyPatch(typeof(NetworkSpawnPlayer), "StartSpawnInvul")]
    class MPSpawnInitialization_ColliderSpawnSync_StartSpawnInvul
    {
        static void Prefix(Player player)
        {
            player.c_player_ship.c_mesh_collider_trans.localPosition = player.c_player_ship.transform.localPosition;
            player.c_player_ship.c_mesh_collider_trans.localRotation = player.c_player_ship.transform.localRotation;
        }
    }
}
