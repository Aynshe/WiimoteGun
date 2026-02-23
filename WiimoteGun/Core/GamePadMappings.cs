using System;

namespace WiimoteGun
{
    /// <summary>
    /// EN: GamePad axis mapping options for VMulti Col06 controller.
    /// FR: Options de mapping des axes GamePad pour le contrôleur VMulti Col06.
    /// </summary>
    public enum GamePadAxis
    {
        None = 0,
        LeftStick = 1,
        RightStick = 2,
        Dpad = 3,
        Throttle = 4
    }

    /// <summary>
    /// EN: GamePad button mapping options for VMulti Col06 controller (DirectInput).
    /// FR: Options de mapping des boutons GamePad pour le contrôleur VMulti Col06 (DirectInput).
    /// </summary>
    public enum GamePadButton
    {
        None = 0,
        Button1 = 1,   // A on most controllers
        Button2 = 2,   // B
        Button3 = 3,   // X
        Button4 = 4,   // Y
        Button5 = 5,   // Left Bumper
        Button6 = 6,   // Right Bumper
        Button7 = 7,   // Left Trigger (as button)
        Button8 = 8,   // Right Trigger (as button)
        Button9 = 9,   // Back/Select
        Button10 = 10, // Start
        Button11 = 11, // Left Stick Click
        Button12 = 12, // Right Stick Click
        DPadUp = 13,
        DPadDown = 14,
        DPadLeft = 15,
        DPadRight = 16
    }

    /// <summary>
    /// EN: Motion mapping modes for physical Wiimote sensors.
    /// FR: Modes de mapping de mouvement pour les capteurs physiques de la Wiimote.
    /// </summary>
    public enum GamePadMotionMode
    {
        None = 0,
        GyroToRightStick = 1,
        AccToRightStick = 2,
        GyroToLeftStick = 3,
        AccToLeftStick = 4,
        AccToThrottle = 5,
        AccNunchukToRightStick = 6,
        AccNunchukToLeftStick = 7,
        AccNunchukToThrottle = 8
    }

    /// <summary>
    /// EN: Target type for motion mapping.
    /// FR: Type de cible pour le mapping de mouvement.
    /// </summary>
    public enum GamePadMotionTargetType
    {
        None = 0,
        Axis = 1,
        Button = 2
    }

    /// <summary>
    /// EN: Represents an action triggered by motion. Can be mapped to an axis or a button.
    /// FR: Représente une action déclenchée par le mouvement. Peut être mappée sur un axe ou un bouton.
    /// </summary>
    public class GamePadMotionAction
    {
        public GamePadMotionTargetType TargetType { get; set; }
        public GamePadAxis TargetAxis { get; set; }
        public GamePadButton TargetButton { get; set; }

        public GamePadMotionAction()
        {
            TargetType = GamePadMotionTargetType.None;
            TargetAxis = GamePadAxis.None;
            TargetButton = GamePadButton.None;
        }

        public GamePadMotionAction Clone()
        {
            return new GamePadMotionAction
            {
                TargetType = this.TargetType,
                TargetAxis = this.TargetAxis,
                TargetButton = this.TargetButton
            };
        }

        public void SetAxisIfNone(GamePadAxis axis)
        {
            if (TargetType == GamePadMotionTargetType.None)
            {
                TargetType = GamePadMotionTargetType.Axis;
                TargetAxis = axis;
            }
        }
    }

    /// <summary>
    /// EN: Configuration class for GamePad button and axis mappings per player.
    /// FR: Classe de configuration pour les mappings boutons et axes GamePad par joueur.
    /// </summary>
    public class GamePadMappings
    {
        // ========== Axis Mappings (EN/FR: Mappings des axes) ==========
        
        /// <summary>
        /// EN: Axis controlled by Wiimote IR sensor (default: Right Stick).
        /// FR: Axe contrôlé par le capteur IR de la Wiimote (défaut: Stick droit).
        /// </summary>
        public GamePadAxis IRSensorAxis { get; set; }

