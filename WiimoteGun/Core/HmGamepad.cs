using System;
using System.Runtime.InteropServices;
using System.Threading;
using WiimoteGun.Core;

namespace WiimoteGun
{
    /// <summary>
    /// [V57g] EN: Virtual gamepad backed by the UMDF2/HIDMaestro host (HmHost), for the
    ///     RawInputUmdf input mode. The device is created on demand by HmHost
    ///     (ACTIVATE_GP:<p>:<DINPUT|XINPUT>) and disposed by DEACTIVATE_GP, exactly like
    ///     the rawinput mouse lifecycle: it exists ONLY while the wiimote is in GamePad
    ///     mode. DInput reuses the vmulti Joystick report semantics verbatim (report
    ///     0x06: Throttle, signed X/Y, neutral Hat, unsigned Rx/Ry, 16 buttons with the
    ///     DPad mirrored to bits 12..15) so existing DuckStation/PCSX2 guncon2 bindings
    ///     and Dolphin device names keep working. XInput uses the Xbox 360 wired profile
    ///     (XUSB companion: invisible in DInput, full ViGEm Xbox360 parity) through
    ///     HMGamepadState normalized [0..1] values.
    ///     FR: Manette virtuelle adossée à l'hôte UMDF2/HIDMaestro (HmHost), pour le mode
    ///     d'entrée RawInputUmdf. Le device est créé à la demande par HmHost
    ///     (ACTIVATE_GP:<p>:<DINPUT|XINPUT>) et supprimé par DEACTIVATE_GP, exactement
    ///     comme le cycle de vie rawinput souris : il n'existe QUE tant que la wiimote
    ///     est en mode GamePad. DInput reprend la sémantique du rapport Joystick vmulti
    ///     verbatim (rapport 0x06 : Throttle, X/Y signés, Hat neutre, Rx/Ry non signés,
    ///     16 boutons avec le DPad reflété sur les bits 12..15) pour que les bindings
    ///     guncon2 DuckStation/PCSX2 existants et les noms de device Dolphin continuent
    ///     de fonctionner. XInput utilise le profil Xbox 360 filaire (companion XUSB :
    ///     invisible en DInput, parité ViGEm Xbox360 complète) via les valeurs
    ///     normalisées [0..1] de HMGamepadState.
    /// </summary>
    public class HmGamepad : IVirtualGamepad
    {
        private readonly int _playerIndex;
        private readonly bool _useXInput;
        private bool _isConnected;
        private bool _disposed;

        /// <summary>EN: The output API this instance drives (DInput vmulti report vs XInput/XUSB).
        /// FR: L'API de sortie pilotée par cette instance (rapport vmulti DInput vs XInput/XUSB).</summary>
        public bool UsesXInput => _useXInput;

        /// <summary>[V57g] EN: XInput slot (0..3) detected at connection, -1 unknown.
        ///     XInput slots follow the CREATION order of the XUSB companions, not the
        ///     player number, so EmulatorProfileAutomator must read this per player.
        ///     FR: Slot XInput (0..3) détecté à la connexion, -1 inconnu. Les slots
        ///     XInput suivent l'ordre de CRÉATION des companions XUSB, pas le numéro
        ///     de joueur : EmulatorProfileAutomator doit le lire par joueur.</summary>
        public int LastXInputSlot { get; private set; } = -1;

        // ---- DInput state (byte-for-byte mirror of VMultiGamepadReport, report 0x06)
        //      (EN/FR: État DInput (reflet octet pour octet de VMultiGamepadReport))
        private byte _dThrottle;          // 0..255
        private sbyte _dX;                // -127..127 (center 0)
        private sbyte _dY;                // -127..127
        private byte _dHat;               // vmulti quirk: always neutral (8)
        private byte _dRx;                // 0..255 (center 128)
        private byte _dRy;                // 0..255 (center 128)
        private ushort _dButtons;         // bits 0..11 = buttons 1..12, bits 12..15 = DPad

