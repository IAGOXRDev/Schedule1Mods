using System;
using MelonLoader;
using UnityEngine;
using Il2CppScheduleOne.PlayerScripts;

namespace ArfSchedule1.Modules
{
    public class InfiniteAmmoModule : IModModule
    {
        public static InfiniteAmmoModule Instance { get; private set; }

        public string Id => "InfiniteBullets";
        public string Name => "Infinite Bullets";
        public string Description => "Infinite ammunition: never spend bullets or need to reload";
        public string Category => "Stuff";

        public bool Enabled
        {
            get => _prefEnabled?.Value ?? false;
            set
            {
                if (_prefEnabled != null)
                {
                    _prefEnabled.Value = value;
                    _prefCategory.SaveToFile();
                }
            }
        }

        public bool IsExpanded { get; set; } = false;

        private MelonPreferences_Category _prefCategory;
        private MelonPreferences_Entry<bool> _prefEnabled;

        public InfiniteAmmoModule()
        {
            Instance = this;
        }

        public void OnInitialize()
        {
            _prefCategory = MelonPreferences.CreateCategory("ArfSchedule1_InfBullets", "Schedule I - Infinite Bullets");
            _prefEnabled = _prefCategory.CreateEntry("Enabled", false, "Enable Infinite Bullets");
            _prefCategory.SaveToFile();
        }

        public void OnUpdate(PlayerMovement pm)
        {
            // Handled by Harmony patches in WeaponPatches
        }

        public void OnDrawSettings()
        {
            GUILayout.BeginVertical();
            GUILayout.Label("<color=#10B981>✓</color> Keeps equipped weapon magazine permanently full.");
            GUILayout.Label("<color=#9CA3AF>Consumes no reserve ammo and allows uninterrupted firing.</color>");
            GUILayout.EndVertical();
        }
    }
}
