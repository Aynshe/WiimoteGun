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
                return Key.ToString();

            return "None";
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
        FourCorners = 2      // 4 LEDs at screen corners - Retroshooter/Gun4All configuration
    }

    public class Options
    {
        private Options(bool assignDefaults)
        {
            if (assignDefaults)
            {
                MonitorId = 0;
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

                Enable4Players = false;
                FirstRun = true;
                UseSharedKeyboard = true;
                
                // Keyboard debugging defaults (EN/FR: Valeurs par défaut pour le débogage clavier)
                ForceKeyboardDeviceIdP1 = 0;
                ForceKeyboardDeviceIdP2 = 0;
                ForceKeyboardDeviceIdP3 = 0;
                ForceKeyboardDeviceIdP4 = 0;
                KeyboardDebugMode = false;

                // Initialize player mappings (EN/FR: Initialiser les mappings par joueur)
                P1Mappings = new PlayerMappings();
                P2Mappings = new PlayerMappings();
                P3Mappings = new PlayerMappings();
                P4Mappings = new PlayerMappings();
            }
        }

        private Options() : this(true)
        {
        }

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
                        
                        // Migrate legacy calibration to per-player if needed (EN/FR: Migrer calibration héritée vers par-joueur si besoin)
                        if (options.CalibrationTop != -1 && options.CalibrationTopP1 == -1)
                        {
                            // Copy legacy calibration to all players
                            for (int i = 1; i <= 4; i++)
                            {
                                options.SetCalibrationForPlayer(i, 
                                    options.CalibrationTop, 
                                    options.CalibrationLeft, 
                                    options.CalibrationCenterX, 
                                    options.CalibrationCenterY);
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

        // Get calibration for specific player (EN/FR: Obtenir calibration pour joueur spécifique)
        public (float top, float left, float centerX, float centerY) GetCalibrationForPlayer(int playerIndex)
        {
            switch (playerIndex)
            {
                case 1: return (CalibrationTopP1, CalibrationLeftP1, CalibrationCenterXP1, CalibrationCenterYP1);
                case 2: return (CalibrationTopP2, CalibrationLeftP2, CalibrationCenterXP2, CalibrationCenterYP2);
                case 3: return (CalibrationTopP3, CalibrationLeftP3, CalibrationCenterXP3, CalibrationCenterYP3);
                case 4: return (CalibrationTopP4, CalibrationLeftP4, CalibrationCenterXP4, CalibrationCenterYP4);
                default: return (-1, -1, -1, -1);
            }
        }

        // Set calibration for specific player (EN/FR: Définir calibration pour joueur spécifique)
        public void SetCalibrationForPlayer(int playerIndex, float top, float left, float centerX, float centerY)
        {
            switch (playerIndex)
            {
                case 1:
                    CalibrationTopP1 = top;
                    CalibrationLeftP1 = left;
                    CalibrationCenterXP1 = centerX;
                    CalibrationCenterYP1 = centerY;
                    break;
                case 2:
                    CalibrationTopP2 = top;
                    CalibrationLeftP2 = left;
                    CalibrationCenterXP2 = centerX;
                    CalibrationCenterYP2 = centerY;
                    break;
                case 3:
                    CalibrationTopP3 = top;
                    CalibrationLeftP3 = left;
                    CalibrationCenterXP3 = centerX;
                    CalibrationCenterYP3 = centerY;
                    break;
                case 4:
                    CalibrationTopP4 = top;
                    CalibrationLeftP4 = left;
                    CalibrationCenterXP4 = centerX;
                    CalibrationCenterYP4 = centerY;
                    break;
            }
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


        [DefaultValue(true)]
        public bool DetectDolphinbar { get; set; }

        [DefaultValue(true)]
        public bool DetectBlueTooth { get; set; }

        [DefaultValue(true)]
        public bool ShowNotifications { get; set; }

        [DefaultValue(5)]
        public int IRSensitivity { get; set; }

        [DefaultValue(false)]
        public bool Enable4Players { get; set; }

        [DefaultValue(true)]
        public bool FirstRun { get; set; }

        [DefaultValue(true)]
        public bool UseSharedKeyboard { get; set; }

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

        // Per-player button mappings (EN/FR: Mappings de boutons par joueur)
        public PlayerMappings P1Mappings { get; set; }
        public PlayerMappings P2Mappings { get; set; }
        public PlayerMappings P3Mappings { get; set; }
        public PlayerMappings P4Mappings { get; set; }

        // Gun4IR Calibration Data (Serialized as "X1,Y1|X2,Y2|...")
        public string CalibrationGun4IR_P1 { get; set; }
        public string CalibrationGun4IR_P2 { get; set; }
        public string CalibrationGun4IR_P3 { get; set; }
        public string CalibrationGun4IR_P4 { get; set; }

        // 4-Corners Calibration Data (Serialized as "X1,Y1|X2,Y2|...")
        public string CalibrationFourCorners_P1 { get; set; }
        public string CalibrationFourCorners_P2 { get; set; }
        public string CalibrationFourCorners_P3 { get; set; }
        public string CalibrationFourCorners_P4 { get; set; }

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