        // ---- XInput state (wire 0..65535, center 32768, Y already in XInput convention)
        //      (EN/FR: État XInput (fil 0..65535, centre 32768, Y déjà en convention XInput))
        private int _xLx, _xLy, _xRx, _xRy; // 0..65535
        private int _xLt, _xRt;             // 0..65535 (0 = released)
        private int _xButtons;              // HMButton bitmask
        private bool _xUp, _xDown, _xLeft, _xRight; // DPad -> HMHat octant
        private byte _xThrottle;            // 0..255 -> RT analog (ViGEm parity)

        public int PlayerIndex => _playerIndex;
        public bool IsConnected => _isConnected && !_disposed;

        public HmGamepad(int playerIndex, bool useXInput)
        {
            _playerIndex = playerIndex;
            _useXInput = useXInput;
            ResetState();
            SimpleLogger.Instance.Info($"[HmGamepad] P{playerIndex}: created ({(useXInput ? "XInput / Xbox 360 wired XUSB" : "DInput / vmulti Joystick 0x06")}) - device lifecycle owned by HmHost.");
        }

        // ------------------------------------------------------------- lifecycle

        public bool Connect()
        {
            if (_disposed) return false;
            if (_isConnected) return true;

            // EN: Fire the creation command (service -> HmHost ACTIVATE_GP), then poll the
            //     host STATUS until it reports this player's gamepad live. Device creation
            //     takes ~0.7-1 s; the poll replaces a blind sleep and keeps working with
            //     the frame watchdog once connected.
            //     FR: Envoie la commande de création (service -> HmHost ACTIVATE_GP), puis
            //     sondage du STATUS de l'hôte jusqu'à ce qu'il rapporte le gamepad du
            //     joueur vivant. La création prend ~0,7-1 s ; le sondage remplace un sleep
            //     aveugle et fonctionne avec le watchdog de frames une fois connecté.
            ServiceClient.EnableGamepad(_playerIndex, _useXInput ? "XINPUT" : "DINPUT");
            bool live = WaitForDeviceLive(attempts: 20, delayMs: 150);

            if (_useXInput)
            {
                // EN: Probe while the game loop is still silent (UpdateGamePadState
                //     early-returns until IsConnected): exclusive control of the device.
                //     FR: Sonde pendant que la boucle de jeu est encore silencieuse
                //     (UpdateGamePadState sort tôt tant que IsConnected est faux) :
                //     contrôle exclusif du device.
                DetectXInputSlot();
            }

            _isConnected = true;
            SimpleLogger.Instance.Info($"[HmGamepad] P{_playerIndex}: connected ({(_useXInput ? "XInput slot " + LastXInputSlot : "DInput")}) - device reported live: {live}.");
            if (!live)
                SimpleLogger.Instance.Warning($"[HmGamepad] P{_playerIndex}: HmHost did not report the gamepad device in time - frames will flow as soon as it appears.");
            return true;
        }

        public void Disconnect()
        {
            if (!_isConnected) return;
            ResetAll(); // EN/FR: neutral frame before going silent (FR: frame neutre avant silence)
            _isConnected = false;
            SimpleLogger.Instance.Info($"[HmGamepad] P{_playerIndex}: disconnected (device still owned by HmHost).");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try { ResetAll(); } catch { }
            _isConnected = false;
            // EN: Ask HmHost to dispose the device (wiimote left GamePad mode / controller
            //     teardown). The identity key keeps the paths stable for the next life.
            //     FR: Demande à HmHost de supprimer le device (la wiimote a quitté le mode
            //     GamePad / démontage du contrôleur). La clé d'identité conserve les
            //     chemins stables pour la prochaine vie.
            ServiceClient.RemoveGamepad(_playerIndex);
            LastXInputSlot = -1;
        }

        // ------------------------------------------------------------- state input

        public void SetButton(GamePadButton button, bool pressed)
        {
            if (_useXInput) SetButtonX(button, pressed);
            else SetButtonD(button, pressed);
        }

