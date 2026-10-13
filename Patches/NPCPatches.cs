using System;
using HarmonyLib;
using Il2CppScheduleOne.NPCs;
using ArfSchedule1.Modules;

using ArfSchedule1.UI;

namespace ArfSchedule1.Patches
{
    [HarmonyPatch(typeof(NPCHealth), "Die", new Type[] { })]
    public static class NPCHealthDiePatch
    {
        [HarmonyPostfix]
        public static void Postfix(NPCHealth __instance)
        {
            if (__instance == null) return;

            string npcName = "Civilian";
            try
            {
                if (__instance.npc != null && !string.IsNullOrEmpty(__instance.npc.FullName))
                    npcName = __instance.npc.FullName;
                else if (__instance.npc != null && !string.IsNullOrEmpty(__instance.npc.name))
                    npcName = __instance.npc.name.Replace("(Clone)", "").Trim();
            }
            catch { }

            HudNotificationManager.LogNpcDeath(npcName);

            var respawn = InstaRespawnNPCModule.Instance;
            if (respawn != null && respawn.Enabled)
            {
                __instance.Revive();
                __instance.RestoreHealth();
            }
        }
    }

    [HarmonyPatch(typeof(NPCHealth), "KnockOut", new Type[] { })]
    public static class NPCHealthKnockOutPatch
    {
        [HarmonyPostfix]
        public static void Postfix(NPCHealth __instance)
        {
            if (__instance == null) return;
            var respawn = InstaRespawnNPCModule.Instance;
            if (respawn != null && respawn.Enabled)
            {
                __instance.Revive();
                __instance.RestoreHealth();
            }
        }
    }
}
