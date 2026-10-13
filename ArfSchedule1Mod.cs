using System;
using MelonLoader;
using UnityEngine;
using HarmonyLib;
using Il2CppScheduleOne.PlayerScripts;
using ArfSchedule1.Modules;
using ArfSchedule1.UI;

[assembly: MelonInfo(typeof(ArfSchedule1.ArfSchedule1Mod), "ArfSchedule1", "1.5.0", "IAGOXR")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace ArfSchedule1
{
    public class ArfSchedule1Mod : MelonMod
    {
        public static ArfSchedule1Mod Instance { get; private set; }

        public BhopModule Bhop { get; private set; }
        public FlightModule Flight { get; private set; }
        public NoclipModule Noclip { get; private set; }
        public EspModule Esp { get; private set; }
        public AimbotModule Aimbot { get; private set; }
        public RapidFireModule RapidFire { get; private set; }
        public InfiniteAmmoModule InfiniteAmmo { get; private set; }
        public NoRecoilModule NoRecoil { get; private set; }
        public GodModeModule GodMode { get; private set; }
        public HateModeModule HateMode { get; private set; }
        public InstaRespawnNPCModule InstaRespawn { get; private set; }
        public InstaGrowModule InstaGrow { get; private set; }
        public InstaFarmModule InstaFarm { get; private set; }
        public InstaFillPotsModule InstaFillPots { get; private set; }
        public InstaPlantModule InstaPlant { get; private set; }
        public InstaWaterPlantsModule InstaWaterPlants { get; private set; }
        public InstaPackageModule InstaPackage { get; private set; }
        public ItemSpawnerModule ItemSpawner { get; private set; }
        public MenuManager Menu { get; private set; }

        private PlayerMovement _cachedMovement = null;

        public override void OnInitializeMelon()
        {
            Instance = this;

            // Movement Modules
            Bhop = new BhopModule();
            Bhop.OnInitialize();

            Flight = new FlightModule();
            Flight.OnInitialize();

            Noclip = new NoclipModule();
            Noclip.OnInitialize();

            // Farming Modules
            InstaGrow = new InstaGrowModule();
            InstaGrow.OnInitialize();

            InstaFarm = new InstaFarmModule();
            InstaFarm.OnInitialize();

            InstaFillPots = new InstaFillPotsModule();
            InstaFillPots.OnInitialize();

            InstaPlant = new InstaPlantModule();
            InstaPlant.OnInitialize();

            InstaWaterPlants = new InstaWaterPlantsModule();
            InstaWaterPlants.OnInitialize();

            InstaPackage = new InstaPackageModule();
            InstaPackage.OnInitialize();

            // Stuff Modules
            GodMode = new GodModeModule();
            GodMode.OnInitialize();

            Esp = new EspModule();
            Esp.OnInitialize();

            Aimbot = new AimbotModule();
            Aimbot.OnInitialize();

            RapidFire = new RapidFireModule();
            RapidFire.OnInitialize();

            InfiniteAmmo = new InfiniteAmmoModule();
            InfiniteAmmo.OnInitialize();

            NoRecoil = new NoRecoilModule();
            NoRecoil.OnInitialize();

            HateMode = new HateModeModule();
            HateMode.OnInitialize();

            InstaRespawn = new InstaRespawnNPCModule();
            InstaRespawn.OnInitialize();

            // Item Spawner
            ItemSpawner = new ItemSpawnerModule();

            Menu = new MenuManager();
            // Movement tab
            Menu.RegisterModule(Bhop);
            Menu.RegisterModule(Flight);
            Menu.RegisterModule(Noclip);
            // Farming tab
            Menu.RegisterModule(InstaGrow);
            Menu.RegisterModule(InstaFarm);
            Menu.RegisterModule(InstaFillPots);
            Menu.RegisterModule(InstaPlant);
            Menu.RegisterModule(InstaWaterPlants);
            Menu.RegisterModule(InstaPackage);
            // Stuff tab
            Menu.RegisterModule(GodMode);
            Menu.RegisterModule(Aimbot);
            Menu.RegisterModule(Esp);
            Menu.RegisterModule(RapidFire);
            Menu.RegisterModule(InfiniteAmmo);
            Menu.RegisterModule(NoRecoil);
            Menu.RegisterModule(HateMode);
            Menu.RegisterModule(InstaRespawn);

            MelonLogger.Msg("=================================================");
            MelonLogger.Msg("  ArfSchedule1 - Mod Client v1.5.0 (ArfHook Edition)");
            MelonLogger.Msg("  * Interactive IMGUI Menu toggled with [INSERT]");
            MelonLogger.Msg("  * Tabs: Movement, Farming, Stuff, Items (Item Spawner), About");
            MelonLogger.Msg("  * Item Spawner: Search, filter & spawn any item instantly");
            MelonLogger.Msg("  * God Mode: Total Invulnerability & Damage Immunity");
            MelonLogger.Msg("  * Flight & Noclip: 6-DoF Free Flying & Wallpass");
            MelonLogger.Msg("  * Hate Mode: All NPCs Enraged & Attack Player");
            MelonLogger.Msg("  * Insta Respawn NPC: Instant Revive on Death");
            MelonLogger.Msg("  * Farming Automation: Insta Grow, Farm, Fill Pots, Plant, Water & Package");
            MelonLogger.Msg("  Press [INSERT] in-game to open the Mod Menu!");
            MelonLogger.Msg("=================================================");
        }

        public override void OnUpdate()
        {
            var player = Player.Local;
            if (player == null)
            {
                _cachedMovement = null;
                Menu?.OnUpdate(null);
                return;
            }

            if (_cachedMovement == null || _cachedMovement.WasCollected)
            {
                _cachedMovement = player.GetComponent<PlayerMovement>();
                if (_cachedMovement == null)
                    _cachedMovement = player.GetComponentInChildren<PlayerMovement>();
            }

            Menu?.OnUpdate(_cachedMovement);
        }

        public override void OnGUI()
        {
            Menu?.OnGUI(_cachedMovement);
        }
    }

    /// <summary>
    /// Harmony patch on PlayerMovement.Move.
    /// </summary>
    [HarmonyPatch(typeof(PlayerMovement), nameof(PlayerMovement.Move))]
    public static class PlayerMovementMovePatch
    {
        [HarmonyPrefix]
        public static void Prefix(PlayerMovement __instance)
        {
            // Block character displacement while Insert menu is open
            if (MenuManager.IsMenuOpen)
            {
                __instance.horizontalAxis = 0f;
                __instance.verticalAxis = 0f;
                return;
            }

            // Nullify input and vertical gravity if Flight or Noclip are active
            var flight = FlightModule.Instance;
            var noclip = NoclipModule.Instance;
            if ((flight != null && flight.Enabled) || (noclip != null && noclip.Enabled))
            {
                __instance.horizontalAxis = 0f;
                __instance.verticalAxis = 0f;
                __instance.movementY = 0f;
                __instance.residualVelocityForce = 0f;
                return;
            }

            var bhop = BhopModule.Instance;
            if (bhop != null && bhop.IsBhopActive && !__instance.IsGrounded)
            {
                // In air during bhop, nullify native input to give pure camera-oriented steering
                __instance.horizontalAxis = 0f;
                __instance.verticalAxis = 0f;
            }
        }

        [HarmonyPostfix]
        public static void Postfix(PlayerMovement __instance)
        {
            if (MenuManager.IsMenuOpen) return;

            var flight = FlightModule.Instance;
            var noclip = NoclipModule.Instance;
            if ((flight != null && flight.Enabled) || (noclip != null && noclip.Enabled)) return;

            var bhop = BhopModule.Instance;
            if (bhop != null && bhop.IsBhopActive)
            {
                bhop.ApplyBhopVelocity(__instance);
            }
        }
    }
}