        public void SetAxis(GamePadAxis axis, float x, float y)
        {
            if (_useXInput) SetAxisX(axis, x, y);
            else SetAxisD(axis, x, y);
        }

        public byte Throttle
        {
            get { return _useXInput ? _xThrottle : _dThrottle; }
            set
            {
                byte v = Math.Max((byte)0, Math.Min((byte)255, value));
                if (_useXInput)
                {
                    // EN: ViGEm parity - Throttle drives the Right Trigger analog value
                    //     FR: Parité ViGEm - Throttle pilote la valeur analogique RT
                    _xThrottle = v;
                    _xRt = v * 257; // 0..255 -> 0..65535
                }
                else
                {
                    _dThrottle = v;
                }
            }
        }

        public bool SendReport()
        {
            if (_disposed) return false;
            if (_useXInput)
            {
                HmFrameClient.SendGamepadXInput(_playerIndex, _xLx, _xLy, _xRx, _xRy, _xLt, _xRt, ComputeXHat(), _xButtons);
            }
            else
            {
                HmFrameClient.SendGamepadRaw(_playerIndex, _dThrottle, _dX, _dY, _dHat, _dRx, _dRy, _dButtons);
            }
            return true;
        }

        public bool ResetAll()
        {
            ResetState();
            return SendReport();
        }

        // ------------------------------------------------------------- DInput (vmulti)

        // EN: Mirrors VMultiGamepadReport.SetButton exactly: the DPad is mirrored to
        //     button bits 12..15 (the Hat is deliberately kept neutral - vmulti quirk,
        //     the app disabled the hat to fix "All Up" conflicts), buttons 1..12 map
        //     to bits 0..11.
        //     FR: Reflète exactement VMultiGamepadReport.SetButton : le DPad est
        //     reflété sur les bits boutons 12..15 (le Hat reste volontairement neutre -
        //     particularité vmulti, l'app a désactivé le hat pour corriger les conflits
        //     « All Up »), les boutons 1..12 mappent sur les bits 0..11.
        private void SetButtonD(GamePadButton button, bool pressed)
        {
            ushort flag = 0;
            switch (button)
            {
                case GamePadButton.Button1: flag = 0x01; break;   // A
                case GamePadButton.Button2: flag = 0x02; break;   // B
                case GamePadButton.Button3: flag = 0x04; break;   // X
                case GamePadButton.Button4: flag = 0x08; break;   // Y
                case GamePadButton.Button5: flag = 0x10; break;   // LB
                case GamePadButton.Button6: flag = 0x20; break;   // RB
                case GamePadButton.Button7: flag = 0x40; break;   // LT (Digital)
                case GamePadButton.Button8: flag = 0x80; break;   // RT (Digital)
                case GamePadButton.Button9: flag = 0x100; break;  // Back
                case GamePadButton.Button10: flag = 0x200; break; // Start
                case GamePadButton.Button11: flag = 0x400; break; // LS Click
                case GamePadButton.Button12: flag = 0x800; break; // RS Click
                case GamePadButton.DPadUp: flag = 0x1000; break;
                case GamePadButton.DPadDown: flag = 0x2000; break;
                case GamePadButton.DPadLeft: flag = 0x4000; break;
                case GamePadButton.DPadRight: flag = 0x8000; break;
            }
            if (flag != 0)
            {
                if (pressed) _dButtons |= flag;
                else _dButtons &= (ushort)~flag;
            }
        }

        // EN: Mirrors VMultiGamepadReport.SetAxis: LeftStick signed -127..127,
        //     RightStick unsigned 0..255 (center 128).
        //     FR: Reflète VMultiGamepadReport.SetAxis : stick gauche signé -127..127,
        //     stick droit non signé 0..255 (centre 128).
        private void SetAxisD(GamePadAxis axis, float x, float y)
        {
            switch (axis)
            {
                case GamePadAxis.LeftStick:
                    _dX = ClampConvertSigned(x);
                    _dY = ClampConvertSigned(y);
                    break;
                case GamePadAxis.RightStick:
                    _dRx = ClampConvertUnsigned(x);
                    _dRy = ClampConvertUnsigned(y);
                    break;
            }
        }