        /// <summary>
        /// EN: Axis controlled by Nunchuk joystick (default: Left Stick).
        /// FR: Axe contrôlé par le joystick du Nunchuk (défaut: Stick gauche).
        /// </summary>
        public GamePadAxis NunchukJoystickAxis { get; set; }

        /// <summary>
        /// EN: IR tracking linearity (S-Curve). Values > 1.0 reduce "advance" effect at edges.
        /// FR: Linéarité du tracking IR (courbe en S). Les valeurs > 1.0 réduisent l'effet d'avance sur les bords.
        /// </summary>
        public float IRLinearity { get; set; }

        /// <summary>
        /// EN: IR tracking overscan margin (0.0 to 0.4). Maps [margin..1-margin] to [0..1].
        /// FR: Marge d'overscan du tracking IR (0.0 à 0.4). Mappe [marge..1-marge] vers [0..1].
        /// </summary>
        public float IROverscan { get; set; }

        /// <summary>
        /// EN: Motion mapping mode for physical sensors (default: None).
        /// FR: Mode de mapping du mouvement pour les capteurs physiques (défaut: Aucun).
        /// </summary>
        public GamePadMotionMode MotionMode { get; set; }
        public float IRAntiDeadzone { get; set; }




        public float AccelWiimoteSensitivity { get; set; }
        public float AccelNunchukSensitivity { get; set; }

        /// <summary>
        /// EN: Gyroscope sensitivity multiplier.
        /// FR: Multiplicateur de sensibilité du gyroscope.
        /// </summary>
        public float GyroSensitivity { get; set; }

        /// <summary>
        /// EN: Accelerometer movement deadzone (G).
        /// FR: Zone morte du mouvement accéléromètre (G).
        /// </summary>
        public float AccDeadzone { get; set; }
        public float AccelWiimoteDeadzone { get; set; }
        public float AccelNunchukDeadzone { get; set; }
        public float GyroDeadzone { get; set; }

        public float AccelWiimoteShakeDeadzone { get; set; }
        public float AccelNunchukShakeDeadzone { get; set; }

        // EN: Number of back-and-forth oscillations required to trigger shake
        // FR: Nombre d'oscillations aller-retour nécessaires pour déclencher le shake
        public int ShakeOscillationRequired { get; set; }


        // Wiener/Accel Movements
        public GamePadMotionAction AccelWiimoteUp { get; set; }
        public GamePadMotionAction AccelWiimoteDown { get; set; }
        public GamePadMotionAction AccelWiimoteLeft { get; set; }
        public GamePadMotionAction AccelWiimoteRight { get; set; }
        public GamePadMotionAction AccelWiimoteShake { get; set; }

        public GamePadMotionAction AccelNunchukUp { get; set; }
        public GamePadMotionAction AccelNunchukDown { get; set; }
        public GamePadMotionAction AccelNunchukLeft { get; set; }
        public GamePadMotionAction AccelNunchukRight { get; set; }
        public GamePadMotionAction AccelNunchukShake { get; set; }

        public GamePadMotionAction GyroMotionPlusUp { get; set; }
        public GamePadMotionAction GyroMotionPlusDown { get; set; }
        public GamePadMotionAction GyroMotionPlusLeft { get; set; }
        public GamePadMotionAction GyroMotionPlusRight { get; set; }
        public GamePadMotionAction GyroMotionPlusRollLeft { get; set; }
        public GamePadMotionAction GyroMotionPlusRollRight { get; set; }

        /// <summary>
        /// EN: Use XInput (ViGEmBus) instead of DirectInput (VMulti).
        /// FR: Utiliser XInput (ViGEmBus) au lieu de DirectInput (VMulti).
        /// </summary>
        public bool UseXInput { get; set; }

        // ========== Wiimote Button Mappings (EN/FR: Mappings boutons Wiimote) ==========
        
