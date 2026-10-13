using System;
using MelonLoader;
using UnityEngine;
using Il2CppScheduleOne.PlayerScripts;

namespace ArfSchedule1.Modules
{
    public class InstaWaterPlantsModule : IModModule
    {
        public static InstaWaterPlantsModule Instance { get; private set; }

        public string Id => "InstaWaterPlants";
        public string Name => "Insta Water Plants";
        public string Description => "Instantly waters plants and pots to 100% full hydration.";
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

        public void OnInitialize()
        {
            Instance = this;
            _prefCategory = MelonPreferences.CreateCategory("ArfSchedule1_InstaWaterPlants", "Insta Water Plants Settings");
            _prefEnabled = _prefCategory.CreateEntry("Enabled", false, "Enable Insta Water Plants");
        }

        public void OnUpdate(PlayerMovement pm)
        {
        }

        public void OnDrawSettings()
        {
            GUILayout.Label("<color=#9CA3AF>When watering crops with a watering can, immediately fills pot soil moisture to 100%.</color>",
                new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true, fontSize = 11 });
        }
    }
}
