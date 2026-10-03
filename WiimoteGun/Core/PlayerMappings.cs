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

        // ========== [V54] Off-Screen Reload / TC Cover (EN/FR: Rechargement hors-écran / planque TC) ==========

        /// <summary>
        /// EN: Per-profile override of the global Off-Screen Reload option:
        /// -1 = follow Options > Gestures (default), 0 = force OFF, 1 = force ON.
        /// FR: Override par profil de l'option globale Off-Screen Reload :
        /// -1 = suivre Options > Gestures (défaut), 0 = forcer DÉSACTIVÉ, 1 = forcer ACTIVÉ.
        /// </summary>
        public int OffScreenReloadOverride { get; set; } = -1;

        /// <summary>
        /// EN: [V55] Per-profile override for the Off-Screen Auto-Reload mode (mouse side):
        /// null/-1 = follow Options > Gestures global setting, false = manual (Trigger),
        /// true = automatic (send one reload when going off-screen without button press).
        /// FR: [V55] Override par profil du mode Auto Off-Screen Reload (côté souris) :
        /// null/-1 = suivre le global Options > Gestures, false = manuel (Trigger),
        /// true = automatique (envoi recharge à la sortie de l'écran sans bouton).
        /// </summary>
        public int OffScreenAutoOverride { get; set; } = -1; // -1=global, 0=Trigger(manual), 1=Auto

        // ========== [V55y] Reload rumble override (EN/FR: Override vibration rechargement) ==========

        /// <summary>
        /// EN: [V55y] Per-profile override of the global Reload Rumble option:
        /// -1 = follow Options > Gestures (default), 0 = force OFF, 1 = force ON.
        /// FR: [V55y] Override par profil de l'option globale vibration rechargement :
        /// -1 = suivre Options > Gestures (défaut), 0 = forcer DÉSACTIVÉ, 1 = forcer ACTIVÉ.
        /// </summary>
        public int ReloadRumbleOverride { get; set; } = -1;

        /// <summary>
        /// EN: [V55y] Per-profile intensity override (0-100): -1 = follow the global setting.
        /// FR: [V55y] Override d'intensité par profil (0-100) : -1 = suivre le réglage global.
        /// </summary>
        public int ReloadRumbleIntensityOverride { get; set; } = -1;

        /// <summary>
        /// EN: [V55y] Per-profile style override: -1 = global, 0 = CriqueClique (mechanical),
        /// 1 = Court (single pulse), 2 = Long (continuous).
        /// FR: [V55y] Override de style par profil : -1 = global, 0 = Crique-Clique (mécanique),
        /// 1 = Court (impulsion unique), 2 = Long (continu).
        /// </summary>
        public int ReloadRumbleStyleOverride { get; set; } = -1;

        /// <summary>
        /// EN: TC Cover auto-reload (Time Crisis): while aiming OFF-screen, HOLD the TC
        /// button's action (cover/hide); aiming back ON-screen releases it. Inhibits the
        /// two global off-screen reload functions for this profile.
        /// FR: Planque TC (Time Crisis) : en visant HORS écran, MAINTENIR l'action du
        /// bouton TC (planque) ; viser à nouveau l'écran la relâche. Inhibe les deux
        /// fonctions globales de rechargement hors-écran pour ce profil.
        /// </summary>
        public bool TCCoverReload { get; set; } = false;

        /// <summary>
        /// EN: Physical Wiimote/Nunchuk button used by the TC cover action:
        /// "auto" = right-click mapping (existing behavior), or WiiA/WiiB/WiiOne/WiiTwo/
        /// WiiPlus/WiiMinus/NunC/NunZ.
        /// FR: Bouton physique Wiimote/Nunchuk utilisé par la planque TC :
        /// "auto" = mapping clic droit (comportement existant), ou WiiA/WiiB/WiiOne/
        /// WiiTwo/WiiPlus/WiiMinus/NunC/NunZ.
        /// </summary>
        public string TCCoverButton { get; set; } = "auto";

        /// <summary>
        /// EN: [V55] TC Bi-directional Pedal mode (TC3/TC4/TC5): Two pedals configured for movement/cover.
        /// FR: [V55] Mode pédale TC bi-directionnelle (TC3/TC4/TC5) : Deux pédales configurées.
        /// </summary>
        public bool TCBiPedal { get; set; } = false;

        /// <summary>
        /// EN: [V55] Physical Wiimote/Nunchuk button used as the LEFT TC pedal (default: WiiLeft).
        /// FR: [V55] Bouton physique Wiimote/Nunchuk utilisé comme pédale TC GAUCHE (défaut: DPad Gauche).
        /// </summary>
        public string TCBiPedalLeftButton { get; set; } = "WiiLeft";

        /// <summary>
        /// EN: [V55] Physical Wiimote/Nunchuk button used as the RIGHT TC pedal (default: WiiRight).
        /// FR: [V55] Bouton physique Wiimote/Nunchuk utilisé comme pédale TC DROITE (défaut: DPad Droit).
        /// </summary>
        public string TCBiPedalRightButton { get; set; } = "WiiRight";
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

            // [V54] Off-Screen Reload / TC Cover (EN/FR: Rechargement hors-écran / planque TC)
            OffScreenReloadOverride = source.OffScreenReloadOverride;
            OffScreenAutoOverride = source.OffScreenAutoOverride;
            TCCoverReload = source.TCCoverReload;
            TCCoverButton = source.TCCoverButton;
            TCBiPedal = source.TCBiPedal;
            TCBiPedalLeftButton = source.TCBiPedalLeftButton;
            TCBiPedalRightButton = source.TCBiPedalRightButton;

            // [V55y] Reload rumble overrides (EN/FR: Overrides vibration rechargement)
            ReloadRumbleOverride = source.ReloadRumbleOverride;
            ReloadRumbleIntensityOverride = source.ReloadRumbleIntensityOverride;
            ReloadRumbleStyleOverride = source.ReloadRumbleStyleOverride;

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
