using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArfSchedule1.UI
{
    public class HudToast
    {
        public string Source;
        public string Content;
        public Color AccentColor;
        public float StartTime;
        public float Duration;
        public float ShakeIntensity;

        public float Elapsed => Time.realtimeSinceStartup - StartTime;
        public float Progress => Mathf.Clamp01(Elapsed / Duration);
        public bool IsExpired => Elapsed >= Duration;
    }

    /// <summary>
    /// HUD notification logging system styled after ArfHook (ArfHUDManager / ArfHookConsole).
    /// Displays in the bottom-left corner:
    /// - Animated rainbow prefix [ArfHook]
    /// - Shake effect on appear (0.5s)
    /// - Two-color format: Cyan/Rainbow tag and green/red status
    /// - Smooth alpha fade out
    /// - Unlimited simultaneous toasts
    /// - Crisp, single-pass label rendering
    /// </summary>
    public static class HudNotificationManager
    {
        private static readonly List<HudToast> _toasts = new List<HudToast>();
        private static readonly List<string> _logHistory = new List<string>();
        private static GUIStyle _logStyle = null;

        private const float DefaultLifeTime = 7.5f;

        public static void LogModuleToggle(string moduleName, bool enabled)
        {
            string statusStr = enabled ? "<color=#00FF00>ON</color>" : "<color=#FF0000>OFF</color>";
            string content = $"<color=#00FFFF>{moduleName}:</color> {statusStr}";
            Color accent = enabled ? new Color(0.0f, 1f, 0.5f, 1f) : new Color(1f, 0.25f, 0.25f, 1f);
            
            AddLog("ArfHook", content, accent, 6.5f);
        }

        public static void LogNpcDeath(string npcName)
        {
            string content = $"<color=#FF5555>[☠] NPC Died:</color> <color=#FFFFFF>{npcName}</color>";
            Color accent = new Color(1f, 0.2f, 0.4f, 1f);
            
            AddLog("ArfHook", content, accent, 7.0f);
        }

        public static void LogItemSpawn(string itemName, int quantity)
        {
            string content = $"<color=#10B981>+{quantity}x</color> <color=#FFFFFF>{itemName}</color>";
            Color accent = new Color(0.1f, 0.9f, 0.5f, 1f);

            AddLog("ArfHook", content, accent, 6.0f);
        }

        public static void LogCustom(string content)
        {
            AddLog("ArfHook", content, new Color(0.7f, 0.7f, 1f, 1f), 6.0f);
        }

        public static void Log(string source, string content, Color accent, float duration = DefaultLifeTime)
        {
            AddLog(source, content, accent, duration);
        }

        private static void AddLog(string source, string content, Color accent, float duration)
        {
            string time = DateTime.Now.ToString("HH:mm:ss");
            _logHistory.Add($"<color=#777777>[{time}]</color> <color=#00FFFF>[{source}]</color> {content}");
            if (_logHistory.Count > 300) _logHistory.RemoveAt(0);

            // Deslimitado: se agregan todos los logs a la lista sin cortar en 7
            _toasts.Add(new HudToast
            {
                Source = source,
                Content = content,
                AccentColor = accent,
                StartTime = Time.realtimeSinceStartup,
                Duration = duration,
                ShakeIntensity = 4.0f
            });
        }

        public static void OnGUI()
        {
            if (_toasts.Count == 0) return;

            InitStyles();

            // Clean up expired toasts
            for (int i = _toasts.Count - 1; i >= 0; i--)
            {
                if (_toasts[i].IsExpired)
                {
                    _toasts.RemoveAt(i);
                }
            }

            if (_toasts.Count == 0) return;

            float marginX = 22f;
            float marginY = 22f;
            float rowHeight = 22f;
            float startY = Screen.height - marginY;

            for (int i = 0; i < _toasts.Count; i++)
            {
                var toast = _toasts[i];
                float elapsed = toast.Elapsed;

                // 1. Alpha calculation:
                // Stays solid for allocated duration, last 1.5s smoothly fades out (1f -> 0f)
                float alpha = 1f;
                float fadeStart = toast.Duration - 1.5f;
                if (elapsed > fadeStart)
                {
                    alpha = Mathf.Clamp01(1f - ((elapsed - fadeStart) / 1.5f));
                }

                // 2. Shake effect (initial shake during the first 0.5s):
                Vector2 shakeOffset = Vector2.zero;
                if (elapsed < 0.5f)
                {
                    float intensity = toast.ShakeIntensity * (1f - (elapsed / 0.5f));
                    shakeOffset = new Vector2(
                        UnityEngine.Random.Range(-intensity, intensity),
                        UnityEngine.Random.Range(-intensity, intensity)
                    );
                }

                // Bottom-to-top stacking in lower left corner
                float y = startY - (_toasts.Count - i) * (rowHeight + 2f);
                if (y < 10f) continue; // Avoid drawing off-screen if many logs are active

                float x = marginX;

                // 3. Tag generation with animated Rainbow or solid text
                string renderedText;
                bool isArfHook = toast.Source.Equals("ArfHook", StringComparison.OrdinalIgnoreCase);
                if (isArfHook)
                {
                    string rainbowTag = BuildRainbowTag($"[{toast.Source}]");
                    renderedText = $"{rainbowTag} {toast.Content}";
                }
                else
                {
                    renderedText = $"<color=#00FFFF>[{toast.Source}]</color> {toast.Content}";
                }

                // Measure text size
                GUIContent guiContent = new GUIContent(renderedText);
                Vector2 size = _logStyle.CalcSize(guiContent);
                float textWidth = size.x + 30f;

                Color prevColor = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, alpha);

                float drawX = x + shakeOffset.x;
                float drawY = y + shakeOffset.y;

                // Render main text label cleanly
                _logStyle.normal.textColor = new Color(1f, 1f, 1f, alpha);
                GUI.Label(new Rect(drawX, drawY, textWidth, rowHeight), renderedText, _logStyle);

                GUI.color = prevColor;
            }
        }

        /// <summary>
        /// Generates the dynamic rainbow gradient effect identical to ArfHUDManager / ArfQMLogManager:
        /// HSVToRGB((Time.time * 1.2f + (i * 0.05f)) % 1f, 1f, 1f)
        /// </summary>
        private static string BuildRainbowTag(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            float time = Time.time * 1.2f;
            for (int i = 0; i < text.Length; i++)
            {
                float hue = (time + (i * 0.06f)) % 1f;
                Color32 col = Color.HSVToRGB(hue, 1f, 1f);
                string hex = col.r.ToString("X2") + col.g.ToString("X2") + col.b.ToString("X2");
                sb.Append($"<color=#{hex}>{text[i]}</color>");
            }
            return sb.ToString();
        }

        private static void InitStyles()
        {
            if (_logStyle == null)
            {
                _logStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    richText = true,
                    alignment = TextAnchor.MiddleLeft
                };
            }
        }
    }
}
