using System;
using MelonLoader;
using UnityEngine;
using Il2CppScheduleOne.PlayerScripts;

namespace ArfSchedule1.Modules
{
    public class NoRecoilModule : IModModule
    {
        public static NoRecoilModule Instance { get; private set; }

        public string Id => "NoRecoil";
        public string Name => "No Recoil";
        public string Description => "Eliminates weapon recoil and camera punch";
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

        public bool RemoveCameraShake
        {
            get => _prefRemoveCameraShake?.Value ?? true;
            set
            {
                if (_prefRemoveCameraShake != null)
                {
                    _prefRemoveCameraShake.Value = value;
                    _prefCategory.SaveToFile();
                }
            }
        }

        public bool RemoveViewmodelRecoil
        {
            get => _prefRemoveViewmodelRecoil?.Value ?? true;
            set
            {
                if (_prefRemoveViewmodelRecoil != null)
                {
                    _prefRemoveViewmodelRecoil.Value = value;
                    _prefCategory.SaveToFile();
                }
            }
        }

        private MelonPreferences_Category _prefCategory;
        private MelonPreferences_Entry<bool> _prefEnabled;
        private MelonPreferences_Entry<bool> _prefRemoveCameraShake;
        private MelonPreferences_Entry<bool> _prefRemoveViewmodelRecoil;

        public NoRecoilModule()
        {
            Instance = this;
        }

        public void OnInitialize()
        {
            _prefCategory = MelonPreferences.CreateCategory("ArfSchedule1_NoRecoil", "Schedule I - No Recoil");
            _prefEnabled = _prefCategory.CreateEntry("Enabled", false, "Enable No Recoil");
            _prefRemoveCameraShake = _prefCategory.CreateEntry("RemoveCameraShake", true, "Remove Camera Punch & Shake");
            _prefRemoveViewmodelRecoil = _prefCategory.CreateEntry("RemoveViewmodelRecoil", true, "Remove Weapon IK Punch");
            _prefCategory.SaveToFile();
        }

        public void OnUpdate(PlayerMovement pm)
        {
            if (!Enabled) return;

            // Ensure camera shake multiplier stays at 0 while active
            if (RemoveCameraShake)
            {
                PlayerCamera.CameraShakeMultiplier = 0f;
            }
        }

        public void OnDrawSettings()
        {
            GUILayout.BeginVertical();

            bool noShake = GUILayout.Toggle(RemoveCameraShake, "  No Camera Punch / Jolt");
            if (noShake != RemoveCameraShake) RemoveCameraShake = noShake;

            bool noVm = GUILayout.Toggle(RemoveViewmodelRecoil, "  No Weapon/Hands Recoil (Viewmodel IK)");
            if (noVm != RemoveViewmodelRecoil) RemoveViewmodelRecoil = noVm;

            GUILayout.Space(4f);
            GUILayout.Label("<color=#10B981>✓</color> Keeps crosshair completely steady with 100% laser-accurate shots.");

            GUILayout.EndVertical();
        }
    }
}
