using System;
using MelonLoader;
using UnityEngine;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.PlayerScripts;

namespace ArfSchedule1.Modules
{
    public class InstaRespawnNPCModule : IModModule
    {
        public static InstaRespawnNPCModule Instance { get; private set; }

        public string Id => "InstaRespawnNPC";
        public string Name => "Insta Respawn NPC";
        public string Description => "Instantly revives and respawns NPCs whenever they die";
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

        private float _timer = 0f;
        private NPC[] _cachedNpcs = null;

        public InstaRespawnNPCModule()
        {
            Instance = this;
        }

        public void OnInitialize()
        {
            _prefCategory = MelonPreferences.CreateCategory("ArfSchedule1_InstaRespawn", "Schedule I - Insta Respawn NPC");
            _prefEnabled = _prefCategory.CreateEntry("Enabled", false, "Enable Insta Respawn NPC");
            _prefCategory.SaveToFile();
        }

        public void OnUpdate(PlayerMovement pm)
        {
            if (!Enabled) return;

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _timer = 0.4f;
                ReviveDeadNPCs();
            }
        }

        public void ReviveDeadNPCs()
        {
            try
            {
                _cachedNpcs = UnityEngine.Object.FindObjectsOfType<NPC>();
            }
            catch
            {
                return;
            }

            if (_cachedNpcs == null || _cachedNpcs.Length == 0) return;

            for (int i = 0; i < _cachedNpcs.Length; i++)
            {
                var npc = _cachedNpcs[i];
                if (npc == null || npc.WasCollected) continue;

                if (npc.Health != null && (npc.Health.IsDead || npc.Health.IsKnockedOut))
                {
                    try
                    {
                        npc.Health.Revive();
                        npc.Health.RestoreHealth();
                    }
                    catch { }
                }
            }
        }

        public void OnDrawSettings()
        {
            GUILayout.BeginVertical();
            GUILayout.Label("<color=#10B981>✓</color> Detects any dead or knocked-out NPC and revives them immediately.");
            GUILayout.Label("<color=#9CA3AF>Restores their health to 100% and restarts their normal game state.</color>");
            GUILayout.EndVertical();
        }
    }
}
