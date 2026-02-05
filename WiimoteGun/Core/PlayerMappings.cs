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
        
        // FPS Gyro Aiming Mode (EN/FR: Mode visée gyroscopique pour FPS)
        public bool EnableGyroAiming { get; set; }

        public PlayerMappings()
        {
            EnableGyroAiming = false;
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
            EnableGyroAiming = source.EnableGyroAiming;
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
