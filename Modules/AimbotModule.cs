using System;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.PlayerScripts;
using ArfSchedule1.UI;

namespace ArfSchedule1.Modules
{
    public class AimbotModule : IModModule
    {
        public static AimbotModule Instance { get; private set; }

        public string Id => "Aimbot";
        public string Name => "Aimbot";
        public string Description => "Automatically locks aim onto NPCs inside the FOV circle";
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

        public bool IsExpanded { get; set; } = true;

        public float FovRadius
        {
            get => _prefFovRadius?.Value ?? 120f;
            set { if (_prefFovRadius != null) { _prefFovRadius.Value = value; _prefCategory.SaveToFile(); } }
        }

        public float SmoothSpeed
        {
            get => _prefSmoothSpeed?.Value ?? 18f;
            set { if (_prefSmoothSpeed != null) { _prefSmoothSpeed.Value = value; _prefCategory.SaveToFile(); } }
        }

        public float MaxDistance
        {
            get => _prefMaxDistance?.Value ?? 100f;
            set { if (_prefMaxDistance != null) { _prefMaxDistance.Value = value; _prefCategory.SaveToFile(); } }
        }

        public bool DrawFovCircle
        {
            get => _prefDrawFovCircle?.Value ?? true;
            set { if (_prefDrawFovCircle != null) { _prefDrawFovCircle.Value = value; _prefCategory.SaveToFile(); } }
        }

        public bool TargetHead
        {
            get => _prefTargetHead?.Value ?? true;
            set { if (_prefTargetHead != null) { _prefTargetHead.Value = value; _prefCategory.SaveToFile(); } }
        }

        public bool IgnoreDead
        {
            get => _prefIgnoreDead?.Value ?? true;
            set { if (_prefIgnoreDead != null) { _prefIgnoreDead.Value = value; _prefCategory.SaveToFile(); } }
        }

        public bool OnlyOnKey
        {
            get => _prefOnlyOnKey?.Value ?? true;
            set { if (_prefOnlyOnKey != null) { _prefOnlyOnKey.Value = value; _prefCategory.SaveToFile(); } }
        }

        public bool SilentAim
        {
            get => _prefSilentAim?.Value ?? false;
            set { if (_prefSilentAim != null) { _prefSilentAim.Value = value; _prefCategory.SaveToFile(); } }
        }

        public bool VisibleOnly
        {
            get => _prefVisibleOnly?.Value ?? true;
            set { if (_prefVisibleOnly != null) { _prefVisibleOnly.Value = value; _prefCategory.SaveToFile(); } }
        }

        private MelonPreferences_Category _prefCategory;
        private MelonPreferences_Entry<bool> _prefEnabled;
        private MelonPreferences_Entry<float> _prefFovRadius;
        private MelonPreferences_Entry<float> _prefSmoothSpeed;
        private MelonPreferences_Entry<float> _prefMaxDistance;
        private MelonPreferences_Entry<bool> _prefDrawFovCircle;
        private MelonPreferences_Entry<bool> _prefTargetHead;
        private MelonPreferences_Entry<bool> _prefIgnoreDead;
        private MelonPreferences_Entry<bool> _prefOnlyOnKey;
        private MelonPreferences_Entry<bool> _prefSilentAim;
        private MelonPreferences_Entry<bool> _prefVisibleOnly;

        // Caching
        private float _cacheTimer = 0f;
        private NPC[] _cachedNpcs = null;
        private NPC _currentTarget = null;
        private Vector3 _currentTargetPos = Vector3.zero;

        // FOV circle texture & styles
        private static Texture2D _circleTexture = null;
        private static GUIStyle _circleStyle = null;

        public AimbotModule()
        {
            Instance = this;
        }

