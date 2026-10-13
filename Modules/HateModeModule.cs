using System;
using MelonLoader;
using UnityEngine;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.NPCs.Relation;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppFishNet.Object;

namespace ArfSchedule1.Modules
{
    public class HateModeModule : IModModule
    {
        public static HateModeModule Instance { get; private set; }

        public string Id => "HateMode";
        public string Name => "Hate Mode";
        public string Description => "Enrages all nearby NPCs to hate and attack the player";
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
                    if (value)
                    {
                        _timer = 0f; // Enrage immediately on activation
                    }
                }
            }
        }

        public bool IsExpanded { get; set; } = false;

        public float AggroRange
        {
            get => _prefRange?.Value ?? 100f;
            set
            {
                if (_prefRange != null)
                {
                    _prefRange.Value = value;
                    _prefCategory.SaveToFile();
                }
            }
        }

        private MelonPreferences_Category _prefCategory;
        private MelonPreferences_Entry<bool> _prefEnabled;
        private MelonPreferences_Entry<float> _prefRange;

        private float _timer = 0f;
        private int _enragedCount = 0;
        private NPC[] _cachedNpcs = null;

        public HateModeModule()
        {
            Instance = this;
        }

        public void OnInitialize()
        {
            _prefCategory = MelonPreferences.CreateCategory("ArfSchedule1_HateMode", "Schedule I - Hate Mode");
            _prefEnabled = _prefCategory.CreateEntry("Enabled", false, "Enable Hate Mode");
            _prefRange = _prefCategory.CreateEntry("AggroRange", 100f, "Aggro Range in meters");
            _prefCategory.SaveToFile();
        }

        public void OnUpdate(PlayerMovement pm)
        {
            if (!Enabled)
            {
                _enragedCount = 0;
                return;
            }

            var localPlayer = Player.Local;
            if (localPlayer == null) return;

            _timer -= Time.deltaTime;
            if (_timer <= 0f)
            {
                _timer = 60.0f; // Run once per minute to prevent performance stutter
                EnrageNPCs(localPlayer);
            }
        }

        private void EnrageNPCs(Player localPlayer)
        {
            try
            {
                _cachedNpcs = UnityEngine.Object.FindObjectsOfType<NPC>();
            }
            catch
            {
                return;
            }

            if (_cachedNpcs == null || _cachedNpcs.Length == 0)
            {
                _enragedCount = 0;
                return;
            }

            int count = 0;
            Vector3 playerPos = localPlayer.transform.position;
            NetworkObject playerNetObj = localPlayer.NetworkObject;
            if (playerNetObj == null)
            {
                playerNetObj = localPlayer.GetComponent<NetworkObject>();
            }

            for (int i = 0; i < _cachedNpcs.Length; i++)
            {
                var npc = _cachedNpcs[i];
                if (npc == null || npc.WasCollected) continue;

                var go = npc.gameObject;
                if (go == null || !go.activeInHierarchy) continue;

                if (npc.Health != null && (npc.Health.IsDead || npc.Health.IsKnockedOut)) continue;

                float dist = Vector3.Distance(playerPos, npc.transform.position);
                if (dist > AggroRange) continue;

                try
                {
                    // 1. Set relationship to maximum hatred (-100)
                    if (npc.RelationData != null)
                    {
                        npc.RelationData.SetRelationship(NPCRelationData.MinRelationship);
                    }

                    // 2. Notify health system that player attacked them
                    if (npc.Health != null)
                    {
                        npc.Health.NotifyAttackedByPlayer(localPlayer);
                    }

                    // 3. Trigger lethal attack response to draw weapons/fists
                    if (npc.Responses != null)
                    {
                        npc.Responses.RespondToLethalAttack(localPlayer, null);
                    }

                    // 4. Set combat target onto player
                    if (npc.Behaviour != null && npc.Behaviour.CombatBehaviour != null && playerNetObj != null)
                    {
                        npc.Behaviour.CombatBehaviour.SetTarget(playerNetObj);
                    }

                    count++;
                }
                catch { }
            }

            _enragedCount = count;
        }

        public void OnDrawSettings()
        {
            GUILayout.BeginVertical();

            GUILayout.Label($"Aggro Range: <color=#00FFFF>{AggroRange:F0} meters</color>");
            float newRange = GUILayout.HorizontalSlider(AggroRange, 20f, 250f);
            if (Math.Abs(newRange - AggroRange) > 1f)
            {
                AggroRange = (float)Math.Round(newRange, 0);
            }

            GUILayout.Space(4f);
            string statusColor = _enragedCount > 0 ? "#EF4444" : "#9CA3AF";
            GUILayout.Label($"Enraged NPCs in Range: <color={statusColor}><b>{_enragedCount}</b></color>");

            if (Enabled)
            {
                GUILayout.Label($"Next automatic enrage in: <color=#00FFFF><b>{Mathf.CeilToInt(_timer)}s</b></color> (Interval: 1 min)");
                if (GUILayout.Button("⚡ Enrage NPCs Now", GUILayout.Height(24)))
                {
                    var lp = Player.Local;
                    if (lp != null)
                    {
                        EnrageNPCs(lp);
                        _timer = 60.0f;
                    }
                }
            }

            GUILayout.Space(2f);
            GUILayout.Label("<color=#9CA3AF>Executes once per minute to avoid lag spikes. Sets NPC relations to maximum hostility.</color>");

            GUILayout.EndVertical();
        }
    }
}
