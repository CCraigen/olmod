using HarmonyLib;
using Overload;
using UnityEngine;

namespace GameMod
{
    public static class MPWeaponCycling
    {
        public static bool[] CPrimaries = new bool[8];
        public static bool[] CSecondaries = new bool[8];

        public static void SetPrimaryCycleEnable(int i)
        {
            CPrimaries[i] = !CPrimaries[i];
            if (!CPrimaries[i])
            {
                SFXCueManager.PlayCue2D(SFXCue.hud_weapon_cycle_picker, 0.8f, 0f, 0f, false);
            }
            else
            {
                SFXCueManager.PlayCue2D(SFXCue.hud_weapon_cycle_close, 0.8f, 0f, 0f, false);
            }
            //ExtendedConfig.Section_AutoSelect.Set(true);
        }

        public static void SetSecondaryCycleEnable(int i)
        {
            CSecondaries[i] = !CSecondaries[i];
            if (!CSecondaries[i])
            {
                SFXCueManager.PlayCue2D(SFXCue.hud_weapon_cycle_picker, 0.8f, 0f, 0f, false);
            }
            else
            {
                SFXCueManager.PlayCue2D(SFXCue.hud_weapon_cycle_close, 0.8f, 0f, 0f, false);
            }
            //ExtendedConfig.Section_AutoSelect.Set(true);
        }
    }
}
