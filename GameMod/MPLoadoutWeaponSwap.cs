using HarmonyLib;
using Overload;
using UnityEngine;
using System;
using System.Reflection.Emit;
using System.Collections.Generic;

namespace GameMod
{
    public static class MPLoadoutWeaponSwapper
    {
        public static int[] weps = new int[2];
        public static int selected = 0;

        public static int SwapWeapon()
        {
            if (selected == 0)
            {
                selected = 1;
            }
            else
            {
                selected = 0;
            }

            return weps[selected];
        }
    }

    [HarmonyPatch(typeof(PlayerShip), "UpdateReadImmediateControls")]
    internal class MPLoadoutWeaponSwap
    {
        static void Postfix(PlayerShip __instance)
        {
            if (Controls.JustPressed(CCInput.SMASH_ATTACK) && GameplayManager.IsMultiplayerActive && !MPSmash.Enabled)
            {
                //Traverse ps = Traverse.Create(__instance).Method("SelectWeapon");

                //ps.Method("SelectWeapon", MPLoadoutWeaponSwapper.SwapWeapon());
                //ps.GetValue(MPLoadoutWeaponSwapper.SwapWeapon());

                // screw it I'm just copy-pasting the original private method.

                int weapon_num = MPLoadoutWeaponSwapper.SwapWeapon();

                Debug.Log("CCC switching to loadout weapon " + MPLoadoutWeaponSwapper.selected + " - " + Enum.GetName(typeof(WeaponType), weapon_num));

                if (__instance.c_player.m_weapon_level[weapon_num] != 0 && __instance.c_player.m_weapon_type != (WeaponType)weapon_num)
                {
                    __instance.c_player.Networkm_weapon_type = (WeaponType)weapon_num;
                    __instance.c_player.CallCmdSetCurrentWeapon(__instance.c_player.m_weapon_type);
                    __instance.WeaponSelectFX();
                    __instance.c_player.UpdateCurrentWeaponName();
                }
            }
        }
    }

    // NEEDED ONLY FOR 0.5.5, .6 moves this over to MPLoadouts
    /*[HarmonyPatch(typeof(Client), "OnRespawnMsg")]
    internal class MPLoadoutWeaponSwap_Client_OnRespawnMsg
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
        {
            foreach (var code in codes)
            {
                if (code.opcode == OpCodes.Call && code.operand == AccessTools.Method(typeof(NetworkSpawnPlayer), "SetMultiplayerLoadout"))
                {
                    yield return new CodeInstruction(OpCodes.Ldloc_3); // int lobby_id
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MPLoadoutWeaponSwap_Client_OnRespawnMsg), "SetMultiplayerLoadoutSwap"));
                    continue;
                }
                yield return code;
            }
        }
        
        static void SetMultiplayerLoadoutSwap(Player player, LoadoutDataMessage loadout_data, bool use_loadout1, int lobby_id)
        {
            NetworkSpawnPlayer.SetMultiplayerLoadout(player, loadout_data, use_loadout1);

            // set the appropriate swapper weapons
            if (NetworkMatch.m_force_loadout == 1)
            {
                MPLoadoutWeaponSwapper.weps[0] = (int)NetworkMatch.m_force_w1;

                if (NetworkMatch.m_force_w2 != WeaponType.NUM)
                {
                    MPLoadoutWeaponSwapper.weps[1] = (int)NetworkMatch.m_force_w2;
                }
                else
                {
                    MPLoadoutWeaponSwapper.weps[1] = (int)NetworkMatch.m_force_w1;
                }
            }
            else
            {
                int idx = ((!use_loadout1) ? loadout_data.m_mp_loadout2 : loadout_data.m_mp_loadout1);
                MPLoadoutWeaponSwapper.weps[0] = (int)loadout_data.GetMpLoadoutWeapon1(idx);
                MPLoadoutWeaponSwapper.weps[1] = (int)loadout_data.GetMpLoadoutWeapon2(idx);
                if (MPLoadoutWeaponSwapper.weps[1] == (int)WeaponType.NUM)
                {
                    MPLoadoutWeaponSwapper.weps[1] = MPLoadoutWeaponSwapper.weps[0];
                }
            }
            MPLoadoutWeaponSwapper.selected = 0;
        }
        
    }*/
}
