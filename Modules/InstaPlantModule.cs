using System;
using MelonLoader;
using UnityEngine;
using Il2CppScheduleOne.PlayerScripts;

namespace ArfSchedule1.Modules
{
    public class InstaPlantModule : IModModule
    {
        public static InstaPlantModule Instance { get; private set; }

        public string Id => "InstaPlant";
        public string Name => "Insta Plant";
        public string Description => "Instantly plants seeds into pots without waiting for planting animations.";
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
            _prefCategory = MelonPreferences.CreateCategory("ArfSchedule1_InstaPlant", "Insta Plant Settings");
            _prefEnabled = _prefCategory.CreateEntry("Enabled", false, "Enable Insta Plant");
        }

        public void OnUpdate(PlayerMovement pm)
        {
        }

        public void OnDrawSettings()
        {
            GUILayout.Label("<color=#9CA3AF>When sowing a seed into any prepared pot, the plant is sown instantly in 0 seconds.</color>",
                new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true, fontSize = 11 });
        }
    }
}