        public GamePadButton WiiA { get; set; }
        public GamePadButton WiiB { get; set; }
        public GamePadButton Wii1 { get; set; }
        public GamePadButton Wii2 { get; set; }
        public GamePadButton WiiPlus { get; set; }
        public GamePadButton WiiMinus { get; set; }
        public GamePadButton WiiUp { get; set; }
        public GamePadButton WiiDown { get; set; }
        public GamePadButton WiiLeft { get; set; }
        public GamePadButton WiiRight { get; set; }
        public GamePadButton WiiHome { get; set; }

        // ========== Nunchuk Button Mappings (EN/FR: Mappings boutons Nunchuk) ==========
        
        public GamePadButton NunchukC { get; set; }
        public GamePadButton NunchukZ { get; set; }
        public GamePadButton NunchukUp { get; set; }
        public GamePadButton NunchukDown { get; set; }
        public GamePadButton NunchukLeft { get; set; }
        public GamePadButton NunchukRight { get; set; }

        // ========== Hybrid Mappings (EN/FR: Mappings Hybrides) ==========
        
        /// <summary>
        /// EN: The button used to trigger the hybrid mode (e.g. "NunchukZ"). null or empty means disabled.
        /// FR: Le bouton utilisé pour déclencher le mode hybride (ex: "NunchukZ"). null ou vide signifie désactivé.
        /// </summary>
        public string HybridTriggerButton { get; set; }
        
        public ButtonAction WiiAHybrid { get; set; }
        public ButtonAction WiiBHybrid { get; set; }
        public ButtonAction Wii1Hybrid { get; set; }
        public ButtonAction Wii2Hybrid { get; set; }
        public ButtonAction WiiPlusHybrid { get; set; }
        public ButtonAction WiiMinusHybrid { get; set; }
        public ButtonAction WiiUpHybrid { get; set; }
        public ButtonAction WiiDownHybrid { get; set; }
        public ButtonAction WiiLeftHybrid { get; set; }
        public ButtonAction WiiRightHybrid { get; set; }
        public ButtonAction WiiHomeHybrid { get; set; }
        public ButtonAction NunchukCHybrid { get; set; }
        public ButtonAction NunchukZHybrid { get; set; }
        public ButtonAction NunchukUpHybrid { get; set; }
        public ButtonAction NunchukDownHybrid { get; set; }
        public ButtonAction NunchukLeftHybrid { get; set; }
        public ButtonAction NunchukRightHybrid { get; set; }

        // Motion Gestures Hybrid
        public ButtonAction AccelWiimoteUpHybrid { get; set; }
        public ButtonAction AccelWiimoteDownHybrid { get; set; }
        public ButtonAction AccelWiimoteLeftHybrid { get; set; }
        public ButtonAction AccelWiimoteRightHybrid { get; set; }
        public ButtonAction AccelWiimoteShakeHybrid { get; set; }

        public ButtonAction AccelNunchukUpHybrid { get; set; }
        public ButtonAction AccelNunchukDownHybrid { get; set; }
        public ButtonAction AccelNunchukLeftHybrid { get; set; }
        public ButtonAction AccelNunchukRightHybrid { get; set; }
        public ButtonAction AccelNunchukShakeHybrid { get; set; }

        public ButtonAction GyroMotionPlusUpHybrid { get; set; }
        public ButtonAction GyroMotionPlusDownHybrid { get; set; }
        public ButtonAction GyroMotionPlusLeftHybrid { get; set; }
        public ButtonAction GyroMotionPlusRightHybrid { get; set; }
        public ButtonAction GyroMotionPlusRollLeftHybrid { get; set; }
        public ButtonAction GyroMotionPlusRollRightHybrid { get; set; }

        /// <summary>
        /// EN: Switch IR to mouse movement when hybrid is active.
        /// FR: Basculer l'IR sur le mouvement de la souris quand l'hybride est actif.
        /// </summary>
        public bool IRHybridAsMouse { get; set; }

        /// <summary>
        /// EN: Use toggle mode for hybrid trigger (press to switch, press again to return).
        /// FR: Utiliser le mode bascule pour le trigger hybride (appuyer pour changer, ré-appuyer pour revenir).
        /// </summary>
        public bool HybridToggle { get; set; }

