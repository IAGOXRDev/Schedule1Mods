using System;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine;
using Il2CppScheduleOne.PlayerScripts;
using Il2CppScheduleOne.ItemFramework;
using Il2CppScheduleOne.DevUtilities;
using Il2CppScheduleOne.Money;
using ArfSchedule1.UI;

namespace ArfSchedule1.Modules
{
    public class ItemSpawnerModule
    {
        public static ItemSpawnerModule Instance { get; private set; }

        public class ItemEntry
        {
            public string Id;
            public string Name;
            public string Description;
            public string Category;
            public ItemDefinition Definition;
        }

        private List<ItemEntry> _allItems = new List<ItemEntry>();
        private List<ItemEntry> _filteredItems = new List<ItemEntry>();
        private List<string> _categories = new List<string> { "All" };

        private string _selectedCategory = "All";
        private string _selectedLetterRange = "All";
        private string _searchQuery = "";
        private bool _isTypingSearch = false;

        private int _spawnQuantity = 1;
        private int _currentPage = 0;
        private const int ItemsPerPage = 12;

        private bool _hasLoaded = false;
        private float _loadCooldown = 0f;

        // Custom GUI styles
        private GUIStyle _styleSearchBtn;
        private GUIStyle _styleItemCard;
        private GUIStyle _styleItemName;
        private GUIStyle _styleItemId;
        private GUIStyle _styleCategoryTag;
        private GUIStyle _styleFilterActive;
        private GUIStyle _styleFilterInactive;

        private static readonly string[] LetterRanges = new string[]
        {
            "All", "A-C", "D-F", "G-I", "J-L", "M-P", "Q-S", "T-V", "W-Z"
        };

        public ItemSpawnerModule()
        {
            Instance = this;
        }

        public void EnsureItemsLoaded()
        {
            if (_hasLoaded && _allItems.Count > 0) return;

            _loadCooldown -= Time.unscaledDeltaTime;
            if (_loadCooldown > 0f) return;
            _loadCooldown = 1.5f;

            try
            {
                var discovered = new Dictionary<string, ItemEntry>(StringComparer.OrdinalIgnoreCase);

                // 1. Registry Singleton
                try
                {
                    var reg = Singleton<Il2CppScheduleOne.Registry>.Instance;
                    if (reg != null)
                    {
                        var regList = reg.ItemRegistry;
                        if (regList != null)
                        {
                            for (int i = 0; i < regList.Count; i++)
                            {
                                var regEntry = regList[i];
                                if (regEntry != null && regEntry.Definition != null)
                                {
                                    AddEntry(discovered, regEntry.Definition);
                                }
                            }
                        }

                        var rtList = reg.ItemsAddedAtRuntime;
                        if (rtList != null)
                        {
                            for (int i = 0; i < rtList.Count; i++)
                            {
                                var regEntry = rtList[i];
                                if (regEntry != null && regEntry.Definition != null)
                                {
                                    AddEntry(discovered, regEntry.Definition);
                                }
                            }
                        }
                    }
                }
                catch { }

                // 2. Resources.FindObjectsOfTypeAll to catch any asset definitions in memory
                try
                {
                    var allDefs = UnityEngine.Resources.FindObjectsOfTypeAll<ItemDefinition>();
                    if (allDefs != null)
                    {
                        for (int i = 0; i < allDefs.Length; i++)
                        {
                            var def = allDefs[i];
                            if (def != null)
                            {
                                AddEntry(discovered, def);
                            }
                        }
                    }
                }
                catch { }

                if (discovered.Count > 0)
                {
                    _allItems = new List<ItemEntry>(discovered.Values);
                    _allItems.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

                    // Extract unique categories
                    var catSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < _allItems.Count; i++)
                    {
                        if (!string.IsNullOrEmpty(_allItems[i].Category))
                        {
                            catSet.Add(_allItems[i].Category);
                        }
                    }

                    _categories = new List<string> { "All" };
                    var sortedCats = new List<string>(catSet);
                    sortedCats.Sort(StringComparer.OrdinalIgnoreCase);
                    _categories.AddRange(sortedCats);

                    FilterItems();
                    _hasLoaded = true;
                    MelonLogger.Msg($"[ItemSpawner] Successfully loaded {_allItems.Count} items ({_categories.Count - 1} categories)!");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning($"[ItemSpawner] Error loading item list: {ex.Message}");
            }
        }

