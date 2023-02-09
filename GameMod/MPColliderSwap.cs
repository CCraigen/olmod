﻿using HarmonyLib;
using Overload;
using UnityEngine;
using UnityEngine.Networking;

namespace GameMod
{
    public static class MPColliderSwap
    {
        public static int selectedCollider = 0;

        public static bool visualizeMe = false; // For debugging. Turn this on to render the collider mesh.
    }

    // replaces the mesh when the PlayerShip object is started, provided that an accurate mesh has been selected instead of the default
    [HarmonyPatch(typeof(PlayerShip), "Start")]
    internal class MPColliderSwap_PlayerShip_Start
    {
        static void Postfix(PlayerShip __instance)
        {
            if (!GameplayManager.IsMultiplayer)
                return;

            if (MPColliderSwap.selectedCollider != 0 && __instance.c_mesh_collider.GetComponent<MeshCollider>() == null)
            {
                var mat_no_friction = __instance.c_mesh_collider.sharedMaterial;
                Object.Destroy(__instance.c_mesh_collider);
                GameObject go = Object.Instantiate(MPShips.selected.collider[MPColliderSwap.selectedCollider - 1]); // -1 since 0 is the "Stock" sphere

                if (!__instance.c_player.m_spectator && __instance.netId != GameManager.m_local_player.c_player_ship.netId)
                {
                    if (MPColliderSwap.visualizeMe)
                    {
                        go.GetComponent<MeshRenderer>().sharedMaterial = UIManager.gm.m_energy_material;
                        go.GetComponent<MeshRenderer>().enabled = true;
                    }
                    else
                    {
                        go.GetComponent<MeshRenderer>().enabled = false;
                    }
                }
                else
                {
                    go.GetComponent<MeshRenderer>().enabled = false;
                }

                go.layer = 16;
                var coll = go.GetComponent<MeshCollider>();
                coll.sharedMaterial = mat_no_friction;
                go.AddComponent<PlayerMeshCollider>().c_player = __instance.c_player;

                __instance.c_mesh_collider = coll;
                __instance.c_mesh_collider_trans = __instance.c_mesh_collider.transform;
                __instance.c_flak_range_go.GetComponent<TriggerFlakRange>().player_collider = coll;
            }
        }
    }

    // replaces the original "PrepareForMP" method with one that can handle mesh vs. sphere colliders
    [HarmonyPatch(typeof(Player), "PrepareForMP")]
    internal class MPPlayer_PrepareForMP_ColliderSwap
    {
        static bool Prefix(Player __instance)
        {
            if (__instance.c_player_ship != null)
            {
                __instance.c_player_ship.c_level_collider.enabled = false;
                __instance.c_player_ship.c_mesh_collider.enabled = false;
            }
            __instance.m_remote_player = true;
            __instance.m_remote_thrusters_active = false;
            __instance.m_server_tick = -1;
            NetworkIdentity component = __instance.GetComponent<NetworkIdentity>();
            if (component != null)
            {
                component.localPlayerAuthority = false;
            }
            if (__instance.c_player_ship.c_mesh_collider.GetType() == typeof(SphereCollider))
            {
                SphereCollider sphereCollider1 = (SphereCollider)__instance.c_player_ship.c_mesh_collider;
                sphereCollider1.radius = 1f;

                //Debug.Log("CCF - lol nope still a SphereCollider");
            }

            return false;
        }
    }

    // adds rotation syncing to the collider since it didn't matter with the sphere previously
    // this also happens once below and twice over in MPClientExtrapolation (look for c_transform.localRotation)
    [HarmonyPatch(typeof(Player), "LerpRemotePlayer")]
    internal class MPPlayer_LerpRemotePlayer_ColliderSwap
    {
        static void Postfix(Player __instance)
        {
            __instance.c_player_ship.c_mesh_collider_trans.localRotation = __instance.c_player_ship.c_transform.localRotation;
        }
    }

    [HarmonyPatch(typeof(PlayerShip), "FixedUpdateProcessControlsInternal")]
    internal class MPPlayerShip_FixedUpdateProcessControlsInternal_ColliderSwap
    {
        static void Postfix(PlayerShip __instance)
        {
            if (GameplayManager.IsMultiplayerActive && __instance.c_mesh_collider_trans != null && __instance.c_transform != null)
            {
                __instance.c_mesh_collider_trans.localRotation = __instance.c_transform.localRotation;
            }
        }
    }
}
