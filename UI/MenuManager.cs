using System;
using System.Collections.Generic;
using UnityEngine;
using MelonLoader;
using ArfSchedule1.Modules;
using Il2CppScheduleOne.PlayerScripts;

namespace ArfSchedule1.UI
{
    public class MenuManager
    {
        public static MenuManager Instance { get; private set; }

        public static bool IsMenuOpen { get; private set; } = false;

        public List<IModModule> Modules { get; } = new List<IModModule>();

        private Rect _windowRect = new Rect(100f, 80f, 480f, 540f);
        public const float MinWindowWidth = 380f;
        public const float MinWindowHeight = 320f;
        private bool _isResizing = false;
        private Vector2 _resizeStartMouse = Vector2.zero;
        private Vector2 _resizeStartSize = Vector2.zero;

        private MelonPreferences_Category _prefCategory;
        private MelonPreferences_Entry<float> _prefWindowWidth;
        private MelonPreferences_Entry<float> _prefWindowHeight;

        private int _selectedCategory = 0;
        private string[] _categories = new string[] { "Movement", "Farming", "Stuff", "Items", "About" };
        private Vector2 _scrollPos = Vector2.zero;

        // Custom Textures & Styles
        private Texture2D _texWindowBg;
        private Texture2D _texCardBg;
        private Texture2D _texButtonOn;
        private Texture2D _texButtonOff;
        private Texture2D _texHeaderBg;

        private GUIStyle _styleWindow;
        private GUIStyle _styleTitle;
        private GUIStyle _styleCard;
        private GUIStyle _styleTabButton;
        private GUIStyle _styleTabActive;
        private GUIStyle _styleToggleOn;
        private GUIStyle _styleToggleOff;
        private GUIStyle _styleExpandButton;
        private GUIStyle _styleModuleHeader;
        private GUIStyle _styleModuleDesc;

        // Speedometer HUD Styles
        private Texture2D _texSpeedoBg;
        private GUIStyle _styleSpeedoBox;
        private GUIStyle _styleSpeedoLabel;
        private GUIStyle _styleSpeedoVal;
        private GUIStyle _styleResizeGrip;

        public MenuManager()
        {
            Instance = this;
            try
            {
                _prefCategory = MelonPreferences.CreateCategory("ArfSchedule1_Menu", "ArfHook Menu Settings");
                _prefWindowWidth = _prefCategory.CreateEntry("WindowWidth", 480f, "Menu Width");
                _prefWindowHeight = _prefCategory.CreateEntry("WindowHeight", 540f, "Menu Height");

                float w = Mathf.Max(MinWindowWidth, _prefWindowWidth.Value);
                float h = Mathf.Max(MinWindowHeight, _prefWindowHeight.Value);
                _windowRect = new Rect(100f, 80f, w, h);
            }
            catch
            {
                _windowRect = new Rect(100f, 80f, 480f, 540f);
            }
        }

        public void SaveWindowSize()
        {
            try
            {
                if (_prefWindowWidth != null) _prefWindowWidth.Value = _windowRect.width;
                if (_prefWindowHeight != null) _prefWindowHeight.Value = _windowRect.height;
                _prefCategory?.SaveToFile();
            }
            catch { }
        }

        public void RegisterModule(IModModule module)
        {
            if (module != null && !Modules.Contains(module))
            {
                Modules.Add(module);
            }
        }

        public void OnUpdate(PlayerMovement pm)
        {
            // Toggle with INSERT key
            if (Input.GetKeyDown(KeyCode.Insert))
            {
                ToggleMenu();
            }

            // Keep cursor unlocked while menu is open
            if (IsMenuOpen)
            {
                if (Cursor.lockState != CursorLockMode.None)
                {
                    Cursor.lockState = CursorLockMode.None;
                }
                if (!Cursor.visible)
                {
                    Cursor.visible = true;
                }
            }

            // Update registered modules
            for (int i = 0; i < Modules.Count; i++)
            {
                Modules[i].OnUpdate(pm);
            }
        }

        public static void ToggleMenu()
        {
            IsMenuOpen = !IsMenuOpen;
            UpdateCursorAndCameraState();
        }

