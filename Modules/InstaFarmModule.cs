using System;
using MelonLoader;
using UnityEngine;
using Il2CppScheduleOne.PlayerScripts;

namespace ArfSchedule1.Modules
{
    public class InstaFarmModule : IModModule
    {
        public static InstaFarmModule Instance { get; private set; }

        public string Id => "InstaFarm";
        public string Name => "Insta Farm";
        public string Description => "Instantly harvests all crop yields and buds when interacting with a mature plant.";
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
            _prefCategory = MelonPreferences.CreateCategory("ArfSchedule1_InstaFarm", "Insta Farm Settings");
            _prefEnabled = _prefCategory.CreateEntry("Enabled", false, "Enable Insta Farm");
        }

        public void OnUpdate(PlayerMovement pm)
        {
        }

        public void OnDrawSettings()
        {
            GUILayout.Label("<color=#9CA3AF>When starting the pruning/shears minigame, instantly harvests all crop yields straight to your inventory without trimming each bud manually.</color>",
                new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true, fontSize = 11 });
        }
    }
}
