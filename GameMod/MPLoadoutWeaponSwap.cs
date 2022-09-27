using HarmonyLib;
using Overload;
using System;
using System.Reflection.Emit;
using System.Collections.Generic;

namespace GameMod
{
    public static class MPLoadoutWeaponSwap
    {
        public static int[] weps = new int[2];
        public static int old = 1;
        public static int selected = 0;
        public static bool active = false;

        public static int SwapWeapon()
        {
            old = selected;
            selected = (selected + 1) % 2;

            return weps[selected];
        }
    }

    [HarmonyPatch(typeof(PlayerShip), "UpdateReadImmediateControls")]
    internal class MPLoadoutWeaponSwap_PlayerShip_UpdateReadImmediateControls
    {
        static void Postfix(PlayerShip __instance)
        {
            if (Controls.JustPressed(CCInput.SMASH_ATTACK) && GameplayManager.IsMultiplayerActive && !MPSmash.Enabled)
            {
                // screw it I'm just copy-pasting the original private SelectWeapon() method.

                MPLoadoutWeaponSwap.active = true;
                int weapon_num = MPLoadoutWeaponSwap.SwapWeapon();

                if (__instance.c_player.m_weapon_level[weapon_num] != 0 && __instance.c_player.m_weapon_type != (WeaponType)weapon_num)
                {
                    __instance.c_player.Networkm_weapon_type = (WeaponType)weapon_num;
                    __instance.c_player.CallCmdSetCurrentWeapon(__instance.c_player.m_weapon_type);
                    __instance.WeaponSelectFX();
                    __instance.c_player.UpdateCurrentWeaponName();
                }
                else
                {
                    MPLoadoutWeaponSwap.SwapWeapon(); // re-swap the variable but don't actually change weapons
                }
                MPLoadoutWeaponSwap.active = false;
            }
        }
    }

    // if player selects the "old" loadout primary using a different key, switch which one is marked as active in the swapper
    [HarmonyPatch(typeof(Player), "CallCmdSetCurrentWeapon")]
    internal class MPLoadoutWeaponSwap_Player_CallCmdSetCurrentWeapon
    {
        static void Postfix(WeaponType weapon_type)
        {
            if (!MPLoadoutWeaponSwap.active)
            {
                if ((int)weapon_type == MPLoadoutWeaponSwap.weps[(MPLoadoutWeaponSwap.selected + 1) % 2])
                {
                    MPLoadoutWeaponSwap.SwapWeapon();
                }
            }
        }
    }

    /*
    // NEEDED ONLY FOR 0.5.5, .6 moves this over to MPLoadouts
    [HarmonyPatch(typeof(Client), "OnRespawnMsg")]
    internal class MPLoadoutWeaponSwap_Client_OnRespawnMsg
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> codes)
        {
            foreach (var code in codes)
            {
                if (code.opcode == OpCodes.Call && code.operand == AccessTools.Method(typeof(NetworkSpawnPlayer), "SetMultiplayerLoadout"))
                {
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(MPLoadoutWeaponSwap_Client_OnRespawnMsg), "SetMultiplayerLoadoutSwap"));
                    continue;
                }
                yield return code;
            }
        }

        static void SetMultiplayerLoadoutSwap(Player player, LoadoutDataMessage loadout_data, bool use_loadout1)
        {
            NetworkSpawnPlayer.SetMultiplayerLoadout(player, loadout_data, use_loadout1);

            // set the appropriate swapper weapons
            if (player == GameManager.m_local_player)
            {
                if (NetworkMatch.m_force_loadout == 1)
                {
                    MPLoadoutWeaponSwap.weps[0] = (int)NetworkMatch.m_force_w1;

                    if (NetworkMatch.m_force_w2 != WeaponType.NUM)
                    {
                        MPLoadoutWeaponSwap.weps[1] = (int)NetworkMatch.m_force_w2;
                    }
                    else
                    {
                        MPLoadoutWeaponSwap.weps[1] = (int)NetworkMatch.m_force_w1;
                    }
                }
                else
                {
                    int idx = ((!use_loadout1) ? loadout_data.m_mp_loadout2 : loadout_data.m_mp_loadout1);
                    MPLoadoutWeaponSwap.weps[0] = (int)loadout_data.GetMpLoadoutWeapon1(idx);
                    MPLoadoutWeaponSwap.weps[1] = (int)loadout_data.GetMpLoadoutWeapon2(idx);
                    if (MPLoadoutWeaponSwap.weps[1] == (int)WeaponType.NUM)
                    {
                        MPLoadoutWeaponSwap.weps[1] = MPLoadoutWeaponSwap.weps[0];
                    }
                }
                MPLoadoutWeaponSwap.selected = 0;
            }
        }
    }*/
}
