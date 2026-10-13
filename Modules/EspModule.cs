using System;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine;
using Il2CppScheduleOne.NPCs;
using Il2CppScheduleOne.PlayerScripts;

namespace ArfSchedule1.Modules
{
    public class EspModule : IModModule
    {
        public static EspModule Instance { get; private set; }

        public string Id => "NpcEsp";
        public string Name => "NPC ESP";
        public string Description => "2D bounding boxes, health bars, and name tags on NPCs";
        public string Category => "Stuff";

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
            }
        }

        public bool IsExpanded { get; set; } = true;

        public float MaxDistance
        {
            get => _prefMaxDistance?.Value ?? 150f;
            set { if (_prefMaxDistance != null) { _prefMaxDistance.Value = value; _prefCategory.SaveToFile(); } }
        }

        public bool ShowBox
        {
            get => _prefShowBox?.Value ?? true;
            set { if (_prefShowBox != null) { _prefShowBox.Value = value; _prefCategory.SaveToFile(); } }
        }

        public bool ShowHealth
        {
            get => _prefShowHealth?.Value ?? true;
            set { if (_prefShowHealth != null) { _prefShowHealth.Value = value; _prefCategory.SaveToFile(); } }
        }

        public bool ShowName
        {
            get => _prefShowName?.Value ?? true;
            set { if (_prefShowName != null) { _prefShowName.Value = value; _prefCategory.SaveToFile(); } }
        }

        public bool ShowDistance
        {
            get => _prefShowDistance?.Value ?? true;
            set { if (_prefShowDistance != null) { _prefShowDistance.Value = value; _prefCategory.SaveToFile(); } }
        }

        private MelonPreferences_Category _prefCategory;
        private MelonPreferences_Entry<bool> _prefEnabled;
        private MelonPreferences_Entry<float> _prefMaxDistance;
        private MelonPreferences_Entry<bool> _prefShowBox;
        private MelonPreferences_Entry<bool> _prefShowHealth;
        private MelonPreferences_Entry<bool> _prefShowName;
        private MelonPreferences_Entry<bool> _prefShowDistance;

        // NPC cache
        private float _cacheTimer = 0f;
        private NPC[] _cachedNpcs = null;

        // GUI Styles
        private GUIStyle _nameStyle = null;

        public EspModule()
        {
            Instance = this;
        }

        public void OnInitialize()
        {
            _prefCategory = MelonPreferences.CreateCategory("ArfSchedule1_ESP", "Schedule I - ESP");
            _prefEnabled = _prefCategory.CreateEntry("Enabled", true, "Enable NPC ESP");
            _prefMaxDistance = _prefCategory.CreateEntry("MaxDistance", 150.0f, "Maximum distance for NPC ESP");
            _prefShowBox = _prefCategory.CreateEntry("ShowBox", true, "Show 2D Bounding Box");
            _prefShowHealth = _prefCategory.CreateEntry("ShowHealth", true, "Show Health Bar");
            _prefShowName = _prefCategory.CreateEntry("ShowName", true, "Show NPC Name");
            _prefShowDistance = _prefCategory.CreateEntry("ShowDistance", true, "Show Distance meters");
            _prefCategory.SaveToFile();
        }

        public void OnUpdate(PlayerMovement pm)
        {
            if (!Enabled) return;

            _cacheTimer -= Time.deltaTime;
            if (_cacheTimer <= 0f)
            {
                _cacheTimer = 0.6f; // Refresh NPC list every 600ms for optimal performance
                try
                {
                    _cachedNpcs = UnityEngine.Object.FindObjectsOfType<NPC>();
                }
                catch
                {
                    _cachedNpcs = null;
                }
            }
        }

        public void DrawESP()
        {
            if (!Enabled || _cachedNpcs == null || _cachedNpcs.Length == 0) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            Vector3 camPos = cam.transform.position;

            if (_nameStyle == null)
            {
                _nameStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 11,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperCenter,
                    richText = true
                };
            }

            for (int i = 0; i < _cachedNpcs.Length; i++)
            {
                var npc = _cachedNpcs[i];
                if (npc == null || npc.WasCollected) continue;

                var go = npc.gameObject;
                if (go == null || !go.activeInHierarchy) continue;

                Vector3 rootPos = npc.transform.position;
                float dist = Vector3.Distance(camPos, rootPos);
                if (dist > MaxDistance || dist < 0.5f) continue;

                // Determinar altura del NPC
                float height = 1.85f;
                var col = npc.GetComponent<Collider>();
                if (col != null && col.enabled)
                {
                    height = col.bounds.size.y;
                    if (height < 1.0f || height > 3.0f) height = 1.85f;
                }

                Vector3 headWorld = rootPos + Vector3.up * height;
                Vector3 feetWorld = rootPos;

                Vector3 headScreen = cam.WorldToScreenPoint(headWorld);
                Vector3 feetScreen = cam.WorldToScreenPoint(feetWorld);

                // Check if behind camera
                if (headScreen.z <= 0f || feetScreen.z <= 0f) continue;

                // GUI coordinates (top-left origin)
                float topY = Screen.height - headScreen.y;
                float bottomY = Screen.height - feetScreen.y;
                float boxHeight = Mathf.Abs(bottomY - topY);
                if (boxHeight < 6f) continue;

                float boxWidth = boxHeight * 0.52f;
                float centerX = feetScreen.x;
                float boxLeft = centerX - (boxWidth / 2f);
                Rect boxRect = new Rect(boxLeft, topY, boxWidth, boxHeight);

                // Fetch health data
                float normHealth = 1.0f;
                bool isDead = false;
                bool isKnockedOut = false;

                try
                {
                    if (npc.Health != null)
                    {
                        normHealth = Mathf.Clamp01(npc.Health.NormalizedHealth);
                        isDead = npc.Health.IsDead;
                        isKnockedOut = npc.Health.IsKnockedOut;
                    }
                }
                catch
                {
                    normHealth = 1.0f;
                }

                // Box color based on state
                Color boxColor = new Color(0.0f, 0.95f, 1.0f, 0.85f); // Cyan default
                if (isDead)
                {
                    boxColor = new Color(0.6f, 0.2f, 0.2f, 0.7f); // Dim red
                }
                else if (isKnockedOut)
                {
                    boxColor = new Color(1.0f, 0.6f, 0.0f, 0.85f); // Orange
                }
                else if (normHealth < 0.35f)
                {
                    boxColor = new Color(1.0f, 0.25f, 0.25f, 0.9f); // Critical red
                }

                // 1. Draw Bounding Box
                if (ShowBox)
                {
                    DrawBoxOutline(boxRect, boxColor, 1.5f);
                }

                // 2. Draw Health Bar on left side
                if (ShowHealth)
                {
                    float barW = 3.5f;
                    float barMargin = 3f;
                    float barLeft = boxLeft - barW - barMargin;

                    // Fondo de la barra (negro semitransparente)
                    Rect bgRect = new Rect(barLeft - 1, topY - 1, barW + 2, boxHeight + 2);
                    DrawRect(bgRect, new Color(0.05f, 0.05f, 0.07f, 0.85f));

                    // Relleno de la barra (de abajo hacia arriba)
                    float fillH = boxHeight * normHealth;
                    float fillY = topY + (boxHeight - fillH);
                    Rect fillRect = new Rect(barLeft, fillY, barW, fillH);

                    Color healthCol = Color.Lerp(new Color(0.95f, 0.2f, 0.2f), new Color(0.1f, 0.95f, 0.35f), normHealth);
                    if (isDead) healthCol = new Color(0.5f, 0.5f, 0.5f);
                    DrawRect(fillRect, healthCol);
                }

                // 3. Dibujar Nombre debajo de la caja
                if (ShowName)
                {
                    string rawName = npc.name.Replace("(Clone)", "").Trim();
                    string statusTag = isDead ? " <color=#FF4444>[DEAD]</color>" : (isKnockedOut ? " <color=#FFAA00>[KO]</color>" : "");
                    string distTag = ShowDistance ? $" <color=#9CA3AF>[{dist:F0}m]</color>" : "";

                    string displayText = $"{rawName}{statusTag}{distTag}";

                    float labelW = 220f;
                    float labelH = 22f;
                    Rect labelRect = new Rect(centerX - (labelW / 2f), bottomY + 2f, labelW, labelH);

                    DrawShadowedText(labelRect, displayText, _nameStyle, Color.white);
                }
            }
        }

        private void DrawBoxOutline(Rect rect, Color col, float thickness)
        {
            // Borde exterior negro para contraste
            DrawRect(new Rect(rect.x - 1, rect.y - 1, rect.width + 2, thickness + 1), new Color(0, 0, 0, 0.7f));
            DrawRect(new Rect(rect.x - 1, rect.y + rect.height - thickness, rect.width + 2, thickness + 1), new Color(0, 0, 0, 0.7f));
            DrawRect(new Rect(rect.x - 1, rect.y - 1, thickness + 1, rect.height + 2), new Color(0, 0, 0, 0.7f));
            DrawRect(new Rect(rect.x + rect.width - thickness, rect.y - 1, thickness + 1, rect.height + 2), new Color(0, 0, 0, 0.7f));

            // Cuadro principal con color
            DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), col);
            DrawRect(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), col);
            DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), col);
            DrawRect(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), col);
        }

        // Solid styles cache to draw rects without GUI.DrawTexture (avoids Method unstripping failed)
        private static readonly Dictionary<uint, GUIStyle> _solidStyleCache = new Dictionary<uint, GUIStyle>();

        private static GUIStyle GetSolidStyle(Color col)
        {
            byte r = (byte)(Mathf.Clamp01(col.r) * 255f);
            byte g = (byte)(Mathf.Clamp01(col.g) * 255f);
            byte b = (byte)(Mathf.Clamp01(col.b) * 255f);
            byte a = (byte)(Mathf.Clamp01(col.a) * 255f);

            // Quantize in 16 steps per channel to minimize texture count (< 30)
            r = (byte)((r / 16) * 16);
            g = (byte)((g / 16) * 16);
            b = (byte)((b / 16) * 16);
            a = (byte)((a / 16) * 16);

            uint key = ((uint)r << 24) | ((uint)g << 16) | ((uint)b << 8) | (uint)a;

            if (!_solidStyleCache.TryGetValue(key, out var style))
            {
                var tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, new Color(r / 255f, g / 255f, b / 255f, a / 255f));
                tex.Apply();

                style = new GUIStyle();
                style.normal.background = tex;
                style.border = new RectOffset(0, 0, 0, 0);
                style.margin = new RectOffset(0, 0, 0, 0);
                style.padding = new RectOffset(0, 0, 0, 0);
                style.overflow = new RectOffset(0, 0, 0, 0);

                _solidStyleCache[key] = style;
            }

            return style;
        }

        private void DrawRect(Rect rect, Color col)
        {
            GUIStyle style = GetSolidStyle(col);
            GUI.Box(rect, GUIContent.none, style);
        }

        private void DrawShadowedText(Rect rect, string text, GUIStyle style, Color col)
        {
            // 4-way black drop shadow for maximum readability
            GUI.Label(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), $"<color=#000000>{StripRichText(text)}</color>", style);
            GUI.Label(rect, text, style);
        }

        private string StripRichText(string input)
        {
            return System.Text.RegularExpressions.Regex.Replace(input, "<.*?>", string.Empty);
        }

        public void OnDrawSettings()
        {
            GUILayout.BeginVertical();

            bool showBox = GUILayout.Toggle(ShowBox, "  Show 2D Bounding Box");
            if (showBox != ShowBox) ShowBox = showBox;

            bool showHealth = GUILayout.Toggle(ShowHealth, "  Show Side Health Bar");
            if (showHealth != ShowHealth) ShowHealth = showHealth;

            bool showName = GUILayout.Toggle(ShowName, "  Show Name Tag");
            if (showName != ShowName) ShowName = showName;

            bool showDist = GUILayout.Toggle(ShowDistance, "  Show Distance in Meters");
            if (showDist != ShowDistance) ShowDistance = showDist;

            GUILayout.Space(8f);

            GUILayout.Label($"Max Distance: <color=#00FFFF>{MaxDistance:F0} meters</color>");
            float newDist = GUILayout.HorizontalSlider(MaxDistance, 20f, 300f);
            if (Math.Abs(newDist - MaxDistance) > 1f) MaxDistance = (float)Math.Round(newDist, 0);

            GUILayout.EndVertical();
        }
    }
}
