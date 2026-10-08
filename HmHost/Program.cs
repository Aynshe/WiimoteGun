using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using HIDMaestro;

// ============================================================================
// HmHost - [V57d] Virtual HID host (UMDF2/HIDMaestro) for Wiimote4Guns.
// EN: Console host spawned by WiimoteGun.Service (SYSTEM, no UAC). Owns the
//     HIDMaestro driver lifecycle and the 4 virtual devices (durable identity
//     keys P1..P4). The WiimoteGun app sends input frames on the named pipe.
// FR: Hôte console lancé par WiimoteGun.Service (SYSTEM, sans UAC). Possède le
//     cycle de vie du pilote HIDMaestro et les 4 périphériques virtuels
//     (identités durables P1..P4). L'app WiimoteGun envoie ses frames d'input
//     via le pipe nommé.
//
// Protocol (one command per line, replies are single lines):
//   PING | ACTIVATE | DEACTIVATE | STATUS | QUIT          -> "OK ..." | "ERR ..."
//   MOUSE:<p>:<x>:<y>:<btn>:<wheel>     absolute (x/y 0..32767)   fire-and-forget
//   MOUSER:<p>:<dx>:<dy>:<btn>:<wheel>  relative (sbyte)           fire-and-forget
//   KEYS:<p>:<mods>:<k0,k1,...>         HID codes (max 6)         fire-and-forget
//   [V57g] GamePad devices (created ONLY while the wiimote is in GamePad mode):
//   ACTIVATE_GP:<p>:<DINPUT|XINPUT>     create/swap the player's gamepad device
//   DEACTIVATE_GP:<p>                   dispose the player's gamepad device
//   GP:<p>:<throttle>:<x>:<y>:<hat>:<rx>:<ry>:<buttons16>
//        vmulti Joystick report 0x06 (x/y signed -127..127, rx/ry 0..255,
//        hat 0..8, buttons 0..65535 with DPad in bits 12-15)  fire-and-forget
//   GPX:<p>:<lx>:<ly>:<rx>:<ry>:<lt>:<rt>:<hat>:<buttons>
//        XInput state (axes/triggers 0..65535, center 32768; hat = HMHat
//        octant 0..8; buttons = HMButton bitmask)             fire-and-forget
// ============================================================================

internal static class Program
{
    private const string PipeName = "WiimoteGunHmHost";
    // [V57d] EN: One profile per player with a UNIQUE VID (the exact vmulti VIDs
    //     001F/002F/003F/004F), so every machine identifies P1..P4 the same way
    //     (hardware IDs like HID\VID_001F&UP:0001_U:0002 - RetroBat/DemulShooter
    //     auto-config compatibility, stable across reinstalls and machines).
    //     FR: Un profil par joueur avec un VID UNIQUE (les VID vmulti exacts
    //     001F/002F/003F/004F), pour que chaque machine identifie P1..P4 pareil
    //     (hardware IDs du type HID\VID_001F&UP:0001_U:0002 - compatibilité
    //     autoconfig RetroBat/DemulShooter, stable entre installs et machines).
    private static readonly string[] ProfileIds =
    {
        "wiimotegun-mkb-p1", "wiimotegun-mkb-p2", "wiimotegun-mkb-p3", "wiimotegun-mkb-p4"
    };
    // [V57g] EN: GamePad profile ids - one DInput (vmulti Joystick descriptor, report
    //     0x06) and one XInput (xbox-360-wired copy: XUSB companion HMXInput.dll serves
    //     XInputGetState, invisible in DInput like real xusb22 hardware) per player.
    //     FR: Ids des profils gamepad - un DInput (descripteur Joystick vmulti, rapport
    //     0x06) et un XInput (copie xbox-360-wired : le companion XUSB HMXInput.dll sert
    //     XInputGetState, invisible en DInput comme le vrai matériel xusb22) par joueur.
    private static readonly string[] GpProfileIds =
    {
        "wiimotegun-gp-p1", "wiimotegun-gp-p2", "wiimotegun-gp-p3", "wiimotegun-gp-p4"
    };
    private static readonly string[] X360ProfileIds =
    {
        "wiimotegun-x360-p1", "wiimotegun-x360-p2", "wiimotegun-x360-p3", "wiimotegun-x360-p4"
    };
    // [V57h] EN: Keyboard-only profiles - the REMOVE_MOUSE_P parity for UMDF2: HIDE_MOUSE_P
    //     swaps the player's device mkb -> kb-only at the SAME identity key (vmulti hid
    //     COL03 but kept COL02; a single UMDF2 devnode can not drop a collection, so the
    //     whole device is re-created with a kb-only descriptor). ACTIVATE_P swaps back to
    //     the full mkb descriptor. PID 0xBA1E: the keyboard is Col01 of this device and
    //     must not match the ES lightgun path VID_001F&PID_BACC&Col01.
    //     FR: Profils clavier-seul - la parité REMOVE_MOUSE_P pour UMDF2 : HIDE_MOUSE_P
    //     échange le device du joueur mkb -> kb-seul à la MÊME clé d'identité (vmulti
    //     masquait COL03 en gardant COL02 ; un devnode UMDF2 unique ne peut pas retirer
    //     une collection, donc le device entier est recréé avec un descripteur
    //     kb-seul). ACTIVATE_P revient au descripteur mkb complet. PID 0xBA1E : le
    //     clavier est le Col01 de ce device et ne doit pas matcher le chemin lightgun
    //     ES VID_001F&PID_BACC&Col01.
    private static readonly string[] KbProfileIds =
    {
        "wiimotegun-kb-p1", "wiimotegun-kb-p2", "wiimotegun-kb-p3", "wiimotegun-kb-p4"
    };
    // [V57h] True when the player's live device carries the kb-only descriptor (mouse hidden).
    //        (EN/FR: Vrai quand le device vivant du joueur porte le descripteur kb-seul (souris masquée).)
    private static readonly bool[] _kbOnly = new bool[PlayerCount];
    // [V57g] EN: GamePad controllers are pinned to indexes 4..7 (the mkb devices
    //     auto-assign 0..3, so the ranges never collide). Pinning keeps the same
    //     creation index across DInput<->XInput swaps (SDK LiveSwap pattern), so
    //     joy.cpl ordering and the XUSB arrival order stay deterministic.
    //     FR: Les contrôleurs gamepad sont épinglés aux index 4..7 (les devices mkb
    //     s'auto-attribuent 0..3, les plages ne se chevauchent jamais). L'épinglage
    //     conserve le même index de création à travers les swaps DInput<->XInput
    //     (motif LiveSwap du SDK), donc l'ordre joy.cpl et l'ordre d'arrivée XUSB
    //     restent déterministes.
    private const int GpBaseIndex = 4;
    // [V57o] EN: GamePad identity key prefix. ROTATED from "GP" to "GPW" in V57o: the
    //     Windows PnP store caches the DeviceDesc (friendly name) per devnode instance
    //     path, and the path derives from the identity key - a user kept seeing the old
    //     "vmultia" name on his P1 HIDMaestro gamepad even after a reboot because the
    //     reused instance kept its stale registry name. A new prefix = brand-new instance
    //     paths = names re-read from the CURRENT profile. Stable across lives from now on
    //     (same key = same paths within the new family).
    //     FR: Préfixe de clé d'identité des gamepads. ROTÉ de « GP » à « GPW » en V57o :
    //     le store PnP de Windows met en cache le DeviceDesc (nom convivial) par chemin
    //     d'instance du devnode, et le chemin dérive de la clé d'identité - un utilisateur
    //     voyait encore l'ancien nom « vmultia » sur son gamepad HIDMaestro P1 même après
    //     un reboot car l'instance réutilisée gardait son nom de registre périmé. Un
    //     nouveau préfixe = chemins d'instance neufs = noms relus depuis le profil
    //     COURANT. Stable à travers les vies à partir de maintenant (même clé = mêmes
    //     chemins au sein de la nouvelle famille).
    private const string GpKeyPrefix = "GPW";
    private const string HostVersion = "3.1.0.0";
    private const int PlayerCount = 4;

