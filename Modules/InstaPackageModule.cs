using System;
using MelonLoader;
using UnityEngine;
using Il2CppScheduleOne.PlayerScripts;

namespace ArfSchedule1.Modules
{
    public class InstaPackageModule : IModModule
    {
        public static InstaPackageModule Instance { get; private set; }

        public string Id => "InstaPackage";
        public string Name => "Insta Package";
        public string Description => "Instantly packages product batches when interacting with the packaging station.";
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
            _prefCategory = MelonPreferences.CreateCategory("ArfSchedule1_InstaPackage", "Insta Package Settings");
            _prefEnabled = _prefCategory.CreateEntry("Enabled", false, "Enable Insta Package");
        }

        public void OnUpdate(PlayerMovement pm)
        {
        }

        public void OnDrawSettings()
        {
            GUILayout.Label("<color=#9CA3AF>When interacting with the packaging station (Standard or Mk2), automatically batches, fills and seals all available products without manual dragging.</color>",
                new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true, fontSize = 11 });
        }
    }
}
