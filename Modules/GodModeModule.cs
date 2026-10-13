using System;
using MelonLoader;
using UnityEngine;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.PlayerScripts.Health;
using ArfSchedule1.UI;

namespace ArfSchedule1.Modules
{
    public class GodModeModule : IModModule
    {
        public static GodModeModule Instance { get; private set; }

        public string Id => "GodMode";
        public string Name => "God Mode";
        public string Description => "Total invulnerability: take no damage and never lose health";
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

        public bool LockMaxHealth
        {
            get => _prefLockMaxHealth?.Value ?? true;
            set
            {
                if (_prefLockMaxHealth != null)
                {
                    _prefLockMaxHealth.Value = value;
                    _prefCategory.SaveToFile();
                }
            }
        }

        public bool PreventAfflictions
        {
            get => _prefPreventAfflictions?.Value ?? true;
            set
            {
                if (_prefPreventAfflictions != null)
                {
                    _prefPreventAfflictions.Value = value;
                    _prefCategory.SaveToFile();
                }
            }
        }

        private MelonPreferences_Category _prefCategory;
        private MelonPreferences_Entry<bool> _prefEnabled;
        private MelonPreferences_Entry<bool> _prefLockMaxHealth;
        private MelonPreferences_Entry<bool> _prefPreventAfflictions;

        public GodModeModule()
        {
            Instance = this;
        }

        public void OnInitialize()
        {
            _prefCategory = MelonPreferences.CreateCategory("ArfSchedule1_GodMode", "Schedule I - God Mode");
            _prefEnabled = _prefCategory.CreateEntry("Enabled", false, "Enable God Mode");
            _prefLockMaxHealth = _prefCategory.CreateEntry("LockMaxHealth", true, "Keep health locked at maximum");
            _prefPreventAfflictions = _prefCategory.CreateEntry("PreventAfflictions", true, "Prevent lethal effects and afflictions");
            _prefCategory.SaveToFile();
        }

        public void OnUpdate(PlayerMovement pm)
        {
            if (!Enabled) return;

            var player = Player.Local;
            if (player == null) return;

            var health = player.Health;
            if (health == null) return;

            // Ensure player stays alive
            if (!health.IsAlive)
            {
                health.IsAlive = true;
            }

            // Restore health to maximum if damaged
            if (LockMaxHealth)
            {
                float max = PlayerHealth.MaxHealth > 0f ? PlayerHealth.MaxHealth : 100f;
                if (health.CurrentHealth < max)
                {
                    health.CurrentHealth = max;
                }
            }

            // Clear lethal effects and harmful afflictions
            if (PreventAfflictions && health.AfflictedWithLethalEffect)
            {
                health.SetAfflictedWithLethalEffect(false);
            }
        }

        public void OnDrawSettings()
        {
            GUILayout.BeginVertical();

            bool curLock = GUILayout.Toggle(LockMaxHealth, "  Force Continuous Maximum Health");
            if (curLock != LockMaxHealth)
            {
                LockMaxHealth = curLock;
            }

            bool curAfflict = GUILayout.Toggle(PreventAfflictions, "  Prevent Lethal Effects & Toxicity");
            if (curAfflict != PreventAfflictions)
            {
                PreventAfflictions = curAfflict;
            }

            var player = Player.Local;
            if (player != null && player.Health != null)
            {
                GUILayout.Space(4f);
                float curHp = player.Health.CurrentHealth;
                float maxHp = PlayerHealth.MaxHealth;
                GUILayout.Label($"<color=#10B981>Current Health: <b>{curHp:F0} / {maxHp:F0}</b></color>",
                    new GUIStyle(GUI.skin.label) { richText = true, fontSize = 11 });
            }

            GUILayout.EndVertical();
        }
    }
}
