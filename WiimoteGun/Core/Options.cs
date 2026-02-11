using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;
using System.Windows.Forms;
using System;

namespace WiimoteGun
{
    public enum SpecialAction
    {
        None,
        LeftMouse,
        RightMouse,
        MiddleMouse
    }

    public class ButtonAction
    {
        [DefaultValue(SpecialAction.None)]
        public SpecialAction Special { get; set; }
        [DefaultValue(Keys.None)]
        public Keys Key { get; set; }

        public ButtonAction()
        {
            Special = SpecialAction.None;
            Key = Keys.None;
        }

        public ButtonAction(SpecialAction special)
        {
            Special = special;
            Key = Keys.None;
        }

        public ButtonAction(Keys key)
        {
            Special = SpecialAction.None;
            Key = key;
        }

        public override string ToString()
        {
            if (Special != SpecialAction.None)
                return Special.ToString();

            if (Key != Keys.None)
                return GetAzertyKeyName(Key);

            return "None";
        }

        /// <summary>
        /// Get AZERTY-friendly display name for a key
        /// (EN/FR: Obtenir nom d'affichage AZERTY pour une touche)
        /// </summary>
        private static string GetAzertyKeyName(Keys key)
        {
            // AZERTY-specific key names showing both characters (normal/Shift)
            // (EN/FR: Noms de touches spécifiques AZERTY montrant les deux caractères)
            switch (key)
            {
                // Number row with AZERTY characters (EN/FR: Rangée chiffres avec caractères AZERTY)
                case Keys.D1: return "1 (&)";
                case Keys.D2: return "2 (é)";
                case Keys.D3: return "3 (\")";
                case Keys.D4: return "4 (')";
                case Keys.D5: return "5 (()";
                case Keys.D6: return "6 (-)";
                case Keys.D7: return "7 (è)";
                case Keys.D8: return "8 (_)";
                case Keys.D9: return "9 (ç)";
                case Keys.D0: return "0 (à)";
                
                // OEM keys with AZERTY mapping (EN/FR: Touches OEM avec mapping AZERTY)
                case Keys.Oemtilde: return "² (Tilde)";
                case Keys.OemMinus: return ") (°)"; // Right of 0
                case Keys.Oemplus: return "= (+)";  // Right of )
                case Keys.OemOpenBrackets: return "^ (¨)"; // Dead key
                case Keys.OemCloseBrackets: return "$ (£)";
                case Keys.OemPipe: return "* (µ)"; // Backslash position
                case Keys.OemSemicolon: return "ù (%)"; // Oem1 = OemSemicolon
                case Keys.OemQuotes: return "' (²)"; // Oem7 = OemQuotes
                case Keys.Oemcomma: return ", (?)";
                case Keys.OemPeriod: return "; (.)";
                case Keys.OemQuestion: return ": (/)"; // Oem2 = OemQuestion
                case Keys.Oem102: return "< (>)"; // Extra key left of Z on AZERTY
                
                // Modified display for common keys (EN/FR: Affichage modifié pour touches courantes)
                case Keys.Return: return "Enter ↵";
                case Keys.Space: return "Space ⎵";
                case Keys.Back: return "Backspace ⌫";
                case Keys.Tab: return "Tab ⇥";
                case Keys.Escape: return "Escape";
                case Keys.Delete: return "Delete";
                case Keys.Insert: return "Insert";
                case Keys.Home: return "Home";
                case Keys.End: return "End";
                case Keys.PageUp: return "Page Up";
                case Keys.PageDown: return "Page Down";
                case Keys.Up: return "↑ Up";
                case Keys.Down: return "↓ Down";
                case Keys.Left: return "← Left";
                case Keys.Right: return "→ Right";
                case Keys.CapsLock: return "Caps Lock";
                case Keys.NumLock: return "Num Lock";
                case Keys.Scroll: return "Scroll Lock";
                case Keys.PrintScreen: return "Print Screen";
                case Keys.Pause: return "Pause";
                
                // Modifiers (EN/FR: Modificateurs)
                case Keys.LShiftKey:
                case Keys.ShiftKey: return "Shift";
                case Keys.RShiftKey: return "Right Shift";
                case Keys.LControlKey:
                case Keys.ControlKey: return "Ctrl";
                case Keys.RControlKey: return "Right Ctrl";
                case Keys.LMenu:
                case Keys.Menu: return "Alt";
                case Keys.RMenu: return "Alt Gr";
                case Keys.LWin: return "Win";
                case Keys.RWin: return "Right Win";
                
                // Numpad (EN/FR: Pavé numérique)
                case Keys.NumPad0: return "Num 0";
                case Keys.NumPad1: return "Num 1";
                case Keys.NumPad2: return "Num 2";
                case Keys.NumPad3: return "Num 3";
                case Keys.NumPad4: return "Num 4";
                case Keys.NumPad5: return "Num 5";
                case Keys.NumPad6: return "Num 6";
                case Keys.NumPad7: return "Num 7";
                case Keys.NumPad8: return "Num 8";
                case Keys.NumPad9: return "Num 9";
                case Keys.Multiply: return "Num *";
                case Keys.Add: return "Num +";
                case Keys.Subtract: return "Num -";
                case Keys.Divide: return "Num /";
                case Keys.Decimal: return "Num .";
                
                // Function keys stay the same (EN/FR: Touches fonction restent identiques)
                default:
                    return key.ToString();
            }
        }

        public override bool Equals(object obj)
        {
            var other = obj as ButtonAction;
            if (other == null)
                return false;

            return Special == other.Special && Key == other.Key;
        }

        public override int GetHashCode()
        {
            return Special.GetHashCode() ^ Key.GetHashCode();
        }
    }


    // LED layout types for different lightgun configurations (EN/FR: Types de configuration LED pour différents lightguns)
    public enum LEDLayoutType
    {
        WiimoteBar = 0,      // Horizontal LED bar (2 LEDs) - Standard Wiimote sensor bar
        Gun4IRDiamond = 1,   // Diamond/Rhombus pattern (4 LEDs) - Gun4IR configuration
        TwoWiimoteBar = 2,   // 2 Wiimote Sensor Bars (Top/Bottom) - Dual sensor bar configuration
        FourCorners = 3      // 4 LEDs at screen corners - Individual corner LEDs
    }

    // Mouse implementation mode (EN/FR: Mode d'implémentation souris)
    public enum MouseMode
    {
        SendInput = 0,   // Legacy single-player mode using SendInput (EN/FR: Mode legacy mono-joueur utilisant SendInput)
        RawInput = 1     // Multi-player mode using VMulti driver (EN/FR: Mode multi-joueur utilisant pilote VMulti)
    }

    public class WiimoteCalibration
    {
        public string UniqueId { get; set; }
        public float PitchOffset { get; set; }
        public float RollOffset { get; set; }
        public float YawOffset { get; set; }
    }