        /// <summary>
        /// EN: Default constructor with default mappings.
        /// FR: Constructeur par défaut avec les mappings par défaut.
        /// </summary>
        public GamePadMappings()
        {
            IRSensorAxis = GamePadAxis.RightStick;
            NunchukJoystickAxis = GamePadAxis.LeftStick;
            IRLinearity = 1.3f;
            IROverscan = 0.05f;
            MotionMode = GamePadMotionMode.None;
            IRAntiDeadzone = 0.20f;   // EN/FR: Default 20% anti-deadzone (to bypass game internal deadzones)


            AccelWiimoteSensitivity = 10.0f;
            AccelNunchukSensitivity = 20.0f;
            GyroSensitivity = 40.0f;
            AccDeadzone = 2.5f;   // EN/FR: Default 2.5G threshold
            AccelWiimoteDeadzone = 2.5f;
            AccelNunchukDeadzone = 1.5f;
            GyroDeadzone = 1.0f;
            AccelWiimoteShakeDeadzone = 2.0f;
            AccelNunchukShakeDeadzone = 2.0f;  // EN/FR: Default 1.0 threshold
            ShakeOscillationRequired = 4;
            AccelWiimoteUp = new GamePadMotionAction();
            AccelWiimoteDown = new GamePadMotionAction();
            AccelWiimoteLeft = new GamePadMotionAction();
            AccelWiimoteRight = new GamePadMotionAction();
            AccelWiimoteShake = new GamePadMotionAction();
            AccelNunchukUp = new GamePadMotionAction();
            AccelNunchukDown = new GamePadMotionAction();
            AccelNunchukLeft = new GamePadMotionAction();
            AccelNunchukRight = new GamePadMotionAction();
            AccelNunchukShake = new GamePadMotionAction();
            GyroMotionPlusUp = new GamePadMotionAction();
            GyroMotionPlusDown = new GamePadMotionAction();
            GyroMotionPlusLeft = new GamePadMotionAction();
            GyroMotionPlusRight = new GamePadMotionAction();
            GyroMotionPlusRollLeft = new GamePadMotionAction();
            GyroMotionPlusRollRight = new GamePadMotionAction();
            UseXInput = false;
            WiiA = GamePadButton.Button1;
            WiiB = GamePadButton.Button2;
            Wii1 = GamePadButton.Button3;
            Wii2 = GamePadButton.Button4;
            WiiPlus = GamePadButton.Button10;
            WiiMinus = GamePadButton.Button9;
            WiiUp = GamePadButton.DPadUp;
            WiiDown = GamePadButton.DPadDown;
            WiiLeft = GamePadButton.DPadLeft;
            WiiRight = GamePadButton.DPadRight;
            WiiHome = GamePadButton.None;
            NunchukC = GamePadButton.Button5;
            NunchukZ = GamePadButton.Button7;
            NunchukUp = GamePadButton.None;
            NunchukDown = GamePadButton.None;
            NunchukLeft = GamePadButton.None;
            NunchukRight = GamePadButton.None;
            
            // Hybrid defaults (EN/FR: Valeurs par défaut hybrides)
            HybridTriggerButton = "";
            WiiAHybrid = new ButtonAction(SpecialAction.LeftMouse);
            WiiBHybrid = new ButtonAction(SpecialAction.RightMouse);
            Wii1Hybrid = new ButtonAction();
            Wii2Hybrid = new ButtonAction();
            WiiPlusHybrid = new ButtonAction();
            WiiMinusHybrid = new ButtonAction();
            WiiUpHybrid = new ButtonAction();
            WiiDownHybrid = new ButtonAction();
            WiiLeftHybrid = new ButtonAction();
            WiiRightHybrid = new ButtonAction();
            WiiHomeHybrid = new ButtonAction();
            NunchukCHybrid = new ButtonAction();
            NunchukZHybrid = new ButtonAction();
            NunchukUpHybrid = new ButtonAction();
            NunchukDownHybrid = new ButtonAction();
            NunchukLeftHybrid = new ButtonAction();
            NunchukRightHybrid = new ButtonAction();

            AccelWiimoteUpHybrid = new ButtonAction();
            AccelWiimoteDownHybrid = new ButtonAction();
            AccelWiimoteLeftHybrid = new ButtonAction();
            AccelWiimoteRightHybrid = new ButtonAction();
            AccelWiimoteShakeHybrid = new ButtonAction();
            AccelNunchukUpHybrid = new ButtonAction();
            AccelNunchukDownHybrid = new ButtonAction();
            AccelNunchukLeftHybrid = new ButtonAction();
            AccelNunchukRightHybrid = new ButtonAction();
            AccelNunchukShakeHybrid = new ButtonAction();
            GyroMotionPlusUpHybrid = new ButtonAction();
            GyroMotionPlusDownHybrid = new ButtonAction();
            GyroMotionPlusLeftHybrid = new ButtonAction();
            GyroMotionPlusRightHybrid = new ButtonAction();
            GyroMotionPlusRollLeftHybrid = new ButtonAction();
            GyroMotionPlusRollRightHybrid = new ButtonAction();
            IRHybridAsMouse = false;
            HybridToggle = false;
        }

