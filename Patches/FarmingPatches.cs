using System;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using Il2CppScheduleOne.Growing;
using Il2CppScheduleOne.ObjectScripts;
using Il2CppScheduleOne.PlayerTasks;
using Il2CppScheduleOne.PlayerTasks.Tasks;
using ArfSchedule1.Modules;
using ArfSchedule1.UI;

namespace ArfSchedule1.Patches
{
    // ==========================================
    // INSTA GROW: PLANT MINPASS PATCH
    // ==========================================
    [HarmonyPatch(typeof(Plant), "MinPass")]
    public static class PlantMinPassPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Plant __instance)
        {
            var mod = InstaGrowModule.Instance;
            if (mod != null && mod.Enabled && __instance != null && !__instance.IsFullyGrown)
            {
                InstaGrowModule.GrowPlantInstantly(__instance);
            }
        }
    }

    // ==========================================
    // INSTA FARM: HARVEST PLANT TASK PATCH
    // ==========================================
    [HarmonyPatch(typeof(HarvestPlant), "Update")]
    public static class HarvestPlantUpdatePatch
    {
        [HarmonyPrefix]
        public static void Prefix(HarvestPlant __instance)
        {
            var mod = InstaFarmModule.Instance;
            if (mod == null || !mod.Enabled || __instance == null) return;

            if (__instance.pot != null && __instance.pot.Plant != null)
            {
                var plant = __instance.pot.Plant;
                var activeIndices = plant.ActiveHarvestables;
                var harvestables = plant._harvestables;

                if (activeIndices != null && activeIndices.Count > 0 && harvestables != null)
                {
                    for (int i = activeIndices.Count - 1; i >= 0; i--)
                    {
                        int index = activeIndices[i];
                        if (index >= 0 && index < harvestables.Count)
                        {
                            var h = harvestables[index];
                            if (h != null)
                            {
                                try
                                {
                                    h.Harvest(true);
                                }
                                catch { }
                            }
                        }
                    }

                    try
                    {
                        __instance.StopTask();
                        HudNotificationManager.LogCustom("<color=#10B981>[Insta Farm] Harvested instantly!</color>");
                    }
                    catch { }
                }
            }
        }
    }

    // ==========================================
    // INSTA FILL POTS: POUR SOIL TASK PATCH
    // ==========================================
    [HarmonyPatch(typeof(PourSoilTask), "Update")]
    public static class PourSoilTaskUpdatePatch
    {
        [HarmonyPrefix]
        public static void Prefix(PourSoilTask __instance)
        {
            var mod = InstaFillPotsModule.Instance;
            if (mod == null || !mod.Enabled || __instance == null || !__instance.TaskActive) return;

            var container = __instance.growContainer ?? __instance._growContainer;
            if (container != null && !container.IsFullyFilledWithSoil)
            {
                // Cut open the soil bag if not already opened
                if (__instance._pourableSoil != null && !__instance._pourableSoil.IsOpen)
                {
                    try { __instance._pourableSoil.Cut(); } catch { }
                    try { __instance._pourableSoil.FinishCut(); } catch { }
                }

                var soilDef = __instance._soilDefinition;
                if (soilDef == null && __instance._pourableSoil != null)
                {
                    soilDef = __instance._pourableSoil.SoilDefinition;
                }
                string soilID = soilDef != null ? soilDef.ID : "Soil";
                float cap = container.SoilCapacity > 0 ? container.SoilCapacity : 1f;
                int uses = soilDef != null && soilDef.Uses > 0 ? soilDef.Uses : 10;

                // Assign SoilDefinition directly to container
                if (soilDef != null)
                {
                    try { container.SetSoil(soilDef); } catch { }
                    try { container.CurrentSoil = soilDef; } catch { }
                }

                try { container.SetSoilAmount(cap); } catch { }
                try { container.SetRemainingSoilUses(uses); } catch { }

                try { container.SetSoilData_Server(soilID, cap, uses); } catch { }
                try { container.SetSoilData_Client(null, soilID, cap, uses); } catch { }

                // If container is a Pot, set soil state to Flat and activate Dirt_Flat
                var pot = container.TryCast<Pot>();
                if (pot != null)
                {
                    try { pot.SetSoilState(Pot.ESoilState.Flat); } catch { }
                    try
                    {
                        if (pot.Dirt_Flat != null) pot.Dirt_Flat.gameObject.SetActive(true);
                        if (pot.Dirt_Parted != null) pot.Dirt_Parted.gameObject.SetActive(false);
                    }
                    catch { }
                }

                // 100% surface coverage
                if (container.SurfaceCover != null)
                {
                    try { container.SurfaceCover.CurrentCoverage = 1f; } catch { }
                }

                try { container.RefreshSoilVisuals(); } catch { }
                try { container.SyncSoilData(); } catch { }

                // Complete native task
                try { __instance.FullyCovered(); } catch { }
                try { __instance.Success(); } catch { }
                try { __instance.StopTask(); } catch { }

                HudNotificationManager.LogCustom("<color=#10B981>[Insta Fill Pots] Soil filled to 100%!</color>");
            }
        }
    }

    // ==========================================
    // INSTA FILL POTS: POT CAN ACCEPT SEED PATCH
    // ==========================================
    [HarmonyPatch(typeof(Pot), nameof(Pot.CanAcceptSeed))]
    public static class PotCanAcceptSeedPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Pot __instance, ref bool __result, ref string reason)
        {
            var mod = InstaFillPotsModule.Instance;
            if (mod == null || !mod.Enabled || __instance == null) return true;

            // If the pot already has soil and no plant, always allow planting
            if (__instance.IsFullyFilledWithSoil || __instance.NormalizedSoilAmount >= 0.9f || __instance.Dirt_Flat != null && __instance.Dirt_Flat.gameObject.activeSelf)
            {
                if (__instance.Plant == null && !__instance.ContainsGrowable())
                {
                    __result = true;
                    reason = string.Empty;
                    return false; // Skip original restrictive check
                }
            }

            return true;
        }
    }

    // ==========================================
    // INSTA PLANT: SOW SEED TASK PATCH
    // ==========================================
    [HarmonyPatch(typeof(SowSeedTask), "Update")]
    public static class SowSeedTaskUpdatePatch
    {
        [HarmonyPrefix]
        public static void Prefix(SowSeedTask __instance)
        {
            var mod = InstaPlantModule.Instance;
            if (mod == null || !mod.Enabled || __instance == null) return;

            if (!__instance.successfullyPlanted && __instance.pot != null)
            {
                try
                {
                    __instance.Success();
                    HudNotificationManager.LogCustom("<color=#10B981>[Insta Plant] Seed planted instantly!</color>");
                }
                catch
                {
                    try
                    {
                        string seedID = __instance.definition != null ? __instance.definition.ID : "WeedSeed";
                        __instance.pot.PlantSeed_Server(seedID, 1.0f);
                        __instance.StopTask();
                        HudNotificationManager.LogCustom("<color=#10B981>[Insta Plant] Seed planted!</color>");
                    }
                    catch { }
                }
            }
        }
    }

    // ==========================================
    // INSTA WATER PLANTS: GROW CONTAINER POUR TASK (POUR WATER)
    // ==========================================
    [HarmonyPatch(typeof(GrowContainerPourTask), nameof(GrowContainerPourTask.Update))]
    public static class GrowContainerPourWaterTaskPatch
    {
        [HarmonyPrefix]
        public static void Prefix(GrowContainerPourTask __instance)
        {
            var mod = InstaWaterPlantsModule.Instance;
            if (mod == null || !mod.Enabled || __instance == null || !__instance.TaskActive) return;

            // Check if this is a water task (PourWaterTask)
            var waterTask = __instance.TryCast<PourWaterTask>();
            if (waterTask != null)
            {
                var container = waterTask.growContainer;
                if (container != null)
                {
                    float cap = container.MoistureCapacity > 0 ? container.MoistureCapacity : 1f;

                    try { container.SetMoistureAmount(cap); } catch { }
                    try { container.SetMoistureData_Server(cap); } catch { }
                    try { container.SetMoistureData_Client(null, cap); } catch { }
                    try { waterTask.TargetReached(); } catch { }
                    try { waterTask.Success(); } catch { }
                    try { waterTask.StopTask(); } catch { }

                    HudNotificationManager.LogCustom("<color=#10B981>[Insta Water] Watered to 100%!</color>");
                }
            }
        }
    }

    // ==========================================
    // INSTA PACKAGE: PACKAGING STATION INTERACTED / USE
    // ==========================================
    [HarmonyPatch(typeof(PackagingStation), nameof(PackagingStation.Interacted))]
    public static class PackagingStationInteractedPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(PackagingStation __instance)
        {
            var mod = InstaPackageModule.Instance;
            if (mod == null || !mod.Enabled || __instance == null) return true;

            try
            {
                // If station has materials ready, pack everything available
                if (__instance.PackagingSlot != null && __instance.PackagingSlot.ItemInstance != null &&
                    __instance.ProductSlot != null && __instance.ProductSlot.ItemInstance != null)
                {
                    int packed = 0;
                    while (__instance.PackagingSlot.ItemInstance != null && 
                           __instance.ProductSlot.ItemInstance != null && 
                           packed < 100)
                    {
                        var beforePackaging = __instance.PackagingSlot.ItemInstance.Quantity;
                        var beforeProduct = __instance.ProductSlot.ItemInstance.Quantity;

                        __instance.PackSingleInstance();
                        packed++;

                        var afterPackaging = __instance.PackagingSlot.ItemInstance != null ? __instance.PackagingSlot.ItemInstance.Quantity : 0;
                        var afterProduct = __instance.ProductSlot.ItemInstance != null ? __instance.ProductSlot.ItemInstance.Quantity : 0;

                        // If quantity didn't change, break to prevent infinite loop
                        if (afterPackaging == beforePackaging && afterProduct == beforeProduct)
                        {
                            break;
                        }
                    }

                    if (packed > 0)
                    {
                        try { __instance.UpdatePackagingVisuals(); } catch { }
                        try { __instance.UpdateProductVisuals(); } catch { }
                        HudNotificationManager.LogCustom($"<color=#10B981>[Insta Package] {packed} packages completed instantly!</color>");
                        return false; // Skip minigame, packaged directly
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[Insta Package] Error in Interacted: {ex.Message}");
            }

            return true;
        }
    }

    // ==========================================
    // INSTA PACKAGE: TASK LATEUPDATE (FALLBACK FOR MINIGAME)
    // ==========================================
    [HarmonyPatch(typeof(Task), nameof(Task.LateUpdate))]
    public static class TaskLateUpdatePackagePatch
    {
        private static IntPtr _lastProcessedTask = IntPtr.Zero;

        [HarmonyPrefix]
        public static void Prefix(Task __instance)
        {
            var mod = InstaPackageModule.Instance;
            if (mod == null || !mod.Enabled || __instance == null || !__instance.TaskActive) return;

            // Safe 64-bit pointer handling
            IntPtr ptr = __instance.Pointer;
            if (_lastProcessedTask == ptr) return;

            var pkgTask = __instance.TryCast<PackageProductTask>();
            if (pkgTask != null)
            {
                _lastProcessedTask = ptr;
                try
                {
                    if (pkgTask.Packaging != null)
                    {
                        var products = pkgTask.Products;
                        if (products != null)
                        {
                            for (int i = 0; i < products.Count; i++)
                            {
                                var prod = products[i];
                                if (prod != null)
                                {
                                    try { pkgTask.Packaging.PackProduct(prod); } catch { }
                                }
                            }
                        }

                        if (!pkgTask.Packaging.IsFull)
                        {
                            try { pkgTask.Packaging.FullyPacked(); } catch { }
                        }
                        if (!pkgTask.Packaging.IsSealed)
                        {
                            try { pkgTask.Packaging.Seal(); } catch { }
                        }
                    }

                    try { pkgTask.FullyPacked(); } catch { }
                    try { pkgTask.Sealed(); } catch { }
                    try { pkgTask.ReachedOutput(); } catch { }
                    try { pkgTask.Success(); } catch { }
                    try { pkgTask.StopTask(); } catch { }

                    HudNotificationManager.LogCustom("<color=#10B981>[Insta Package] Packaging completed!</color>");
                }
                catch { }
                return;
            }

            var mk2Task = __instance.TryCast<PackageProductTaskMk2>();
            if (mk2Task != null)
            {
                _lastProcessedTask = ptr;
                try
                {
                    if (mk2Task.Packaging != null)
                    {
                        if (!mk2Task.Packaging.IsFull)
                        {
                            try { mk2Task.Packaging.FullyPacked(); } catch { }
                        }
                        if (!mk2Task.Packaging.IsSealed)
                        {
                            try { mk2Task.Packaging.Seal(); } catch { }
                        }
                    }

                    if (mk2Task.station != null && mk2Task.station.PackagingTool != null)
                    {
                        try { mk2Task.station.PackagingTool.Finalize(); } catch { }
                    }

                    try { mk2Task.Success(); } catch { }
                    try { mk2Task.StopTask(); } catch { }

                    HudNotificationManager.LogCustom("<color=#10B981>[Insta Package Mk2] Packaging completed!</color>");
                }
                catch { }
            }
        }
    }
}