    public class Options
    {
        private Options(bool assignDefaults)
        {
            if (assignDefaults)
            {
                MonitorId = 0;
                FirstRun = true; // Default to true so Setup Wizard shows up
                CalibrationTop = -1;
                CalibrationLeft = -1;
                CalibrationCenterX = -1;
                CalibrationCenterY = -1;
                IRSensitivity = 5;
                DetectDolphinbar = true;
                DetectBlueTooth = true;
                ShowNotifications = true;

                WiiA = new ButtonAction(SpecialAction.RightMouse);
                WiiB = new ButtonAction(SpecialAction.LeftMouse);
                WiiUp = new ButtonAction(Keys.Up);
                WiiDown = new ButtonAction(Keys.Down);
                WiiLeft = new ButtonAction(Keys.Left);
                WiiRight = new ButtonAction(Keys.Right);
                WiiOne = new ButtonAction(SpecialAction.MiddleMouse);
                WiiTwo = new ButtonAction(Keys.Z);
                WiiPlus = new ButtonAction(Keys.Return);
                WiiMinus = new ButtonAction(Keys.ControlKey);
                NunC = new ButtonAction(SpecialAction.RightMouse);
                NunZ = new ButtonAction(SpecialAction.LeftMouse);
                NunUp = new ButtonAction(Keys.Up);
                NunDown = new ButtonAction(Keys.Down);
                NunLeft = new ButtonAction(Keys.Left);
                NunRight = new ButtonAction(Keys.Right);

                PreferredMacP1 = "";
                PreferredMacP2 = "";
                PreferredMacP3 = "";
                PreferredMacP4 = "";

                Enable4Players = true;
                FirstRun = true;
                ShowSetupWizard = true;
                UseSharedKeyboard = true;
                
                // Keyboard debugging defaults (EN/FR: Valeurs par défaut pour le débogage clavier)
                ForceKeyboardDeviceIdP1 = 0;
                ForceKeyboardDeviceIdP2 = 0;
                ForceKeyboardDeviceIdP3 = 0;
                ForceKeyboardDeviceIdP4 = 0;
                KeyboardDebugMode = false;

                // Emulator restart options (EN/FR: Options redémarrage émulateur)
                RestartOnDolphin = true;
                RestartOnCemu = true;

                // Initialize player mappings (EN/FR: Initialiser les mappings par joueur)
                P1Mappings = new PlayerMappings();
                P2Mappings = new PlayerMappings();
                P3Mappings = new PlayerMappings();
                P3Mappings = new PlayerMappings();
                P4Mappings = new PlayerMappings();
                
                // Gesture defaults (EN/FR: Valeurs par défaut pour les gestes)
                EnableOffScreenReload = false;
                OffScreenReloadAuto = false;
                EnableDevGestures = false; // Hidden: Must be manually enabled in XML (EN/FR: Caché : Doit être activé manuellement)
                EnableShakeReload = false;
                ShakeSensitivity = 1; // 0=Low, 1=Medium, 2=High
                ShakeFromNunchuk = false; // false=Wiimote, true=Nunchuk
                EnableGrenadeGesture = false;
                GrenadeFromNunchuk = false;

                PermissiveWiimoteBarCalibration = false;

                UseDynamicPerspective_P1 = false;
                UseDynamicPerspective_P2 = false;
                UseDynamicPerspective_P3 = false;
                UseDynamicPerspective_P4 = false;

                DynamicPerspectiveOffsetY_P1 = 0;
                DynamicPerspectiveOffsetY_P2 = 0;
                DynamicPerspectiveOffsetY_P3 = 0;
                DynamicPerspectiveOffsetY_P4 = 0;

                DynamicPerspectiveOffsetX_P1 = 0;
                DynamicPerspectiveOffsetX_P2 = 0;
                DynamicPerspectiveOffsetX_P3 = 0;
                DynamicPerspectiveOffsetX_P4 = 0;

                EnableWeaponRumble_P1 = true;
                EnableWeaponRumble_P2 = true;
                EnableWeaponRumble_P3 = true;
                EnableWeaponRumble_P4 = true;

                AllowContinuousRumble_P1 = true;
                AllowContinuousRumble_P2 = true;
                AllowContinuousRumble_P3 = true;
                AllowContinuousRumble_P4 = true;

                RumbleIntensity_P1 = 50;
                RumbleIntensity_P2 = 50;
                RumbleIntensity_P3 = 50;
                RumbleIntensity_P4 = 50;

                RumbleDurationMs_P1 = 60;
                RumbleDurationMs_P2 = 60;
                RumbleDurationMs_P3 = 60;
                RumbleDurationMs_P4 = 60;

                RumbleRepetitionMs_P1 = 150;
                RumbleRepetitionMs_P2 = 150;
                RumbleRepetitionMs_P3 = 150;
                RumbleRepetitionMs_P4 = 150;

                // Locked player slots (EN/FR: Slots joueur verrouillés)
                LockedSlot_P1 = false;
                LockedSlot_P2 = false;
                LockedSlot_P3 = false;
                LockedSlot_P4 = false;

                // IR Tracking Optimizations (EN/FR: Optimisations tracking IR)
                EnableIRSmoothing = false;
                IRSmoothingStrength = 5;
                UseHighPerfTimers = false;
                EnableHomographyCache = false;

                DefaultMouseMode = MouseMode.RawInput;
                GyroSensitivityX = 1.0f;
                GyroSensitivityY = 1.0f;
                GyroSmoothingFrames = 3;
                AutoLockVMultiDevices = true;
                PersistentGamePads = false;
                EnableGamePadSwapMode = false;

                GamePadMappingsP1 = new GamePadMappings();
                GamePadMappingsP2 = new GamePadMappings();
                GamePadMappingsP3 = new GamePadMappings();
                GamePadMappingsP4 = new GamePadMappings();

                LoggingLevel = LogLevel.INFO;
            }
        }

        private Options() : this(true)
        {
        }
        
        public float GyroDeadzone;

        public System.Collections.Generic.List<WiimoteCalibration> SavedCalibrations = new System.Collections.Generic.List<WiimoteCalibration>();

        private static Options _instance;

