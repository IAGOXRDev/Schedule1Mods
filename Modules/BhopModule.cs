using System;
using MelonLoader;
using UnityEngine;
using Il2CppScheduleOne.PlayerScripts;

namespace ArfSchedule1.Modules
{
    public class BhopModule : IModModule
    {
        public static BhopModule Instance { get; private set; }

        public string Id => "BunnyHop";
        public string Name => "BunnyHop";
        public string Description => "Auto-jump, air strafing momentum & uncapped speed";
        public string Category => "Movement";

        public bool Enabled
        {
            get => _prefEnabled?.Value ?? true;
            set
            {
                if (_prefEnabled != null)
                {
                    _prefEnabled.Value = value;
                    _prefCategory.SaveToFile();
                }
                if (!value)
                {
                    CurrentSpeed = 0f;
                    IsBhopping = false;
                }
            }
        }

        public bool IsExpanded { get; set; } = true;

        public bool AutoJump
        {
            get => _prefAutoJump?.Value ?? true;
            set { if (_prefAutoJump != null) { _prefAutoJump.Value = value; _prefCategory.SaveToFile(); } }
        }

        public float Acceleration
        {
            get => _prefAcceleration?.Value ?? 2.0f;
            set { if (_prefAcceleration != null) { _prefAcceleration.Value = value; _prefCategory.SaveToFile(); } }
        }

        public float BoostMultiplier
        {
            get => _prefBoostMultiplier?.Value ?? 3.0f;
            set { if (_prefBoostMultiplier != null) { _prefBoostMultiplier.Value = value; _prefCategory.SaveToFile(); } }
        }

        public float AirFriction
        {
            get => _prefAirFriction?.Value ?? 0.5f;
            set { if (_prefAirFriction != null) { _prefAirFriction.Value = value; _prefCategory.SaveToFile(); } }
        }

        public bool InfiniteStamina
        {
            get => _prefInfiniteStamina?.Value ?? true;
            set { if (_prefInfiniteStamina != null) { _prefInfiniteStamina.Value = value; _prefCategory.SaveToFile(); } }
        }

        public bool ShowSpeedometer
        {
            get => _prefShowSpeedometer?.Value ?? true;
            set { if (_prefShowSpeedometer != null) { _prefShowSpeedometer.Value = value; _prefCategory.SaveToFile(); } }
        }

        public bool CanLoseSpeed
        {
            get => _prefCanLoseSpeed?.Value ?? true;
            set { if (_prefCanLoseSpeed != null) { _prefCanLoseSpeed.Value = value; _prefCategory.SaveToFile(); } }
        }

        public float CurrentSpeed { get; private set; } = 0f;
        public bool IsBhopping { get; private set; } = false;
        public bool IsBhopActive => Enabled && IsBhopping && CurrentSpeed > 0.1f;

        private MelonPreferences_Category _prefCategory;
        private MelonPreferences_Entry<bool> _prefEnabled;
        private MelonPreferences_Entry<bool> _prefAutoJump;
        private MelonPreferences_Entry<float> _prefSpeedCap;
        private MelonPreferences_Entry<float> _prefAcceleration;
        private MelonPreferences_Entry<float> _prefAirFriction;
        private MelonPreferences_Entry<float> _prefGroundFriction;
        private MelonPreferences_Entry<float> _prefBoostMultiplier;
        private MelonPreferences_Entry<bool> _prefCanLoseSpeed;
        private MelonPreferences_Entry<bool> _prefInfiniteStamina;
        private MelonPreferences_Entry<bool> _prefShowSpeedometer;

        private float _jumpDebounce = 0f;

        public BhopModule()
        {
            Instance = this;
        }