        /// <summary>
        /// EN: Copy mappings from another GamePadMappings instance.
        /// FR: Copier les mappings depuis une autre instance GamePadMappings.
        /// </summary>
        public void CopyFrom(GamePadMappings source)
        {
            if (source == null) return;

            IRSensorAxis = source.IRSensorAxis;
            NunchukJoystickAxis = source.NunchukJoystickAxis;
            IRLinearity = source.IRLinearity;
            IROverscan = source.IROverscan;
            MotionMode = source.MotionMode;
            IRAntiDeadzone = source.IRAntiDeadzone;

            AccelWiimoteSensitivity = source.AccelWiimoteSensitivity;
            AccelNunchukSensitivity = source.AccelNunchukSensitivity;
            GyroSensitivity = source.GyroSensitivity;
            AccDeadzone = source.AccDeadzone;
            AccelWiimoteDeadzone = source.AccelWiimoteDeadzone;
            AccelNunchukDeadzone = source.AccelNunchukDeadzone;
            GyroDeadzone = source.GyroDeadzone;
            AccelWiimoteShakeDeadzone = source.AccelWiimoteShakeDeadzone;
            AccelNunchukShakeDeadzone = source.AccelNunchukShakeDeadzone;
            ShakeOscillationRequired = source.ShakeOscillationRequired;
            
            AccelWiimoteUp = source.AccelWiimoteUp?.Clone() ?? new GamePadMotionAction();
            AccelWiimoteDown = source.AccelWiimoteDown?.Clone() ?? new GamePadMotionAction();
            AccelWiimoteLeft = source.AccelWiimoteLeft?.Clone() ?? new GamePadMotionAction();
            AccelWiimoteRight = source.AccelWiimoteRight?.Clone() ?? new GamePadMotionAction();
            AccelWiimoteShake = source.AccelWiimoteShake?.Clone() ?? new GamePadMotionAction();
            
            AccelNunchukUp = source.AccelNunchukUp?.Clone() ?? new GamePadMotionAction();
            AccelNunchukDown = source.AccelNunchukDown?.Clone() ?? new GamePadMotionAction();
            AccelNunchukLeft = source.AccelNunchukLeft?.Clone() ?? new GamePadMotionAction();
            AccelNunchukRight = source.AccelNunchukRight?.Clone() ?? new GamePadMotionAction();
            AccelNunchukShake = source.AccelNunchukShake?.Clone() ?? new GamePadMotionAction();
            
            GyroMotionPlusUp = source.GyroMotionPlusUp?.Clone() ?? new GamePadMotionAction();
            GyroMotionPlusDown = source.GyroMotionPlusDown?.Clone() ?? new GamePadMotionAction();
            GyroMotionPlusLeft = source.GyroMotionPlusLeft?.Clone() ?? new GamePadMotionAction();
            GyroMotionPlusRight = source.GyroMotionPlusRight?.Clone() ?? new GamePadMotionAction();
            GyroMotionPlusRollLeft = source.GyroMotionPlusRollLeft?.Clone() ?? new GamePadMotionAction();
            GyroMotionPlusRollRight = source.GyroMotionPlusRollRight?.Clone() ?? new GamePadMotionAction();
            
            UseXInput = source.UseXInput;
            WiiA = source.WiiA;
            WiiB = source.WiiB;
            Wii1 = source.Wii1;
            Wii2 = source.Wii2;
            WiiPlus = source.WiiPlus;
            WiiMinus = source.WiiMinus;
            WiiUp = source.WiiUp;
            WiiDown = source.WiiDown;
            WiiLeft = source.WiiLeft;
            WiiRight = source.WiiRight;
            WiiHome = source.WiiHome;
            NunchukC = source.NunchukC;
            NunchukZ = source.NunchukZ;
            NunchukUp = source.NunchukUp;
            NunchukDown = source.NunchukDown;
            NunchukLeft = source.NunchukLeft;
            NunchukRight = source.NunchukRight;
            
            HybridTriggerButton = source.HybridTriggerButton;
            WiiAHybrid = source.WiiAHybrid?.Clone() ?? new ButtonAction(SpecialAction.LeftMouse);
            WiiBHybrid = source.WiiBHybrid?.Clone() ?? new ButtonAction(SpecialAction.RightMouse);
            Wii1Hybrid = source.Wii1Hybrid?.Clone() ?? new ButtonAction();
            Wii2Hybrid = source.Wii2Hybrid?.Clone() ?? new ButtonAction();
            WiiPlusHybrid = source.WiiPlusHybrid?.Clone() ?? new ButtonAction();
            WiiMinusHybrid = source.WiiMinusHybrid?.Clone() ?? new ButtonAction();
            WiiUpHybrid = source.WiiUpHybrid?.Clone() ?? new ButtonAction();
            WiiDownHybrid = source.WiiDownHybrid?.Clone() ?? new ButtonAction();
            WiiLeftHybrid = source.WiiLeftHybrid?.Clone() ?? new ButtonAction();
            WiiRightHybrid = source.WiiRightHybrid?.Clone() ?? new ButtonAction();
            WiiHomeHybrid = source.WiiHomeHybrid?.Clone() ?? new ButtonAction();
            NunchukCHybrid = source.NunchukCHybrid?.Clone() ?? new ButtonAction();
            NunchukZHybrid = source.NunchukZHybrid?.Clone() ?? new ButtonAction();
            NunchukUpHybrid = source.NunchukUpHybrid?.Clone() ?? new ButtonAction();
            NunchukDownHybrid = source.NunchukDownHybrid?.Clone() ?? new ButtonAction();
            NunchukLeftHybrid = source.NunchukLeftHybrid?.Clone() ?? new ButtonAction();
            NunchukRightHybrid = source.NunchukRightHybrid?.Clone() ?? new ButtonAction();

            AccelWiimoteUpHybrid = source.AccelWiimoteUpHybrid?.Clone() ?? new ButtonAction();
            AccelWiimoteDownHybrid = source.AccelWiimoteDownHybrid?.Clone() ?? new ButtonAction();
            AccelWiimoteLeftHybrid = source.AccelWiimoteLeftHybrid?.Clone() ?? new ButtonAction();
            AccelWiimoteRightHybrid = source.AccelWiimoteRightHybrid?.Clone() ?? new ButtonAction();
            AccelWiimoteShakeHybrid = source.AccelWiimoteShakeHybrid?.Clone() ?? new ButtonAction();
            
            AccelNunchukUpHybrid = source.AccelNunchukUpHybrid?.Clone() ?? new ButtonAction();
            AccelNunchukDownHybrid = source.AccelNunchukDownHybrid?.Clone() ?? new ButtonAction();
            AccelNunchukLeftHybrid = source.AccelNunchukLeftHybrid?.Clone() ?? new ButtonAction();
            AccelNunchukRightHybrid = source.AccelNunchukRightHybrid?.Clone() ?? new ButtonAction();
            AccelNunchukShakeHybrid = source.AccelNunchukShakeHybrid?.Clone() ?? new ButtonAction();
            
            GyroMotionPlusUpHybrid = source.GyroMotionPlusUpHybrid?.Clone() ?? new ButtonAction();
            GyroMotionPlusDownHybrid = source.GyroMotionPlusDownHybrid?.Clone() ?? new ButtonAction();
            GyroMotionPlusLeftHybrid = source.GyroMotionPlusLeftHybrid?.Clone() ?? new ButtonAction();
            GyroMotionPlusRightHybrid = source.GyroMotionPlusRightHybrid?.Clone() ?? new ButtonAction();
            GyroMotionPlusRollLeftHybrid = source.GyroMotionPlusRollLeftHybrid?.Clone() ?? new ButtonAction();
            GyroMotionPlusRollRightHybrid = source.GyroMotionPlusRollRightHybrid?.Clone() ?? new ButtonAction();
            
            IRHybridAsMouse = source.IRHybridAsMouse;
            HybridToggle = source.HybridToggle;
        }