    // Report ids from vmulti.h (descriptor ships in profiles\wiimotegun-mkb.json)
    private const byte RidAbsMouse = 0x03;
    private const byte RidRelMouse = 0x04;
    private const byte RidKeyboard = 0x07;
    // [V57g] vmulti Joystick report id (profiles\wiimotegun-gp-p*.json)
    private const byte RidJoystick = 0x06;

    private static readonly object _gate = new object();
    private static readonly object _logLock = new object();
    private static HMContext _ctx;
    private static readonly HMController[] _ctrl = new HMController[PlayerCount];
    private static bool _active;

    // [V57d] EN: Frame-loss watchdog. The driver only completes a pending read when the
    //     shared SeqNo CHANGES, so a report submitted while no read is armed is not
    //     redelivered - a lost key/button RELEASE then sticks forever (auto-repeat in
    //     games, "continuous input"). The watchdog re-submits each player's last frame
    //     every 150 ms while something is held (and for 1 s after any update): identical
    //     consecutive reports are no-ops for mouhid/kbdhid, but they heal any dropped
    //     transition.
    //     FR: Chien de garde anti-perte de frames. Le driver ne complète un read en
    //     attente que lorsque le SeqNo partagé CHANGE : un rapport soumis sans read armé
    //     n'est pas relivré - un RELÂCHEMENT de touche/bouton perdu reste bloqué
    //     indéfiniment (auto-repeat dans les jeux, « input en continu »). Le chien de
    //     garde re-soumet la dernière frame de chaque joueur toutes les 150 ms tant
    //     qu'une entrée est tenue (et pendant 1 s après toute mise à jour) : des
    //     rapports identiques consécutifs sont sans effet pour mouhid/kbdhid, mais ils
    //     guérissent toute transition perdue.
    private static readonly byte[][] _lastKeysFrame = new byte[PlayerCount][];
    private static readonly byte[][] _lastMouseFrame = new byte[PlayerCount][];
    private static readonly DateTime[] _lastKeysUtc = new DateTime[PlayerCount];
    private static readonly DateTime[] _lastMouseUtc = new DateTime[PlayerCount];
    private static bool _quit;
    private static string _logPath;
    private static long _frameCount;
    private static long _parseFailCount;

    // [V57g] EN: Per-player gamepad devices (created only while the wiimote is in
    //     GamePad mode; the mouse/keyboard device above is independent). The identity
    //     key "GP<n>" keeps the SAME device paths across create/dispose cycles AND
    //     across DInput<->XInput swaps (a different profile at the same key refreshes
    //     the descriptor at the same paths - SDK issue #60).
    //     FR: Devices gamepad par joueur (créés seulement tant que la wiimote est en
    //     mode GamePad ; le device souris/clavier ci-dessus est indépendant). La clé
    //     d'identité « GP<n> » conserve les MÊMES chemins à travers les cycles
    //     création/suppression ET les swaps DInput<->XInput (un profil différent à la
    //     même clé rafraîchit le descripteur aux mêmes chemins - issue #60 du SDK).
    private static readonly HMController[] _gpCtrl = new HMController[PlayerCount];
    private static readonly bool[] _gpIsXInput = new bool[PlayerCount];
    // [V57j] EN: Product string of the profile each gp device was created with - when the
    //     profile is UPDATED (e.g. a device rename in an app update), the old device keeps
    //     the name frozen at creation while the current profile carries the new one; the
    //     ghost name then sticks in DirectInput/Dolphin until the device is recreated.
    //     ActivateGamepad compares and recreates when they differ.
    //     FR: Product string du profil avec lequel chaque device gp a été créé - quand le
    //     profil est MIS À JOUR (ex. un renommage de device dans une mise à jour de
    //     l'app), le vieux device garde le nom figé à la création alors que le profil
    //     courant porte le nouveau nom ; le nom fantôme colle alors dans
    //     DirectInput/Dolphin tant que le device n'est pas recréé. ActivateGamepad
    //     compare et recrée quand ils diffèrent.
    private static readonly string[] _gpProfileNames = new string[PlayerCount];
    // DInput path: last 9-byte vmulti report (id 0x06 + payload) for the watchdog.
    private static readonly byte[][] _lastGpRawFrame = new byte[PlayerCount][];
    // XInput path: last HMGamepadState (Axes dict shared with _gpAxes) + axis keys
    // resolved once per player at creation ([lx, ly, rx, ry, lt, rt]).
    private static readonly HMGamepadState[] _lastGpState = new HMGamepadState[PlayerCount];
    private static readonly HMAxis[][] _gpAxisKeys = new HMAxis[PlayerCount][];
    private static readonly Dictionary<HMAxis, float>[] _gpAxes = new Dictionary<HMAxis, float>[PlayerCount];
    private static readonly bool[] _gpHeld = new bool[PlayerCount];
    private static readonly DateTime[] _lastGpUtc = new DateTime[PlayerCount];

    private static int Main(string[] args)
    {
        _logPath = Path.Combine(AppContext.BaseDirectory, "HmHost.log");

        // EN: Single instance guard (the service is the only legitimate spawner)
        // FR: Garde à instance unique (le service est le seul lanceur légitime)
        bool createdNew;
        using (var mutex = new Mutex(true, "Global\\WiimoteGunHmHost", out createdNew))
        {
            if (!createdNew)
            {
                Log("Another HmHost instance is already running - exiting.");
                return 3;
            }

            Log("=".PadRight(78, '='));
            Log($"HmHost {HostVersion} starting (pid={Environment.ProcessId}, elevated={IsElevated()}).");

            // [V57d] EN: Frame-loss watchdog (see the field comment above)
            //     FR: Chien de garde anti-perte de frames (voir le commentaire des champs)
            var watchdogThread = new Thread(WatchdogLoop) { IsBackground = true, Name = "HmWatchdog" };
            watchdogThread.Start();

            try
            {
                RunPipeLoop();
            }
            catch (Exception ex)
            {
                Log($"FATAL: {ex}");
                CleanupAll();
                return 1;
            }

            Log("HmHost exited cleanly.");
            return 0;
        }
    }

    // ------------------------------------------------------------- pipe loop

    private static void RunPipeLoop()
    {
        while (!_quit)
        {
            NamedPipeServerStream server;
            try
            {
                server = CreatePipe();
            }
            catch (Exception ex)
            {
                // EN: Never die on a pipe creation failure - log, back off, retry
                // FR: Ne jamais mourir sur un échec de création de pipe - log, pause, réessai
                Log("Pipe creation failed (retry in 2s): " + ex.Message);
                Thread.Sleep(2000);
                continue;
            }

            try
            {
                server.WaitForConnection();
            }
            catch (Exception ex)
            {
                Log("WaitForConnection failed: " + ex.Message);
                try { server.Dispose(); } catch { }
                Thread.Sleep(1000);
                continue;
            }

            // EN: Handle each connection on its own thread (the app keeps ONE
            //     persistent connection for frames, the service connects for lifecycle)
            // FR: Une connexion = un thread (l'app garde UNE connexion persistante
            //     pour les frames, le service se connecte pour le cycle de vie)
            var t = new Thread(() => HandleConnection(server)) { IsBackground = true };
            t.Start();
        }
        CleanupAll();
    }