        public void OnInitialize()
        {
            _prefCategory = MelonPreferences.CreateCategory("ArfSchedule1_Aimbot", "Schedule I - Aimbot");
            _prefEnabled = _prefCategory.CreateEntry("Enabled", false, "Enable Aimbot");
            _prefFovRadius = _prefCategory.CreateEntry("FovRadius", 120.0f, "Screen radius for FOV circle in pixels");
            _prefSmoothSpeed = _prefCategory.CreateEntry("SmoothSpeed", 18.0f, "Smooth aiming speed");
            _prefMaxDistance = _prefCategory.CreateEntry("MaxDistance", 100.0f, "Maximum target distance in meters");
            _prefDrawFovCircle = _prefCategory.CreateEntry("DrawFovCircle", true, "Draw FOV Circle on screen");
            _prefTargetHead = _prefCategory.CreateEntry("TargetHead", true, "Aim at head instead of chest");
            _prefIgnoreDead = _prefCategory.CreateEntry("IgnoreDead", true, "Ignore dead or knocked out NPCs");
            _prefOnlyOnKey = _prefCategory.CreateEntry("OnlyOnKey", true, "Only aim when holding Right Click");
            _prefSilentAim = _prefCategory.CreateEntry("SilentAim", false, "Instant snap instead of smooth");
            _prefVisibleOnly = _prefCategory.CreateEntry("VisibleOnly", true, "Only target visible NPCs (line-of-sight check)");
            _prefCategory.SaveToFile();
        }

        public void OnUpdate(PlayerMovement pm)
        {
            if (!Enabled || Player.Local == null || MenuManager.IsMenuOpen)
            {
                _currentTarget = null;
                return;
            }

            // Cache de NPCs cada 300ms
            _cacheTimer -= Time.deltaTime;
            if (_cacheTimer <= 0f)
            {
                _cacheTimer = 0.3f;
                try
                {
                    _cachedNpcs = UnityEngine.Object.FindObjectsOfType<NPC>();
                }
                catch
                {
                    _cachedNpcs = null;
                }
            }

            // Check if aimbot should engage (requires RMB if OnlyOnKey is active)
            bool shouldAim = !OnlyOnKey || Input.GetMouseButton(1);
            if (!shouldAim)
            {
                _currentTarget = null;
                return;
            }

            Camera cam = Camera.main;
            if (cam == null) return;

            Transform camTransform = cam.transform;
            Vector3 camPos = camTransform.position;
            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            NPC bestTarget = null;
            Vector3 bestTargetWorldPos = Vector3.zero;
            float closestScreenDist = FovRadius;

            if (_cachedNpcs != null)
            {
                for (int i = 0; i < _cachedNpcs.Length; i++)
                {
                    var npc = _cachedNpcs[i];
                    if (npc == null || npc.WasCollected) continue;

                    var go = npc.gameObject;
                    if (go == null || !go.activeInHierarchy) continue;

                    // Check life state
                    if (IgnoreDead && npc.Health != null && (npc.Health.IsDead || npc.Health.IsKnockedOut))
                    {
                        continue;
                    }

                    Vector3 rootPos = npc.transform.position;
                    float dist = Vector3.Distance(camPos, rootPos);
                    if (dist > MaxDistance || dist < 0.5f) continue;

                    // Calculate target point (Head or Chest)
                    float height = 1.8f;
                    var col = npc.GetComponent<Collider>();
                    if (col != null && col.enabled)
                    {
                        height = col.bounds.size.y;
                        if (height < 1.0f || height > 2.8f) height = 1.8f;
                    }

                    Vector3 targetWorld = TargetHead ? (rootPos + Vector3.up * (height * 0.88f)) : (rootPos + Vector3.up * (height * 0.55f));

                    // Project to screen
                    Vector3 screenPos = cam.WorldToScreenPoint(targetWorld);
                    if (screenPos.z <= 0f) continue; // Behind camera

                    Vector2 screenPoint = new Vector2(screenPos.x, Screen.height - screenPos.y);
                    float screenDist = Vector2.Distance(screenCenter, screenPoint);

                    if (screenDist <= closestScreenDist)
                    {
                        // Check line of sight if VisibleOnly is active
                        if (VisibleOnly && !IsVisibleToPlayer(camPos, targetWorld, npc.gameObject))
                        {
                            continue;
                        }

                        closestScreenDist = screenDist;
                        bestTarget = npc;
                        bestTargetWorldPos = targetWorld;
                    }
                }
            }

            _currentTarget = bestTarget;
            _currentTargetPos = bestTargetWorldPos;

            if (bestTarget != null)
            {
                ApplyAim(cam, bestTargetWorldPos);
            }
        }