        /// <summary>
        /// EN: Create a deep copy of this GamePadMappings instance.
        /// FR: Créer une copie profonde de cette instance GamePadMappings.
        /// </summary>
        public GamePadMappings Clone()
        {
            GamePadMappings clone = new GamePadMappings();
            clone.CopyFrom(this);
            return clone;
        }

        /// <summary>
        /// EN: Check if any hybrid action is mapped to a mouse button.
        /// FR: Vérifier si une action hybride est mappée sur un bouton de souris.
        /// </summary>
        public bool HasAnyHybridMouseAction()
        {
            return IsMouseAction(WiiAHybrid) || IsMouseAction(WiiBHybrid) || IsMouseAction(Wii1Hybrid) ||
                   IsMouseAction(Wii2Hybrid) || IsMouseAction(WiiPlusHybrid) || IsMouseAction(WiiMinusHybrid) ||
                   IsMouseAction(WiiUpHybrid) || IsMouseAction(WiiDownHybrid) || IsMouseAction(WiiLeftHybrid) ||
                   IsMouseAction(WiiRightHybrid) || IsMouseAction(WiiHomeHybrid) || IsMouseAction(NunchukCHybrid) ||
                   IsMouseAction(NunchukZHybrid) || IsMouseAction(NunchukUpHybrid) || IsMouseAction(NunchukDownHybrid) ||
                   IsMouseAction(NunchukLeftHybrid) || IsMouseAction(NunchukRightHybrid) ||
                   IsMouseAction(AccelWiimoteUpHybrid) || IsMouseAction(AccelWiimoteDownHybrid) ||
                   IsMouseAction(AccelWiimoteLeftHybrid) || IsMouseAction(AccelWiimoteRightHybrid) ||
                   IsMouseAction(AccelWiimoteShakeHybrid) ||
                   IsMouseAction(AccelNunchukUpHybrid) || IsMouseAction(AccelNunchukDownHybrid) ||
                   IsMouseAction(AccelNunchukLeftHybrid) || IsMouseAction(AccelNunchukRightHybrid) ||
                   IsMouseAction(AccelNunchukShakeHybrid) ||
                   IsMouseAction(GyroMotionPlusUpHybrid) || IsMouseAction(GyroMotionPlusDownHybrid) ||
                   IsMouseAction(GyroMotionPlusLeftHybrid) || IsMouseAction(GyroMotionPlusRightHybrid) ||
                   IsMouseAction(GyroMotionPlusRollLeftHybrid) || IsMouseAction(GyroMotionPlusRollRightHybrid);
        }

        private bool IsMouseAction(ButtonAction action)
        {
            if (action == null) return false;
            return action.Special == SpecialAction.LeftMouse || 
                   action.Special == SpecialAction.RightMouse || 
                   action.Special == SpecialAction.MiddleMouse;
        }
    }
}
