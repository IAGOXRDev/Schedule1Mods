using System;
using MelonLoader;
using UnityEngine;
using Il2CppScheduleOne.PlayerScripts;

namespace ArfSchedule1.Modules
{
    public class RapidFireModule : IModModule
    {
        public static RapidFireModule Instance { get; private set; }

        public string Id => "RapidFire";
        public string Name => "Rapid Fire";
        public string Description => "Continuous automatic fire and boosted fire rate";
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

        public float RoundsPerSecond
        {
            get => _prefRps?.Value ?? 16f;
            set
            {
                if (_prefRps != null)
                {
                    _prefRps.Value = value;
                    _prefCategory.SaveToFile();
                }
            }
        }

        public bool NoSpread
        {
            get => _prefNoSpread?.Value ?? true;
            set
            {
                if (_prefNoSpread != null)
                {
                    _prefNoSpread.Value = value;
                    _prefCategory.SaveToFile();
                }
            }
        }

        private MelonPreferences_Category _prefCategory;
        private MelonPreferences_Entry<bool> _prefEnabled;
        private MelonPreferences_Entry<float> _prefRps;
        private MelonPreferences_Entry<bool> _prefNoSpread;

        public RapidFireModule()
        {
            Instance = this;
        }

        public void OnInitialize()
        {
            _prefCategory = MelonPreferences.CreateCategory("ArfSchedule1_RapidFire", "Schedule I - Rapid Fire");
            _prefEnabled = _prefCategory.CreateEntry("Enabled", false, "Enable Rapid Fire");
            _prefRps = _prefCategory.CreateEntry("RoundsPerSecond", 16f, "Rounds per second");
            _prefNoSpread = _prefCategory.CreateEntry("NoSpread", true, "Max accuracy / No spread");
            _prefCategory.SaveToFile();
        }

        public void OnUpdate(PlayerMovement pm)
        {
            // Handled by Harmony patches in WeaponPatches
        }

        public void OnDrawSettings()
        {
            GUILayout.BeginVertical();

            GUILayout.Label($"Fire Rate: <color=#00FFFF>{RoundsPerSecond:F0} rounds/sec</color>");
            float newRps = GUILayout.HorizontalSlider(RoundsPerSecond, 5f, 35f);
            if (Math.Abs(newRps - RoundsPerSecond) > 0.5f)
            {
                RoundsPerSecond = (float)Math.Round(newRps, 0);
            }

            GUILayout.Space(6f);

            bool noSpread = GUILayout.Toggle(NoSpread, "  Maximum Accuracy (No bullet spread)");
            if (noSpread != NoSpread) NoSpread = noSpread;

            GUILayout.EndVertical();
        }
    }
}
