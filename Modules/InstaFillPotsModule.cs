using System;
using MelonLoader;
using UnityEngine;
using Il2CppScheduleOne.PlayerScripts;

namespace ArfSchedule1.Modules
{
    public class InstaFillPotsModule : IModModule
    {
        public static InstaFillPotsModule Instance { get; private set; }

        public string Id => "InstaFillPots";
        public string Name => "Insta Fill Pots";
        public string Description => "Instantly fills plant pots with soil to 100% capacity.";
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
            _prefCategory = MelonPreferences.CreateCategory("ArfSchedule1_InstaFillPots", "Insta Fill Pots Settings");
            _prefEnabled = _prefCategory.CreateEntry("Enabled", false, "Enable Insta Fill Pots");
        }

        public void OnUpdate(PlayerMovement pm)
        {
        }

        public void OnDrawSettings()
        {
            GUILayout.Label("<color=#9CA3AF>When pouring soil into any pot, instantly fills it to 100% capacity without having to pour the entire soil bag.</color>",
                new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true, fontSize = 11 });
        }
    }
}