        public void OnInitialize()
        {
            _prefCategory = MelonPreferences.CreateCategory("ArfSchedule1", "Schedule I - BunnyHop");
            _prefEnabled = _prefCategory.CreateEntry("Enabled", true, "Enable BunnyHop");
            _prefAutoJump = _prefCategory.CreateEntry("AutoJump", true, "Auto-jump while holding Space");
            _prefSpeedCap = _prefCategory.CreateEntry("SpeedCap", 100000.0f, "Maximum horizontal Bhop extra speed (uncapped)");
            _prefAcceleration = _prefCategory.CreateEntry("Acceleration", 2.0f, "Air strafe acceleration");
            _prefAirFriction = _prefCategory.CreateEntry("AirFriction", 0.5f, "Air friction deceleration when not strafing");
            _prefGroundFriction = _prefCategory.CreateEntry("GroundFriction", 12.0f, "Ground friction deceleration when stopping");
            _prefBoostMultiplier = _prefCategory.CreateEntry("BoostMultiplier", 3.0f, "Acceleration multiplier when sprinting (Shift)");
            _prefCanLoseSpeed = _prefCategory.CreateEntry("CanLoseSpeed", true, "Lose speed in air when not strafing");
            _prefInfiniteStamina = _prefCategory.CreateEntry("InfiniteStamina", true, "Maintain full stamina while bhopping");
            _prefShowSpeedometer = _prefCategory.CreateEntry("ShowSpeedometer", true, "Show on-screen speedometer HUD");

            // Automatic migration
            if (_prefSpeedCap.Value <= 100.0f) _prefSpeedCap.Value = 100000.0f;
            if (_prefAcceleration.Value < 0.5f || _prefAcceleration.Value > 10.0f) _prefAcceleration.Value = 2.0f;
            if (_prefAirFriction.Value > 1.0f || _prefAirFriction.Value < 0.2f) _prefAirFriction.Value = 0.5f;
            if (_prefBoostMultiplier.Value < 2.0f) _prefBoostMultiplier.Value = 3.0f;

            _prefCategory.SaveToFile();
        }

        public void OnUpdate(PlayerMovement pm)
        {
            if (_jumpDebounce > 0f)
            {
                _jumpDebounce -= Time.deltaTime;
            }

            if (!Enabled || pm == null)
            {
                CurrentSpeed = 0f;
                IsBhopping = false;
                return;
            }

            if (pm.IsOnLadder || pm.CurrentVehicle != null || pm.IsRagdolled)
            {
                CurrentSpeed = 0f;
                IsBhopping = false;
                return;
            }

            if (InfiniteStamina)
            {
                RefillStamina(pm);
            }

            bool isGrounded = pm.IsGrounded;
            bool isPressingJump = Input.GetKey(KeyCode.Space);

            // Auto Jump
            if (AutoJump && isPressingJump && isGrounded && _jumpDebounce <= 0f)
            {
                DoJump(pm);
            }

            // Bhop physics
            if (!isGrounded && IsBhopping)
            {
                UpdateInertia();
            }
            else if (isGrounded)
            {
                if (!isPressingJump)
                {
                    CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, 0f, _prefGroundFriction.Value * Time.deltaTime * 3f);
                    if (CurrentSpeed < 0.1f)
                    {
                        CurrentSpeed = 0f;
                        IsBhopping = false;
                    }
                }
            }
        }

        private void RefillStamina(PlayerMovement pm)
        {
            if (pm == null) return;
            float maxStamina = PlayerMovement.StaminaReserveMax;
            if (maxStamina <= 0.1f) maxStamina = 100f;

            if (pm.CurrentStaminaReserve < maxStamina * 0.95f)
            {
                pm.CurrentStaminaReserve = maxStamina;
                pm.SetStamina(maxStamina, false);
            }
        }

        private void DoJump(PlayerMovement pm)
        {
            if (pm == null) return;

            _jumpDebounce = 0.08f;
            RefillStamina(pm);

            pm.CanJump = true;
            pm.IsJumping = false;

            float nativeForce = PlayerMovement.JumpForce * Mathf.Max(PlayerMovement.JumpMultiplier, 1.0f);
            if (nativeForce <= 0.1f) nativeForce = 5.25f;

            pm.movementY = nativeForce;
            pm.Jump();
            pm.movementY = nativeForce;
            pm.IsJumping = true;
            IsBhopping = true;

            if (pm.Controller != null && pm.Controller.enabled)
            {
                pm.Controller.Move(Vector3.up * 0.05f);
            }

            // Initialize forward velocity upon jump if moving forward
            Vector3 v = pm.Controller != null ? pm.Controller.velocity : Vector3.zero;
            Vector3 horizontalVel = new Vector3(v.x, 0f, v.z);
            float currentHVel = horizontalVel.magnitude;

            Camera cam = Camera.main;
            Vector3 camForward = cam != null ? cam.transform.forward : pm.transform.forward;
            camForward.y = 0f;
            camForward.Normalize();

            bool movingForward = currentHVel > 1.5f && Vector3.Dot(horizontalVel.normalized, camForward) > 0.2f;

            if (movingForward && CurrentSpeed < currentHVel)
            {
                CurrentSpeed = currentHVel;
            }
            else if (CurrentSpeed > 0.5f)
            {
                CurrentSpeed *= 0.995f;
            }
        }