        public static void UpdateCursorAndCameraState()
        {
            if (IsMenuOpen)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                // Register active UI element on player camera to block look/aim input
                try
                {
                    var player = Player.Local;
                    if (player != null)
                    {
                        var cam = player.GetComponentInChildren<PlayerCamera>();
                        if (cam != null)
                        {
                            cam.AddActiveUIElement("ArfHookMenu");
                        }
                    }
                }
                catch { }
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                try
                {
                    var player = Player.Local;
                    if (player != null)
                    {
                        var cam = player.GetComponentInChildren<PlayerCamera>();
                        if (cam != null)
                        {
                            cam.RemoveActiveUIElement("ArfHookMenu");
                        }
                    }
                }
                catch { }
            }
        }

        public void OnGUI(PlayerMovement pm)
        {
            InitStyles();

            // Draw NPC ESP if enabled
            var esp = EspModule.Instance;
            if (esp != null && esp.Enabled && Player.Local != null)
            {
                esp.DrawESP();
            }

            // Draw Aimbot FOV Circle if enabled
            var aimbot = AimbotModule.Instance;
            if (aimbot != null && aimbot.Enabled && Player.Local != null)
            {
                aimbot.DrawGUI();
            }

            // Draw Speedometer if enabled in BhopModule
            var bhop = BhopModule.Instance;
            if (bhop != null && bhop.ShowSpeedometer && Player.Local != null)
            {
                DrawSpeedometer(pm, bhop);
            }

            // Draw menu if open
            if (IsMenuOpen)
            {
                HandleWindowResize();
                _windowRect = GUI.Window(1337, _windowRect, (GUI.WindowFunction)DrawWindow, GUIContent.none, _styleWindow);
            }

            // Draw HUD toasts (ArfHook toasts in bottom-left corner)
            HudNotificationManager.OnGUI();
        }

        private void HandleWindowResize()
        {
            Event e = Event.current;
            if (e == null) return;

            // Grip rectangle in screen coordinates (26x26 px)
            Rect gripScreenRect = new Rect(
                _windowRect.x + _windowRect.width - 26f,
                _windowRect.y + _windowRect.height - 26f,
                26f,
                26f
            );

            if (e.type == EventType.MouseDown && e.button == 0 && gripScreenRect.Contains(e.mousePosition))
            {
                _isResizing = true;
                _resizeStartMouse = e.mousePosition;
                _resizeStartSize = new Vector2(_windowRect.width, _windowRect.height);
                e.Use();
            }
            else if (e.rawType == EventType.MouseUp || (e.type == EventType.MouseUp && e.button == 0))
            {
                if (_isResizing)
                {
                    _isResizing = false;
                    SaveWindowSize();
                }
            }
            else if (_isResizing)
            {
                if (!Input.GetMouseButton(0))
                {
                    _isResizing = false;
                    SaveWindowSize();
                    return;
                }

                Vector2 delta = e.mousePosition - _resizeStartMouse;
                float maxW = Mathf.Max(MinWindowWidth, Screen.width - _windowRect.x - 10f);
                float maxH = Mathf.Max(MinWindowHeight, Screen.height - _windowRect.y - 10f);

                _windowRect.width = Mathf.Clamp(_resizeStartSize.x + delta.x, MinWindowWidth, maxW);
                _windowRect.height = Mathf.Clamp(_resizeStartSize.y + delta.y, MinWindowHeight, maxH);
            }
        }