        // ------------------------------------------------------------- XInput (XUSB)

        // EN: HMButton bitmask (the SDK packs the GIP buffer + native report). Buttons
        //     7/8 are the triggers as DIGITAL pulls (ViGEm slider parity, analog
        //     releases are restored by the next game loop report). The DPad goes to the
        //     HMHat octant (the only path the GIP buffer carries for XInput consumers).
        //     FR: Masque HMButton (le SDK packe le buffer GIP + le rapport natif). Les
        //     boutons 7/8 sont les gâchettes en appuis NUMÉRIQUES (parité slider ViGEm,
        //     les relâchements analogiques reviennent au prochain rapport de la boucle
        //     de jeu). Le DPad part sur l'octant HMHat (le seul chemin que le buffer GIP
        //     transporte pour les consommateurs XInput).
        private void SetButtonX(GamePadButton button, bool pressed)
        {
            switch (button)
            {
                case GamePadButton.Button1: SetXButtonFlag(0x1, pressed); break;     // A
                case GamePadButton.Button2: SetXButtonFlag(0x2, pressed); break;     // B
                case GamePadButton.Button3: SetXButtonFlag(0x4, pressed); break;     // X
                case GamePadButton.Button4: SetXButtonFlag(0x8, pressed); break;     // Y
                case GamePadButton.Button5: SetXButtonFlag(0x10, pressed); break;    // LB
                case GamePadButton.Button6: SetXButtonFlag(0x20, pressed); break;    // RB
                case GamePadButton.Button7: _xLt = pressed ? 65535 : 0; break;       // LT analog
                case GamePadButton.Button8: _xRt = pressed ? 65535 : 0; break;       // RT analog
                case GamePadButton.Button9: SetXButtonFlag(0x40, pressed); break;    // Back
                case GamePadButton.Button10: SetXButtonFlag(0x80, pressed); break;   // Start
                case GamePadButton.Button11: SetXButtonFlag(0x100, pressed); break;  // L3
                case GamePadButton.Button12: SetXButtonFlag(0x200, pressed); break;  // R3
                case GamePadButton.DPadUp: _xUp = pressed; break;
                case GamePadButton.DPadDown: _xDown = pressed; break;
                case GamePadButton.DPadLeft: _xLeft = pressed; break;
                case GamePadButton.DPadRight: _xRight = pressed; break;
            }
        }

        private void SetXButtonFlag(int flag, bool pressed)
        {
            if (pressed) _xButtons |= flag;
            else _xButtons &= ~flag;
        }

        // EN: Wire values are 0..65535 with 32768 = center. Y is inverted exactly like
        //     ViGEmGamepad (internal convention: negative = up; XInput: positive = up).
        //     FR: Les valeurs fil font 0..65535 avec 32768 = centre. Y est inversé
        //     exactement comme ViGEmGamepad (convention interne : négatif = haut ;
        //     XInput : positif = haut).
        private void SetAxisX(GamePadAxis axis, float x, float y)
        {
            switch (axis)
            {
                case GamePadAxis.LeftStick:
                    _xLx = FloatToWire(x);
                    _xLy = FloatToWire(-y); // EN/FR: invert Y (ViGEm parity)
                    break;
                case GamePadAxis.RightStick:
                    _xRx = FloatToWire(x);
                    _xRy = FloatToWire(-y);
                    break;
            }
        }

        private static int FloatToWire(float v)
        {
            v = Math.Max(-1.0f, Math.Min(1.0f, v));
            return Math.Max(0, Math.Min(65535, 32768 + (int)Math.Round(v * 32767.0)));
        }

