using System;
using MelonLoader;
using UnityEngine;
using Il2CppScheduleOne.PlayerScripts;
using ArfSchedule1.UI;

namespace ArfSchedule1.Modules
{
    public class FlightModule : IModModule
    {
        public static FlightModule Instance { get; private set; }

        public string Id => "Flight";
        public string Name => "Flight";
        public string Description => "Fly freely in any direction with 6 degrees of freedom (WASD + Space/Ctrl)";
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
                }
            }
        }

        public bool IsExpanded { get; set; } = false;

        public float Speed
        {
            get => _prefSpeed?.Value ?? 12f;
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

        public FlightModule()
        {
            Instance = this;
        }

        public void OnInitialize()
        {
            _prefCategory = MelonPreferences.CreateCategory("ArfSchedule1_Flight", "Schedule I - Flight");
            _prefEnabled = _prefCategory.CreateEntry("Enabled", false, "Enable Flight");
            _prefSpeed = _prefCategory.CreateEntry("Speed", 12f, "Flight Speed");
            _prefBoost = _prefCategory.CreateEntry("BoostMultiplier", 2.5f, "Shift Boost Multiplier");
            _prefCategory.SaveToFile();
        }

        public void OnUpdate(PlayerMovement pm)
        {
            if (!Enabled || pm == null || pm.Controller == null || MenuManager.IsMenuOpen) return;

            // Nullify gravity while flying
            pm.movementY = 0f;
            pm.residualVelocityForce = 0f;

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
                pm.Controller.Move(moveDir * currentSpeed * Time.deltaTime);
            }
        }

        public void OnDrawSettings()
        {
            GUILayout.BeginVertical();

            GUILayout.Label($"Flight Speed: <color=#00FFFF>{Speed:F0} m/s</color>");
            float newSpeed = GUILayout.HorizontalSlider(Speed, 4f, 40f);
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
            GUILayout.Label("<color=#9CA3AF>Controls: [W/A/S/D] Fly | [Space] Up | [Ctrl/C] Down | [Shift] Boost</color>");

            GUILayout.EndVertical();
        }
    }
}