        /// <summary>
        /// Checks if there is a direct line-of-sight between camera and NPC (no walls/obstacles)
        /// </summary>
        private bool IsVisibleToPlayer(Vector3 camPos, Vector3 targetPos, GameObject targetNpcGo)
        {
            Vector3 diff = targetPos - camPos;
            float dist = diff.magnitude;
            if (dist < 0.2f) return true;

            Vector3 dir = diff / dist;

            // Ignore triggers and check collision against world obstacles
            RaycastHit hit;
            if (Physics.Raycast(camPos, dir, out hit, dist - 0.2f, ~0, QueryTriggerInteraction.Ignore))
            {
                // If raycast hit something, verify if it belongs to target NPC or an obstacle
                var hitGo = hit.collider != null ? hit.collider.gameObject : null;
                if (hitGo != null)
                {
                    if (hitGo == targetNpcGo || hitGo.transform.IsChildOf(targetNpcGo.transform))
                    {
                        return true;
                    }

                    // Ignore if hitting the local player
                    if (Player.Local != null && (hitGo == Player.Local.gameObject || hitGo.transform.IsChildOf(Player.Local.transform)))
                    {
                        return true;
                    }

                    // Obstructed by wall or cover
                    return false;
                }
            }

            return true;
        }

        private void ApplyAim(Camera cam, Vector3 targetWorldPos)
        {
            Vector3 dir = (targetWorldPos - cam.transform.position).normalized;
            if (dir == Vector3.zero) return;

            Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);

            // Get rotation transform for camera and player
            var playerCam = cam.GetComponentInParent<PlayerCamera>();
            Transform rotTransform = null;
            if (playerCam != null && playerCam.CameraContainer != null)
            {
                rotTransform = playerCam.CameraContainer;
            }
            else
            {
                rotTransform = cam.transform;
            }

            if (rotTransform == null) return;

            if (SilentAim || SmoothSpeed >= 50f)
            {
                rotTransform.rotation = targetRot;
                if (Player.Local != null)
                {
                    // Alinear cuerpo del jugador horizontalmente
                    Vector3 fwd = dir;
                    fwd.y = 0f;
                    if (fwd.sqrMagnitude > 0.001f)
                    {
                        Player.Local.transform.forward = fwd.normalized;
                    }
                }
            }
            else
            {
                float step = SmoothSpeed * Time.deltaTime * 60f;
                rotTransform.rotation = Quaternion.RotateTowards(rotTransform.rotation, targetRot, step);

                if (Player.Local != null)
                {
                    Vector3 fwd = dir;
                    fwd.y = 0f;
                    if (fwd.sqrMagnitude > 0.001f)
                    {
                        Quaternion bodyTarget = Quaternion.LookRotation(fwd.normalized, Vector3.up);
                        Player.Local.transform.rotation = Quaternion.RotateTowards(Player.Local.transform.rotation, bodyTarget, step);
                    }
                }
            }
        }

        public void DrawGUI()
        {
            if (!Enabled || !DrawFovCircle || MenuManager.IsMenuOpen) return;

            Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Color circleColor = (_currentTarget != null) 
                ? new Color(0.0f, 1.0f, 0.4f, 0.85f) // Verde si tiene objetivo
                : new Color(0.7f, 0.7f, 1.0f, 0.55f); // Azul/violeta si no

            DrawCircle(center, FovRadius, circleColor, 1.5f);
        }

