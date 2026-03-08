using System.ComponentModel;
using System.Windows.Forms;

namespace WiimoteGun
{
    /// <summary>
    /// Button mappings for a single player (EN/FR: Mappings des boutons pour un seul joueur)
    /// </summary>
    public class PlayerMappings
    {
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
        
        // Wiener/Accel Movements for Keyboard/Mouse triggers
        public ButtonAction AccelWiimoteUp { get; set; }
        public ButtonAction AccelWiimoteDown { get; set; }
        public ButtonAction AccelWiimoteLeft { get; set; }
        public ButtonAction AccelWiimoteRight { get; set; }
        public ButtonAction AccelWiimoteShake { get; set; }

        public ButtonAction AccelNunchukUp { get; set; }
        public ButtonAction AccelNunchukDown { get; set; }
        public ButtonAction AccelNunchukLeft { get; set; }
        public ButtonAction AccelNunchukRight { get; set; }
        public ButtonAction AccelNunchukShake { get; set; }

        public ButtonAction GyroMotionPlusUp { get; set; }
        public ButtonAction GyroMotionPlusDown { get; set; }
        public ButtonAction GyroMotionPlusLeft { get; set; }
        public ButtonAction GyroMotionPlusRight { get; set; }
        public ButtonAction GyroMotionPlusRollLeft { get; set; }
        public ButtonAction GyroMotionPlusRollRight { get; set; }

        public float AccelWiimoteSensitivity { get; set; }
        public float AccelNunchukSensitivity { get; set; }
        public float GyroSensitivity { get; set; }

        public float AccelWiimoteDeadzone { get; set; }
        public float AccelNunchukDeadzone { get; set; }
        public float GyroDeadzone { get; set; }

        public float AccelWiimoteShakeDeadzone { get; set; }
        public float AccelNunchukShakeDeadzone { get; set; }

        // EN: Number of back-and-forth oscillations required to trigger shake
        // FR: Nombre d'oscillations aller-retour nécessaires pour déclencher le shake
        public int ShakeOscillationRequired { get; set; }


        public PlayerMappings()
        {

            // Default mappings (EN/FR: Mappings par défaut)
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
            AccelWiimoteUp = new ButtonAction();
            AccelWiimoteDown = new ButtonAction();
            AccelWiimoteLeft = new ButtonAction();
            AccelWiimoteRight = new ButtonAction();
            AccelWiimoteShake = new ButtonAction();
            AccelNunchukUp = new ButtonAction();
            AccelNunchukDown = new ButtonAction();
            AccelNunchukLeft = new ButtonAction();
            AccelNunchukRight = new ButtonAction();
            AccelNunchukShake = new ButtonAction();
            GyroMotionPlusUp = new ButtonAction();
            GyroMotionPlusDown = new ButtonAction();
            GyroMotionPlusLeft = new ButtonAction();
            GyroMotionPlusRight = new ButtonAction();
            GyroMotionPlusRollLeft = new ButtonAction();
            GyroMotionPlusRollRight = new ButtonAction();

            AccelWiimoteSensitivity = 10.0f;
            AccelNunchukSensitivity = 20.0f;
            GyroSensitivity = 40.0f;
            AccelWiimoteDeadzone = 2.5f;
            AccelNunchukDeadzone = 1.5f;
            GyroDeadzone = 1.0f;
            AccelWiimoteShakeDeadzone = 2.0f;
            AccelNunchukShakeDeadzone = 2.0f;
            ShakeOscillationRequired = 4;
        }

        /// <summary>
        /// EN: Create a default mapping for a specific player (1-4).
        /// FR: Créer un mapping par défaut pour un joueur spécifique (1-4).
        /// </summary>
        public static PlayerMappings CreateDefault(int playerIndex)
        {
            PlayerMappings mappings = new PlayerMappings();

            // Apply specific overrides for Plus/Minus to avoid conflicts in emulators (start/credits)
            // (EN/FR: Appliquer surcharges Plus/Minus pour éviter conflits émulateurs)
            switch (playerIndex)
            {
                case 1:
                    mappings.WiiPlus = new ButtonAction(Keys.D5);
                    mappings.WiiMinus = new ButtonAction(Keys.D1);
                    break;
                case 2:
                    mappings.WiiPlus = new ButtonAction(Keys.D6);
                    mappings.WiiMinus = new ButtonAction(Keys.D2);
                    break;
                case 3:
                    mappings.WiiPlus = new ButtonAction(Keys.D7);
                    mappings.WiiMinus = new ButtonAction(Keys.D3);
                    break;
                case 4:
                    mappings.WiiPlus = new ButtonAction(Keys.D8);
                    mappings.WiiMinus = new ButtonAction(Keys.D4);
                    break;
            }

            return mappings;
        }

        /// <summary>
        /// Copy mappings from another PlayerMappings instance (EN/FR: Copier les mappings depuis une autre instance)
        /// </summary>
        public void CopyFrom(PlayerMappings source)
        {
            WiiA = source.WiiA;
            WiiB = source.WiiB;
            WiiUp = source.WiiUp;
            WiiDown = source.WiiDown;
            WiiLeft = source.WiiLeft;
            WiiRight = source.WiiRight;
            WiiOne = source.WiiOne;
            WiiTwo = source.WiiTwo;
            WiiPlus = source.WiiPlus;
            WiiMinus = source.WiiMinus;
            NunC = source.NunC;
            NunZ = source.NunZ;
            NunUp = source.NunUp;
            NunDown = source.NunDown;
            NunLeft = source.NunLeft;
            NunRight = source.NunRight;
            AccelWiimoteUp = source.AccelWiimoteUp;
            AccelWiimoteDown = source.AccelWiimoteDown;
            AccelWiimoteLeft = source.AccelWiimoteLeft;
            AccelWiimoteRight = source.AccelWiimoteRight;
            AccelWiimoteShake = source.AccelWiimoteShake;
            AccelNunchukUp = source.AccelNunchukUp;
            AccelNunchukDown = source.AccelNunchukDown;
            AccelNunchukLeft = source.AccelNunchukLeft;
            AccelNunchukRight = source.AccelNunchukRight;
            AccelNunchukShake = source.AccelNunchukShake;
            GyroMotionPlusUp = source.GyroMotionPlusUp;
            GyroMotionPlusDown = source.GyroMotionPlusDown;
            GyroMotionPlusLeft = source.GyroMotionPlusLeft;
            GyroMotionPlusRight = source.GyroMotionPlusRight;
            GyroMotionPlusRollLeft = source.GyroMotionPlusRollLeft;
            GyroMotionPlusRollRight = source.GyroMotionPlusRollRight;

            AccelWiimoteSensitivity = source.AccelWiimoteSensitivity;
            AccelNunchukSensitivity = source.AccelNunchukSensitivity;
            GyroSensitivity = source.GyroSensitivity;
            AccelWiimoteDeadzone = source.AccelWiimoteDeadzone;
            AccelNunchukDeadzone = source.AccelNunchukDeadzone;
            GyroDeadzone = source.GyroDeadzone;
            AccelWiimoteShakeDeadzone = source.AccelWiimoteShakeDeadzone;
            AccelNunchukShakeDeadzone = source.AccelNunchukShakeDeadzone;
            ShakeOscillationRequired = source.ShakeOscillationRequired;
        }

        /// <summary>
        /// Create a deep copy of this PlayerMappings instance (EN/FR: Créer une copie profonde de cette instance)
        /// </summary>
        public PlayerMappings Clone()
        {
            PlayerMappings clone = new PlayerMappings();
            clone.CopyFrom(this);
            return clone;
        }
    }
}
