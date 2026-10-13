using System;
using HarmonyLib;
using UnityEngine;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Equipping;
using ArfSchedule1.UI;

namespace ArfSchedule1.Patches
{
    /// <summary>
    /// Harmony patches to lock camera rotation, mouse look,
    /// and accidental weapon attacks while the ArfHook menu is open.
    /// </summary>
    [HarmonyPatch(typeof(PlayerCamera), "RotateCamera", new Type[] { })]
    public static class PlayerCameraRotateCameraPatch
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            if (MenuManager.IsMenuOpen)
            {
                // Completely blocks camera rotation from mouse movements
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerCamera), "get_CanLook", new Type[] { })]
    public static class PlayerCameraCanLookPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(ref bool __result)
        {
            if (MenuManager.IsMenuOpen)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Equippable_MeleeWeapon), "UpdateInput", new Type[] { })]
    public static class MeleeWeaponUpdateInputPatch
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            if (MenuManager.IsMenuOpen)
            {
                // Prevents melee attacks when clicking menu buttons
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Equippable_MeleeWeapon), "CanStartLoading", new Type[] { })]
    public static class MeleeWeaponCanStartLoadingPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(ref bool __result)
        {
            if (MenuManager.IsMenuOpen)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}
