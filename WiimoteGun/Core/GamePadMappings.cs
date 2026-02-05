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
                IRLinearity = this.IRLinearity,
                IROverscan = this.IROverscan,
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