        private void DrawCircle(Vector2 center, float radius, Color color, float thickness)
        {
            int segments = 48;
            float step = (Mathf.PI * 2f) / segments;

            Vector2 prevPoint = center + new Vector2(Mathf.Cos(0) * radius, Mathf.Sin(0) * radius);

            for (int i = 1; i <= segments; i++)
            {
                float angle = i * step;
                Vector2 nextPoint = center + new Vector2(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius);

                DrawLine(prevPoint, nextPoint, color, thickness);
                prevPoint = nextPoint;
            }
        }

        private void DrawLine(Vector2 pA, Vector2 pB, Color col, float thickness)
        {
            InitCircleStyle();

            Vector2 diff = pB - pA;
            float length = diff.magnitude;
            if (length < 0.001f) return;

            float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

            Matrix4x4 matrixBackup = GUI.matrix;
            Color colorBackup = GUI.color;

            GUI.color = col;
            GUIUtility.RotateAroundPivot(angle, pA);
            GUI.Box(new Rect(pA.x, pA.y - (thickness * 0.5f), length, thickness), GUIContent.none, _circleStyle);

            GUI.matrix = matrixBackup;
            GUI.color = colorBackup;
        }

        private void InitCircleStyle()
        {
            if (_circleStyle == null)
            {
                _circleTexture = new Texture2D(1, 1);
                _circleTexture.SetPixel(0, 0, Color.white);
                _circleTexture.Apply();

                _circleStyle = new GUIStyle();
                _circleStyle.normal.background = _circleTexture;
            }
        }

        public void OnDrawSettings()
        {
            GUILayout.BeginVertical();

            bool drawFov = GUILayout.Toggle(DrawFovCircle, "  Draw FOV Circle on Screen");
            if (drawFov != DrawFovCircle) DrawFovCircle = drawFov;

            bool onlyKey = GUILayout.Toggle(OnlyOnKey, "  Activate Only when Holding Right Click [RMB]");
            if (onlyKey != OnlyOnKey) OnlyOnKey = onlyKey;

            bool targetHead = GUILayout.Toggle(TargetHead, "  Prioritize Head (Headshot)");
            if (targetHead != TargetHead) TargetHead = targetHead;

            bool ignoreDead = GUILayout.Toggle(IgnoreDead, "  Ignore Dead or Unconscious NPCs");
            if (ignoreDead != IgnoreDead) IgnoreDead = ignoreDead;

            bool visibleOnly = GUILayout.Toggle(VisibleOnly, "  Visible NPCs Only (Line of Sight Check)");
            if (visibleOnly != VisibleOnly) VisibleOnly = visibleOnly;

            bool silent = GUILayout.Toggle(SilentAim, "  Instant Snap Mode (Silent Aim)");
            if (silent != SilentAim) SilentAim = silent;

            GUILayout.Space(8f);

            // FOV radius slider
            GUILayout.Label($"FOV Radius: <color=#00FFFF>{FovRadius:F0} px</color>");
            float newFov = GUILayout.HorizontalSlider(FovRadius, 30f, 450f);
            if (Math.Abs(newFov - FovRadius) > 1f) FovRadius = (float)Math.Round(newFov, 0);

            GUILayout.Space(4f);

            // Aim smoothness slider
            GUILayout.Label($"Aim Speed / Smoothness: <color=#00FFFF>{SmoothSpeed:F1}</color>");
            float newSpeed = GUILayout.HorizontalSlider(SmoothSpeed, 1.0f, 50.0f);
            if (Math.Abs(newSpeed - SmoothSpeed) > 0.1f) SmoothSpeed = (float)Math.Round(newSpeed, 1);

            GUILayout.Space(4f);

            // Max distance slider
            GUILayout.Label($"Max Distance: <color=#00FFFF>{MaxDistance:F0} meters</color>");
            float newDist = GUILayout.HorizontalSlider(MaxDistance, 10f, 250f);
            if (Math.Abs(newDist - MaxDistance) > 1f) MaxDistance = (float)Math.Round(newDist, 0);

            GUILayout.EndVertical();
        }
    }
}