        private void DrawWindow(int windowId)
        {
            // Drag bar on top (first 35px)
            GUI.DragWindow(new Rect(0, 0, _windowRect.width - 35, 35));

            GUILayout.BeginArea(new Rect(12, 8, _windowRect.width - 24, _windowRect.height - 16));

            // Header with title and close button [X]
            GUILayout.BeginHorizontal();
            GUILayout.Label("<color=#A78BFA><b>ARFHOOK</b></color> <color=#9CA3AF>|</color> <size=13><color=#E5E7EB>Schedule I</color></size>", _styleTitle);
            if (GUILayout.Button("✕", new GUIStyle(GUI.skin.button)
            {
                fixedWidth = 24,
                fixedHeight = 22,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.8f, 0.3f, 0.3f) }
            }))
            {
                ToggleMenu();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);

            // Tab bar
            GUILayout.BeginHorizontal();
            for (int i = 0; i < _categories.Length; i++)
            {
                bool isSelected = (_selectedCategory == i);
                GUIStyle tabStyle = isSelected ? _styleTabActive : _styleTabButton;
                if (GUILayout.Button(_categories[i], tabStyle, GUILayout.Height(26)))
                {
                    _selectedCategory = i;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);

            // Tab content
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, false, false);

            if (_selectedCategory == 0) // Movement
            {
                DrawModulesForCategory("Movement");
            }
            else if (_selectedCategory == 1) // Farming
            {
                DrawModulesForCategory("Farming");
            }
            else if (_selectedCategory == 2) // Stuff
            {
                DrawModulesForCategory("Stuff");
            }
            else if (_selectedCategory == 3) // Items
            {
                ArfSchedule1Mod.Instance?.ItemSpawner?.OnDrawTab(_styleCard, _styleToggleOn, _styleToggleOff);
            }
            else if (_selectedCategory == 4) // About
            {
                DrawAboutTab();
            }

            GUILayout.EndScrollView();

            // Window footer
            GUILayout.Space(4f);
            GUILayout.Label("<color=#6B7280>Press [<b>Insert</b>] to toggle | ArfHook Engine</color>",
                new GUIStyle(GUI.skin.label) { richText = true, fontSize = 10, alignment = TextAnchor.MiddleCenter });

            GUILayout.EndArea();

            // Interactive resize grip in bottom-right corner
            Rect gripLocalRect = new Rect(_windowRect.width - 20f, _windowRect.height - 20f, 20f, 20f);
            Event we = Event.current;
            if (we != null && we.type == EventType.MouseDown && we.button == 0 && gripLocalRect.Contains(we.mousePosition))
            {
                _isResizing = true;
                _resizeStartMouse = GUIUtility.GUIToScreenPoint(we.mousePosition);
                _resizeStartSize = new Vector2(_windowRect.width, _windowRect.height);
                we.Use();
            }

            string gripColor = _isResizing ? "#38BDF8" : "#8B5CF6";
            GUI.Label(gripLocalRect, $"<color={gripColor}><b>◢</b></color>", _styleResizeGrip);
        }

        private void DrawModulesForCategory(string category)
        {
            for (int i = 0; i < Modules.Count; i++)
            {
                var mod = Modules[i];
                if (mod.Category != category) continue;

                // Card container
                GUILayout.BeginVertical(_styleCard);

                // Module main row
                GUILayout.BeginHorizontal();

                // Name and description
                GUILayout.BeginVertical();
                GUILayout.Label($"<b>{mod.Name}</b>", _styleModuleHeader);
                if (!string.IsNullOrEmpty(mod.Description))
                {
                    GUILayout.Label(mod.Description, _styleModuleDesc);
                }
                GUILayout.EndVertical();

                GUILayout.FlexibleSpace();

                // Settings expand button
                string expandText = mod.IsExpanded ? "▲ Settings" : "▼ Settings";
                if (GUILayout.Button(expandText, _styleExpandButton, GUILayout.Width(72), GUILayout.Height(26)))
                {
                    mod.IsExpanded = !mod.IsExpanded;
                }

                GUILayout.Space(4f);

                // Toggle ON/OFF button
                string toggleText = mod.Enabled ? "ON" : "OFF";
                GUIStyle toggleStyle = mod.Enabled ? _styleToggleOn : _styleToggleOff;
                if (GUILayout.Button(toggleText, toggleStyle, GUILayout.Width(48), GUILayout.Height(26)))
                {
                    mod.Enabled = !mod.Enabled;
                    HudNotificationManager.LogModuleToggle(mod.Name, mod.Enabled);
                }

                GUILayout.EndHorizontal();

                // Expandable settings submenu
                if (mod.IsExpanded)
                {
                    GUILayout.Space(6f);
                    GUILayout.Box(GUIContent.none, GUILayout.Height(1), GUILayout.ExpandWidth(true)); // Divider line
                    GUILayout.Space(4f);
                    mod.OnDrawSettings();
                }

                GUILayout.EndVertical();
                GUILayout.Space(6f);
            }
        }

        private void DrawAboutTab()
        {
            GUILayout.BeginVertical(_styleCard);
            GUILayout.Label("<b><size=14><color=#A78BFA>ArfSchedule1 - Schedule I</color></size></b>", new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.Space(4f);
            GUILayout.Label("Enhanced modification client powered by <b>ArfHook Engine</b> for <b>Schedule I</b> (TVGS).", new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true });
            GUILayout.Space(6f);
            GUILayout.Label("<b>Features:</b>", new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.Label("• Smooth bunnyhop engine (camera-oriented movement)", new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.Label("• Continuous auto-jump without landing delays", new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.Label("• Progressive uncapped momentum & speed gain", new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.Label("• Shift speed booster multiplier (3x)", new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.Label("• Full farming automation suite (Grow, Farm, Fill Pots, Plant, Water, Package)", new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.Label("• Combat suite: God Mode, Aimbot, ESP, Rapid Fire, Infinite Ammo & No Recoil", new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.Label("• Interactive IMGUI menu toggled with [Insert]", new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.Space(8f);
            GUILayout.Label("<color=#9CA3AF>Created by <b>IAGOXR</b></color>", new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.EndVertical();
        }

        private void DrawSpeedometer(PlayerMovement pm, BhopModule bhop)
        {
            float boxWidth = 190f;
            float boxHeight = 65f;
            float margin = 20f;
            Rect rect = new Rect(Screen.width - boxWidth - margin, Screen.height - boxHeight - margin, boxWidth, boxHeight);

            GUI.Box(rect, GUIContent.none, _styleSpeedoBox);

            GUILayout.BeginArea(new Rect(rect.x + 10, rect.y + 8, rect.width - 20, rect.height - 16));

            GUILayout.BeginHorizontal();
            GUILayout.Label("BHOP", _styleSpeedoLabel);
            string statusText = bhop.Enabled ? "<color=#10B981>ON</color>" : "<color=#EF4444>OFF</color>";
            GUILayout.Label(statusText, new GUIStyle(_styleSpeedoLabel) { alignment = TextAnchor.MiddleRight });
            GUILayout.EndHorizontal();

            float displaySpeed = 0f;
            if (pm != null && pm.Controller != null)
            {
                Vector3 vel = pm.Controller.velocity;
                float hSpeed = new Vector2(vel.x, vel.z).magnitude;
                displaySpeed = bhop.IsBhopping ? Mathf.Max(hSpeed, bhop.CurrentSpeed) : hSpeed;
            }

            string speedColor = displaySpeed > 16f ? "#06B6D4" : (displaySpeed > 8f ? "#10B981" : "#F3F4F6");
            string speedString = $"<color={speedColor}>{displaySpeed:F1}</color> <size=12><color=#9CA3AF>m/s</color></size>";
            GUILayout.Label(speedString, _styleSpeedoVal);

            GUILayout.EndArea();
        }

        private void InitStyles()
        {
            if (_texWindowBg != null) return;

            // Modern dark ArfHook theme with clean borders
            Color colWinBg = new Color(0.10f, 0.10f, 0.13f, 0.98f);
            Color colWinBorder = new Color(0.35f, 0.28f, 0.52f, 1.0f); // Subtle violet ArfHook border

            Color colCardBg = new Color(0.14f, 0.14f, 0.18f, 0.95f);
            Color colCardBorder = new Color(0.22f, 0.22f, 0.28f, 1.0f);

            _texWindowBg = MakeBorderTex(16, 16, colWinBg, colWinBorder);
            _texCardBg = MakeBorderTex(16, 16, colCardBg, colCardBorder);
            _texButtonOn = MakeTex(1, 1, new Color(0.06f, 0.70f, 0.44f, 1.0f));   // Emerald Green
            _texButtonOff = MakeTex(1, 1, new Color(0.22f, 0.22f, 0.26f, 1.0f));  // Dark Slate
            _texHeaderBg = MakeTex(1, 1, new Color(0.26f, 0.20f, 0.40f, 1.0f));
            _texSpeedoBg = MakeTex(1, 1, new Color(0.08f, 0.08f, 0.10f, 0.85f));

            // Window Style: Created from scratch to eliminate Unity's default white outline
            _styleWindow = new GUIStyle();
            SetAllBackgrounds(_styleWindow, _texWindowBg);
            _styleWindow.border = new RectOffset(2, 2, 2, 2);
            _styleWindow.padding = new RectOffset(0, 0, 0, 0);
            _styleWindow.margin = new RectOffset(0, 0, 0, 0);
            _styleWindow.overflow = new RectOffset(0, 0, 0, 0);

            // Title
            _styleTitle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                richText = true,
                normal = { textColor = Color.white }
            };

            // Card Style: Clean bordered container
            _styleCard = new GUIStyle();
            SetAllBackgrounds(_styleCard, _texCardBg);
            _styleCard.border = new RectOffset(2, 2, 2, 2);
            _styleCard.padding = new RectOffset(10, 10, 8, 8);
            _styleCard.margin = new RectOffset(0, 0, 0, 0);

            // Tab Buttons
            _styleTabButton = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            SetAllBackgrounds(_styleTabButton, _texButtonOff);
            _styleTabButton.normal.textColor = new Color(0.7f, 0.7f, 0.75f);
            _styleTabButton.hover.textColor = Color.white;

            _styleTabActive = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
            SetAllBackgrounds(_styleTabActive, _texHeaderBg);
            _styleTabActive.normal.textColor = new Color(0.75f, 0.65f, 1.0f); // Violeta activo
            _styleTabActive.hover.textColor = Color.white;

            // Toggle ON/OFF
            _styleToggleOn = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            SetAllBackgrounds(_styleToggleOn, _texButtonOn);
            _styleToggleOn.normal.textColor = Color.white;

            _styleToggleOff = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            SetAllBackgrounds(_styleToggleOff, _texButtonOff);
            _styleToggleOff.normal.textColor = new Color(0.65f, 0.65f, 0.70f);

            // Expand Button
            _styleExpandButton = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter
            };
            SetAllBackgrounds(_styleExpandButton, _texButtonOff);
            _styleExpandButton.normal.textColor = new Color(0.85f, 0.85f, 0.90f);

            // Module Texts
            _styleModuleHeader = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                richText = true,
                normal = { textColor = new Color(0.95f, 0.95f, 0.98f) }
            };

            _styleModuleDesc = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                richText = true,
                wordWrap = true,
                normal = { textColor = new Color(0.60f, 0.60f, 0.68f) }
            };

            // Speedometer styles
            _styleSpeedoBox = new GUIStyle();
            SetAllBackgrounds(_styleSpeedoBox, _texSpeedoBg);
            _styleSpeedoBox.padding = new RectOffset(8, 8, 6, 6);

            _styleSpeedoLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                richText = true,
                normal = { textColor = new Color(0.85f, 0.85f, 0.85f, 1f) }
            };

            _styleSpeedoVal = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                richText = true,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            // Resize Grip Style
            _styleResizeGrip = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.LowerRight,
                fontSize = 15,
                richText = true,
                padding = new RectOffset(0, 2, 0, 2)
            };
        }

        private void SetAllBackgrounds(GUIStyle style, Texture2D tex)
        {
            style.normal.background = tex;
            style.hover.background = tex;
            style.active.background = tex;
            style.focused.background = tex;
            style.onNormal.background = tex;
            style.onHover.background = tex;
            style.onActive.background = tex;
            style.onFocused.background = tex;
        }

        private Texture2D MakeBorderTex(int width, int height, Color bg, Color border)
        {
            Color[] pix = new Color[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                        pix[y * width + x] = border;
                    else
                        pix[y * width + x] = bg;
                }
            }
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i)
            {
                pix[i] = col;
            }
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}
