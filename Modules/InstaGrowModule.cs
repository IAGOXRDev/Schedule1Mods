using System;
using MelonLoader;
using UnityEngine;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.Growing;
using Il2CppScheduleOne.ObjectScripts;

namespace ArfSchedule1.Modules
{
    public class InstaGrowModule : IModModule
    {
        public static InstaGrowModule Instance { get; private set; }

        public string Id => "InstaGrow";
        public string Name => "Insta Grow";
        public string Description => "Instantly accelerates all planted crops to 100% full maturity.";
        public string Category => "Farming";

        public bool Enabled
        {
            get => _prefEnabled?.Value ?? false;
            set
            {
                if (_prefEnabled != null)
                {
                    _prefEnabled.Value = value;
                    _prefCategory?.SaveToFile();
                }
            }
        }

        public bool IsExpanded { get; set; } = false;

        private MelonPreferences_Category _prefCategory;
        private MelonPreferences_Entry<bool> _prefEnabled;

        private float _lastScanTime = 0f;

        public void OnInitialize()
        {
            Instance = this;
            _prefCategory = MelonPreferences.CreateCategory("ArfSchedule1_InstaGrow", "Insta Grow Settings");
            _prefEnabled = _prefCategory.CreateEntry("Enabled", false, "Enable Insta Grow");
        }

        public void OnUpdate(PlayerMovement pm)
        {
            if (!Enabled) return;

            // Scan and force growth every 0.5 seconds to preserve CPU performance
            if (Time.time - _lastScanTime < 0.5f) return;
            _lastScanTime = Time.time;

            try
            {
                var plants = UnityEngine.Object.FindObjectsOfType<Plant>();
                if (plants != null)
                {
                    for (int i = 0; i < plants.Length; i++)
                    {
                        var plant = plants[i];
                        if (plant != null && !plant.IsFullyGrown)
                        {
                            GrowPlantInstantly(plant);
                        }
                    }
                }
            }
            catch { }
        }

        public static void GrowPlantInstantly(Plant plant)
        {
            if (plant == null) return;

            try
            {
                plant.SetNormalizedGrowthProgress(1.0f);
            }
            catch { }

            try
            {
                plant.GrowthDone();
            }
            catch { }

            try
            {
                plant.UpdateVisuals();
            }
            catch { }

            if (plant.Pot != null)
            {
                try
                {
                    plant.Pot.SetGrowthProgress_Server(1.0f);
                }
                catch { }
            }
        }

        public void OnDrawSettings()
        {
            GUILayout.Label("<color=#9CA3AF>All planted and newly sown crops will reach 100% maturity immediately with ready-to-harvest yields.</color>",
                new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true, fontSize = 11 });
        }
    }
}