        // EN: Exact copies of VMultiGamepadReport's axis converters so the DInput
        //     encoding stays bit-identical to the vmulti path.
        //     FR: Copies exactes des convertisseurs d'axes de VMultiGamepadReport pour
        //     que l'encodage DInput reste identique au bit près au chemin vmulti.
        private static byte ClampConvertUnsigned(float val)
        {
            // Clamp -1.0 to 1.0 -> 0..255 (center 128)
            // (EN/FR: Borner -1.0..1.0 -> 0..255, centre 128)
            val = Math.Max(-1.0f, Math.Min(1.0f, val));
            float scaled = (val + 1.0f) * 127.5f;
            return (byte)Math.Max(0, Math.Min(255, (int)Math.Round(scaled)));
        }

        private static sbyte ClampConvertSigned(float val)
        {
            // Clamp -1.0 to 1.0 -> -127..127 (center 0)
            // (EN/FR: Borner -1.0..1.0 -> -127..127, centre 0)
            val = Math.Max(-1.0f, Math.Min(1.0f, val));
            float scaled = val * 127.0f;
            return (sbyte)Math.Round(scaled);
        }

        // EN: HMHat octant: None=0, North=1, clockwise to NorthWest=8.
        //     FR: Octant HMHat : 0 neutre, 1 nord, sens horaire jusqu'à 8 nord-ouest.
        private int ComputeXHat()
        {
            if (_xUp) return _xRight ? 2 : (_xLeft ? 8 : 1);
            if (_xDown) return _xRight ? 4 : (_xLeft ? 6 : 5);
            if (_xLeft) return 7;
            if (_xRight) return 3;
            return 0;
        }

        private void ResetState()
        {
            // DInput neutral (vmulti semantics)
            _dThrottle = 0;
            _dX = 0; _dY = 0;
            _dHat = 8; // vmulti HatNeutral
            _dRx = 128; _dRy = 128;
            _dButtons = 0;
            // XInput neutral
            _xLx = 32768; _xLy = 32768; _xRx = 32768; _xRy = 32768;
            _xLt = 0; _xRt = 0;
            _xButtons = 0;
            _xUp = _xDown = _xLeft = _xRight = false;
            _xThrottle = 0;
        }

        // ------------------------------------------------------------- connection helpers

        // EN: Poll the service/HmHost STATUS ("OK ACTIVE GP=1D,2X,3-,4-") until this
        //     player's gamepad shows live with the requested api. Returns false after
        //     the attempt budget (old service without GP map, HmHost slow to start...).
        //     FR: Sondage du STATUS service/HmHost (« OK ACTIVE GP=1D,2X,3-,4- ») jusqu'à
        //     ce que le gamepad du joueur apparaisse vivant avec l'api demandée.
        //     Renvoie faux après le budget de tentatives (vieux service sans carte GP,
        //     HmHost lent à démarrer...).
        private bool WaitForDeviceLive(int attempts, int delayMs)
        {
            string want = _playerIndex.ToString() + (_useXInput ? "X" : "D");
            for (int i = 0; i < attempts; i++)
            {
                Thread.Sleep(delayMs);
                try
                {
                    string s = ServiceClient.HmStatusRaw();
                    if (s == null) continue;
                    int idx = s.IndexOf("GP=", StringComparison.Ordinal);
                    if (idx < 0) continue;
                    foreach (string entry in s.Substring(idx + 3).Split(','))
                    {
                        if (entry.Trim() == want) return true;
                    }
                }
                catch { }
            }
            return false;
        }

        // ── [V57g] XInput slot detection (mark-and-poll) ─────────────────────────
        // EN: XInput slots follow the XUSB companion CREATION order (0..3 contiguous),
        //     NOT the player number: P3 alone lands in slot 0. The emulator profile
        //     automation therefore needs the real slot per player. The probe submits a
        //     state that cannot come from gameplay (A pressed + left stick in this
        //     player's private LX window) and polls XInputGetState; because the probe
        //     runs BEFORE IsConnected turns true, the game loop is silent and the probe
        //     owns the device exclusively. The per-player LX window makes simultaneous
        //     multi-player probes unambiguous.
        //     FR: Les slots XInput suivent l'ordre de CRÉATION des companions XUSB (0..3
        //     contigus), PAS le numéro de joueur : P3 seul atterrit en slot 0.
        //     L'automatisation des profils émulateurs a donc besoin du vrai slot par
        //     joueur. La sonde soumet un état impossible en jeu (A pressé + stick gauche
        //     dans la fenêtre LX privée du joueur) et sondage XInputGetState ; comme la
        //     sonde s'exécute AVANT qu'IsConnected passe à vrai, la boucle de jeu est
        //     silencieuse et la sonde possède le device exclusivement. La fenêtre LX
        //     par joueur rend les sondages multi-joueurs simultanés non ambigus.

