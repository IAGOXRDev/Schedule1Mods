using System;
using MelonLoader;
using UnityEngine;
using Il2CppScheduleOne.PlayerScripts;
using ArfSchedule1.UI;

namespace ArfSchedule1.Modules
{
    public class NoclipModule : IModModule
    {
        public static NoclipModule Instance { get; private set; }

        public string Id => "Noclip";
        public string Name => "Noclip";
        public string Description => "Pass through walls and obstacles while flying freely";
        public string Category => "Movement";

        public bool Enabled
        {
            get => _prefEnabled?.Value ?? false;
            set
            {
                if (_prefEnabled != null)
                {
                    _prefEnabled.Value = value;
                    _prefCategory.SaveToFile();
                    if (!value)
                    {
                        RestoreCollision();
                    }
                }
            }
        }

        public bool IsExpanded { get; set; } = false;

        public float Speed
        {
            get => _prefSpeed?.Value ?? 14f;
            set
            {
                if (_prefSpeed != null)
                {
                    _prefSpeed.Value = value;
                    _prefCategory.SaveToFile();
                }
            }
        }

        public float BoostMultiplier
        {
            get => _prefBoost?.Value ?? 2.5f;
            set
            {
                if (_prefBoost != null)
                {
                    _prefBoost.Value = value;
                    _prefCategory.SaveToFile();
                }
            }
        }

        private MelonPreferences_Category _prefCategory;
        private MelonPreferences_Entry<bool> _prefEnabled;
        private MelonPreferences_Entry<float> _prefSpeed;
        private MelonPreferences_Entry<float> _prefBoost;

        private CharacterController _disabledController = null;
        private CapsuleCollider _disabledCapCol = null;

        public NoclipModule()
        {
            Instance = this;
        }

        public void OnInitialize()
        {
            _prefCategory = MelonPreferences.CreateCategory("ArfSchedule1_Noclip", "Schedule I - Noclip");
            _prefEnabled = _prefCategory.CreateEntry("Enabled", false, "Enable Noclip");
            _prefSpeed = _prefCategory.CreateEntry("Speed", 14f, "Noclip Speed");
            _prefBoost = _prefCategory.CreateEntry("BoostMultiplier", 2.5f, "Shift Boost Multiplier");
            _prefCategory.SaveToFile();
        }

        public void OnUpdate(PlayerMovement pm)
        {
            if (!Enabled)
            {
                RestoreCollision();
                return;
            }

            if (pm == null || MenuManager.IsMenuOpen) return;

            // Nullify gravity
            pm.movementY = 0f;
            pm.residualVelocityForce = 0f;

            // Disable collisions to pass through geometry
            if (pm.Controller != null && pm.Controller.enabled)
            {
                _disabledController = pm.Controller;
                pm.Controller.enabled = false;
            }

            var localPlayer = Player.Local;
            if (localPlayer != null && localPlayer.CapCol != null && localPlayer.CapCol.enabled)
            {
                _disabledCapCol = localPlayer.CapCol;
                localPlayer.CapCol.enabled = false;
            }

            Camera cam = Camera.main;
            Vector3 camFwd = cam != null ? cam.transform.forward : pm.transform.forward;
            Vector3 camRight = cam != null ? cam.transform.right : pm.transform.right;

            Vector3 moveDir = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) moveDir += camFwd;
            if (Input.GetKey(KeyCode.S)) moveDir -= camFwd;
            if (Input.GetKey(KeyCode.D)) moveDir += camRight;
            if (Input.GetKey(KeyCode.A)) moveDir -= camRight;
            if (Input.GetKey(KeyCode.Space)) moveDir += Vector3.up;
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C)) moveDir -= Vector3.up;

            if (moveDir.sqrMagnitude > 0.001f)
            {
                moveDir.Normalize();
                float currentSpeed = Speed * (Input.GetKey(KeyCode.LeftShift) ? BoostMultiplier : 1.0f);
                pm.transform.position += moveDir * currentSpeed * Time.deltaTime;
            }
        }

        public void RestoreCollision()
        {
            if (_disabledController != null)
            {
                try { _disabledController.enabled = true; } catch { }
                _disabledController = null;
            }
            if (_disabledCapCol != null)
            {
                try { _disabledCapCol.enabled = true; } catch { }
                _disabledCapCol = null;
            }

            var localPlayer = Player.Local;
            if (localPlayer != null)
            {
                if (localPlayer.CharacterController != null && !localPlayer.CharacterController.enabled)
                {
                    try { localPlayer.CharacterController.enabled = true; } catch { }
                }
                if (localPlayer.CapCol != null && !localPlayer.CapCol.enabled)
                {
                    try { localPlayer.CapCol.enabled = true; } catch { }
                }
            }
        }

        public void OnDrawSettings()
        {
            GUILayout.BeginVertical();

            GUILayout.Label($"Noclip Speed: <color=#00FFFF>{Speed:F0} m/s</color>");
            float newSpeed = GUILayout.HorizontalSlider(Speed, 4f, 50f);
            if (Math.Abs(newSpeed - Speed) > 0.5f)
            {
                Speed = (float)Math.Round(newSpeed, 0);
            }

            GUILayout.Space(4f);

            GUILayout.Label($"Shift Boost Multiplier: <color=#A78BFA>{BoostMultiplier:F1}x</color>");
            float newBoost = GUILayout.HorizontalSlider(BoostMultiplier, 1.5f, 5.0f);
            if (Math.Abs(newBoost - BoostMultiplier) > 0.1f)
            {
                BoostMultiplier = (float)Math.Round(newBoost, 1);
            }

            GUILayout.Space(4f);
            GUILayout.Label("<color=#10B981>✓</color> Pass through walls, doors, floors and vaults.");
            GUILayout.Label("<color=#9CA3AF>Controls: [W/A/S/D] Fly | [Space] Up | [Ctrl/C] Down | [Shift] Boost</color>");

            GUILayout.EndVertical();
        }
    }
}