        private void UpdateInertia()
        {
            float mouseX = Input.GetAxis("Mouse X");
            bool pressingA = Input.GetKey(KeyCode.A) && !Input.GetKey(KeyCode.D);
            bool pressingD = Input.GetKey(KeyCode.D) && !Input.GetKey(KeyCode.A);

            bool isStrafing = (pressingA && mouseX < -0.02f) || (pressingD && mouseX > 0.02f);

            float currentAcceleration = Acceleration;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                currentAcceleration *= BoostMultiplier;
            }

            if (isStrafing)
            {
                CurrentSpeed += currentAcceleration * Time.deltaTime;
            }
            else
            {
                if (CanLoseSpeed)
                {
                    CurrentSpeed = Mathf.MoveTowards(CurrentSpeed, 0f, AirFriction * Time.deltaTime);
                }
            }
        }

        public void ApplyBhopVelocity(PlayerMovement pm)
        {
            if (CurrentSpeed <= 0.1f || pm == null || pm.Controller == null || !pm.Controller.enabled)
                return;

            Camera cam = Camera.main;
            Vector3 forward = cam != null ? cam.transform.forward : pm.transform.forward;
            forward.y = 0f;
            forward.Normalize();

            Vector3 motion = forward * CurrentSpeed * Time.deltaTime;
            pm.Controller.Move(motion);
        }

        public void OnDrawSettings()
        {
            GUILayout.BeginVertical();

            // Auto-Jump
            bool autoJump = GUILayout.Toggle(AutoJump, "  Auto-Jump (Hold Space)");
            if (autoJump != AutoJump) AutoJump = autoJump;

            // Infinite Stamina
            bool infStam = GUILayout.Toggle(InfiniteStamina, "  Infinite Stamina while Bhopping");
            if (infStam != InfiniteStamina) InfiniteStamina = infStam;

            // Show Speedometer
            bool showSpeedo = GUILayout.Toggle(ShowSpeedometer, "  Show Speedometer HUD");
            if (showSpeedo != ShowSpeedometer) ShowSpeedometer = showSpeedo;

            GUILayout.Space(8f);

            // Air Strafe Acceleration
            GUILayout.Label($"Air Strafe Acceleration: <color=#00FFFF>{Acceleration:F1} m/s²</color>");
            float newAccel = GUILayout.HorizontalSlider(Acceleration, 0.5f, 8.0f);
            if (Math.Abs(newAccel - Acceleration) > 0.01f) Acceleration = (float)Math.Round(newAccel, 1);

            GUILayout.Space(4f);

            // Shift Boost Multiplier
            GUILayout.Label($"Shift Boost Multiplier: <color=#FFD700>{BoostMultiplier:F1}x</color>");
            float newBoost = GUILayout.HorizontalSlider(BoostMultiplier, 1.0f, 6.0f);
            if (Math.Abs(newBoost - BoostMultiplier) > 0.01f) BoostMultiplier = (float)Math.Round(newBoost, 1);

            GUILayout.Space(4f);

            // Air Friction
            GUILayout.Label($"Air Friction Deceleration: <color=#AAAAAA>{AirFriction:F2}</color>");
            float newFriction = GUILayout.HorizontalSlider(AirFriction, 0.1f, 2.0f);
            if (Math.Abs(newFriction - AirFriction) > 0.01f) AirFriction = (float)Math.Round(newFriction, 2);

            GUILayout.Space(4f);
            GUILayout.Label("<color=#777777>Max Speed: Uncapped (100,000 m/s)</color>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 11 });

            GUILayout.EndVertical();
        }
    }
}