    private static NamedPipeServerStream CreatePipe()
    {
        // [V57d] EN: SECURITY FIX (first-run crash root cause). Creating server instances
        //     #2..#16 requires FILE_CREATE_PIPE_INSTANCE on the EXISTING pipe object, which
        //     ReadWrite does NOT include - the 2nd instance creation was denied even for
        //     SYSTEM, killing the host right after the first PING. SYSTEM and Administrators
        //     therefore get FullControl (they own the instances), while Authenticated Users
        //     keep ReadWrite (the non-elevated app connects and sends frames, but can never
        //     spawn its own pipe instances).
        //     FR: CORRECTIF SÉCURITÉ (cause racine du crash du premier lancement). Créer
        //     les instances serveur #2..#16 exige FILE_CREATE_PIPE_INSTANCE sur l'objet pipe
        //     EXISTANT, ce que ReadWrite n'inclut PAS - la création de la 2e instance était
        //     refusée même pour SYSTEM, tuant l'hôte juste après le premier PING. SYSTEM et
        //     les Administrateurs reçoivent donc FullControl (ils possèdent les instances),
        //     tandis que les utilisateurs authentifiés gardent ReadWrite (l'app non élevée
        //     se connecte et envoie ses frames, mais ne peut pas créer d'instances).
        var ps = new PipeSecurity();
        ps.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
            PipeAccessRights.FullControl, AccessControlType.Allow));
        ps.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
            PipeAccessRights.FullControl, AccessControlType.Allow));
        ps.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null),
            PipeAccessRights.ReadWrite, AccessControlType.Allow));
        // EN: .NET (Core) exposes the security-enabled constructor through
        //     NamedPipeServerStreamAcl (the classic ctor ignores security)
        // FR: En .NET (Core), le constructeur avec sécurité passe par
        //     NamedPipeServerStreamAcl (le constructeur classique ignore la sécurité)
        return NamedPipeServerStreamAcl.Create(PipeName, PipeDirection.InOut, 16,
            PipeTransmissionMode.Message, PipeOptions.None, 8192, 8192, ps);
    }

    private static void HandleConnection(NamedPipeServerStream pipe)
    {
        try
        {
            using (pipe)
            {
                var sr = new StreamReader(pipe, Encoding.UTF8, false, 8192, true);
                string line;
                while (!_quit && (line = sr.ReadLine()) != null)
                {
                    string reply = ProcessLine(line.Trim());
                    if (reply != null)
                    {
                        byte[] payload = Encoding.UTF8.GetBytes(reply + "\n");
                        pipe.Write(payload, 0, payload.Length);
                        pipe.Flush();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // EN: Client disconnects are normal (fire-and-forget frame pipe)
            // FR: Les déconnexions client sont normales (pipe de frames)
            Log("Connection ended: " + ex.GetType().Name + " " + ex.Message);
        }
    }

    // ------------------------------------------------------------- protocol

    private static string ProcessLine(string line)
    {
        if (line.Length == 0)
            return null;

        try
        {
            if (line.StartsWith("MOUSE:", StringComparison.OrdinalIgnoreCase))
                return ProcessMouse(line, relative: false);
            if (line.StartsWith("MOUSER:", StringComparison.OrdinalIgnoreCase))
                return ProcessMouse(line, relative: true);
            if (line.StartsWith("KEYS:", StringComparison.OrdinalIgnoreCase))
                return ProcessKeys(line);

            // [V57g] GamePad frames (fire-and-forget) + lifecycle.
            //     FR: Frames gamepad (fire-and-forget) + cycle de vie.
            if (line.StartsWith("GPX:", StringComparison.OrdinalIgnoreCase))
                return ProcessGpX(line);
            if (line.StartsWith("GP:", StringComparison.OrdinalIgnoreCase))
                return ProcessGpRaw(line);
            if (line.StartsWith("ACTIVATE_GP:", StringComparison.OrdinalIgnoreCase))
                return ActivateGamepad(line.Substring("ACTIVATE_GP:".Length));
            if (line.StartsWith("DEACTIVATE_GP:", StringComparison.OrdinalIgnoreCase))
                return DeactivateGamepad(line.Substring("DEACTIVATE_GP:".Length));

            // [V57e] EN: Per-player device lifecycle (vmulti enable/disable parity)
            //     FR: Cycle de vie du device par joueur (parité enable/disable vmulti)
            if (line.StartsWith("ACTIVATE_P:", StringComparison.OrdinalIgnoreCase))
            {
                return TryParse(line.Substring("ACTIVATE_P:".Length), out int ap) && ap >= 1 && ap <= PlayerCount
                    ? ActivatePlayer(ap) : ParseFail(line);
            }
            // [V57h] EN: Hide the player's MOUSE while keeping the keyboard (vmulti COL03
            //     parity): swaps the live mkb device to the kb-only descriptor at the same
            //     identity key. No "HIDE_MOUSE_P_ALL"/"EXCEPT" variants: the service routes
            //     the existing REMOVE_MOUSE_ALL/EXCEPT commands to the full dispose paths.
            //     FR: Masque la SOURIS du joueur en gardant le clavier (parité COL03
            //     vmulti) : échange le device mkb vivant vers le descripteur kb-seul à la
            //     même clé d'identité. Pas de variantes « HIDE_MOUSE_P_ALL »/« EXCEPT » :
            //     le service route les commandes REMOVE_MOUSE_ALL/EXCEPT existantes vers
            //     les chemins de suppression complète.
            if (line.StartsWith("HIDE_MOUSE_P:", StringComparison.OrdinalIgnoreCase))
            {
                return TryParse(line.Substring("HIDE_MOUSE_P:".Length), out int hp) && hp >= 1 && hp <= PlayerCount
                    ? HideMouseForPlayer(hp) : ParseFail(line);
            }
            if (line.StartsWith("DEACTIVATE_P_EXCEPT:", StringComparison.OrdinalIgnoreCase))
            {
                return DeactivatePlayersExcept(line.Substring("DEACTIVATE_P_EXCEPT:".Length));
            }
            if (line.StartsWith("DEACTIVATE_P_ALL", StringComparison.OrdinalIgnoreCase))
            {
                return DeactivateAllPlayers();
            }
            if (line.StartsWith("DEACTIVATE_P:", StringComparison.OrdinalIgnoreCase))
            {
                return TryParse(line.Substring("DEACTIVATE_P:".Length), out int dp) && dp >= 1 && dp <= PlayerCount
                    ? DeactivatePlayer(dp) : ParseFail(line);
            }

            switch (line.ToUpperInvariant())
            {
                case "PING":
                    Log("PING");
                    return "PONG " + HostVersion;
                case "ACTIVATE":
                    return Activate();
                case "DEACTIVATE":
                    return Deactivate();
                case "STATUS":
                    // [V57g] Append the live gamepad map (players 1..4 as D/X/-) for
                    //     diagnostics; callers only check the "OK ACTIVE" prefix.
                    //     FR: Ajoute la carte gamepad active (joueurs 1..4 en D/X/-)
                    //     pour le diagnostic ; les appelants ne vérifient que le
                    //     préfixe « OK ACTIVE ».
                    if (!_active) return "OK INACTIVE";
                    var gpMap = new StringBuilder();
                    for (int i = 0; i < PlayerCount; i++)
                    {
                        gpMap.Append(i + 1);
                        gpMap.Append(_gpCtrl[i] == null ? '-' : (_gpIsXInput[i] ? 'X' : 'D'));
                        if (i < PlayerCount - 1) gpMap.Append(',');
                    }
                    return "OK ACTIVE GP=" + gpMap;
                case "QUIT":
                    Log("QUIT requested.");
                    string r = Deactivate();
                    _quit = true;
                    return "OK BYE " + r;
                default:
                    Log("Unknown command ignored: " + line);
                    return "ERR UNKNOWN_COMMAND";
            }
        }
        catch (Exception ex)
        {
            Log("Command error (" + line + "): " + ex.Message);
            return "ERR " + ex.Message;
        }
    }

    // MOUSE:<p>:<x>:<y>:<btn>:<wheel>  |  MOUSER:<p>:<dx>:<dy>:<btn>:<wheel>
    private static string ProcessMouse(string line, bool relative)
    {
        string[] parts = line.Split(':');
        if (parts.Length != 6 || !TryParse(parts[1], out int p) || p < 1 || p > PlayerCount)
            return ParseFail(line);

        if (!TryParse(parts[2], out int x) || !TryParse(parts[3], out int y) ||
            !TryParse(parts[4], out int btn) || !TryParse(parts[5], out int wheel))
            return ParseFail(line);

        lock (_gate)
        {
            if (!_active) return null;
            var c = _ctrl[p - 1];
            if (c == null) return null;
            // [V57h] EN: Mouse frames are dropped while the player's device carries the
            //     kb-only descriptor (mouse hidden): report 0x03 is not declared there.
            //     FR: Les frames souris sont abandonnées tant que le device du joueur porte
            //     le descripteur kb-seul (souris masquée) : le rapport 0x03 n'y est pas
            //     déclaré.
            if (_kbOnly[p - 1]) return null;

            if (relative)
            {
                sbyte dx = (sbyte)Math.Max(-127, Math.Min(127, x));
                sbyte dy = (sbyte)Math.Max(-127, Math.Min(127, y));
                sbyte w = (sbyte)Math.Max(-127, Math.Min(127, wheel));
                Span<byte> rep = stackalloc byte[5];
                rep[0] = RidRelMouse;
                rep[1] = unchecked((byte)btn);
                unchecked { rep[2] = (byte)dx; rep[3] = (byte)dy; rep[4] = (byte)w; }
                c.SubmitRawExtendedReport(rep);
            }
            else
            {
                x = Math.Max(0, Math.Min(0x7FFF, x));
                y = Math.Max(0, Math.Min(0x7FFF, y));
                Span<byte> rep = stackalloc byte[7];
                rep[0] = RidAbsMouse;
                rep[1] = unchecked((byte)btn);
                ushort ux = (ushort)x, uy = (ushort)y;
                MemoryMarshal.Write(rep.Slice(2), ref ux);
                MemoryMarshal.Write(rep.Slice(4), ref uy);
                rep[6] = unchecked((byte)wheel);
                c.SubmitRawExtendedReport(rep);
                _lastMouseFrame[p - 1] = rep.ToArray();
                _lastMouseUtc[p - 1] = DateTime.UtcNow;
            }
        }

        FrameCounted();
        return null; // fire-and-forget: no reply for frame commands
    }

    // KEYS:<p>:<mods>:<k0,k1,...> (HID codes, empty list allowed)
    private static string ProcessKeys(string line)
    {
        string[] parts = line.Split(':');
        if (parts.Length != 4 || !TryParse(parts[1], out int p) || p < 1 || p > PlayerCount)
            return ParseFail(line);

        byte mods = unchecked((byte)(TryParse(parts[2], out int m) ? m : 0));
        Span<byte> rep = stackalloc byte[9];
        rep[0] = RidKeyboard;
        rep[1] = mods;
        rep[2] = 0;
        for (int i = 3; i < 9; i++) rep[i] = 0;

        string keys = parts[3].Trim();
        if (keys.Length > 0)
        {
            string[] codes = keys.Split(',');
            for (int i = 0; i < codes.Length && i < 6; i++)
            {
                if (TryParse(codes[i], out int kc))
                    rep[3 + i] = unchecked((byte)kc);
            }
        }

        lock (_gate)
        {
            if (!_active) return null;
            var c = _ctrl[p - 1];
            if (c == null) return null;
            c.SubmitRawExtendedReport(rep);
            _lastKeysFrame[p - 1] = rep.ToArray();
            _lastKeysUtc[p - 1] = DateTime.UtcNow;
        }

        FrameCounted();
        return null;
    }

    // ----------------------------------------------------- [V57g] gamepad frames

    // GP:<p>:<throttle>:<x>:<y>:<hat>:<rx>:<ry>:<buttons16>  (vmulti Joystick report)
    // EN: The 8 payload bytes mirror VMultiGamepadReport exactly: throttle 0..255,
    //     x/y signed -127..127, hat 0..8 (8 neutral - the app keeps it neutral and
    //     mirrors the DPad to button bits 12..15, vmulti quirk), rx/ry 0..255
    //     (center 128), buttons 0..65535. Submitted verbatim as report 0x06.
    // FR: Les 8 octets de charge reflètent exactement VMultiGamepadReport : throttle
    //     0..255, x/y signés -127..127, hat 0..8 (8 neutre - l'app le garde neutre et
    //     reflète le DPad sur les bits boutons 12..15, particularité vmulti), rx/ry
    //     0..255 (centre 128), boutons 0..65535. Soumis verbatim comme rapport 0x06.
    private static string ProcessGpRaw(string line)
    {
        string[] parts = line.Split(':');
        if (parts.Length != 9 || !TryParse(parts[1], out int p) || p < 1 || p > PlayerCount)
            return ParseFail(line);

        if (!TryParse(parts[2], out int throttle) || !TryParse(parts[3], out int x) ||
            !TryParse(parts[4], out int y) || !TryParse(parts[5], out int hat) ||
            !TryParse(parts[6], out int rx) || !TryParse(parts[7], out int ry) ||
            !TryParse(parts[8], out int buttons))
            return ParseFail(line);

        throttle = Math.Max(0, Math.Min(255, throttle));
        x = Math.Max(-127, Math.Min(127, x));
        y = Math.Max(-127, Math.Min(127, y));
        hat = Math.Max(0, Math.Min(8, hat));
        rx = Math.Max(0, Math.Min(255, rx));
        ry = Math.Max(0, Math.Min(255, ry));

        lock (_gate)
        {
            if (!_active) return null;
            var c = _gpCtrl[p - 1];
            if (c == null || _gpIsXInput[p - 1]) return null; // no DInput gp device live

            Span<byte> rep = stackalloc byte[9];
            rep[0] = RidJoystick;
            rep[1] = unchecked((byte)throttle);
            rep[2] = unchecked((byte)x);
            rep[3] = unchecked((byte)y);
            rep[4] = unchecked((byte)hat);
            rep[5] = unchecked((byte)rx);
            rep[6] = unchecked((byte)ry);
            rep[7] = unchecked((byte)(buttons & 0xFF));
            rep[8] = unchecked((byte)((buttons >> 8) & 0xFF));
            c.SubmitRawExtendedReport(rep);

            _lastGpRawFrame[p - 1] = rep.ToArray();
            _gpHeld[p - 1] = buttons != 0 || throttle != 0 || x != 0 || y != 0 || rx != 128 || ry != 128;
            _lastGpUtc[p - 1] = DateTime.UtcNow;
        }

        FrameCounted();
        return null; // fire-and-forget: no reply for frame commands
    }

    // GPX:<p>:<lx>:<ly>:<rx>:<ry>:<lt>:<rt>:<hat>:<buttons>  (XInput / Xbox 360 wired)
    // EN: axes and triggers 0..65535 (center 32768 for sticks, 0 released for
    //     triggers), hat = HMHat octant 0..8 (0 none, 1 north, clockwise), buttons =
    //     HMButton bitmask. Values normalize to [0..1] and go through SubmitState,
    //     which packs the GIP buffer for the XUSB companion + the native report.
    // FR: axes et gâchettes 0..65535 (centre 32768 pour les sticks, 0 relâché pour
    //     les gâchettes), hat = octant HMHat 0..8 (0 neutre, 1 nord, sens horaire),
    //     boutons = masque HMButton. Les valeurs se normalisent en [0..1] et passent
    //     par SubmitState, qui packe le buffer GIP pour le companion XUSB + le
    //     rapport natif.
    private static string ProcessGpX(string line)
    {
        string[] parts = line.Split(':');
        if (parts.Length != 10 || !TryParse(parts[1], out int p) || p < 1 || p > PlayerCount)
            return ParseFail(line);

        if (!TryParse(parts[2], out int lx) || !TryParse(parts[3], out int ly) ||
            !TryParse(parts[4], out int rx) || !TryParse(parts[5], out int ry) ||
            !TryParse(parts[6], out int lt) || !TryParse(parts[7], out int rt) ||
            !TryParse(parts[8], out int hat) || !TryParse(parts[9], out int buttons))
            return ParseFail(line);

        const int Center = 32768;
        lx = Math.Max(0, Math.Min(65535, lx));
        ly = Math.Max(0, Math.Min(65535, ly));
        rx = Math.Max(0, Math.Min(65535, rx));
        ry = Math.Max(0, Math.Min(65535, ry));
        lt = Math.Max(0, Math.Min(65535, lt));
        rt = Math.Max(0, Math.Min(65535, rt));
        hat = Math.Max(0, Math.Min(8, hat));

        lock (_gate)
        {
            if (!_active) return null;
            var c = _gpCtrl[p - 1];
            if (c == null || !_gpIsXInput[p - 1]) return null; // no XInput gp device live

            var axes = _gpAxes[p - 1];
            var k = _gpAxisKeys[p - 1];
            if (axes == null || k == null) return null; // axes not initialized (should not happen)

            axes[k[0]] = lx / 65535f;
            axes[k[1]] = ly / 65535f;
            axes[k[2]] = rx / 65535f;
            axes[k[3]] = ry / 65535f;
            axes[k[4]] = lt / 65535f;
            axes[k[5]] = rt / 65535f;

            var st = new HMGamepadState
            {
                Axes = axes,
                Buttons = unchecked((HMButton)(uint)buttons),
                Hat = (HMHat)hat
            };
            c.SubmitState(in st);

            _lastGpState[p - 1] = st;
            _gpHeld[p - 1] = buttons != 0 || hat != 0 ||
                             lx != Center || ly != Center || rx != Center || ry != Center ||
                             lt != 0 || rt != 0;
            _lastGpUtc[p - 1] = DateTime.UtcNow;
        }

        FrameCounted();
        return null; // fire-and-forget: no reply for frame commands
    }

    // ------------------------------------------------------- lifecycle

    private static string Activate()
    {
        lock (_gate)
        {
            if (_active)
                return "OK ALREADY_ACTIVE";

            var sw = Stopwatch.StartNew();
            try
            {
                if (_ctx == null)
                {
                    _ctx = new HMContext();
                    string profilesDir = Path.Combine(AppContext.BaseDirectory, "profiles");
                    int n = _ctx.LoadProfilesFromDirectory(profilesDir);
                    Log($"Loaded {n} profile(s) from {profilesDir}");
                }

                // EN: Idempotent; first ever run creates the self-signed certificate
                //     (machine Root + TrustedPublisher), signs and installs the driver.
                // FR: Idempotent ; le tout premier lancement crée le certificat
                //     auto-signé (Root + TrustedPublisher machine), signe et installe le pilote.
                _ctx.InstallDriver();
                Log($"InstallDriver OK ({sw.ElapsedMilliseconds} ms).");

                // [V57e] EN: ACTIVATE now only readies the driver - the per-player devices
                //     are CREATED ON DEMAND by ACTIVATE_P (wiimote connects) and disposed
                //     by DEACTIVATE_P, mirroring the vmulti enable/disable flow. Emulation
                //     Station therefore never sees "phantom" lightguns for empty players.
                //     FR: ACTIVATE prépare désormais seulement le pilote - les devices par
                //     joueur sont CRÉÉS À LA DEMANDE par ACTIVATE_P (connexion wiimote)
                //     et supprimés par DEACTIVATE_P, comme le flux enable/disable vmulti.
                //     EmulationStation ne voit donc jamais de lightguns « fantômes »
                //     pour les joueurs inoccupés.
                _active = true;
                Log("ACTIVATED (driver ready; devices are created on demand per player).");
                return "OK ACTIVATED";
            }
            catch (Exception ex)
            {
                Log("Activate FAILED: " + ex);
                // EN: Best-effort cleanup of partially created controllers
                // FR: Nettoyage best-effort des contrôleurs partiellement créés
                for (int i = 0; i < PlayerCount; i++)
                {
                    try { _ctrl[i]?.Dispose(); } catch { }
                    _ctrl[i] = null;
                }
                _active = false;
                return "ERR " + ex.Message;
            }
        }
    }

    // ------------------------------------------------------- per-player lifecycle [V57e]

    // [V57e] EN: Create the player's device on demand (wiimote connected). The identity
    //     key "P<n>" keeps the SAME device paths across create/dispose cycles, exactly
    //     like the vmulti enable/disable flow.
    //     FR: Crée le device du joueur à la demande (wiimote connectée). La clé
    //     d'identité « P<n> » conserve les MÊMES chemins à travers les cycles
    //     création/suppression, exactement comme le flux enable/disable vmulti.
    private static string ActivatePlayer(int p)
    {
        lock (_gate)
        {
            if (!_active)
                return "ERR NOT_ACTIVATED";

            // [V57h] EN: If the device is live with the kb-only descriptor (mouse hidden),
            //     ACTIVATE_P restores the full mkb descriptor at the same identity key.
            //     FR: Si le device vit avec le descripteur kb-seul (souris masquée),
            //     ACTIVATE_P restaure le descripteur mkb complet à la même clé d'identité.
            if (_ctrl[p - 1] != null && _kbOnly[p - 1])
            {
                try
                {
                    _ctrl[p - 1].Dispose();
                    HMProfile profile = _ctx.GetProfile(ProfileIds[p - 1])
                        ?? throw new InvalidOperationException($"Profile '{ProfileIds[p - 1]}' not found in profiles directory.");
                    var sw = Stopwatch.StartNew();
                    _ctrl[p - 1] = _ctx.CreateController(profile, "P" + p);
                    _kbOnly[p - 1] = false;
                    Log($"P{p}: swapped kb-only -> full mkb (mouse restored) in {sw.ElapsedMilliseconds} ms.");
                    return "OK ACTIVATED_P" + p;
                }
                catch (Exception ex)
                {
                    Log($"P{p}: kb->mkb swap FAILED: {ex.Message}");
                    try { _ctrl[p - 1]?.Dispose(); } catch { }
                    _ctrl[p - 1] = null;
                    _kbOnly[p - 1] = false;
                    return "ERR " + ex.Message;
                }
            }

            if (_ctrl[p - 1] != null)
                return "OK ALREADY_ACTIVE_P" + p;
            try
            {
                HMProfile profile = _ctx.GetProfile(ProfileIds[p - 1])
                    ?? throw new InvalidOperationException($"Profile '{ProfileIds[p - 1]}' not found in profiles directory.");
                var sw = Stopwatch.StartNew();
                _ctrl[p - 1] = _ctx.CreateController(profile, "P" + p);
                _kbOnly[p - 1] = false;
                Log($"Created controller P{p} (profile {ProfileIds[p - 1]}) in {sw.ElapsedMilliseconds} ms (wiimote connected).");
                return "OK ACTIVATED_P" + p;
            }
            catch (Exception ex)
            {
                Log($"ActivatePlayer P{p} FAILED: {ex.Message}");
                try { _ctrl[p - 1]?.Dispose(); } catch { }
                _ctrl[p - 1] = null;
                return "ERR " + ex.Message;
            }
        }
    }

    private static string DeactivatePlayer(int p)
    {
        lock (_gate)
        {
            bool hadGp = _gpCtrl[p - 1] != null;
            if (_ctrl[p - 1] == null && !hadGp)
                return "OK ALREADY_INACTIVE_P" + p;
            try { _ctrl[p - 1]?.Dispose(); } catch { }
            _ctrl[p - 1] = null;
            _kbOnly[p - 1] = false; // [V57h]
            _lastKeysFrame[p - 1] = null;
            _lastMouseFrame[p - 1] = null;
            // [V57g] The gamepad device follows the player device down (wiimote
            //     disconnected / app cleanup): never outlive the mkb device.
            //     FR: Le device gamepad suit le device joueur à la baisse (wiimote
            //     déconnectée / nettoyage app) : ne jamais lui survivre.
            DisposeGpPlayer(p - 1);
            Log($"Disposed controller P{p} (identity preserved for the next activation){(hadGp ? " + gamepad" : "")}.");
            return "OK DEACTIVATED_P" + p;
        }
    }

    // [V57h] EN: Hide the player's mouse while keeping the keyboard alive (vmulti COL03
    //     parity for the UMDF2 single-devnode world): swap the live mkb device to the
    //     kb-only descriptor at the SAME identity key "P<n>" (a different profile at the
    //     same key refreshes the descriptor at the same paths - SDK issue #60). Used by
    //     GamePad non-hybrid, Keyboardpad and Disabled modes via the service's
    //     REMOVE_MOUSE_Pn routing. ACTIVATE_P restores the full mkb device.
    //     FR: Masque la souris du joueur en gardant le clavier vivant (parité COL03
    //     vmulti dans le monde mono-devnode UMDF2) : échange le device mkb vivant vers
    //     le descripteur kb-seul à la MÊME clé d'identité « P<n> » (un profil différent
    //     à la même clé rafraîchit le descripteur aux mêmes chemins - issue #60 du SDK).
    //     Utilisé par les modes GamePad non-hybride, Keyboardpad et Disabled via le
    //     routage REMOVE_MOUSE_Pn du service. ACTIVATE_P restaure le device mkb complet.
    private static string HideMouseForPlayer(int p)
    {
        lock (_gate)
        {
            if (!_active)
                return "ERR NOT_ACTIVATED";
            if (_ctrl[p - 1] == null)
            {
                // EN: No device at all: nothing to hide (the mouse is already absent).
                //     FR: Aucun device : rien à masquer (la souris est déjà absente).
                return "OK ALREADY_HIDDEN_P" + p;
            }
            if (_kbOnly[p - 1])
                return "OK ALREADY_HIDDEN_P" + p;
            try
            {
                _ctrl[p - 1].Dispose();
                HMProfile profile = _ctx.GetProfile(KbProfileIds[p - 1])
                    ?? throw new InvalidOperationException($"Profile '{KbProfileIds[p - 1]}' not found in profiles directory.");
                var sw = Stopwatch.StartNew();
                _ctrl[p - 1] = _ctx.CreateController(profile, "P" + p);
                _kbOnly[p - 1] = true;
                // EN: Drop the pending mouse frame: the watchdog must never re-submit a
                //     mouse report to a kb-only device (the keyboard frame survives the
                //     swap and keeps the watchdog healing).
                //     FR: Jeter la frame souris en attente : le watchdog ne doit jamais
                //     re-soumettre un rapport souris à un device kb-seul (la frame
                //     clavier survit à l'échange et continue d'être guérie par le
                //     watchdog).
                _lastMouseFrame[p - 1] = null;
                Log($"P{p}: swapped mkb -> kb-only (mouse hidden, keyboard kept) in {sw.ElapsedMilliseconds} ms.");
                return "OK HIDDEN_P" + p;
            }
            catch (Exception ex)
            {
                Log($"P{p}: mkb->kb-only swap FAILED: {ex.Message}");
                try { _ctrl[p - 1]?.Dispose(); } catch { }
                _ctrl[p - 1] = null;
                _kbOnly[p - 1] = false;
                _lastMouseFrame[p - 1] = null;
                return "ERR " + ex.Message;
            }
        }
    }

    private static string DeactivateAllPlayers()
    {
        int n = 0;
        lock (_gate)
        {
            for (int i = 0; i < PlayerCount; i++)
            {
                bool hadGp = _gpCtrl[i] != null;
                if (_ctrl[i] == null && !hadGp)
                    continue;
                try { _ctrl[i]?.Dispose(); } catch { }
                _ctrl[i] = null;
                _kbOnly[i] = false; // [V57h]
                _lastKeysFrame[i] = null;
                _lastMouseFrame[i] = null;
                DisposeGpPlayer(i); // [V57g] gp goes down with the player device
                n++;
            }
        }
        Log($"Disposed {n} player controller(s).");
        return "OK DEACTIVATED_PLAYERS_" + n;
    }

    private static string DeactivatePlayersExcept(string csv)
    {
        var keep = new HashSet<int>();
        foreach (string part in csv.Split(','))
        {
            if (TryParse(part.Trim(), out int p) && p >= 1 && p <= PlayerCount)
                keep.Add(p);
        }
        int removed = 0;
        for (int i = 1; i <= PlayerCount; i++)
        {
            if (keep.Contains(i))
                continue;
            string r = DeactivatePlayer(i);
            if (r != null && r.StartsWith("OK DEACTIVATED_P", StringComparison.Ordinal))
                removed++;
        }
        Log($"DEACTIVATE_P_EXCEPT: kept {keep.Count} player(s), removed {removed}.");
        return "OK EXCEPT_KEPT_" + keep.Count + "_REMOVED_" + removed;
    }

    // ----------------------------------------------------- [V57g] gamepad lifecycle

    // [V57g] EN: Create (or swap) the player's gamepad device on demand - the wiimote
    //     entered GamePad mode, or the user swapped DInput<->XInput. The identity key
    //     "GP<n>" keeps the SAME device paths across cycles AND swaps; the creation
    //     index is pinned (GpBaseIndex + n - 1) so the swap follows the SDK LiveSwap
    //     pattern (dispose frees the index, CreateControllerAt re-takes it).
    //     FR: Crée (ou échange) le device gamepad du joueur à la demande - la wiimote
    //     est entrée en mode GamePad, ou l'utilisateur a basculé DInput<->XInput. La
    //     clé d'identité « GP<n> » conserve les MÊMES chemins à travers les cycles ET
    //     les swaps ; l'index de création est épinglé (GpBaseIndex + n - 1) pour que
    //     le swap suive le motif LiveSwap du SDK (le dispose libère l'index,
    //     CreateControllerAt le reprend).
    private static string ActivateGamepad(string args)
    {
        // Expected "<p>:<DINPUT|XINPUT>"
        // (EN/FR: Format attendu « <p>:<DINPUT|XINPUT> »)
        string[] parts = args.Split(':');
        if (parts.Length != 2 || !TryParse(parts[0], out int p) || p < 1 || p > PlayerCount)
            return ParseFail("ACTIVATE_GP:" + args);

        bool xinput;
        if (parts[1].Trim().Equals("XINPUT", StringComparison.OrdinalIgnoreCase)) xinput = true;
        else if (parts[1].Trim().Equals("DINPUT", StringComparison.OrdinalIgnoreCase)) xinput = false;
        else return ParseFail("ACTIVATE_GP:" + args);

        lock (_gate)
        {
            if (!_active)
                return "ERR NOT_ACTIVATED";
            if (_gpCtrl[p - 1] != null && _gpIsXInput[p - 1] == xinput)
            {
                // [V57j] EN: Same api already live - but the profile may have been UPDATED
                //     (device rename in an app update): the old device keeps the name
                //     frozen at creation. Compare the current profile's product string and
                //     RECREATE the device at the same identity when it differs, so a name
                //     change applies without restarting HmHost.
                //     FR: Même api déjà vivante - mais le profil a pu être MIS À JOUR
                //     (renommage de device dans une mise à jour de l'app) : le vieux
                //     device garde le nom figé à la création. Comparer le product string
                //     du profil courant et RECRÉER le device à la même identité s'il
                //     diffère, pour qu'un changement de nom s'applique sans redémarrer
                //     HmHost.
                string currentId = xinput ? X360ProfileIds[p - 1] : GpProfileIds[p - 1];
                HMProfile currentProfile = _ctx.GetProfile(currentId);
                if (currentProfile != null &&
                    !string.Equals(currentProfile.ProductString, _gpProfileNames[p - 1], StringComparison.Ordinal))
                {
                    Log($"GP{p}: profile product string changed ('{_gpProfileNames[p - 1]}' -> '{currentProfile.ProductString}') - recreating the device.");
                    try { _gpCtrl[p - 1].Dispose(); } catch { }
                    _gpCtrl[p - 1] = null;
                    ClearGpFrame(p - 1);
                    // Fall through to the creation below (same identity key -> same paths)
                }
                else
                {
                    return "OK ALREADY_ACTIVE_GP" + p + (xinput ? "_XINPUT" : "_DINPUT");
                }
            }

            try
            {
                // EN: Different api requested -> dispose first (identity preserved,
                //     the pinned index is freed for the re-creation below)
                // FR: Autre api demandée -> disposer d'abord (identité conservée,
                //     l'index épinglé est libéré pour la re-création ci-dessous)
                if (_gpCtrl[p - 1] != null)
                {
                    try { _gpCtrl[p - 1].Dispose(); } catch { }
                    _gpCtrl[p - 1] = null;
                    ClearGpFrame(p - 1);
                }

                string profId = xinput ? X360ProfileIds[p - 1] : GpProfileIds[p - 1];
                HMProfile profile = _ctx.GetProfile(profId)
                    ?? throw new InvalidOperationException($"Profile '{profId}' not found in profiles directory.");
                var sw = Stopwatch.StartNew();
                _gpCtrl[p - 1] = _ctx.CreateControllerAt(GpBaseIndex + p - 1, profile, GpKeyPrefix + p);
                _gpIsXInput[p - 1] = xinput;
                _gpProfileNames[p - 1] = profile.ProductString; // [V57j] for the rename check
                if (xinput) InitGpAxes(p - 1, profile);
                Log($"Created GP{p} ({(xinput ? "XInput" : "DInput")}, profile {profId}, index {GpBaseIndex + p - 1}) in {sw.ElapsedMilliseconds} ms (wiimote in GamePad mode).");
                return "OK ACTIVATED_GP" + p + (xinput ? "_XINPUT" : "_DINPUT");
            }
            catch (Exception ex)
            {
                Log($"ActivateGamepad GP{p} FAILED: {ex.Message}");
                try { _gpCtrl[p - 1]?.Dispose(); } catch { }
                _gpCtrl[p - 1] = null;
                ClearGpFrame(p - 1);
                return "ERR " + ex.Message;
            }
        }
    }

    private static string DeactivateGamepad(string args)
    {
        if (!TryParse(args, out int p) || p < 1 || p > PlayerCount)
            return ParseFail("DEACTIVATE_GP:" + args);

        lock (_gate)
        {
            if (_gpCtrl[p - 1] == null)
                return "OK ALREADY_INACTIVE_GP" + p;
            try { _gpCtrl[p - 1].Dispose(); } catch { }
            _gpCtrl[p - 1] = null;
            ClearGpFrame(p - 1);
            Log($"Disposed GP{p} controller (identity preserved for the next activation).");
            return "OK DEACTIVATED_GP" + p;
        }
    }

    // EN: Resolve the XInput profile's axis keys once per player ([lx, ly, rx, ry,
    //     lt, rt]) and pre-center the shared axes dictionary (hot-path friendly:
    //     ProcessGpX only updates the six entries, no per-frame allocation).
    //     FR: Résout une seule fois les clés d'axes du profil XInput par joueur
    //     ([lx, ly, rx, ry, lt, rt]) et pré-centre le dictionnaire d'axes partagé
    //     (adapté au chemin chaud : ProcessGpX ne fait que mettre à jour les six
    //     entrées, aucune allocation par frame).
    private static void InitGpAxes(int i, HMProfile profile)
    {
        var sticks = profile.Sticks;
        var triggers = profile.Triggers;
        HMAxis lx = HMAxis.X, ly = HMAxis.Y, rx = HMAxis.Rx, ry = HMAxis.Ry, lt = HMAxis.Z, rt = HMAxis.Rz;
        if (sticks.Count > 0)
        {
            lx = sticks[0].XAxis;
            if (sticks[0].YAxis != HMAxis.None) ly = sticks[0].YAxis;
        }
        if (sticks.Count > 1)
        {
            rx = sticks[1].XAxis;
            if (sticks[1].YAxis != HMAxis.None) ry = sticks[1].YAxis;
        }
        if (triggers.Count > 0) lt = triggers[0].Axis;
        if (triggers.Count > 1) rt = triggers[1].Axis;

        var axes = new Dictionary<HMAxis, float>
        {
            [lx] = 0.5f, [ly] = 0.5f, [rx] = 0.5f, [ry] = 0.5f,
            [lt] = 0.0f, [rt] = 0.0f
        };
        _gpAxisKeys[i] = new[] { lx, ly, rx, ry, lt, rt };
        _gpAxes[i] = axes;
    }

    private static void ClearGpFrame(int i)
    {
        _lastGpRawFrame[i] = null;
        _lastGpState[i] = default;
        _gpHeld[i] = false;
    }

    // EN: Dispose a player's gamepad device if live (used by every mkb teardown path
    //     so the gp never outlives the wiimote's player device).
    //     FR: Supprime le device gamepad du joueur s'il est vivant (utilisé par tous
    //     les chemins de démontage mkb pour que le gp ne survive jamais au device
    //     joueur de la wiimote).
    private static void DisposeGpPlayer(int i)
    {
        if (_gpCtrl[i] == null) return;
        try { _gpCtrl[i].Dispose(); } catch { }
        _gpCtrl[i] = null;
        ClearGpFrame(i);
    }

    private static string Deactivate()
    {
        lock (_gate)
        {
            if (!_active)
                return "OK ALREADY_INACTIVE";
            for (int i = 0; i < PlayerCount; i++)
            {
                try { _ctrl[i]?.Dispose(); } catch { }
                _ctrl[i] = null;
                _kbOnly[i] = false; // [V57h]
                _lastKeysFrame[i] = null;
                _lastMouseFrame[i] = null;
                DisposeGpPlayer(i); // [V57g] gp devices go down with the mode
            }
            _active = false;
            Log("DEACTIVATED (P1..P4 + gamepads disposed).");
            return "OK DEACTIVATED";
        }
    }

    // [V57d] EN: See the field comment above. Re-submits each player's last frame while
    //     something is held or was recently updated, so a lost release never sticks.
    //     FR: Voir le commentaire des champs. Re-soumet la dernière frame de chaque
    //     joueur tant qu'une entrée est tenue ou récente, pour ne jamais rester bloqué.
    private static void WatchdogLoop()
    {
        int tick = 0;
        while (true)
        {
            Thread.Sleep(25);
            try
            {
                // [V57e2] EN: keys are re-sent EVERY 25 ms (40 Hz) for 1 s after any keys
                //     update, mouse frames every ~150 ms. Root cause of the on-screen KB
                //     bug: a keys frame only lives in the driver's shared buffer for ~10 ms
                //     before the next mouse frame (100 Hz while pointing) overwrites it -
                //     one missed read completion and the key event (usually the RELEASE)
                //     is lost, sticking the key. 40 independent delivery attempts per
                //     event make the loss negligible even under full mouse storm.
                //     FR: les touches sont re-soumises TOUTES les 25 ms (40 Hz) pendant
                //     1 s après toute mise à jour clavier, la souris toutes les ~150 ms.
                //     Cause racine du bug KB en visée écran : une frame clavier ne vit
                //     que ~10 ms dans le buffer partagé du driver avant que la frame
                //     souris suivante (100 Hz en visée) ne l'écrase - une seule
                //     complétion de lecture manquée perd l'événement (souvent le
                //     RELÂCHEMENT) et laisse la touche bloquée. 40 tentatives de
                //     livraison indépendantes rendent la perte négligeable même en
                //     pleine tempête souris.
                bool slowTick = (tick % 6 == 0);
                tick++;
                lock (_gate)
                {
                    if (!_active)
                        continue;
                    DateTime now = DateTime.UtcNow;
                    for (int i = 0; i < PlayerCount; i++)
                    {
                        var c = _ctrl[i];
                        if (c == null)
                            continue;

                        var km = _lastKeysFrame[i];
                        if (km != null)
                        {
                            bool held = false;
                            for (int b = 1; b < km.Length; b++)
                            {
                                if (km[b] != 0) { held = true; break; }
                            }
                            if (held || (now - _lastKeysUtc[i]).TotalMilliseconds < 1000)
                                c.SubmitRawExtendedReport(km);
                        }

                        var mm = _lastMouseFrame[i];
                        if (mm != null && slowTick)
                        {
                            bool button = mm.Length > 1 && mm[1] != 0;
                            if (button || (now - _lastMouseUtc[i]).TotalMilliseconds < 1000)
                                c.SubmitRawExtendedReport(mm);
                        }

                        // [V57g] GamePad watchdog parity: re-submit the last gp state
                        //     while something is held (or for 1 s after any update) so
                        //     a lost read completion can never stick a button/axis -
                        //     same healing technique as the KB frames (V57e2). The gp
                        //     device has its own shared buffer, so there is no mouse
                        //     storm competing for it; identical reports are no-ops for
                        //     the HID stack / XUSB companion.
                        //     FR: Parité watchdog gamepad : re-soumettre le dernier
                        //     état gp tant qu'une entrée est tenue (ou pendant 1 s
                        //     après toute mise à jour) pour qu'une complétion de
                        //     lecture perdue ne puisse jamais bloquer un bouton/un
                        //     axe - même technique de guérison que les frames KB
                        //     (V57e2). Le device gp a son propre buffer partagé, donc
                        //     aucune tempête souris ne lui dispute la place ; des
                        //     rapports identiques sont sans effet pour la pile HID /
                        //     le companion XUSB.
                        var gc = _gpCtrl[i];
                        if (gc != null && (_gpHeld[i] || (now - _lastGpUtc[i]).TotalMilliseconds < 1000))
                        {
                            if (_gpIsXInput[i])
                            {
                                var gs = _lastGpState[i];
                                gc.SubmitState(in gs);
                            }
                            else
                            {
                                var gr = _lastGpRawFrame[i];
                                if (gr != null) gc.SubmitRawExtendedReport(gr);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Watchdog error: " + ex.Message);
            }
        }
    }

    private static void CleanupAll()
    {
        try
        {
            lock (_gate)
            {
                for (int i = 0; i < PlayerCount; i++)
                {
                    try { _ctrl[i]?.Dispose(); } catch { }
                    _ctrl[i] = null;
                    _kbOnly[i] = false; // [V57h]
                    _lastKeysFrame[i] = null;
                    _lastMouseFrame[i] = null;
                    DisposeGpPlayer(i); // [V57g] no orphan gamepads either
                }
                _active = false;
                _ctx?.Dispose();
                _ctx = null;
            }
        }
        catch (Exception ex)
        {
            Log("Cleanup error: " + ex.Message);
        }
    }

    // ------------------------------------------------------- helpers

    private static bool TryParse(string s, out int value)
        => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);

    private static string ParseFail(string line)
    {
        _parseFailCount++;
        if (_parseFailCount == 1 || _parseFailCount % 100 == 0)
            Log($"Parse failure #{_parseFailCount}: '{line}'");
        return null; // never kill the frame pipe on a bad frame
    }

    private static void FrameCounted()
    {
        _frameCount++;
        if (_frameCount == 1 || _frameCount % 5000 == 0)
            Log($"Frames submitted: {_frameCount}");
    }

    private static bool IsElevated()
    {
        try
        {
            using var id = System.Security.Principal.WindowsIdentity.GetCurrent();
            return new System.Security.Principal.WindowsPrincipal(id)
                .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
        }
        catch { return false; }
    }

    private static void Log(string message)
    {
        try
        {
            lock (_logLock)
            {
                File.AppendAllText(_logPath,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}{Environment.NewLine}");
            }
        }
        catch { }
        Console.WriteLine(message);
    }
}