        [StructLayout(LayoutKind.Sequential)]
        private struct XINPUT_GAMEPAD
        {
            public short wButtons;
            public byte bLeftTrigger;
            public byte bRightTrigger;
            public short sThumbLX;
            public short sThumbLY;
            public short sThumbRX;
            public short sThumbRY;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct XINPUT_STATE
        {
            public uint dwPacketNumber;
            public XINPUT_GAMEPAD Gamepad;
        }

        [DllImport("xinput1_4.dll")]
        private static extern uint XInputGetState(uint dwUserIndex, out XINPUT_STATE pState);

        private const int XINPUT_GAMEPAD_A = 0x1000;

        public void DetectXInputSlot()
        {
            LastXInputSlot = -1;
            if (!_useXInput) return;
            try
            {
                // Per-player private probe window on the left-stick X axis:
                // P1 = -32768, P2 = -16384, P3 = 0, P4 = +16384 (wire center 32768).
                // (EN/FR: Fenêtre de sonde privée par joueur sur l'axe X du stick gauche.)
                float probeX = (_playerIndex - 1) * -0.5f - 0.5f; // P1: -1.0, P2: -0.5, P3: 0.0, P4: +0.5
                SetAxis(GamePadAxis.LeftStick, probeX, 1f); // Y+ (internal) = stick fully DOWN
                SetButton(GamePadButton.Button1, true);    // A pressed
                SendReport();

                for (int poll = 0; poll < 60 && LastXInputSlot < 0; poll++)
                {
                    Thread.Sleep(10);
                    for (uint slot = 0; slot < 4; slot++)
                    {
                        XINPUT_STATE st;
                        if (XInputGetState(slot, out st) != 0) continue; // ERROR_SUCCESS = 0
                        if ((st.Gamepad.wButtons & XINPUT_GAMEPAD_A) == 0) continue;
                        int lx = st.Gamepad.sThumbLX;
                        if (ProbeWindowContains(lx))
                        {
                            LastXInputSlot = (int)slot;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Warning($"[HmGamepad] P{_playerIndex}: XInput slot probe failed: {ex.Message}");
            }
            finally
            {
                ResetAll(); // EN/FR: always restore a neutral state (FR: toujours restaurer un état neutre)
            }

            if (LastXInputSlot >= 0)
                SimpleLogger.Instance.Info($"[HmGamepad] P{_playerIndex}: XInput slot detected: {LastXInputSlot}.");
            else
                SimpleLogger.Instance.Warning($"[HmGamepad] P{_playerIndex}: XInput slot NOT detected (device late? old XInput consumers?) - emulator profiles stay uninhibited for this player.");
        }

        // EN: Match the probe's own LX window (probe wire value +/- ~6000 raw units).
        //     FR: Correspondance dans la fenêtre LX propre à la sonde (valeur fil +/- ~6000 unités brutes).
        private bool ProbeWindowContains(int rawLx)
        {
            // Raw XInput short = wire 0..65535 mapped to -32768..32767.
            // (EN/FR: Le short XInput brut = fil 0..65535 projeté sur -32768..32767.)
            float probeX = (_playerIndex - 1) * -0.5f - 0.5f;
            int probeWire = FloatToWire(probeX);
            int probeRaw = probeWire - 32768;
            return Math.Abs(rawLx - probeRaw) < 6000;
        }
    }
}
