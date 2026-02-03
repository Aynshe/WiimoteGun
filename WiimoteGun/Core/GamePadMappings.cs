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
        Dpad = 3
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
        public GamePadAxis IRSensorAxis { get; set; } = GamePadAxis.RightStick;

        /// <summary>
        /// EN: Axis controlled by Nunchuk joystick (default: Left Stick).
        /// FR: Axe contrôlé par le joystick du Nunchuk (défaut: Stick gauche).
        /// </summary>
        public GamePadAxis NunchukJoystickAxis { get; set; } = GamePadAxis.LeftStick;

        // ========== Wiimote Button Mappings (EN/FR: Mappings boutons Wiimote) ==========
        
        public GamePadButton WiiA { get; set; } = GamePadButton.Button1;      // A
        public GamePadButton WiiB { get; set; } = GamePadButton.Button2;      // B
        public GamePadButton Wii1 { get; set; } = GamePadButton.Button3;      // X
        public GamePadButton Wii2 { get; set; } = GamePadButton.Button4;      // Y
        public GamePadButton WiiPlus { get; set; } = GamePadButton.Button10;  // Start
        public GamePadButton WiiMinus { get; set; } = GamePadButton.Button9;  // Back
        public GamePadButton WiiUp { get; set; } = GamePadButton.DPadUp;
        public GamePadButton WiiDown { get; set; } = GamePadButton.DPadDown;
        public GamePadButton WiiLeft { get; set; } = GamePadButton.DPadLeft;
        public GamePadButton WiiRight { get; set; } = GamePadButton.DPadRight;
        public GamePadButton WiiHome { get; set; } = GamePadButton.None;

        // ========== Nunchuk Button Mappings (EN/FR: Mappings boutons Nunchuk) ==========
        
        public GamePadButton NunchukC { get; set; } = GamePadButton.Button5;  // Left Bumper
        public GamePadButton NunchukZ { get; set; } = GamePadButton.Button7;  // Left Trigger

        /// <summary>
        /// EN: Default constructor with default mappings.
        /// FR: Constructeur par défaut avec les mappings par défaut.
        /// </summary>
        public GamePadMappings()
        {
            // Defaults already set via property initializers
            // (EN/FR: Les défauts sont déjà définis via les initialiseurs de propriétés)
        }

        /// <summary>
        /// EN: Create a copy of these mappings.
        /// FR: Créer une copie de ces mappings.
        /// </summary>
        public GamePadMappings Clone()
        {
            return new GamePadMappings
            {
                IRSensorAxis = this.IRSensorAxis,
                NunchukJoystickAxis = this.NunchukJoystickAxis,
                WiiA = this.WiiA,
                WiiB = this.WiiB,
                Wii1 = this.Wii1,
                Wii2 = this.Wii2,
                WiiPlus = this.WiiPlus,
                WiiMinus = this.WiiMinus,
                WiiUp = this.WiiUp,
                WiiDown = this.WiiDown,
                WiiLeft = this.WiiLeft,
                WiiRight = this.WiiRight,
                WiiHome = this.WiiHome,
                NunchukC = this.NunchukC,
                NunchukZ = this.NunchukZ
            };
        }
    }
}
