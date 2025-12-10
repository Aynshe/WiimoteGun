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

        // Home button state tracking (EN/FR: Suivi état bouton Home)
        private static Dictionary<int, bool> _homeButtonPressed = new Dictionary<int, bool>();
        private static Dictionary<int, DateTime> _lastButtonPressTime = new Dictionary<int, DateTime>();
        private static Dictionary<int, string> _lastButtonPressed = new Dictionary<int, string>();

        // Constants (EN/FR: Constantes)
        private const int LONG_PRESS_THRESHOLD_MS = 500; // Seuil pression longue
        private const string HOME_BUTTON = "Home";
        private const string PLUS_BUTTON = "Plus"; // Reserved for overlay toggle

        // Overlay state delegate (EN/FR: Délégué état overlay)
        public static Func<bool> IsOverlayOpen { get; set; }

        /// <summary>
        /// Initialize the hotkey manager (EN/FR: Initialiser gestionnaire)
        /// </summary>
        public static void Initialize()
        {
            _playerHotkeys.Clear();
            _homeButtonPressed.Clear();
            _lastButtonPressTime.Clear();
            _lastButtonPressed.Clear();

            // Initialize profiles for 4 players (EN/FR: Initialiser profils pour 4 joueurs)
            for (int i = 1; i <= 4; i++)
            {
                _playerHotkeys[i] = new HotkeyProfile(i);
                _homeButtonPressed[i] = false;
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
                    SimpleLogger.Instance.Info($"[HotkeyManager] Loaded {options.HotkeyProfileP1.Count} hotkeys for P1");
                }
                if (options.HotkeyProfileP2 != null && options.HotkeyProfileP2.Hotkeys != null)
                {
                    _playerHotkeys[2] = options.HotkeyProfileP2;
                    _playerHotkeys[2].PlayerIndex = 2;
                    SimpleLogger.Instance.Info($"[HotkeyManager] Loaded {options.HotkeyProfileP2.Count} hotkeys for P2");
                }
                if (options.HotkeyProfileP3 != null && options.HotkeyProfileP3.Hotkeys != null)
                {
                    _playerHotkeys[3] = options.HotkeyProfileP3;
                    _playerHotkeys[3].PlayerIndex = 3;
                    SimpleLogger.Instance.Info($"[HotkeyManager] Loaded {options.HotkeyProfileP3.Count} hotkeys for P3");
                }
                if (options.HotkeyProfileP4 != null && options.HotkeyProfileP4.Hotkeys != null)
                {
                    _playerHotkeys[4] = options.HotkeyProfileP4;
                    _playerHotkeys[4].PlayerIndex = 4;
                    SimpleLogger.Instance.Info($"[HotkeyManager] Loaded {options.HotkeyProfileP4.Count} hotkeys for P4");
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Warning($"[HotkeyManager] Failed to load hotkeys from Options: {ex.Message}");
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
                SimpleLogger.Instance.Error($"[HotkeyManager] Failed to save hotkeys: {ex.Message}");
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
            // (EN/FR: CRITIQUE: Hotkeys actives seulement si overlay FERMÉ)
            if (IsOverlayOpen != null && IsOverlayOpen())
            {
                return; // Ignore all hotkey processing if overlay open
            }

            if (button == HOME_BUTTON)
            {
                // Home button pressed, start tracking (EN/FR: Home pressé, commencer suivi)
                _homeButtonPressed[playerIndex] = true;
                SimpleLogger.Instance.Debug($"[Hotkey] P{playerIndex} Home pressed");
                return;
            }

            // Check if Home is currently held down (EN/FR: Vérifier si Home maintenu)
            if (_homeButtonPressed.ContainsKey(playerIndex) && _homeButtonPressed[playerIndex])
            {
                // Home + Plus = Reserved for overlay toggle (EN/FR: Réservé pour overlay)
                if (button == PLUS_BUTTON)
                {
                    SimpleLogger.Instance.Debug($"[Hotkey] P{playerIndex} Home+Plus (reserved for overlay)");
                    return; // Let overlay system handle this
                }

                // Record button press time for short/long detection
                // (EN/FR: Enregistrer temps pression pour détection court/long)
                _lastButtonPressed[playerIndex] = button;
                _lastButtonPressTime[playerIndex] = DateTime.Now;
                
                SimpleLogger.Instance.Debug($"[Hotkey] P{playerIndex} Home+{button} detected, waiting for release...");
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

            if (button == HOME_BUTTON)
            {
                // Home button released, reset state (EN/FR: Home relâché, réinitialiser)
                _homeButtonPressed[playerIndex] = false;
                _lastButtonPressed.Remove(playerIndex);
                _lastButtonPressTime.Remove(playerIndex);
                SimpleLogger.Instance.Debug($"[Hotkey] P{playerIndex} Home released");
                return;
            }

            // Check if this is a hotkey button release (EN/FR: Vérifier si relâchement hotkey)
            if (_homeButtonPressed.ContainsKey(playerIndex) && _homeButtonPressed[playerIndex] &&
                _lastButtonPressed.ContainsKey(playerIndex) && _lastButtonPressed[playerIndex] == button &&
                _lastButtonPressTime.ContainsKey(playerIndex))
            {
                // Calculate press duration (EN/FR: Calculer durée pression)
                TimeSpan pressDuration = DateTime.Now - _lastButtonPressTime[playerIndex];
                bool isLongPress = pressDuration.TotalMilliseconds >= LONG_PRESS_THRESHOLD_MS;
                HotkeyPressType pressType = isLongPress ? HotkeyPressType.Long : HotkeyPressType.Short;

                SimpleLogger.Instance.Info($"[Hotkey] P{playerIndex} Home+{button} released after {pressDuration.TotalMilliseconds:F0}ms ({pressType})");

                // Find and execute hotkey (EN/FR: Trouver et exécuter hotkey)
                var profile = GetProfile(playerIndex);
                var hotkey = profile.GetHotkey(button, pressType);

                if (hotkey != null)
                {
                    SimpleLogger.Instance.Info($"[Hotkey] Executing: {hotkey.GetDisplayName()}");
                    ExecuteHotkey(hotkey);
                }
                else
                {
                    SimpleLogger.Instance.Debug($"[Hotkey] No hotkey defined for Home+{button} ({pressType})");
                }

                // Clear tracking (EN/FR: Effacer suivi)
                _lastButtonPressed.Remove(playerIndex);
                _lastButtonPressTime.Remove(playerIndex);
            }
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

                SimpleLogger.Instance.Info($"[Hotkey] Sent keys: {string.Join("+", hotkey.KeyCombination)}");
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"[Hotkey] Failed to execute hotkey: {ex.Message}");
            }
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
        public static void ClearAll()
        {
            foreach (var profile in _playerHotkeys.Values)
            {
                profile.ClearAll();
            }
            SimpleLogger.Instance.Info("[HotkeyManager] Cleared all hotkeys");
        }
    }
}