        public static Options Load()
        {
            string path = GetSettingsFilename();

            if (File.Exists(path))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(Options));
                    using (FileStream stream = File.OpenRead(path))
                    {
                        var options = serializer.Deserialize(stream) as Options;
                        
                        // Apply log level immediately (EN/FR: Appliquer niveau de log immédiatement)
                        SimpleLogger.Instance.Threshold = options.LoggingLevel;

                        // Migrate legacy calibration to per-player if needed (EN/FR: Migrer calibration héritée vers par-joueur si besoin)
                        if (options.CalibrationTop != -1 && options.CalibrationTopP1 == -1)
                        {
                            // Copy legacy calibration to all players (6-parameter format)
                            // (EN/FR: Copier calibration héritée vers tous les joueurs - format 6 paramètres)
                            for (int i = 1; i <= 4; i++)
                            {
                                options.SetCalibrationForPlayer(i, 
                                    options.CalibrationLeft,     // topLeftX
                                    options.CalibrationTop,      // topLeftY
                                    -1, -1, -1, -1, -1, -1);    // TR, BR, BL = -1 (non calibré)
                            }
                            SimpleLogger.Instance.Info("Migrated legacy calibration to per-player calibration");
                            options.Save(); // Save migration
                        }
                        
                        return options;
                    }
                }
                catch (Exception ex)
                {
                    SimpleLogger.Instance.Error("Failed to load settings: " + ex.ToString());
                }
            }

            return new Options(true);
        }

        // Get calibration for specific player - 4-POINT FORMAT (EN/FR: Obtenir calibration pour joueur spécifique - FORMAT 4-POINTS)
        // Returns: float[8] = (TL.X, TL.Y, TR.X, TR.Y, BR.X, BR.Y, BL.X, BL.Y)
        // AUTO-MIGRATES from old 3-point format (no BL) or 2-point format (centerX/centerY)
        // (EN/FR: MIGRATION AUTO depuis ancien format 3-points ou 2-points)
        public float[] GetCalibrationForPlayer(int playerIndex)
        {
            // Get stored values based on player (EN/FR: Obtenir valeurs stockées selon joueur)
            float topLeftY, topLeftX, centerX, centerY, topRightX, topRightY, bottomRightX, bottomRightY, bottomLeftX, bottomLeftY;
            
            switch (playerIndex)
            {
                case 1:
                    topLeftY = CalibrationTopP1;
                    topLeftX = CalibrationLeftP1;
                    centerX = CalibrationCenterXP1;
                    centerY = CalibrationCenterYP1;
                    topRightX = CalibrationTopRightXP1;
                    topRightY = CalibrationTopRightYP1;
                    bottomRightX = CalibrationBottomRightXP1;
                    bottomRightY = CalibrationBottomRightYP1;
                    bottomLeftX = CalibrationBottomLeftXP1;
                    bottomLeftY = CalibrationBottomLeftYP1;
                    break;
                case 2:
                    topLeftY = CalibrationTopP2;
                    topLeftX = CalibrationLeftP2;
                    centerX = CalibrationCenterXP2;
                    centerY = CalibrationCenterYP2;
                    topRightX = CalibrationTopRightXP2;
                    topRightY = CalibrationTopRightYP2;
                    bottomRightX = CalibrationBottomRightXP2;
                    bottomRightY = CalibrationBottomRightYP2;
                    bottomLeftX = CalibrationBottomLeftXP2;
                    bottomLeftY = CalibrationBottomLeftYP2;
                    break;
                case 3:
                    topLeftY = CalibrationTopP3;
                    topLeftX = CalibrationLeftP3;
                    centerX = CalibrationCenterXP3;
                    centerY = CalibrationCenterYP3;
                    topRightX = CalibrationTopRightXP3;
                    topRightY = CalibrationTopRightYP3;
                    bottomRightX = CalibrationBottomRightXP3;
                    bottomRightY = CalibrationBottomRightYP3;
                    bottomLeftX = CalibrationBottomLeftXP3;
                    bottomLeftY = CalibrationBottomLeftYP3;
                    break;
                case 4:
                    topLeftY = CalibrationTopP4;
                    topLeftX = CalibrationLeftP4;
                    centerX = CalibrationCenterXP4;
                    centerY = CalibrationCenterYP4;
                    topRightX = CalibrationTopRightXP4;
                    topRightY = CalibrationTopRightYP4;
                    bottomRightX = CalibrationBottomRightXP4;
                    bottomRightY = CalibrationBottomRightYP4;
                    bottomLeftX = CalibrationBottomLeftXP4;
                    bottomLeftY = CalibrationBottomLeftYP4;
                    break;
                default:
                    return new float[] { -1, -1, - 1, -1, -1, -1, -1, -1 };
            }

            // AUTO-MIGRATION 1: 2-point → 3-point (centerX/centerY → topRight/bottomRight)
            // (EN/FR: MIGRATION AUTO 1: 2-points → 3-points)
            if (topRightX == -1 && centerX != -1 && centerY != -1 && topLeftX != -1 && topLeftY != -1)
            {
                float deltaX = centerX - topLeftX;
                float deltaY = centerY - topLeftY;
                topRightX = centerX + deltaX;
                topRightY = topLeftY;
                bottomRightX = centerX + deltaX;
                bottomRightY = centerY + deltaY;
                SimpleLogger.Instance.Info(string.Format("Migrated Player {0} from old 2-point to 3-point calibration format", playerIndex));
            }

            // AUTO-MIGRATION 2: 3-point → 4-point (extrapolate bottomLeft)
            // (EN/FR: MIGRATION AUTO 2: 3-points → 4-points - extrapoler bottomLeft)
            if (bottomLeftX == -1 && topLeftX != -1 && bottomRightY != -1)
            {
                // Extrapolate BL from TL (same X) and BR (same Y)
                bottomLeftX = topLeftX;
                bottomLeftY = bottomRightY;
                SimpleLogger.Instance.Info(string.Format("Migrated Player {0} from 3-point to 4-point calibration (extrapolated BL)", playerIndex));
            }

            // Return 8 values: TL.X, TL.Y, TR.X, TR.Y, BR.X, BR.Y, BL.X, BL.Y
            return new float[] { topLeftX, topLeftY, topRightX, topRightY, bottomRightX, bottomRightY, bottomLeftX, bottomLeftY };
        }

        // Set calibration for specific player - 4-POINT FORMAT (EN/FR: Définir calibration pour joueur spécifique - FORMAT 4-POINTS)
        // Parameters: TL.X, TL.Y, TR.X, TR.Y, BR.X, BR.Y, BL.X, BL.Y
        public void SetCalibrationForPlayer(int playerIndex, float topLeftX, float topLeftY, float topRightX, float topRightY, float bottomRightX, float bottomRightY, float bottomLeftX, float bottomLeftY)
        {
            switch (playerIndex)
            {
                case 1:
                    CalibrationLeftP1 = topLeftX;
                    CalibrationTopP1 = topLeftY;
                    CalibrationTopRightXP1 = topRightX;
                    CalibrationTopRightYP1 = topRightY;
                    CalibrationBottomRightXP1 = bottomRightX;
                    CalibrationBottomRightYP1 = bottomRightY;
                    CalibrationBottomLeftXP1 = bottomLeftX;
                    CalibrationBottomLeftYP1 = bottomLeftY;
                    break;
                case 2:
                    CalibrationLeftP2 = topLeftX;
                    CalibrationTopP2 = topLeftY;
                    CalibrationTopRightXP2 = topRightX;
                    CalibrationTopRightYP2 = topRightY;
                    CalibrationBottomRightXP2 = bottomRightX;
                    CalibrationBottomRightYP2 = bottomRightY;
                    CalibrationBottomLeftXP2 = bottomLeftX;
                    CalibrationBottomLeftYP2 = bottomLeftY;
                    break;
                case 3:
                    CalibrationLeftP3 = topLeftX;
                    CalibrationTopP3 = topLeftY;
                    CalibrationTopRightXP3 = topRightX;
                    CalibrationTopRightYP3 = topRightY;
                    CalibrationBottomRightXP3 = bottomRightX;
                    CalibrationBottomRightYP3 = bottomRightY;
                    CalibrationBottomLeftXP3 = bottomLeftX;
                    CalibrationBottomLeftYP3 = bottomLeftY;
                    break;
                case 4:
                    CalibrationLeftP4 = topLeftX;
                    CalibrationTopP4 = topLeftY;
                    CalibrationTopRightXP4 = topRightX;
                    CalibrationTopRightYP4 = topRightY;
                    CalibrationBottomRightXP4 = bottomRightX;
                    CalibrationBottomRightYP4 = bottomRightY;
                    CalibrationBottomLeftXP4 = bottomLeftX;
                    CalibrationBottomLeftYP4 = bottomLeftY;
                    break;
            }
            Save();
        }
        public static Options Instance
        {
            get
            {
                if (_instance == null)
                {
                    try
                    {
                        var settingsFile = GetSettingsFilename();
                        if (File.Exists(settingsFile))
                        {
                            _instance = settingsFile.FromXml<Options>();

                            // Merge with default values for missing properties
                            var defaultInstance = new Options(true);
                            foreach (var prop in typeof(Options).GetProperties())
                            {
                                if (prop.GetValue(_instance) == null)
                                {
                                    prop.SetValue(_instance, prop.GetValue(defaultInstance));
                                }
                            }
                        }
                        else
                        {
                            _instance = new Options();
                        }
                    }
                    catch { _instance = new Options(); }

                    // Migrate old mappings to P1 if PlayerMappings are null (EN/FR: Migrer anciens mappings vers P1)
                    if (_instance.P1Mappings == null)
                    {
                        _instance.P1Mappings = new PlayerMappings(); // Has defaults already
                        
                        // Only copy old mappings if they are valid (not None/None)
                        if (_instance.WiiA != null && (_instance.WiiA.Special != SpecialAction.None || _instance.WiiA.Key != Keys.None)) 
                            _instance.P1Mappings.WiiA = _instance.WiiA;
                        if (_instance.WiiB != null && (_instance.WiiB.Special != SpecialAction.None || _instance.WiiB.Key != Keys.None)) 
                            _instance.P1Mappings.WiiB = _instance.WiiB;
                        if (_instance.WiiUp != null && (_instance.WiiUp.Special != SpecialAction.None || _instance.WiiUp.Key != Keys.None)) 
                            _instance.P1Mappings.WiiUp = _instance.WiiUp;
                        if (_instance.WiiDown != null && (_instance.WiiDown.Special != SpecialAction.None || _instance.WiiDown.Key != Keys.None)) 
                            _instance.P1Mappings.WiiDown = _instance.WiiDown;
                        if (_instance.WiiLeft != null && (_instance.WiiLeft.Special != SpecialAction.None || _instance.WiiLeft.Key != Keys.None)) 
                            _instance.P1Mappings.WiiLeft = _instance.WiiLeft;
                        if (_instance.WiiRight != null && (_instance.WiiRight.Special != SpecialAction.None || _instance.WiiRight.Key != Keys.None)) 
                            _instance.P1Mappings.WiiRight = _instance.WiiRight;
                        if (_instance.WiiOne != null && (_instance.WiiOne.Special != SpecialAction.None || _instance.WiiOne.Key != Keys.None)) 
                            _instance.P1Mappings.WiiOne = _instance.WiiOne;
                        if (_instance.WiiTwo != null && (_instance.WiiTwo.Special != SpecialAction.None || _instance.WiiTwo.Key != Keys.None)) 
                            _instance.P1Mappings.WiiTwo = _instance.WiiTwo;
                        if (_instance.WiiPlus != null && (_instance.WiiPlus.Special != SpecialAction.None || _instance.WiiPlus.Key != Keys.None)) 
                            _instance.P1Mappings.WiiPlus = _instance.WiiPlus;
                        if (_instance.WiiMinus != null && (_instance.WiiMinus.Special != SpecialAction.None || _instance.WiiMinus.Key != Keys.None)) 
                            _instance.P1Mappings.WiiMinus = _instance.WiiMinus;
                        if (_instance.NunC != null && (_instance.NunC.Special != SpecialAction.None || _instance.NunC.Key != Keys.None)) 
                            _instance.P1Mappings.NunC = _instance.NunC;
                        if (_instance.NunZ != null && (_instance.NunZ.Special != SpecialAction.None || _instance.NunZ.Key != Keys.None)) 
                            _instance.P1Mappings.NunZ = _instance.NunZ;
                        if (_instance.NunUp != null && (_instance.NunUp.Special != SpecialAction.None || _instance.NunUp.Key != Keys.None)) 
                            _instance.P1Mappings.NunUp = _instance.NunUp;
                        if (_instance.NunDown != null && (_instance.NunDown.Special != SpecialAction.None || _instance.NunDown.Key != Keys.None)) 
                            _instance.P1Mappings.NunDown = _instance.NunDown;
                        if (_instance.NunLeft != null && (_instance.NunLeft.Special != SpecialAction.None || _instance.NunLeft.Key != Keys.None)) 
                            _instance.P1Mappings.NunLeft = _instance.NunLeft;
                        if (_instance.NunRight != null && (_instance.NunRight.Special != SpecialAction.None || _instance.NunRight.Key != Keys.None)) 
                            _instance.P1Mappings.NunRight = _instance.NunRight;
                    }
                    if (_instance.P2Mappings == null) _instance.P2Mappings = new PlayerMappings();
                    if (_instance.P3Mappings == null) _instance.P3Mappings = new PlayerMappings();
                    if (_instance.P4Mappings == null) _instance.P4Mappings = new PlayerMappings();

                    // Apply log level immediately (EN/FR: Appliquer niveau de log immédiatement)
                    SimpleLogger.Instance.Threshold = _instance.LoggingLevel;
                }

                return _instance;
            }
        }

        public void Save()
        {
            string xml = this.ToXml();

            try { File.WriteAllText(GetSettingsFilename(), xml); }
            catch { }
        }

        public PlayerMappings GetMappingsForPlayer(int playerIndex)
        {
            switch (playerIndex)
            {
                case 1: return P1Mappings;
                case 2: return P2Mappings;
                case 3: return P3Mappings;
                case 4: return P4Mappings;
                default: return P1Mappings;
            }
        }


        public void ResetMappings()
        {
            var defaultOptions = new Options(true);
            WiiA = defaultOptions.WiiA;
            WiiB = defaultOptions.WiiB;
            WiiUp = defaultOptions.WiiUp;
            WiiDown = defaultOptions.WiiDown;
            WiiLeft = defaultOptions.WiiLeft;
            WiiRight = defaultOptions.WiiRight;
            WiiOne = defaultOptions.WiiOne;
            WiiTwo = defaultOptions.WiiTwo;
            WiiPlus = defaultOptions.WiiPlus;
            WiiMinus = defaultOptions.WiiMinus;
            NunC = defaultOptions.NunC;
            NunZ = defaultOptions.NunZ;
            NunUp = defaultOptions.NunUp;
            NunDown = defaultOptions.NunDown;
            NunLeft = defaultOptions.NunLeft;
            NunRight = defaultOptions.NunRight;

            // Also reset all player mappings (EN/FR: Réinitialiser aussi tous les mappings joueurs)
            ResetPlayerMappings(1);
            ResetPlayerMappings(2);
            ResetPlayerMappings(3);
            ResetPlayerMappings(4);
        }

        public void ResetPlayerMappings(int playerIndex)
        {
            var defaultMappings = new PlayerMappings();
            GetMappingsForPlayer(playerIndex).CopyFrom(defaultMappings);
        }

        private static string GetSettingsFilename()
        {
            return Path.Combine(Path.GetDirectoryName(typeof(VirtualSendKey).Assembly.Location), "settings.cfg");
        }

        [DefaultValue(LogLevel.INFO)]
        public LogLevel LoggingLevel { get; set; }

        [DefaultValue(0)]
        public int MonitorId { get; set; }
        
        [DefaultValue(-1)]
        public float CalibrationTop { get; set; }
        
        [DefaultValue(-1)]
        public float CalibrationLeft { get; set; }

        [DefaultValue(-1)]
        public float CalibrationCenterX { get; set; }

        [DefaultValue(-1)]
        public float CalibrationCenterY { get; set; }

        // Emulator restart options (EN/FR: Options redémarrage émulateur)
        [DefaultValue(true)]
        public bool RestartOnDolphin { get; set; }
        [DefaultValue(true)]
        public bool RestartOnCemu { get; set; }

        [DefaultValue("")]
        public string TwoWiimoteBarCalibrationP1 { get; set; }

        [DefaultValue("")]
        public string TwoWiimoteBarCalibrationP2 { get; set; }

        [DefaultValue("")]
        public string FourCornersCalibrationP1 { get; set; }

        [DefaultValue("")]
        public string FourCornersCalibrationP2 { get; set; }

        // PER-PLAYER CALIBRATION (EN/FR: Calibration par joueur)
        // Each player can have their own screen calibration for independent positioning
        // (EN/FR: Chaque joueur peut avoir sa propre calibration d'écran pour positionnement indépendant)
        
        // Player 1
        [DefaultValue(-1f)]
        public float CalibrationTopP1 { get; set; }
        [DefaultValue(-1f)]
        public float CalibrationLeftP1 { get; set; }
        [DefaultValue(-1f)]
        public float CalibrationCenterXP1 { get; set; }
        [DefaultValue(-1f)]
        public float CalibrationCenterYP1 { get; set; }

        // Player 2
        [DefaultValue(-1f)]
        public float CalibrationTopP2 { get; set; }
        [DefaultValue(-1f)]
        public float CalibrationLeftP2 { get; set; }
        [DefaultValue(-1f)]
        public float CalibrationCenterXP2 { get; set; }
        [DefaultValue(-1f)]
        public float CalibrationCenterYP2 { get; set; }

        // Player 3
        [DefaultValue(-1f)]
        public float CalibrationTopP3 { get; set; }
        [DefaultValue(-1f)]
        public float CalibrationLeftP3 { get; set; }
        [DefaultValue(-1f)]
        public float CalibrationCenterXP3 { get; set; }
        [DefaultValue(-1f)]
        public float CalibrationCenterYP3 { get; set; }

        // Player 4
        [DefaultValue(-1f)]
        public float CalibrationTopP4 { get; set; }
        [DefaultValue(-1f)]
        public float CalibrationLeftP4 { get; set; }
        [DefaultValue(-1f)]
        public float CalibrationCenterXP4 { get; set; }
        [DefaultValue(-1f)]
        public float CalibrationCenterYP4 { get; set; }

        // 3-POINT CALIBRATION EXTENSION (EN/FR: Extension calibration 3-points)
        // Adds TopRight and BottomRight for improved large screen coverage
        // Compatible with existing CenterX/CenterY (migration automatic)
        // (EN/FR: Ajoute TopRight et BottomRight pour meilleur couverture grands écrans)
        
        // Player 1 - 4-Point Calibration (TL, TR, BR, BL)
        [DefaultValue(-1f)]
        public float CalibrationTopRightXP1 { get; set; }
        [DefaultValue(-1f)]
        public float CalibrationTopRightYP1 { get; set; }
        [DefaultValue(-1f)]
        public float CalibrationBottomRightXP1 { get; set; }
        [DefaultValue(-1f)]
        public float CalibrationBottomRightYP1 { get; set; }
        [DefaultValue(-1f)]
        public float CalibrationBottomLeftXP1 { get; set; }
        [DefaultValue(-1f)]
        public float CalibrationBottomLeftYP1 { get; set; }

        // Player 2 - 4-Point Calibration (TL, TR, BR, BL)
        [DefaultValue(-1f)]
        public float CalibrationTopRightXP2 { get; set; }
        [DefaultValue(-1f)]
        public float CalibrationTopRightYP2 { get; set; }
        [DefaultValue(-1f)]
        public float CalibrationBottomRightXP2 { get; set; }
        [DefaultValue(-1f)]
        public float CalibrationBottomRightYP2 { get; set; }
        [DefaultValue(-1f)]
        public float CalibrationBottomLeftXP2 { get; set; }
        [DefaultValue(-1f)]
        public float CalibrationBottomLeftYP2 { get; set; }

        // Player 3 - 4-Point Calibration (TL, TR, BR, BL)
        [DefaultValue(-1f)]
        public float CalibrationTopRightXP3 { get; set; }
        [DefaultValue(-1f)]
        public float CalibrationTopRightYP3 { get; set; }
        [DefaultValue(-1f)]
        public float CalibrationBottomRightXP3 { get; set; }
        [DefaultValue(-1f)]
        public float CalibrationBottomRightYP3 { get; set; }
        [DefaultValue(-1f)]
        public float CalibrationBottomLeftXP3 { get; set; }
        [DefaultValue(-1f)]
        public float CalibrationBottomLeftYP3 { get; set; }

        // Player 4 - 4-Point Calibration (TL, TR, BR, BL)
        [DefaultValue(-1f)]
        public float CalibrationTopRightXP4 { get; set; }
        [DefaultValue(-1f)]
        public float CalibrationTopRightYP4 { get; set; }
        [DefaultValue(-1f)]
        public float CalibrationBottomRightXP4 { get; set; }
        [DefaultValue(-1f)]
        public float CalibrationBottomRightYP4 { get; set; }
        [DefaultValue(-1f)]
        public float CalibrationBottomLeftXP4 { get; set; }
        [DefaultValue(-1f)]
        public float CalibrationBottomLeftYP4 { get; set; }


        [DefaultValue(true)]
        public bool DetectDolphinbar { get; set; }

        // Permissive calibration mode for WiimoteBar on very large screens
        // Allows 3-point calibration when 4th corner is unreachable (extrapolates missing point)
        // (EN/FR: Mode calibration permissif pour WiimoteBar sur très grands écrans - extrapole point manquant)
        [DefaultValue(false)]
        public bool PermissiveWiimoteBarCalibration { get; set; }


        [DefaultValue(true)]
        public bool DetectBlueTooth { get; set; }

        [DefaultValue(true)]
        public bool ShowNotifications { get; set; }

        [DefaultValue(5)]
        public int IRSensitivity { get; set; }

        [DefaultValue(true)]
        public bool Enable4Players { get; set; }

        [DefaultValue(true)]
        public bool FirstRun { get; set; }

        public bool ShowSetupWizard { get; set; }

        [DefaultValue(true)]
        public bool UseSharedKeyboard { get; set; }

        [DefaultValue(false)]
        public bool UseSharedHotkeys { get; set; }

        // Keyboard Device ID forcing for TeknoParrot compatibility (EN/FR: Forçage des Device ID clavier pour compatibilité TeknoParrot)
        // Set to 0 for auto-detect, or 1-10 to force a specific keyboard Device ID (EN/FR: 0 pour auto-détection, ou 1-10 pour forcer un Device ID)
        [DefaultValue(0)]
        public int ForceKeyboardDeviceIdP1 { get; set; }
        
        [DefaultValue(0)]
        public int ForceKeyboardDeviceIdP2 { get; set; }
        
        [DefaultValue(0)]
        public int ForceKeyboardDeviceIdP3 { get; set; }
        
        [DefaultValue(0)]
        public int ForceKeyboardDeviceIdP4 { get; set; }

        // Enable verbose keyboard debug logging for troubleshooting (EN/FR: Activer les logs détaillés clavier pour dépannage)
        [DefaultValue(false)]
        public bool KeyboardDebugMode { get; set; }

        // LED layout type for calibration (EN/FR: Type de disposition LED pour calibration)
        // Determines how IR sensors are positioned and how position is calculated
        // (EN/FR: Détermine comment les capteurs IR sont positionnés et comment la position est calculée)
        // (EN/FR: Détermine comment les capteurs IR sont positionnés et comment la position est calculée)


        [DefaultValue(LEDLayoutType.WiimoteBar)]
        public LEDLayoutType LEDLayout { get; set; }

        public ButtonAction WiiA { get; set; }
        public ButtonAction WiiB { get; set; }
        public ButtonAction WiiUp { get; set; }
        public ButtonAction WiiDown { get; set; }
        public ButtonAction WiiLeft { get; set; }
        public ButtonAction WiiRight { get; set; }
        public ButtonAction WiiOne { get; set; }
        public ButtonAction WiiTwo { get; set; }
        public ButtonAction WiiPlus { get; set; }
        public ButtonAction WiiMinus { get; set; }
        public ButtonAction NunC { get; set; }
        public ButtonAction NunZ { get; set; }
        public ButtonAction NunUp { get; set; }
        public ButtonAction NunDown { get; set; }
        public ButtonAction NunLeft { get; set; }
        public ButtonAction NunRight { get; set; }

        // Removed DefaultValue to ensure empty strings are serialized to XML
        // This allows "None (Auto)" setting to persist correctly
        public string PreferredMacP1 { get; set; }
        public string PreferredMacP2 { get; set; }
        public string PreferredMacP3 { get; set; }
        public string PreferredMacP4 { get; set; }

        // Preferred Mouse Hardware ID (VID/PID) per player (EN/FR: ID Matériel Souris Préféré par joueur)
        public string PreferredMouseIdP1 { get; set; }
        public string PreferredMouseIdP2 { get; set; }
        public string PreferredMouseIdP3 { get; set; }
        public string PreferredMouseIdP4 { get; set; }

        // Preferred keyboard hardware ID per player (EN/FR: ID matériel clavier préféré par joueur)
        public string PreferredKeyboardIdP1 { get; set; }
        public string PreferredKeyboardIdP2 { get; set; }
        public string PreferredKeyboardIdP3 { get; set; }
        public string PreferredKeyboardIdP4 { get; set; }

        // Per-player button mappings (EN/FR: Mappings de boutons par joueur)
        public PlayerMappings P1Mappings { get; set; }
        public PlayerMappings P2Mappings { get; set; }
        public PlayerMappings P3Mappings { get; set; }
        public PlayerMappings P4Mappings { get; set; }

        // Per-player hotkey profiles (EN/FR: Profils hotkeys par joueur)
        public HotkeyProfile HotkeyProfileP1 { get; set; }
        public HotkeyProfile HotkeyProfileP2 { get; set; }
        public HotkeyProfile HotkeyProfileP3 { get; set; }
        public HotkeyProfile HotkeyProfileP4 { get; set; }

        // Gun4IR Calibration Data (Serialized as "X1,Y1|X2,Y2|...")
        public string CalibrationGun4IR_P1 { get; set; }
        public string CalibrationGun4IR_P2 { get; set; }
        public string CalibrationGun4IR_P3 { get; set; }
        public string CalibrationGun4IR_P4 { get; set; }

        // 2-Wiimote Bar Calibration (Top/Bottom) - 5 points: Center + 4 Corners
        // (EN/FR: Calibration 2 Wiimote Bars - 5 points : Centre + 4 Coins)
        public string CalibrationTwoWiimoteBar_P1 { get; set; }
        public string CalibrationTwoWiimoteBar_P2 { get; set; }
        public string CalibrationTwoWiimoteBar_P3 { get; set; }
        public string CalibrationTwoWiimoteBar_P4 { get; set; }

        // 4 Corner LEDs Calibration - 5 points: Center + 4 Corners  
        // (EN/FR: Calibration 4 LEDs Coins - 5 points : Centre + 4 Coins)
        public string CalibrationFourCorners_P1 { get; set; }
        public string CalibrationFourCorners_P2 { get; set; }
        public string CalibrationFourCorners_P3 { get; set; }
        public string CalibrationFourCorners_P4 { get; set; }

        // Dynamic Perspective Mode per player (EN/FR: Mode Perspective Dynamique par joueur)
        [DefaultValue(false)]
        public bool UseDynamicPerspective_P1 { get; set; }
        [DefaultValue(false)]
        public bool UseDynamicPerspective_P2 { get; set; }
        [DefaultValue(false)]
        public bool UseDynamicPerspective_P3 { get; set; }
        [DefaultValue(false)]
        public bool UseDynamicPerspective_P4 { get; set; }

        // Vertical offset for Dynamic Perspective center (EN/FR: Offset vertical pour le centre Dynamic Perspective)
        public int DynamicPerspectiveOffsetY_P1 { get; set; }
        public int DynamicPerspectiveOffsetY_P2 { get; set; }
        public int DynamicPerspectiveOffsetY_P3 { get; set; }
        public int DynamicPerspectiveOffsetY_P4 { get; set; }

        // Horizontal offset for Dynamic Perspective center (EN/FR: Offset horizontal pour le centre Dynamic Perspective)
        public int DynamicPerspectiveOffsetX_P1 { get; set; }
        public int DynamicPerspectiveOffsetX_P2 { get; set; }
        public int DynamicPerspectiveOffsetX_P3 { get; set; }
        public int DynamicPerspectiveOffsetX_P4 { get; set; }

        // Weapon Recoil Rumble Settings (EN/FR: Paramètres vibration recul arme)
        public bool EnableWeaponRumble_P1 { get; set; }
        public bool EnableWeaponRumble_P2 { get; set; }
        public bool EnableWeaponRumble_P3 { get; set; }
        public bool EnableWeaponRumble_P4 { get; set; }

        // Allow continuous rumble when trigger held (EN/FR: Autoriser vibration continue si gâchette maintenue)
        public bool AllowContinuousRumble_P1 { get; set; }
        public bool AllowContinuousRumble_P2 { get; set; }
        public bool AllowContinuousRumble_P3 { get; set; }
        public bool AllowContinuousRumble_P4 { get; set; }

        // Rumble intensity (0-100%) (EN/FR: Intensité vibration)
        public int RumbleIntensity_P1 { get; set; }
        public int RumbleIntensity_P2 { get; set; }
        public int RumbleIntensity_P3 { get; set; }
        public int RumbleIntensity_P4 { get; set; }

        // Rumble duration per shot in milliseconds (EN/FR: Durée vibration par tir)
        public int RumbleDurationMs_P1 { get; set; }
        public int RumbleDurationMs_P2 { get; set; }
        public int RumbleDurationMs_P3 { get; set; }
        public int RumbleDurationMs_P4 { get; set; }

        // Repetition interval for continuous fire (ms) (EN/FR: Intervalle répétition tir continu)
        public int RumbleRepetitionMs_P1 { get; set; }
        public int RumbleRepetitionMs_P2 { get; set; }
        public int RumbleRepetitionMs_P3 { get; set; }
        public int RumbleRepetitionMs_P4 { get; set; }

        // Locked player slots — reserved for external devices like Gun4IR
        // (EN/FR: Slots joueur verrouillés — réservés pour périphériques externes comme Gun4IR)
        [DefaultValue(false)]
        public bool LockedSlot_P1 { get; set; }
        [DefaultValue(false)]
        public bool LockedSlot_P2 { get; set; }
        [DefaultValue(false)]
        public bool LockedSlot_P3 { get; set; }
        [DefaultValue(false)]
        public bool LockedSlot_P4 { get; set; }

        // IR Tracking Optimizations — optional, disabled by default
        // (EN/FR: Optimisations tracking IR — optionnelles, désactivées par défaut)
        [DefaultValue(false)]
        public bool EnableIRSmoothing { get; set; }

        [DefaultValue(5)]
        public int IRSmoothingStrength { get; set; } // 1=minimal, 10=heavy (EN/FR: 1=minimal, 10=fort)

        [DefaultValue(false)]
        public bool UseHighPerfTimers { get; set; }

        [DefaultValue(false)]
        public bool EnableHomographyCache { get; set; } // Cache static homography matrix (EN/FR: Mise en cache matrice homographie statique)

        // Gesture & Reload Settings (EN/FR: Paramètres Gestes & Rechargement)
        [DefaultValue(false)]
        public bool EnableOffScreenReload { get; set; }

        [DefaultValue(false)]
        public bool OffScreenReloadAuto { get; set; }

        // DEV ONLY: Hidden setting to enable experimental gestures (EN/FR: Paramètre caché pour gestes expérimentaux)
        // Must be manually set to true in XML config file (EN/FR: Doit être activé manuellement dans le fichier XML)
        [DefaultValue(false)]
        public bool EnableDevGestures { get; set; }

        [DefaultValue(false)]
        public bool EnableShakeReload { get; set; }

        [DefaultValue(1)]
        public int ShakeSensitivity { get; set; } // 0=Low, 1=Medium, 2=High

        [DefaultValue(false)]
        public bool ShakeFromNunchuk { get; set; } // false=Wiimote, true=Nunchuk

        [DefaultValue(false)]
        public bool EnableGrenadeGesture { get; set; }
        
        [DefaultValue(false)]
        public bool GrenadeFromNunchuk { get; set; } // false=Wiimote Y-axis, true=Nunchuk Y-axis

        // Mouse Mode (SendInput vs RawInput) (EN/FR: Mode Souris (SendInput vs RawInput))
        [DefaultValue(MouseMode.RawInput)]
        public MouseMode DefaultMouseMode { get; set; }
        
        // Gyroscope Aiming Settings for FPS Mode (EN/FR: Paramètres visée gyroscopique pour mode FPS)
        [DefaultValue(1.0f)]
        public float GyroSensitivityX { get; set; }
        
        [DefaultValue(1.0f)]
        public float GyroSensitivityY { get; set; }
        
        [DefaultValue(3)]
        public int GyroSmoothingFrames { get; set; }

        // Auto-lock VMulti devices to Player 1 and 2 (EN/FR: Verrouiller auto périphériques VMulti aux Player 1 et 2)
        // When enabled, VMulti2 (VID_002F) is locked to P1 and VMulti1 (VID_00FF) is locked to P2
        // (EN/FR: Si activé, VMulti2 (VID_002F) verrouillé au P1 et VMulti1 (VID_00FF) verrouillé au P2)
        [DefaultValue(true)]
        public bool AutoLockVMultiDevices { get; set; }

        /// <summary>
        /// EN: Keep GamePad devices enabled even when Wiimotes are not connected.
        /// FR: Garder les périphériques GamePad activés même quand les Wiimotes ne sont pas connectées.
        /// Helps guarantee consistent DInput indexing at startup.
        /// </summary>
        public bool PersistentGamePads { get; set; }

        // ========== GamePad Mode Settings (EN/FR: Paramètres Mode GamePad) ==========
        
        /// <summary>
        /// EN: Enable GamePad mode in the Home button swap cycle (Mouse→Keyboard→GamePad→Disabled).
        /// FR: Activer le mode GamePad dans le cycle swap bouton Home (Mouse→Keyboard→GamePad→Disabled).
        /// Default: false (disabled, user must enable in options).
        /// </summary>
        [DefaultValue(false)]
        public bool EnableGamePadSwapMode { get; set; }

        /// <summary>
        /// EN: GamePad button and axis mappings per player.
        /// FR: Mappings boutons et axes GamePad par joueur.
        /// </summary>
        public GamePadMappings GamePadMappingsP1 { get; set; }
        public GamePadMappings GamePadMappingsP2 { get; set; }
        public GamePadMappings GamePadMappingsP3 { get; set; }
        public GamePadMappings GamePadMappingsP4 { get; set; }

        /// <summary>
        /// EN: Get GamePad mappings for a specific player (1-4).
        /// FR: Obtenir les mappings GamePad pour un joueur spécifique (1-4).
        /// </summary>
        public GamePadMappings GetGamePadMappingsForPlayer(int playerIndex)
        {
            switch (playerIndex)
            {
                case 1: return GamePadMappingsP1 ?? (GamePadMappingsP1 = new GamePadMappings());
                case 2: return GamePadMappingsP2 ?? (GamePadMappingsP2 = new GamePadMappings());
                case 3: return GamePadMappingsP3 ?? (GamePadMappingsP3 = new GamePadMappings());
                case 4: return GamePadMappingsP4 ?? (GamePadMappingsP4 = new GamePadMappings());
                default: return new GamePadMappings();
            }
        }


        public WiimoteLib.Geometry.Point2F?[] GetGun4IRCalibration(int playerIndex)
        {
            string data = "";
            switch (playerIndex)
            {
                case 1: data = CalibrationGun4IR_P1; break;
                case 2: data = CalibrationGun4IR_P2; break;
                case 3: data = CalibrationGun4IR_P3; break;
                case 4: data = CalibrationGun4IR_P4; break;
            }

            var points = new WiimoteLib.Geometry.Point2F?[5];
            if (string.IsNullOrEmpty(data)) return points;

            var parts = data.Split('|');
            for (int i = 0; i < parts.Length && i < 5; i++)
            {
                var coords = parts[i].Split(',');
                if (coords.Length == 2 && 
                    float.TryParse(coords[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x) && 
                    float.TryParse(coords[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y))
                {
                    points[i] = new WiimoteLib.Geometry.Point2F(x, y);
                }
            }
            return points;
        }

        public void SetGun4IRCalibration(int playerIndex, WiimoteLib.Geometry.Point2F?[] points)
        {
            if (points == null || points.Length < 5) return;

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < 5; i++)
            {
                if (i > 0) sb.Append("|");
                if (points[i].HasValue)
                    sb.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0},{1}", points[i].Value.X, points[i].Value.Y));
                else
                    sb.Append("0,0"); 
            }
            string data = sb.ToString();

            switch (playerIndex)
            {
                case 1: CalibrationGun4IR_P1 = data; break;
                case 2: CalibrationGun4IR_P2 = data; break;
                case 3: CalibrationGun4IR_P3 = data; break;
                case 4: CalibrationGun4IR_P4 = data; break;
            }
        }

        // Get TwoWiimoteBar Calibration (Top/Bottom sensor bars)
        // (EN/FR: Obtenir calibration 2 Wiimote Bars)
        public WiimoteLib.Geometry.Point2F?[] GetTwoWiimoteBarCalibration(int playerIndex)
        {
            string data = "";
            switch (playerIndex)
            {
                case 1: data = CalibrationTwoWiimoteBar_P1; break;
                case 2: data = CalibrationTwoWiimoteBar_P2; break;
                case 3: data = CalibrationTwoWiimoteBar_P3; break;
                case 4: data = CalibrationTwoWiimoteBar_P4; break;
            }

            var points = new WiimoteLib.Geometry.Point2F?[5];
            if (string.IsNullOrEmpty(data)) return points;

            var parts = data.Split('|');
            for (int i = 0; i < parts.Length && i < 5; i++)
            {
                var coords = parts[i].Split(',');
                if (coords.Length == 2 && 
                    float.TryParse(coords[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x) && 
                    float.TryParse(coords[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y))
                {
                    points[i] = new WiimoteLib.Geometry.Point2F(x, y);
                }
            }
            return points;
        }

        public WiimoteLib.Geometry.Point2F?[] GetFourCornersCalibration(int playerIndex)
        {
            string data = "";
            switch (playerIndex)
            {
                case 1: data = CalibrationFourCorners_P1; break;
                case 2: data = CalibrationFourCorners_P2; break;
                case 3: data = CalibrationFourCorners_P3; break;
                case 4: data = CalibrationFourCorners_P4; break;
            }

            var points = new WiimoteLib.Geometry.Point2F?[5];
            if (string.IsNullOrEmpty(data)) return points;

            var parts = data.Split('|');
            for (int i = 0; i < parts.Length && i < 5; i++)
            {
                var coords = parts[i].Split(',');
                if (coords.Length == 2 && 
                    float.TryParse(coords[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x) && 
                    float.TryParse(coords[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y))
                {
                    points[i] = new WiimoteLib.Geometry.Point2F(x, y);
                }
            }
            return points;
        }

        public void SetFourCornersCalibration(int playerIndex, WiimoteLib.Geometry.Point2F?[] points)
        {
            if (points == null || points.Length < 5) return;

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < 5; i++)
            {
                if (i > 0) sb.Append("|");
                if (points[i].HasValue)
                    sb.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0},{1}", points[i].Value.X, points[i].Value.Y));
                else
                    sb.Append("0,0"); 
            }
            string data = sb.ToString();

            switch (playerIndex)
            {
                case 1: CalibrationFourCorners_P1 = data; break;
                case 2: CalibrationFourCorners_P2 = data; break;
                case 3: CalibrationFourCorners_P3 = data; break;
                case 4: CalibrationFourCorners_P4 = data; break;
            }
        }

        public string GetPreferredMouseId(int playerIndex)
        {
            switch (playerIndex)
            {
                case 1: return PreferredMouseIdP1;
                case 2: return PreferredMouseIdP2;
                case 3: return PreferredMouseIdP3;
                case 4: return PreferredMouseIdP4;
                default: return null;
            }
        }

        public string GetPreferredKeyboardId(int playerIndex)
        {
            switch (playerIndex)
            {
                case 1: return PreferredKeyboardIdP1;
                case 2: return PreferredKeyboardIdP2;
                case 3: return PreferredKeyboardIdP3;
                case 4: return PreferredKeyboardIdP4;
                default: return null;
            }
        }

        public void SetPreferredKeyboardId(int playerIndex, string hardwareId)
        {
            switch (playerIndex)
            {
                case 1: PreferredKeyboardIdP1 = hardwareId; break;
                case 2: PreferredKeyboardIdP2 = hardwareId; break;
                case 3: PreferredKeyboardIdP3 = hardwareId; break;
                case 4: PreferredKeyboardIdP4 = hardwareId; break;
            }
            Save();
        }

        public void SetPreferredMouseId(int playerIndex, string hardwareId)
        {
            switch (playerIndex)
            {
                case 1: PreferredMouseIdP1 = hardwareId; break;
                case 2: PreferredMouseIdP2 = hardwareId; break;
                case 3: PreferredMouseIdP3 = hardwareId; break;
                case 4: PreferredMouseIdP4 = hardwareId; break;
            }
            Save();
        }

        // Set TwoWiimoteBar Calibration (Top/Bottom sensor bars)
        // (EN/FR: Définir calibration 2 Wiimote Bars)
        public void SetTwoWiimoteBarCalibration(int playerIndex, WiimoteLib.Geometry.Point2F?[] points)
        {
            if (points == null || points.Length < 5) return;

            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < 5; i++)
            {
                if (i > 0) sb.Append("|");
                if (points[i].HasValue)
                    sb.Append(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0},{1}", points[i].Value.X, points[i].Value.Y));
                else
                    sb.Append("0,0"); 
            }
            string data = sb.ToString();

            switch (playerIndex)
            {
                case 1: CalibrationTwoWiimoteBar_P1 = data; break;
                case 2: CalibrationTwoWiimoteBar_P2 = data; break;
                case 3: CalibrationTwoWiimoteBar_P3 = data; break;
                case 4: CalibrationTwoWiimoteBar_P4 = data; break;
            }
        }

        // Get/Set Dynamic Perspective Mode (EN/FR: Récupérer/Définir Mode Perspective Dynamique)
        public bool GetUseDynamicPerspective(int playerIndex)
        {
            switch (playerIndex)
            {
                case 1: return UseDynamicPerspective_P1;
                case 2: return UseDynamicPerspective_P2;
                case 3: return UseDynamicPerspective_P3;
                case 4: return UseDynamicPerspective_P4;
                default: return false;
            }
        }

        public void SetUseDynamicPerspective(int playerIndex, bool useDynamic)
        {
            switch (playerIndex)
            {
                case 1: UseDynamicPerspective_P1 = useDynamic; break;
                case 2: UseDynamicPerspective_P2 = useDynamic; break;
                case 3: UseDynamicPerspective_P3 = useDynamic; break;
                case 4: UseDynamicPerspective_P4 = useDynamic; break;
            }
            Save(); // Save immediately to persist the setting (EN/FR: Sauvegarder immédiatement pour persister le paramètre)
        }

        public int GetDynamicPerspectiveOffsetY(int playerIndex)
        {
            switch (playerIndex)
            {
                case 1: return DynamicPerspectiveOffsetY_P1;
                case 2: return DynamicPerspectiveOffsetY_P2;
                case 3: return DynamicPerspectiveOffsetY_P3;
                case 4: return DynamicPerspectiveOffsetY_P4;
                default: return 0;
            }
        }

        public void SetDynamicPerspectiveOffsetY(int playerIndex, int offsetY)
        {
            // Clamp to reasonable range (EN/FR: Limiter à une plage raisonnable)
            offsetY = Math.Max(-200, Math.Min(200, offsetY));
            
            switch (playerIndex)
            {
                case 1: DynamicPerspectiveOffsetY_P1 = offsetY; break;
                case 2: DynamicPerspectiveOffsetY_P2 = offsetY; break;
                case 3: DynamicPerspectiveOffsetY_P3 = offsetY; break;
                case 4: DynamicPerspectiveOffsetY_P4 = offsetY; break;
            }
            Save(); // Save immediately to persist the setting (EN/FR: Sauvegarder immédiatement pour persister le paramètre)
        }

        public int GetDynamicPerspectiveOffsetX(int playerIndex)
        {
            switch (playerIndex)
            {
                case 1: return DynamicPerspectiveOffsetX_P1;
                case 2: return DynamicPerspectiveOffsetX_P2;
                case 3: return DynamicPerspectiveOffsetX_P3;
                case 4: return DynamicPerspectiveOffsetX_P4;
                default: return 0;
            }
        }

        public void SetDynamicPerspectiveOffsetX(int playerIndex, int offsetX)
        {
            // Clamp to reasonable range (EN/FR: Limiter à une plage raisonnable)
            offsetX = Math.Max(-200, Math.Min(200, offsetX));
            
            switch (playerIndex)
            {
                case 1: DynamicPerspectiveOffsetX_P1 = offsetX; break;
                case 2: DynamicPerspectiveOffsetX_P2 = offsetX; break;
                case 3: DynamicPerspectiveOffsetX_P3 = offsetX; break;
                case 4: DynamicPerspectiveOffsetX_P4 = offsetX; break;
            }
            Save(); // Save immediately to persist the setting (EN/FR: Sauvegarder immédiatement pour persister le paramètre)
        }

        public bool GetEnableWeaponRumble(int playerIndex)
        {
            switch (playerIndex)
            {
                case 1: return EnableWeaponRumble_P1;
                case 2: return EnableWeaponRumble_P2;
                case 3: return EnableWeaponRumble_P3;
                case 4: return EnableWeaponRumble_P4;
                default: return false;
            }
        }

        public bool GetAllowContinuousRumble(int playerIndex)
        {
            switch (playerIndex)
            {
                case 1: return AllowContinuousRumble_P1;
                case 2: return AllowContinuousRumble_P2;
                case 3: return AllowContinuousRumble_P3;
                case 4: return AllowContinuousRumble_P4;
                default: return false;
            }
        }

        public int GetRumbleIntensity(int playerIndex)
        {
            switch (playerIndex)
            {
                case 1: return RumbleIntensity_P1;
                case 2: return RumbleIntensity_P2;
                case 3: return RumbleIntensity_P3;
                case 4: return RumbleIntensity_P4;
                default: return 75;
            }
        }

        public int GetRumbleDurationMs(int playerIndex)
        {
            switch (playerIndex)
            {
                case 1: return RumbleDurationMs_P1;
                case 2: return RumbleDurationMs_P2;
                case 3: return RumbleDurationMs_P3;
                case 4: return RumbleDurationMs_P4;
                default: return 60;
            }
        }

        public int GetRumbleRepetitionMs(int playerIndex)
        {
            switch (playerIndex)
            {
                case 1: return RumbleRepetitionMs_P1;
                case 2: return RumbleRepetitionMs_P2;
                case 3: return RumbleRepetitionMs_P3;
                case 4: return RumbleRepetitionMs_P4;
                default: return 150;
            }
        }

        public void SetEnableWeaponRumble(int playerIndex, bool enabled)
        {
            switch (playerIndex)
            {
                case 1: EnableWeaponRumble_P1 = enabled; break;
                case 2: EnableWeaponRumble_P2 = enabled; break;
                case 3: EnableWeaponRumble_P3 = enabled; break;
                case 4: EnableWeaponRumble_P4 = enabled; break;
            }
        }

        public void SetRumbleIntensity(int playerIndex, int intensity)
        {
            switch (playerIndex)
            {
                case 1: RumbleIntensity_P1 = intensity; break;
                case 2: RumbleIntensity_P2 = intensity; break;
                case 3: RumbleIntensity_P3 = intensity; break;
                case 4: RumbleIntensity_P4 = intensity; break;
            }
        }

        public void SetRumbleDurationMs(int playerIndex, int durationMs)
        {
            switch (playerIndex)
            {
                case 1: RumbleDurationMs_P1 = durationMs; break;
                case 2: RumbleDurationMs_P2 = durationMs; break;
                case 3: RumbleDurationMs_P3 = durationMs; break;
                case 4: RumbleDurationMs_P4 = durationMs; break;
            }
        }

        /// <summary>
        /// EN: Check if a player slot is locked (reserved for external device).
        /// FR: Vérifier si un slot joueur est verrouillé (réservé pour périphérique externe).
        /// </summary>
        public bool GetLockedSlot(int playerIndex)
        {
            switch (playerIndex)
            {
                case 1: return LockedSlot_P1;
                case 2: return LockedSlot_P2;
                case 3: return LockedSlot_P3;
                case 4: return LockedSlot_P4;
                default: return false;
            }
        }

        /// <summary>
        /// EN: Lock or unlock a player slot.
        /// FR: Verrouiller ou déverrouiller un slot joueur.
        /// </summary>
        public void SetLockedSlot(int playerIndex, bool locked)
        {
            switch (playerIndex)
            {
                case 1: LockedSlot_P1 = locked; break;
                case 2: LockedSlot_P2 = locked; break;
                case 3: LockedSlot_P3 = locked; break;
                case 4: LockedSlot_P4 = locked; break;
            }
        }

        // Gyroscope Calibration Persistence (EN/FR: Persistance calibration gyroscope)
        public WiimoteCalibration GetCalibration(string uniqueId)
        {
            if (string.IsNullOrEmpty(uniqueId)) return null;
            return SavedCalibrations.Find(c => c.UniqueId == uniqueId);
        }

        public void SetCalibration(string uniqueId, float pitch, float roll, float yaw)
        {
            if (string.IsNullOrEmpty(uniqueId)) return;

            var calib = SavedCalibrations.Find(c => c.UniqueId == uniqueId);
            if (calib == null)
            {
                calib = new WiimoteCalibration { UniqueId = uniqueId };
                SavedCalibrations.Add(calib);
            }

            calib.PitchOffset = pitch;
            calib.RollOffset = roll;
            calib.YawOffset = yaw;
            Save();
        }
        [XmlIgnore]
        public bool StartWithWindows
        {
            get
            {
                bool ret = false;

                try
                {
                    RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                    if (rk != null)
                    {
                        ret = rk.GetValue("WiimoteGun") != null;
                        rk.Close();
                    }
                }
                catch { }

                return ret;
            }
            set
            {
                try
                {
                    RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                    if (rk != null)
                    {
                        if (value)
                            rk.SetValue("WiimoteGun", typeof(Program).Assembly.Location);
                        else
                            rk.DeleteValue("WiimoteGun", false);

                        rk.Close();
                    }
                }
                catch { }
            }
        }
    }
}
