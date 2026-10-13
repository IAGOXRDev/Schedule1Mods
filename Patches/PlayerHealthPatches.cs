using System;
using HarmonyLib;
using UnityEngine;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.PlayerScripts.Health;
using Il2CppScheduleOne.Combat;
using ArfSchedule1.Modules;

namespace ArfSchedule1.Patches
{
    // ==========================================
    // CAN TAKE DAMAGE GETTER PATCH
    // ==========================================
    [HarmonyPatch(typeof(PlayerHealth), "get_CanTakeDamage", new Type[] { })]
    public static class PlayerHealthCanTakeDamagePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(ref bool __result)
        {
            var godMode = GodModeModule.Instance;
            if (godMode != null && godMode.Enabled)
            {
                __result = false;
                return false; // Skip original method and return false
            }
            return true;
        }
    }

    // ==========================================
    // TAKE DAMAGE METHOD PATCH
    // ==========================================
    [HarmonyPatch(typeof(PlayerHealth), "TakeDamage", new Type[] { typeof(float), typeof(bool), typeof(bool) })]
    public static class PlayerHealthTakeDamagePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(PlayerHealth __instance, float damage, bool flinch, bool playBloodMist)
        {
            var godMode = GodModeModule.Instance;
            if (godMode != null && godMode.Enabled)
            {
                // Completely cancel taking damage
                return false;
            }
            return true;
        }
    }

    // ==========================================
    // DIE PATCH (PREVENT PLAYER DEATH)
    // ==========================================
    [HarmonyPatch(typeof(PlayerHealth), "Die", new Type[] { })]
    public static class PlayerHealthDiePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(PlayerHealth __instance)
        {
            var godMode = GodModeModule.Instance;
            if (godMode != null && godMode.Enabled)
            {
                // Prevent the player from dying while GodMode is active
                if (__instance != null)
                {
                    __instance.IsAlive = true;
                    if (__instance.CurrentHealth <= 0f)
                    {
                        __instance.CurrentHealth = PlayerHealth.MaxHealth > 0f ? PlayerHealth.MaxHealth : 100f;
                    }
                }
                return false;
            }
            return true;
        }
    }

    // ==========================================
    // RECEIVE IMPACT PATCH (PREVENT DAMAGE ON IMPACT)
    // ==========================================
    [HarmonyPatch(typeof(Player), "ReceiveImpact", new Type[] { typeof(Impact) })]
    public static class PlayerReceiveImpactPatch
    {
        [HarmonyPrefix]
        public static void Prefix(Player __instance, Impact impact)
        {
            var godMode = GodModeModule.Instance;
            if (godMode != null && godMode.Enabled && impact != null)
            {
                // Nullify impact damage (explosions, projectiles, etc.) while preserving physics impulses
                impact.ImpactDamage = 0f;
            }
        }
    }
}
