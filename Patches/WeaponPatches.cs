using System;
using HarmonyLib;
using UnityEngine;
using Il2CppScheduleOne.Equipping;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppRootMotion.FinalIK;
using ArfSchedule1.Modules;
using ArfSchedule1.UI;

namespace ArfSchedule1.Patches
{
    // ==========================================
    // CAN FIRE PATCH
    // ==========================================
    [HarmonyPatch(typeof(Equippable_RangedWeapon), "CanFire", new Type[] { typeof(bool) })]
    public static class RangedWeaponCanFirePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Equippable_RangedWeapon __instance, ref bool checkAmmo, ref bool __result)
        {
            if (MenuManager.IsMenuOpen)
            {
                __result = false;
                return false;
            }

            var infAmmo = InfiniteAmmoModule.Instance;
            if (infAmmo != null && infAmmo.Enabled)
            {
                checkAmmo = false;
                if (__instance != null && __instance.weaponItem != null && __instance.weaponItem.Value <= 0)
                {
                    __instance.weaponItem.SetValue(__instance.MagazineSize);
                }
            }
            return true;
        }

        [HarmonyPostfix]
        public static void Postfix(Equippable_RangedWeapon __instance, ref bool __result)
        {
            if (MenuManager.IsMenuOpen)
            {
                __result = false;
                return;
            }

            var rapid = RapidFireModule.Instance;
            if (rapid != null && rapid.Enabled && __instance != null && !__instance.IsReloading)
            {
                __result = true;
            }
        }
    }

    // ==========================================
    // SPREAD ANGLE PATCH (NO SPREAD / NO RECOIL)
    // ==========================================
    [HarmonyPatch(typeof(Equippable_RangedWeapon), "GetSpreadAngle", new Type[] { })]
    public static class RangedWeaponSpreadPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(ref float __result)
        {
            var noRecoil = NoRecoilModule.Instance;
            var rapid = RapidFireModule.Instance;

            if ((noRecoil != null && noRecoil.Enabled) || (rapid != null && rapid.Enabled && rapid.NoSpread))
            {
                __result = 0f;
                return false;
            }
            return true;
        }
    }

    // ==========================================
    // FIRE PATCH (RANGED WEAPON)
    // ==========================================
    [HarmonyPatch(typeof(Equippable_RangedWeapon), "Fire", new Type[] { })]
    public static class RangedWeaponFirePatch
    {
        [HarmonyPrefix]
        public static void Prefix(Equippable_RangedWeapon __instance)
        {
            if (__instance == null) return;
            var infAmmo = InfiniteAmmoModule.Instance;
            if (infAmmo != null && infAmmo.Enabled && __instance.weaponItem != null)
            {
                if (__instance.weaponItem.Value <= 0)
                {
                    __instance.weaponItem.SetValue(__instance.MagazineSize);
                }
            }
        }

        [HarmonyPostfix]
        public static void Postfix(Equippable_RangedWeapon __instance)
        {
            if (__instance == null) return;

            var infAmmo = InfiniteAmmoModule.Instance;
            if (infAmmo != null && infAmmo.Enabled && __instance.weaponItem != null)
            {
                __instance.weaponItem.SetValue(__instance.MagazineSize);
            }

            var rapid = RapidFireModule.Instance;
            if (rapid != null && rapid.Enabled)
            {
                __instance.TimeSinceFire = 999f;
                __instance.IsCocked = true;
                __instance.IsCocking = false;
            }

            var noRecoil = NoRecoilModule.Instance;
            if (noRecoil != null && noRecoil.Enabled)
            {
                __instance.Accuracy = 1.0f;
                __instance.AccuracyDropPerShot = 0f;
                __instance.MinSpread = 0f;
                __instance.MaxSpread = 0f;
            }
        }
    }

    // ==========================================
    // FIRE PATCH (REVOLVER)
    // ==========================================
    [HarmonyPatch(typeof(Equippable_Revolver), "Fire", new Type[] { })]
    public static class RevolverFirePatch
    {
        [HarmonyPostfix]
        public static void Postfix(Equippable_Revolver __instance)
        {
            if (__instance == null) return;

            var infAmmo = InfiniteAmmoModule.Instance;
            if (infAmmo != null && infAmmo.Enabled && __instance.weaponItem != null)
            {
                __instance.weaponItem.SetValue(__instance.MagazineSize);
            }

            var rapid = RapidFireModule.Instance;
            if (rapid != null && rapid.Enabled)
            {
                __instance.TimeSinceFire = 999f;
                __instance.IsCocked = true;
                __instance.IsCocking = false;
            }

            var noRecoil = NoRecoilModule.Instance;
            if (noRecoil != null && noRecoil.Enabled)
            {
                __instance.Accuracy = 1.0f;
                __instance.AccuracyDropPerShot = 0f;
                __instance.MinSpread = 0f;
                __instance.MaxSpread = 0f;
            }
        }
    }

    // ==========================================
    // UPDATE PATCH (RAPID FIRE, AMMO, NO RECOIL)
    // ==========================================
    [HarmonyPatch(typeof(Equippable_RangedWeapon), "Update", new Type[] { })]
    public static class RangedWeaponUpdatePatch
    {
        private static float _lastFireTime = 0f;

        [HarmonyPostfix]
        public static void Postfix(Equippable_RangedWeapon __instance)
        {
            if (__instance == null || !__instance.gameObject.activeInHierarchy) return;

            // 1. Manejar Infinite Bullets
            var infAmmo = InfiniteAmmoModule.Instance;
            if (infAmmo != null && infAmmo.Enabled)
            {
                if (__instance.weaponItem != null && __instance.weaponItem.Value < __instance.MagazineSize)
                {
                    __instance.weaponItem.SetValue(__instance.MagazineSize);
                }
            }

            // 2. Manejar No Recoil
            var noRecoil = NoRecoilModule.Instance;
            if (noRecoil != null && noRecoil.Enabled)
            {
                __instance.Accuracy = 1.0f;
                __instance.AccuracyDropPerShot = 0f;
                __instance.MinSpread = 0f;
                __instance.MaxSpread = 0f;
            }

            // 3. Handle Rapid Fire
            var rapid = RapidFireModule.Instance;
            if (rapid != null && rapid.Enabled)
            {
                __instance.FireCooldown = 0.01f;
                __instance.IsCocked = true;
                __instance.IsCocking = false;

                if (rapid.NoSpread)
                {
                    __instance.Accuracy = 1.0f;
                    __instance.MinSpread = 0f;
                    __instance.MaxSpread = 0f;
                }

                // Continuous automatic fire while holding LMB
                if (Input.GetMouseButton(0) && !MenuManager.IsMenuOpen && !__instance.IsReloading)
                {
                    float interval = 1f / Mathf.Clamp(rapid.RoundsPerSecond, 5f, 40f);
                    if (Time.time - _lastFireTime >= interval)
                    {
                        _lastFireTime = Time.time;
                        __instance.TimeSinceFire = 999f;
                        __instance.IsCocked = true;
                        __instance.IsCocking = false;
                        __instance.Fire();
                    }
                }
            }
        }
    }

    // ==========================================
    // NO PUNCH: CAMERA JOLT PATCH
    // ==========================================
    [HarmonyPatch(typeof(PlayerCamera), "JoltCamera", new Type[] { })]
    public static class PlayerCameraJoltPatch
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            var noRecoil = NoRecoilModule.Instance;
            if (noRecoil != null && noRecoil.Enabled && noRecoil.RemoveCameraShake)
            {
                return false; // Block camera punch / jolt
            }
            return true;
        }
    }

    // ==========================================
    // NO PUNCH: CAMERA SHAKE PATCH
    // ==========================================
    [HarmonyPatch(typeof(PlayerCamera), "StartCameraShake", new Type[] { typeof(float), typeof(float), typeof(bool) })]
    public static class PlayerCameraShakePatch
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            var noRecoil = NoRecoilModule.Instance;
            if (noRecoil != null && noRecoil.Enabled && noRecoil.RemoveCameraShake)
            {
                return false; // Block camera shake
            }
            return true;
        }
    }

    // ==========================================
    // NO RECOIL: FINAL IK WEAPON PUNCH PATCH
    // ==========================================
    [HarmonyPatch(typeof(Recoil), "Fire", new Type[] { typeof(float) })]
    public static class FinalIKRecoilFirePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Recoil __instance)
        {
            var noRecoil = NoRecoilModule.Instance;
            if (noRecoil != null && noRecoil.Enabled && noRecoil.RemoveViewmodelRecoil)
            {
                return false; // Block physical viewmodel/hands IK recoil
            }
            return true;
        }
    }
}
