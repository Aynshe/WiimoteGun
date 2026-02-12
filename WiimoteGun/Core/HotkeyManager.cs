using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace WiimoteGun
{
    /// <summary>
    /// Central manager for hotkey detection and execution
    /// (EN/FR: Gestionnaire central pour détection et exécution des hotkeys)
    /// </summary>
    public static class HotkeyManager
    {
        // Hotkey profiles per player (EN/FR: Profils hotkeys par joueur)
        private static Dictionary<int, HotkeyProfile> _playerHotkeys = new Dictionary<int, HotkeyProfile>();

        /// <summary>
        /// Reload profiles from Options - useful after edits or resets
        /// (EN/FR: Recharger les profils depuis Options - utile après édition ou reset)
        /// </summary>
        public static void ReloadProfiles()
        {
            LoadFromOptions();
            ClearActiveState(); // Prevent stuck keys if reloading during use
            SimpleLogger.Instance.Info("[HotkeyManager] Profiles reloaded from Options");
        }

        // Active modifiers tracking: PlayerIndex -> ModifierName -> WasConsumed (EN/FR: Suivi modificateurs: Idx -> Nom -> Consommé)
        private static Dictionary<int, Dictionary<string, bool>> _activeModifiers = new Dictionary<int, Dictionary<string, bool>>();
        
        // Active triggers tracking: PlayerIndex -> HashSet<ButtonName> (EN/FR: Suivi déclencheurs actifs)
        private static Dictionary<int, HashSet<string>> _activeTriggers = new Dictionary<int, HashSet<string>>();
        
        private static Dictionary<int, DateTime> _lastButtonPressTime = new Dictionary<int, DateTime>();
        private static Dictionary<int, string> _lastButtonPressed = new Dictionary<int, string>();

        // Constants (EN/FR: Constantes)
        private const int LONG_PRESS_THRESHOLD_MS = 500; // Seuil pression longue
        private const string PLUS_BUTTON = "Plus"; // Reserved for overlay toggle
        
        // Allowed modifiers (EN/FR: Modificateurs autorisés)
        public static readonly HashSet<string> AllowedModifiers = new HashSet<string> 
        { 
            "Home", "Minus", "Plus", "One", "Two", "A", "B", "Up", "Down", "Left", "Right",
            "NunC", "NunZ", "NunUp", "NunDown", "NunLeft", "NunRight"
        };

        // Overlay state delegate (EN/FR: Délégué état overlay)
        public static Func<bool> IsOverlayOpen { get; set; }

        /// <summary>
        /// Initialize the hotkey manager (EN/FR: Initialiser gestionnaire)
        /// </summary>
        public static void Initialize()
        {
            _playerHotkeys.Clear();
            _activeModifiers.Clear();
            _activeTriggers.Clear();
            _lastButtonPressTime.Clear();
            _lastButtonPressed.Clear();

            // Initialize profiles for 4 players (EN/FR: Initialiser profils pour 4 joueurs)
            for (int i = 1; i <= 4; i++)
            {
                _playerHotkeys[i] = new HotkeyProfile(i);
                _activeModifiers[i] = new Dictionary<string, bool>();
                _activeTriggers[i] = new HashSet<string>();
            }

            // Load saved hotkeys from Options (EN/FR: Charger hotkeys sauvegardées depuis Options)
            LoadFromOptions();

            SimpleLogger.Instance.Info("[HotkeyManager] Initialized");
        }

        /// <summary>
        /// Load hotkey profiles from Options.cs (settings.cfg)
        /// (EN/FR: Charger profils hotkeys depuis Options.cs)
        /// </summary>
        public static void LoadFromOptions()
        {
            try
            {
                var options = Options.Instance;
                
                if (options.HotkeyProfileP1 != null && options.HotkeyProfileP1.Hotkeys != null)
                {
                    _playerHotkeys[1] = options.HotkeyProfileP1;
                    _playerHotkeys[1].PlayerIndex = 1;
                    SimpleLogger.Instance.Info(string.Format("[HotkeyManager] Loaded {0} hotkeys for P1", options.HotkeyProfileP1.Count));
                }
                if (options.HotkeyProfileP2 != null && options.HotkeyProfileP2.Hotkeys != null)
                {
                    _playerHotkeys[2] = options.HotkeyProfileP2;
                    _playerHotkeys[2].PlayerIndex = 2;
                    SimpleLogger.Instance.Info(string.Format("[HotkeyManager] Loaded {0} hotkeys for P2", options.HotkeyProfileP2.Count));
                }
                if (options.HotkeyProfileP3 != null && options.HotkeyProfileP3.Hotkeys != null)
                {
                    _playerHotkeys[3] = options.HotkeyProfileP3;
                    _playerHotkeys[3].PlayerIndex = 3;
                    SimpleLogger.Instance.Info(string.Format("[HotkeyManager] Loaded {0} hotkeys for P3", options.HotkeyProfileP3.Count));
                }
                if (options.HotkeyProfileP4 != null && options.HotkeyProfileP4.Hotkeys != null)
                {
                    _playerHotkeys[4] = options.HotkeyProfileP4;
                    _playerHotkeys[4].PlayerIndex = 4;
                    SimpleLogger.Instance.Info(string.Format("[HotkeyManager] Loaded {0} hotkeys for P4", options.HotkeyProfileP4.Count));
                }
                
                // Debug log total loaded (EN/FR: Log total chargé)
                SimpleLogger.Instance.Info(string.Format("[HotkeyManager] Total Loaded: P1={0}, P2={1}, P3={2}, P4={3}",
                    _playerHotkeys.ContainsKey(1) ? _playerHotkeys[1].Count : 0,
                    _playerHotkeys.ContainsKey(2) ? _playerHotkeys[2].Count : 0,
                    _playerHotkeys.ContainsKey(3) ? _playerHotkeys[3].Count : 0,
                    _playerHotkeys.ContainsKey(4) ? _playerHotkeys[4].Count : 0));
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Warning(string.Format("[HotkeyManager] Failed to load hotkeys from Options: {0}", ex.Message));
            }
        }

        /// <summary>
        /// Save hotkey profiles to Options.cs (settings.cfg)
        /// (EN/FR: Sauvegarder profils hotkeys vers Options.cs)
        /// </summary>
        public static void SaveToOptions()
        {
            try
            {
                var options = Options.Instance;
                
                options.HotkeyProfileP1 = _playerHotkeys.ContainsKey(1) ? _playerHotkeys[1] : null;
                options.HotkeyProfileP2 = _playerHotkeys.ContainsKey(2) ? _playerHotkeys[2] : null;
                options.HotkeyProfileP3 = _playerHotkeys.ContainsKey(3) ? _playerHotkeys[3] : null;
                options.HotkeyProfileP4 = _playerHotkeys.ContainsKey(4) ? _playerHotkeys[4] : null;
                
                // Save Options to disk (EN/FR: Sauvegarder Options sur disque)
                options.Save();
                
                SimpleLogger.Instance.Info("[HotkeyManager] Hotkeys saved to settings.cfg");

                // Sync to active remap profile if one is loaded (EN/FR: Synchroniser vers le profil remap actif si chargé)
                string activeProfileName = Program.GetActiveRemapProfile();
                if (!string.IsNullOrEmpty(activeProfileName))
                {
                    try
                    {
                        var profile = RemapProfileManager.LoadProfile(activeProfileName);
                        if (profile != null)
                        {
                            // Update Hotkeys in the profile (EN/FR: Mettre à jour les Hotkeys dans le profil)
                            profile.P1Hotkeys = _playerHotkeys.ContainsKey(1) ? _playerHotkeys[1].Hotkeys.Select(h => h.Clone()).ToList() : new List<Hotkey>();
                            profile.P2Hotkeys = _playerHotkeys.ContainsKey(2) ? _playerHotkeys[2].Hotkeys.Select(h => h.Clone()).ToList() : new List<Hotkey>();
                            profile.P3Hotkeys = _playerHotkeys.ContainsKey(3) ? _playerHotkeys[3].Hotkeys.Select(h => h.Clone()).ToList() : new List<Hotkey>();
                            profile.P4Hotkeys = _playerHotkeys.ContainsKey(4) ? _playerHotkeys[4].Hotkeys.Select(h => h.Clone()).ToList() : new List<Hotkey>();
                            
                            // Determine folder and filename (EN/FR: Déterminer dossier et nom de fichier)
                            string subfolder = System.IO.Path.GetDirectoryName(activeProfileName);
                            string filename = System.IO.Path.GetFileName(activeProfileName);

                            if (RemapProfileManager.SaveProfile(filename, subfolder, profile))
                            {
                                SimpleLogger.Instance.Info(string.Format("[HotkeyManager] Synced hotkeys to active profile: {0}", activeProfileName));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                         SimpleLogger.Instance.Warning(string.Format("[HotkeyManager] Failed to sync to active profile: {0}", ex.Message));
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error(string.Format("[HotkeyManager] Failed to save hotkeys: {0}", ex.Message));
            }
        }

        /// <summary>
        /// Get hotkey profile for a player (EN/FR: Obtenir profil hotkey)
        /// </summary>
        public static HotkeyProfile GetProfile(int playerIndex)
        {
            if (!_playerHotkeys.ContainsKey(playerIndex))
            {
                _playerHotkeys[playerIndex] = new HotkeyProfile(playerIndex);
            }
            return _playerHotkeys[playerIndex];
        }

        /// <summary>
        /// Get the hotkey profile for a specific player (Raw access without Shared Logic)
        /// (EN/FR: Obtenir le profil hotkey pour un joueur donné (Accès brut sans logique partagée))
        /// </summary>
        public static HotkeyProfile GetRawProfile(int playerIndex)
        {
            if (!_playerHotkeys.ContainsKey(playerIndex))
            {
                _playerHotkeys[playerIndex] = new HotkeyProfile(playerIndex);
            }
            return _playerHotkeys[playerIndex];
        }

        /// <summary>
        /// Set hotkey profile for a player and save to Options (EN/FR: Définir profil hotkey et sauvegarder)
        /// </summary>
        public static void SetProfile(int playerIndex, HotkeyProfile profile)
        {
            SimpleLogger.Instance.Info(string.Format("[HotkeyManager] SetProfile P{0} with {1} hotkeys. Updating Options...", playerIndex, profile.Hotkeys.Count));
            _playerHotkeys[playerIndex] = profile;
            
            // Clear any stuck states since profile changed (EN/FR: Nettoyer états bloqués car profil changé)
            ClearActiveState();

            // Auto-save to Options when profile is updated (EN/FR: Auto-sauvegarde quand profil modifié)
            SaveToOptions();
        }

        /// <summary>
        /// Handle button press event (EN/FR: Gérer événement pression bouton)
        /// </summary>
        public static void OnButtonPressed(int playerIndex, string button)
        {
            // Debug Log: Trace every button press to see if it reaches Manager
            // (EN/FR: Log de débogage : Tracer chaque pression bouton)
            SimpleLogger.Instance.Info(string.Format("[Hotkey] OnButtonPressed P{0} Button={1}", playerIndex, button));

            if (IsOverlayOpen != null && IsOverlayOpen())
            {
                 SimpleLogger.Instance.Warning("[Hotkey] Input blocked because Overlay is Open");
                 return;
            }

            var profile = GetProfile(playerIndex);
            
            // 1. Modifier Check: If button is a configured modifier, track it
            // (EN/FR: 1. Vérification Modificateur : si bouton est modificateur configuré, le suivre)
            bool isConfiguredModifier = AllowedModifiers.Contains(button) && 
                                        (profile.Hotkeys.Any(h => string.Equals(h.ModifierButton, button, StringComparison.OrdinalIgnoreCase)) ||
                                        (playerIndex > 1 && GetProfile(1).Hotkeys.Any(h => h.IsShared && string.Equals(h.ModifierButton, button, StringComparison.OrdinalIgnoreCase))));

            if (isConfiguredModifier)
            {
                if (!_activeModifiers.ContainsKey(playerIndex))
                    _activeModifiers[playerIndex] = new Dictionary<string, bool>();

                if (!_activeModifiers[playerIndex].ContainsKey(button))
                {
                    _activeModifiers[playerIndex][button] = false; // Not yet consumed
                    SimpleLogger.Instance.Debug(string.Format("[Hotkey] P{0} Modifier {1} down", playerIndex, button));
                }
            }

            // 2. Combo Check: Check active modifiers
            if (_activeModifiers.ContainsKey(playerIndex) && _activeModifiers[playerIndex].Count > 0)
            {
                foreach (var modifier in _activeModifiers[playerIndex].Keys.ToList())
                {
                    if (modifier == button) continue;

                    // Case-Inensitive comparison
                    if (string.Equals(modifier, "Home", StringComparison.OrdinalIgnoreCase) && 
                        string.Equals(button, PLUS_BUTTON, StringComparison.OrdinalIgnoreCase)) continue;

                    // CHECK FOR ANY HOTKEY WITH THIS TRIGGER+MODIFIER
                    bool hasHotkey = profile.HasHotkey(button, modifier);
                    
                    // FALLBACK: If not found in player profile, check shared hotkeys in P1 profile
                    // (EN/FR: REPLI : Si non trouvé, vérifier hotkeys partagées de P1)
                    if (!hasHotkey && playerIndex > 1)
                    {
                        var p1Profile = GetProfile(1);
                        var sharedHotkey = p1Profile.GetHotkey(button, modifier);
                        if (sharedHotkey != null && sharedHotkey.IsShared)
                        {
                            hasHotkey = true;
                            SimpleLogger.Instance.Info(string.Format("[Hotkey] Shared Match Found from P1: {0} + {1}", modifier, button));
                        }
                    }

                    if (hasHotkey)
                    {
                        SimpleLogger.Instance.Info(string.Format("[Hotkey] Match Found: {0} + {1}", modifier, button));
                        // Match found!
                        _activeModifiers[playerIndex][modifier] = true; // Modifier Consumed

                        // Mark Trigger as Consumed
                        if (!_activeTriggers.ContainsKey(playerIndex))
                            _activeTriggers[playerIndex] = new HashSet<string>();
                        _activeTriggers[playerIndex].Add(button); // Trigger Consumed

                        // Start Timing
                        _lastButtonPressed[playerIndex] = button;
                        _lastButtonPressTime[playerIndex] = DateTime.Now;

                        SimpleLogger.Instance.Debug(string.Format("[Hotkey] P{0} {1}+{2} combo started (Consumed)", playerIndex, modifier, button));
                        break; // Only trigger one combo at a time per button press
                    }
                }
            }
        }

        /// <summary>
        /// Handle button release event (EN/FR: Gérer événement relâchement bouton)
        /// </summary>
        public static void OnButtonReleased(int playerIndex, string button)
        {
            // CRITICAL: Hotkeys only active when overlay is CLOSED
            if (IsOverlayOpen != null && IsOverlayOpen())
            {
                return;
            }

            // 1. Handle Trigger Release
            if (_activeTriggers.ContainsKey(playerIndex) && _activeTriggers[playerIndex].Contains(button))
            {
                _activeTriggers[playerIndex].Remove(button);

                // Check if it was tracked for timing
                if (_lastButtonPressed.ContainsKey(playerIndex) && _lastButtonPressed[playerIndex] == button)
                {
                    TimeSpan pressDuration = DateTime.Now - _lastButtonPressTime[playerIndex];
                    // Clean up tracking
                    _lastButtonPressed.Remove(playerIndex);
                    _lastButtonPressTime.Remove(playerIndex);

                    bool isLongPress = pressDuration.TotalMilliseconds >= LONG_PRESS_THRESHOLD_MS;
                    
                    var profile = GetProfile(playerIndex);
                    
                    // Find which modifier triggered this
                    if (_activeModifiers.ContainsKey(playerIndex))
                    {
                        foreach (var mod in _activeModifiers[playerIndex].Keys)
                        {
                            var hotkey = profile.GetHotkey(button, mod);
                            
                            // FALLBACK: If not found in player profile, check shared hotkeys in P1 profile
                            if (hotkey == null && playerIndex > 1)
                            {
                                var p1Hotkey = GetProfile(1).GetHotkey(button, mod);
                                if (p1Hotkey != null && p1Hotkey.IsShared)
                                {
                                    hotkey = p1Hotkey;
                                }
                            }
                            
                            if (hotkey != null)
                            {
                                List<Keys> keysToExecute = null;

                                if (isLongPress && hotkey.LongPressKeys.Count > 0)
                                {
                                    keysToExecute = hotkey.LongPressKeys;
                                    SimpleLogger.Instance.Info(string.Format("[Hotkey] P{0} Executing LONG: {1}", playerIndex, hotkey.GetDisplayName()));
                                }
                                else if (!isLongPress && hotkey.ShortPressKeys.Count > 0)
                                {
                                    keysToExecute = hotkey.ShortPressKeys;
                                    SimpleLogger.Instance.Info(string.Format("[Hotkey] P{0} Executing SHORT: {1}", playerIndex, hotkey.GetDisplayName()));
                                }
                                else if (isLongPress && hotkey.LongPressKeys.Count == 0 && hotkey.ShortPressKeys.Count > 0)
                                {
                                     // Fallback or explicit ignore? User plan said differentiation is key.
                                     // But if user holds it too long by accident, maybe trigger short?
                                     // Let's STICK TO STRICT for now as per plan logic "Differentiation is key"
                                     // If Short is defined but user presses Long (and Long is empty), do NOTHING?
                                     // Use case: assigning Long to something else. 
                                     // If user wants same action on both, they assign both.
                                     SimpleLogger.Instance.Info(string.Format("[Hotkey] P{0} Long press detected but no Long action defined. Ignored.", playerIndex));
                                }

                                if (keysToExecute != null && keysToExecute.Count > 0)
                                {
                                    ExecuteHotkey(playerIndex, keysToExecute);
                                }
                                break; 
                            }
                        }
                    }
                }
                return; // Trigger released, done.
            }

            // 2. Handle Modifier Release
            if (_activeModifiers.ContainsKey(playerIndex) && _activeModifiers[playerIndex].ContainsKey(button))
            {
                bool wasConsumed = _activeModifiers[playerIndex][button];
                _activeModifiers[playerIndex].Remove(button);
                
                SimpleLogger.Instance.Debug(string.Format("[Hotkey] P{0} Modifier {1} released (Consumed: {2})", playerIndex, button, wasConsumed));
            }
        }

        /// <summary>
        /// Check if a button should be suppressed (swallowed) because it's a modifier
        /// (EN/FR: Vérifier si bouton doit être supprimé car modificateur)
        /// </summary>
        public static bool ShouldSuppressButton(int playerIndex, string button)
        {
            // If button is currently acting as a Modifier (Held), suppress it.
            // (EN/FR: Si bouton agit comme modificateur (Maintenu), supprimer)
            if (_activeModifiers.ContainsKey(playerIndex) && _activeModifiers[playerIndex].ContainsKey(button))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Check if any modifier is currently active (used to suppress triggers)
        /// (EN/FR: Vérifier si un modificateur est actif)
        /// </summary>
        public static bool IsAnyModifierActive(int playerIndex)
        {
             return _activeModifiers.ContainsKey(playerIndex) && _activeModifiers[playerIndex].Count > 0;
        }

        /// <summary>
        /// Execute a list of keys
        /// (EN/FR: Exécuter une liste de touches)
        /// </summary>
        private static void ExecuteHotkey(int playerIndex, List<Keys> keysToExecute)
        {
            if (keysToExecute == null || keysToExecute.Count == 0)
            {
                return;
            }

            try
            {
                // Separate modifier keys and regular keys
                // (EN/FR: Séparer touches modificatrices et touches régulières)
                List<Keys> modifiers = new List<Keys>();
                List<Keys> regularKeys = new List<Keys>();

                foreach (Keys key in keysToExecute)
                {
                    if (IsModifierKey(key))
                    {
                        modifiers.Add(key);
                    }
                    else
                    {
                        regularKeys.Add(key);
                    }
                }

                // Press modifiers first (EN/FR: Presser modificateurs d'abord)
                foreach (Keys modifier in modifiers)
                {
                    SendKeyPress(playerIndex, modifier, true);
                    System.Threading.Thread.Sleep(10); // Small delay for stability
                }

                // Press regular keys (EN/FR: Presser touches régulières)
                foreach (Keys key in regularKeys)
                {
                    SendKeyPress(playerIndex, key, true);
                    System.Threading.Thread.Sleep(10);
                    SendKeyPress(playerIndex, key, false); // Release immediately
                }

                // Release modifiers in reverse order (EN/FR: Relâcher modificateurs en ordre inverse)
                for (int i = modifiers.Count - 1; i >= 0; i--)
                {
                    SendKeyPress(playerIndex, modifiers[i], false);
                    System.Threading.Thread.Sleep(10);
                }

                SimpleLogger.Instance.Info(string.Format("[Hotkey] Sent keys: {0}", string.Join("+", keysToExecute.Select(k => k.ToString()).ToArray())));
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"[Hotkey] Failed to execute hotkey: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if a button has been consumed by a hotkey combo
        /// (EN/FR: Vérifier si bouton a été consommé par un combo)
        /// </summary>
        public static bool IsButtonConsumed(int playerIndex, string button)
        {
            // 1. Check Modifiers (Only suppress if consumed by a combo)
            // (EN/FR: 1. Vérifier Modificateurs — supprimer uniquement si consommé par un combo)
            if (_activeModifiers.ContainsKey(playerIndex) && 
                _activeModifiers[playerIndex].ContainsKey(button))
            {
                // Only consumed if a trigger was activated (true = combo active)
                // (EN/FR: Consommé uniquement si un trigger a été activé)
                return _activeModifiers[playerIndex][button];
            }

            // 2. Check Triggers (Suppress inputs that are part of a combo)
            if (_activeTriggers.ContainsKey(playerIndex) && _activeTriggers[playerIndex].Contains(button))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check if a key is a modifier (Alt, Ctrl, Shift, Win)
        /// (EN/FR: Vérifier si touche est modificateur)
        /// </summary>
        private static bool IsModifierKey(Keys key)
        {
            return key == Keys.ControlKey || key == Keys.Control || key == Keys.LControlKey || key == Keys.RControlKey ||
                   key == Keys.Menu || key == Keys.Alt || key == Keys.LMenu || key == Keys.RMenu ||
                   key == Keys.ShiftKey || key == Keys.Shift || key == Keys.LShiftKey || key == Keys.RShiftKey ||
                   key == Keys.LWin || key == Keys.RWin;
        }

        /// <summary>
        /// Convert modifier flags to actual VK codes (EN/FR: Convertir flags en codes VK réels)
        /// Keys.Alt = 0x40000 (flag), but VK_MENU = 0x12 (actual key)
        /// </summary>
        private static Keys ConvertToVirtualKey(Keys key)
        {
            // CRITICAL: Keys.Alt, Keys.Control, Keys.Shift are FLAGS (0x40000, 0x20000, 0x10000)
            // not actual VK codes! Must convert to ControlKey, Menu, ShiftKey
            // (EN/FR: Keys.Alt, Control, Shift sont des FLAGS, pas des codes VK!)
            if (key == Keys.Alt || key == Keys.LMenu || key == Keys.RMenu)
                return Keys.Menu; // VK_MENU = 0x12
            if (key == Keys.Control || key == Keys.LControlKey || key == Keys.RControlKey)
                return Keys.ControlKey; // VK_CONTROL = 0x11
            if (key == Keys.Shift || key == Keys.LShiftKey || key == Keys.RShiftKey)
                return Keys.ShiftKey; // VK_SHIFT = 0x10
            
            return key;
        }

        // Shared keyboard instance to avoid creating new one for each key
        // (EN/FR: Instance clavier partagée pour éviter création à chaque touche)
        private static VirtualSendInputKeyboard _sharedKeyboard;

        /// <summary>
        /// Send key press/release via Interception or SendInput
        /// (EN/FR: Envoyer pression/relâchement touche)
        /// </summary>
        private static void SendKeyPress(int playerIndex, Keys key, bool pressed)
        {
            // Use VirtualSendInputKeyboard to send keys
            // (EN/FR: Utiliser VirtualSendInputKeyboard pour envoyer touches)
            try
            {
                // Convert modifier flags to actual VK codes (EN/FR: Convertir flags en codes VK)
                Keys actualKey = ConvertToVirtualKey(key);
                
                // Try to find the specific player's controller to use its virtual keyboard (VMulti/SendInput)
                // (EN/FR: Essayer de trouver le contrôleur du joueur pour utiliser son clavier virtuel)
                var controller = Program.WiiMoteManager?.Controllers?.FirstOrDefault(c => c.PlayerIndex == playerIndex);
                if (controller != null && controller.VirtualJoy != null)
                {
                    controller.VirtualJoy.SendKeyEvent(actualKey, pressed);
                }
                else
                {
                    // Fallback to shared SendInput keyboard if controller not found
                    // (EN/FR: Repli sur clavier SendInput partagé si contrôleur non trouvé)
                    if (_sharedKeyboard == null)
                    {
                        _sharedKeyboard = new VirtualSendInputKeyboard();
                    }
                    _sharedKeyboard.SendKeyEvent(actualKey, pressed);
                }

                SimpleLogger.Instance.Debug($"[Hotkey] P{playerIndex} SendKeyPress: {key} -> VK {(int)actualKey:X2} ({(pressed ? "DOWN" : "UP")})");
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"[Hotkey] Failed to send key {key}: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear active state (EN/FR: Effacer état actif)
        /// </summary>
        public static void ClearActiveState()
        {
            _activeModifiers.Clear();
            _activeTriggers.Clear();
            _lastButtonPressTime.Clear();
            _lastButtonPressed.Clear();
        }

        public static void ClearAll()
        {
            foreach (var profile in _playerHotkeys.Values)
            {
                profile.ClearAll();
            }
            ClearActiveState();
            SimpleLogger.Instance.Info("[HotkeyManager] Cleared all hotkeys");
        }
    }
}