        private void AddEntry(Dictionary<string, ItemEntry> dict, ItemDefinition def)
        {
            if (def == null) return;
            string id = def.ID;
            if (string.IsNullOrEmpty(id))
            {
                id = def.name;
            }
            if (string.IsNullOrEmpty(id)) return;

            if (!dict.ContainsKey(id))
            {
                string dispName = !string.IsNullOrEmpty(def.Name) ? def.Name : id;
                string catName = "General";
                try
                {
                    catName = def.Category.ToString();
                }
                catch { }

                dict[id] = new ItemEntry
                {
                    Id = id,
                    Name = dispName,
                    Description = def.Description ?? "",
                    Category = catName,
                    Definition = def
                };
            }
        }

        private void FilterItems()
        {
            string q = _searchQuery.Trim().ToLowerInvariant();

            _filteredItems = _allItems.FindAll(it =>
            {
                // Category filter
                if (_selectedCategory != "All" && !string.Equals(it.Category, _selectedCategory, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                // Letter range filter
                if (_selectedLetterRange != "All")
                {
                    char firstChar = char.ToUpperInvariant(string.IsNullOrEmpty(it.Name) ? ' ' : it.Name[0]);
                    if (!MatchesLetterRange(firstChar, _selectedLetterRange))
                    {
                        return false;
                    }
                }

                // Search query filter
                if (!string.IsNullOrEmpty(q))
                {
                    bool matchName = it.Name != null && it.Name.ToLowerInvariant().Contains(q);
                    bool matchId = it.Id != null && it.Id.ToLowerInvariant().Contains(q);
                    bool matchCat = it.Category != null && it.Category.ToLowerInvariant().Contains(q);
                    if (!matchName && !matchId && !matchCat)
                    {
                        return false;
                    }
                }

                return true;
            });

            _currentPage = 0;
        }

        private bool MatchesLetterRange(char c, string range)
        {
            if (range.Length == 3 && range[1] == '-')
            {
                char start = range[0];
                char end = range[2];
                return c >= start && c <= end;
            }
            return true;
        }

        public void SpawnItem(ItemEntry item, int quantity)
        {
            if (item == null || item.Definition == null) return;

            var player = Player.Local;
            if (player == null)
            {
                HudNotificationManager.LogCustom("<color=#EF4444>[Error] Not in a game</color>");
                return;
            }

            var inventory = PlayerSingleton<PlayerInventory>.Instance;
            if (inventory == null)
            {
                inventory = player.GetComponent<PlayerInventory>();
                if (inventory == null)
                    inventory = player.GetComponentInChildren<PlayerInventory>();
            }

            if (inventory == null)
            {
                HudNotificationManager.LogCustom("<color=#EF4444>[Error] Inventario no encontrado</color>");
                return;
            }

            int qty = Mathf.Max(1, quantity);

            try
            {
                // ==========================================
                // 1. CASO ESPECIAL: DINERO / CASH
                // ==========================================
                bool isCash = item.Id.Equals("Cash", StringComparison.OrdinalIgnoreCase) ||
                              (item.Definition is CashDefinition);

                if (isCash)
                {
                    float cashAmount = qty * 100f;
                    bool cashGiven = false;

                    try
                    {
                        var mm = NetworkSingleton<MoneyManager>.Instance;
                        if (mm != null)
                        {
                            mm.ChangeCashBalance(cashAmount, true, true);
                            cashGiven = true;
                        }
                    }
                    catch { }

                    if (!cashGiven)
                    {
                        try
                        {
                            if (inventory.cashInstance != null)
                            {
                                inventory.cashInstance.ChangeBalance(cashAmount);
                                cashGiven = true;
                            }
                        }
                        catch { }
                    }

                    if (cashGiven)
                    {
                        HudNotificationManager.LogItemSpawn($"Cash (${cashAmount:N0})", 1);
                        return;
                    }
                }

                // ==========================================
                // 2. CREATE NATIVE ITEM INSTANCE
                // ==========================================
                ItemInstance instance = null;
                try
                {
                    instance = item.Definition.GetDefaultInstance(qty);
                }
                catch { }

                if (instance == null)
                {
                    try
                    {
                        instance = new ItemInstance(item.Definition, qty);
                    }
                    catch { }
                }

                if (instance == null)
                {
                    HudNotificationManager.LogCustom($"<color=#EF4444>[Error] Could not create instance of {item.Name}</color>");
                    return;
                }

                // ==========================================
                // 3. DIRECT INSERTION INTO INVENTORY SLOTS
                // (Bypasses AddItemToInventory which is stripped in Il2Cpp)
                // ==========================================
                bool inserted = false;

                // Step A: Try to stack in Hotbar slots
                if (inventory.hotbarSlots != null)
                {
                    for (int i = 0; i < inventory.hotbarSlots.Count; i++)
                    {
                        var slot = inventory.hotbarSlots[i];
                        if (slot != null && !slot.IsLocked && slot.ItemInstance != null)
                        {
                            if (slot.ItemInstance.Definition == item.Definition ||
                                string.Equals(slot.ItemInstance.Definition?.ID, item.Definition.ID, StringComparison.OrdinalIgnoreCase))
                            {
                                if (!slot.IsAtCapacity)
                                {
                                    try
                                    {
                                        slot.ChangeQuantity(qty, false);
                                        inserted = true;
                                        break;
                                    }
                                    catch { }
                                }
                            }
                        }
                    }

                    // If not stacked, place into first empty Hotbar slot
                    if (!inserted)
                    {
                        for (int i = 0; i < inventory.hotbarSlots.Count; i++)
                        {
                            var slot = inventory.hotbarSlots[i];
                            if (slot != null && !slot.IsLocked && slot.ItemInstance == null)
                            {
                                if (TryInsertIntoSlot(slot, instance))
                                {
                                    inserted = true;
                                    break;
                                }
                            }
                        }
                    }
                }

                // Step B: If Hotbar is full, search all remaining inventory slots
                if (!inserted)
                {
                    try
                    {
                        var allSlots = inventory.GetAllInventorySlots();
                        if (allSlots != null)
                        {
                            // Try stacking
                            for (int i = 0; i < allSlots.Count; i++)
                            {
                                var slot = allSlots[i];
                                if (slot != null && !slot.IsLocked && slot.ItemInstance != null)
                                {
                                    if (slot.ItemInstance.Definition == item.Definition ||
                                        string.Equals(slot.ItemInstance.Definition?.ID, item.Definition.ID, StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (!slot.IsAtCapacity)
                                        {
                                            try
                                            {
                                                slot.ChangeQuantity(qty, false);
                                                inserted = true;
                                                break;
                                            }
                                            catch { }
                                        }
                                    }
                                }
                            }

                            // Try empty slot
                            if (!inserted)
                            {
                                for (int i = 0; i < allSlots.Count; i++)
                                {
                                    var slot = allSlots[i];
                                    if (slot != null && !slot.IsLocked && slot.ItemInstance == null)
                                    {
                                        if (TryInsertIntoSlot(slot, instance))
                                        {
                                            inserted = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }

                // Step C: Try equippedSlot if empty
                if (!inserted && inventory.equippedSlot != null && inventory.equippedSlot.ItemInstance == null)
                {
                    if (TryInsertIntoSlot(inventory.equippedSlot, instance))
                    {
                        inserted = true;
                    }
                }

                if (inserted)
                {
                    try
                    {
                        inventory.EquippedSlotChanged();
                    }
                    catch { }

                    HudNotificationManager.LogItemSpawn(item.Name, qty);
                }
                else
                {
                    HudNotificationManager.LogCustom("<color=#EF4444>[Inventory Full] No free slots available</color>");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[ItemSpawner] Failed to spawn {item.Name}: {ex.Message}");
                HudNotificationManager.LogCustom($"<color=#EF4444>[Error spawning {item.Name}]</color>");
            }
        }

        private bool TryInsertIntoSlot(ItemSlot slot, ItemInstance instance)
        {
            if (slot == null || instance == null) return false;

            try
            {
                slot.InsertItem(instance);
                return true;
            }
            catch { }

            try
            {
                slot.SetStoredItem(instance, false);
                return true;
            }
            catch { }

            try
            {
                slot.AddItem(instance, false);
                return true;
            }
            catch { }

            return false;
        }

        public void OnDrawTab(GUIStyle cardStyle, GUIStyle buttonOnStyle, GUIStyle buttonOffStyle)
        {
            EnsureItemsLoaded();
            InitStyles(cardStyle, buttonOnStyle, buttonOffStyle);

            // Handle keyboard input for search without using GUI.DoTextField
            HandleSearchKeyboardInput();

            // ================= HEADER CONTAINER =================
            GUILayout.BeginVertical(_styleItemCard);

            // 1. Interactive Search Bar
            GUILayout.BeginHorizontal();
            GUILayout.Label("<color=#A78BFA><b>Search:</b></color>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 12 }, GUILayout.Width(55));

            string searchDisplay;
            if (_isTypingSearch)
            {
                string curText = string.IsNullOrEmpty(_searchQuery) ? "Type with your keyboard..." : _searchQuery;
                searchDisplay = $"<color=#00FFFF>⌨ [ {curText} ] ▌</color>";
            }
            else
            {
                searchDisplay = string.IsNullOrEmpty(_searchQuery) 
                    ? "<color=#9CA3AF>🔍 Click here to type name or ID...</color>" 
                    : $"<color=#FFFFFF>🔍 <b>{_searchQuery}</b></color>";
            }

            GUIStyle searchBoxStyle = _isTypingSearch ? buttonOnStyle : _styleSearchBtn;
            if (GUILayout.Button(searchDisplay, searchBoxStyle, GUILayout.Height(26)))
            {
                _isTypingSearch = !_isTypingSearch;
            }

            if (!string.IsNullOrEmpty(_searchQuery) || _isTypingSearch)
            {
                if (GUILayout.Button("✕ Clear", buttonOffStyle, GUILayout.Width(70), GUILayout.Height(26)))
                {
                    _searchQuery = "";
                    _isTypingSearch = false;
                    FilterItems();
                }
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(6f);

            // 2. Category Filter
            if (_categories.Count > 1)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("<color=#9CA3AF>Category:</color>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 11 }, GUILayout.Width(65));
                
                // Show category buttons in compact rows
                int catsPerRow = 4;
                for (int i = 0; i < _categories.Count; i++)
                {
                    if (i > 0 && i % catsPerRow == 0)
                    {
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(69f);
                    }

                    string cat = _categories[i];
                    bool isSelected = (_selectedCategory == cat);
                    GUIStyle catBtnStyle = isSelected ? _styleFilterActive : _styleFilterInactive;

                    if (GUILayout.Button(cat, catBtnStyle, GUILayout.Height(22)))
                    {
                        _selectedCategory = cat;
                        FilterItems();
                    }
                    GUILayout.Space(2f);
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(4f);

            // 3. Letter Range Filter
            GUILayout.BeginHorizontal();
            GUILayout.Label("<color=#9CA3AF>Letter:</color>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 11 }, GUILayout.Width(65));
            for (int i = 0; i < LetterRanges.Length; i++)
            {
                string r = LetterRanges[i];
                bool isSelected = (_selectedLetterRange == r);
                GUIStyle rBtnStyle = isSelected ? _styleFilterActive : _styleFilterInactive;

                if (GUILayout.Button(r, rBtnStyle, GUILayout.Height(20)))
                {
                    _selectedLetterRange = r;
                    FilterItems();
                }
                GUILayout.Space(2f);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);

            // 4. Spawn Quantity Selector
            GUILayout.BeginHorizontal();
            GUILayout.Label("<color=#9CA3AF>Spawn Quantity:</color>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 11 }, GUILayout.Width(130));

            int[] quickQtys = new int[] { 1, 5, 10, 50, 100, 999 };
            for (int i = 0; i < quickQtys.Length; i++)
            {
                int q = quickQtys[i];
                bool isCur = (_spawnQuantity == q);
                GUIStyle qStyle = isCur ? buttonOnStyle : buttonOffStyle;
                if (GUILayout.Button($"{q}", qStyle, GUILayout.Width(36), GUILayout.Height(22)))
                {
                    _spawnQuantity = q;
                }
                GUILayout.Space(2f);
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("↻ Reload", buttonOffStyle, GUILayout.Width(76), GUILayout.Height(22)))
            {
                _hasLoaded = false;
                EnsureItemsLoaded();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUILayout.Space(6f);

            // ================= STATS & PAGINATION =================
            int totalFiltered = _filteredItems.Count;
            int totalPages = Mathf.Max(1, (int)Math.Ceiling(totalFiltered / (double)ItemsPerPage));
            if (_currentPage >= totalPages) _currentPage = totalPages - 1;
            if (_currentPage < 0) _currentPage = 0;

            GUILayout.BeginHorizontal();
            GUILayout.Label($"<color=#9CA3AF>Showing <b>{totalFiltered}</b> of <b>{_allItems.Count}</b> items</color>",
                new GUIStyle(GUI.skin.label) { richText = true, fontSize = 11 });

            GUILayout.FlexibleSpace();

            if (totalPages > 1)
            {
                if (GUILayout.Button("◀ Prev", buttonOffStyle, GUILayout.Width(50), GUILayout.Height(20)))
                {
                    if (_currentPage > 0) _currentPage--;
                }

                GUILayout.Label($"<color=#D1D5DB>Page {_currentPage + 1}/{totalPages}</color>",
                    new GUIStyle(GUI.skin.label) { richText = true, fontSize = 11, alignment = TextAnchor.MiddleCenter }, GUILayout.Width(80));

                if (GUILayout.Button("Next ▶", buttonOffStyle, GUILayout.Width(50), GUILayout.Height(20)))
                {
                    if (_currentPage < totalPages - 1) _currentPage++;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);

            // ================= ITEM LIST =================
            if (totalFiltered == 0)
            {
                GUILayout.BeginVertical(_styleItemCard);
                if (_allItems.Count == 0)
                {
                    GUILayout.Label("<color=#F59E0B>Loading game items... Load or start a game.</color>",
                        new GUIStyle(GUI.skin.label) { richText = true, alignment = TextAnchor.MiddleCenter, fontSize = 12 });
                }
                else
                {
                    GUILayout.Label("<color=#9CA3AF>No items found matching the current filters.</color>",
                        new GUIStyle(GUI.skin.label) { richText = true, alignment = TextAnchor.MiddleCenter, fontSize = 12 });
                }
                GUILayout.EndVertical();
            }
            else
            {
                int startIndex = _currentPage * ItemsPerPage;
                int endIndex = Math.Min(startIndex + ItemsPerPage, totalFiltered);

                for (int i = startIndex; i < endIndex; i++)
                {
                    var item = _filteredItems[i];
                    DrawItemRow(item, buttonOnStyle);
                }
            }
        }

        private void HandleSearchKeyboardInput()
        {
            if (!_isTypingSearch) return;

            try
            {
                string input = UnityEngine.Input.inputString;
                if (!string.IsNullOrEmpty(input))
                {
                    bool changed = false;
                    for (int i = 0; i < input.Length; i++)
                    {
                        char c = input[i];
                        if (c == '\b') // Backspace
                        {
                            if (_searchQuery.Length > 0)
                            {
                                _searchQuery = _searchQuery.Substring(0, _searchQuery.Length - 1);
                                changed = true;
                            }
                        }
                        else if (c == '\n' || c == '\r') // Enter
                        {
                            _isTypingSearch = false;
                        }
                        else if (c == 27) // Escape
                        {
                            _isTypingSearch = false;
                        }
                        else if (!char.IsControl(c))
                        {
                            _searchQuery += c;
                            changed = true;
                        }
                    }

                    if (changed)
                    {
                        FilterItems();
                    }
                }
            }
            catch { }
        }

        private void DrawItemRow(ItemEntry item, GUIStyle spawnBtnStyle)
        {
            GUILayout.BeginVertical(_styleItemCard);
            GUILayout.BeginHorizontal();

            // Item Information
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label($"<b>{item.Name}</b>", _styleItemName);
            GUILayout.Space(6f);
            GUILayout.Label($"<color=#818CF8>[{item.Category}]</color>", _styleCategoryTag);
            GUILayout.EndHorizontal();

            GUILayout.Label($"<color=#6B7280>ID: {item.Id}</color>", _styleItemId);

            if (!string.IsNullOrEmpty(item.Description))
            {
                GUILayout.Label($"<color=#9CA3AF><size=10>{item.Description}</size></color>",
                    new GUIStyle(GUI.skin.label) { richText = true, wordWrap = true });
            }
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            // Spawn Button
            string btnText = _spawnQuantity > 1 ? $"Spawn ({_spawnQuantity})" : "Spawn";
            if (GUILayout.Button(btnText, spawnBtnStyle, GUILayout.Width(80), GUILayout.Height(30)))
            {
                SpawnItem(item, _spawnQuantity);
            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.Space(4f);
        }

        private void InitStyles(GUIStyle cardStyle, GUIStyle buttonOnStyle, GUIStyle buttonOffStyle)
        {
            if (_styleSearchBtn != null) return;

            // Search input styled button
            _styleSearchBtn = new GUIStyle(buttonOffStyle)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleLeft,
                richText = true
            };
            _styleSearchBtn.padding = new RectOffset(10, 10, 4, 4);

            _styleItemCard = new GUIStyle(cardStyle);
            _styleItemCard.padding = new RectOffset(8, 8, 6, 6);

            _styleItemName = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                richText = true,
                normal = { textColor = new Color(0.95f, 0.95f, 0.98f) }
            };

            _styleCategoryTag = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                richText = true,
                normal = { textColor = new Color(0.6f, 0.65f, 0.95f) }
            };

            _styleItemId = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                richText = true,
                normal = { textColor = new Color(0.5f, 0.5f, 0.55f) }
            };

            _styleFilterActive = new GUIStyle(buttonOnStyle)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(6, 6, 2, 2)
            };

            _styleFilterInactive = new GUIStyle(buttonOffStyle)
            {
                fontSize = 10,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(6, 6, 2, 2)
            };
        }
    }
}
