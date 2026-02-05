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
            "Home", "Minus", "Plus", "One", "Two", "A", "B", "Up", "Down", "Left", "Right" 
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
            if (Options.Instance.UseSharedHotkeys)
            {
                // If shared, always use Player 1's profile (EN/FR: Si partagé, toujours utiliser profil P1)
                if (!_playerHotkeys.ContainsKey(1))
                {
                    _playerHotkeys[1] = new HotkeyProfile(1);
                }
                return _playerHotkeys[1];
            }

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
            _playerHotkeys[playerIndex] = profile;
            
            // Auto-save to Options when profile is updated (EN/FR: Auto-sauvegarde quand profil modifié)
            SaveToOptions();
        }

        /// <summary>
        /// Handle button press event (EN/FR: Gérer événement pression bouton)
        /// </summary>
        public static void OnButtonPressed(int playerIndex, string button)
        {
            // CRITICAL: Hotkeys only active when overlay is CLOSED
            if (IsOverlayOpen != null && IsOverlayOpen())
                return;

            var profile = GetProfile(playerIndex);
            
            // 1. Modifier Check: If button is a configured modifier, track it
            // (EN/FR: 1. Vérification Modificateur : si bouton est modificateur configuré, le suivre)
            bool isConfiguredModifier = AllowedModifiers.Contains(button) && 
                                        profile.Hotkeys.Any(h => string.Equals(h.ModifierButton, button, StringComparison.OrdinalIgnoreCase));

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

                    if (profile.HasHotkey(button, HotkeyPressType.Short, modifier) || 
                        profile.HasHotkey(button, HotkeyPressType.Long, modifier))
                    {
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
                    HotkeyPressType pressType = isLongPress ? HotkeyPressType.Long : HotkeyPressType.Short;
                    
                    var profile = GetProfile(playerIndex);
                    
                    // Find which modifier triggered this
                    if (_activeModifiers.ContainsKey(playerIndex))
                    {
                        foreach (var mod in _activeModifiers[playerIndex].Keys)
                        {
                            var hotkey = profile.GetHotkey(button, pressType, mod);
                            
                            // Fallback: If Long press detected but no Long hotkey defined, try Short
                            // (EN/FR: Fallback : Si appui long détecté mais pas de hotkey long, essayer court)
                            if (hotkey == null && pressType == HotkeyPressType.Long)
                            {
                                hotkey = profile.GetHotkey(button, HotkeyPressType.Short, mod);
                            }

                            if (hotkey != null)
                            {
                                SimpleLogger.Instance.Info(string.Format("[Hotkey] P{0} Executing {1}", playerIndex, hotkey.GetDisplayName()));
                                ExecuteHotkey(hotkey);
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
        /// Execute a hotkey by sending keyboard combination
        /// (EN/FR: Exécuter hotkey en envoyant combinaison clavier)
        /// </summary>
        private static void ExecuteHotkey(Hotkey hotkey)
        {
            if (hotkey == null || hotkey.KeyCombination == null || hotkey.KeyCombination.Count == 0)
            {
                SimpleLogger.Instance.Warning("[Hotkey] No keys to send");
                return;
            }

            try
            {
                // Separate modifier keys and regular keys
                // (EN/FR: Séparer touches modificatrices et touches régulières)
                List<Keys> modifiers = new List<Keys>();
                List<Keys> regularKeys = new List<Keys>();

                foreach (Keys key in hotkey.KeyCombination)
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
                    SendKeyPress(modifier, true);
                    System.Threading.Thread.Sleep(10); // Small delay for stability
                }

                // Press regular keys (EN/FR: Presser touches régulières)
                foreach (Keys key in regularKeys)
                {
                    SendKeyPress(key, true);
                    System.Threading.Thread.Sleep(10);
                    SendKeyPress(key, false); // Release immediately
                }

                // Release modifiers in reverse order (EN/FR: Relâcher modificateurs en ordre inverse)
                for (int i = modifiers.Count - 1; i >= 0; i--)
                {
                    SendKeyPress(modifiers[i], false);
                    System.Threading.Thread.Sleep(10);
                }

                SimpleLogger.Instance.Info(string.Format("[Hotkey] Sent keys: {0}", string.Join("+", hotkey.KeyCombination.Select(k => k.ToString()).ToArray())));
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
            // 1. Check Modifiers (Always suppress active modifiers)
            if (_activeModifiers.ContainsKey(playerIndex) && 
                _activeModifiers[playerIndex].ContainsKey(button))
            {
                return true;
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
        private static void SendKeyPress(Keys key, bool pressed)
        {
            // Use VirtualSendInputKeyboard to send keys
            // (EN/FR: Utiliser VirtualSendInputKeyboard pour envoyer touches)
            try
            {
                // Convert modifier flags to actual VK codes (EN/FR: Convertir flags en codes VK)
                Keys actualKey = ConvertToVirtualKey(key);
                
                // Use shared keyboard instance (EN/FR: Utiliser instance partagée)
                if (_sharedKeyboard == null)
                {
                    _sharedKeyboard = new VirtualSendInputKeyboard();
                }
                
                _sharedKeyboard.SendKeyEvent(actualKey, pressed);
                SimpleLogger.Instance.Debug($"[Hotkey] SendKeyPress: {key} -> VK {(int)actualKey:X2} ({(pressed ? "DOWN" : "UP")})");
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"[Hotkey] Failed to send key {key}: {ex.Message}");
            }
        }

        /// <summary>
        /// Clear all hotkeys for all players (EN/FR: Effacer toutes les hotkeys)
        /// </summary>
        /// <summary>
        /// Force clear active modifier/trigger states (EN/FR: Forcer l'effacement des états modificateurs/déclencheurs)
        /// Used when switching profiles or modes
        /// </summary>
        public static void ClearActiveState()
        {
            _activeModifiers.Clear();
            _activeTriggers.Clear();
            _lastButtonPressTime.Clear();
            _lastButtonPressed.Clear();
            
            // Release held keys just in case? Usually risky.
            // Better to just clear tracking.
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
