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
    // [V83-UMDF2-VERIFY] Identity-key rotation counters. When the stable P<n> devnode is wedged
    // (pending-removal after a vetoed removal: node LIVE + problem=0 but its HID child never
    // reaches DN_STARTED until reboot, or a zombie after dispose: Enum key gone but live node
    // left = "not owned" Code 1), we recreate on P<n>R, P<n>RR... so the SDK makes a FRESH node
    // instead of re-adopting the dead one. Same trick as V57o (GP -> GPW). A fresh node starts
    // instantly, so the mouse comes back WITHOUT a Windows reboot.
    // FR: Compteurs de rotation de clé d'identité. Quand le devnode stable P<n> est bloqué
    // (suppression vetoed : nœud LIVE + problem=0 mais fils HID jamais DN_STARTED avant le
    // reboot, ou zombie après dispose : clé Enum supprimée mais nœud live restant = Code 1
    // « not owned »), on recrée sur P<n>R, P<n>RR... pour forcer un nœud NEUF au lieu de
    // ré-adopter le mort. Un nœud neuf démarre immédiatement : la souris revient SANS reboot.
    private static readonly int[] _rotCount = new int[PlayerCount];
    private static readonly int[] _gpRotCount = new int[PlayerCount];
    private static string KeyFor(int p) => "P" + p + new string('R', _rotCount[p - 1]);
    private static string GpKeyFor(int p) => GpKeyPrefix + new string('X', _gpRotCount[p - 1]) + p;
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
    private const string HostVersion = "3.1.7.2";
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
        // [V83-UMDF2-VERIFY] EN: enable the SDK's own diag/timing logs BEFORE any SDK
        //     static initializer runs (DeviceOrchestrator reads HIDMAESTRO_DIAG once at
        //     type init), and remember the current file offsets so DrainSdkDiag only
        //     mirrors THIS process's lines into HmHost.log (user bundles stay clean).
        //     FR: activer les logs diag/timing du SDK AVANT tout init statique
        //     (DeviceOrchestrator lit HIDMAESTRO_DIAG une fois à l'init du type) et
        //     mémoriser les offsets courants pour que DrainSdkDiag ne miroite que les
        //     lignes de CE process dans HmHost.log.
        Environment.SetEnvironmentVariable("HIDMAESTRO_DIAG", "1");
        Environment.SetEnvironmentVariable("HIDMAESTRO_TIMING", "1");
        _diagOffset = InitialLen(Path.Combine(Path.GetTempPath(), "HIDMaestro", "teardown_diag.log"));
        _timingOffset = InitialLen(Path.Combine(Path.GetTempPath(), "HIDMaestro", "setup_timing.log"));

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

            // [V83-UMDF2-ZOMBIE] sweep leftover HIDMaestro devnodes from previous runs at host
            // start (kernel enumeration: zombies have lost their Enum key, so a registry-only
            // walk misses them). Idempotent, safe, runs before any device creation.
            // FR: balayage des devnodes HIDMaestro restants au démarrage de l'hôte (énumération
            // kernel : les zombies ont perdu leur clé Enum, un parcours registre les rate).
            SweepLeftoverHmNodes();

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
                DrainSdkDiag("INSTALL");
                // [V83-UMDF2-ZOMBIE] clean leftover HM nodes from previous runs (app startup,
                // nothing holds them yet -> hard evict succeeds; if a live one survives, it is
                // disabled + named instead of showing as "unknown device" Code 1). No reboot needed.
                SweepLeftoverHmNodes();
                DrainSdkDiag("SWEEP");

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
                DrainSdkDiag("ACTIVATE");
                return "OK ACTIVATED";
            }
            catch (Exception ex)
            {
                Log("Activate FAILED: " + ex);
                // EN: Best-effort cleanup of partially created controllers
                // FR: Nettoyage best-effort des contrôleurs partiellement créés
                for (int i = 0; i < PlayerCount; i++)
                {
                    HMProfile cp = null;
                    try { cp = _ctx.GetProfile(_kbOnly[i] ? KbProfileIds[i] : ProfileIds[i]); } catch { }
                    DisposePlayerCtrl(i + 1, cp); // [V83] + clean leftover node
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
                    // [V83] dispose the kb-only device + clean its leftover node
                    DisposePlayerCtrl(p, _ctx.GetProfile(KbProfileIds[p - 1]));
                    HMProfile profile = _ctx.GetProfile(ProfileIds[p - 1])
                        ?? throw new InvalidOperationException($"Profile '{ProfileIds[p - 1]}' not found in profiles directory.");
                    var sw = Stopwatch.StartNew();
                    // [V83-UMDF2-VERIFY] create + verify + heal chain (restart / remove+recreate /
                    // rotate identity key) - see CreateVerifiedPlayer.
                    string res = CreateVerifiedPlayer(p, profile, kbOnly: false, okPrefix: "OK ACTIVATED_P");
                    Log($"P{p}: swapped kb-only -> full mkb (mouse restored) in {sw.ElapsedMilliseconds} ms.");
                    return res;
                }
                catch (Exception ex)
                {
                    Log($"P{p}: kb->mkb swap FAILED: {ex.Message}");
                    try { DisposePlayerCtrl(p, _ctx.GetProfile(KbProfileIds[p - 1])); } catch { try { _ctrl[p - 1]?.Dispose(); } catch { } _ctrl[p - 1] = null; }
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
                // [V83-UMDF2-VERIFY] EN: create + verify (HID child DN_STARTED) + heal chain
                //     (restart / remove+recreate / rotate identity key to bypass a wedged
                //     pending-removal devnode). The mouse must come back WITHOUT a reboot.
                //     FR: création + vérification (fils HID DN_STARTED) + chaîne de réparation
                //     (restart / remove+recreate / rotation de clé pour contourner un devnode
                //     bloqué). La souris doit revenir SANS redémarrer Windows.
                return CreateVerifiedPlayer(p, profile, kbOnly: false, okPrefix: "OK ACTIVATED_P");
            }
                catch (Exception ex)
                {
                    Log($"ActivatePlayer P{p} FAILED: {ex.Message}");
                    try { DisposePlayerCtrl(p, _ctx.GetProfile(ProfileIds[p - 1])); } catch { try { _ctrl[p - 1]?.Dispose(); } catch { } _ctrl[p - 1] = null; }
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
            bool wasKbOnly = _kbOnly[p - 1];
            // [V83-UMDF2-ZOMBIE] dispose + clean the leftover node (disable+name if still live,
            // else uninstall+delete) so no "unknown device" zombie accumulates per disconnect.
            HMProfile dp = null;
            try { dp = _ctx.GetProfile(wasKbOnly ? KbProfileIds[p - 1] : ProfileIds[p - 1]); } catch { }
            DisposePlayerCtrl(p, dp);
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
                // [V83] dispose the mkb device + clean its leftover node
                DisposePlayerCtrl(p, _ctx.GetProfile(ProfileIds[p - 1]));
                HMProfile profile = _ctx.GetProfile(KbProfileIds[p - 1])
                    ?? throw new InvalidOperationException($"Profile '{KbProfileIds[p - 1]}' not found in profiles directory.");
                var sw = Stopwatch.StartNew();
                // EN: Drop the pending mouse frame: the watchdog must never re-submit a
                //     mouse report to a kb-only device (the keyboard frame survives the
                //     swap and keeps the watchdog healing).
                //     FR: Jeter la frame souris en attente : le watchdog ne doit jamais
                //     re-soumettre un rapport souris à un device kb-seul (la frame
                //     clavier survit à l'échange et continue d'être guérie par le
                //     watchdog).
                _lastMouseFrame[p - 1] = null;
                // [V83-UMDF2-VERIFY] same verify+heal chain as ActivatePlayer.
                string res = CreateVerifiedPlayer(p, profile, kbOnly: true, okPrefix: "OK HIDDEN_P");
                Log($"P{p}: swapped mkb -> kb-only (mouse hidden, keyboard kept) in {sw.ElapsedMilliseconds} ms.");
                return res;
            }
            catch (Exception ex)
            {
                Log($"P{p}: mkb->kb-only swap FAILED: {ex.Message}");
                try { DisposePlayerCtrl(p, _ctx.GetProfile(ProfileIds[p - 1])); } catch { try { _ctrl[p - 1]?.Dispose(); } catch { } _ctrl[p - 1] = null; }
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
                bool wasKbOnly = _kbOnly[i];
                // [V83-UMDF2-ZOMBIE] dispose + clean leftover node (no "unknown device" zombie)
                HMProfile dp = null;
                try { dp = _ctx.GetProfile(wasKbOnly ? KbProfileIds[i] : ProfileIds[i]); } catch { }
                DisposePlayerCtrl(i + 1, dp);
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
                    DisposeGpCtrl(p, currentProfile); // [V83] + clean leftover node
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
                    // [V83] + clean leftover node
                    try { DisposeGpCtrl(p, _ctx.GetProfile(_gpIsXInput[p - 1] ? X360ProfileIds[p - 1] : GpProfileIds[p - 1])); } catch { }
                    ClearGpFrame(p - 1);
                }

                string profId = xinput ? X360ProfileIds[p - 1] : GpProfileIds[p - 1];
                HMProfile profile = _ctx.GetProfile(profId)
                    ?? throw new InvalidOperationException($"Profile '{profId}' not found in profiles directory.");
                var sw = Stopwatch.StartNew();
                string gpKey = GpKeyFor(p);
                string gpInstId = null;
                bool gpLive = false;
                // [V83-UMDF2-VERIFY] same verified-create + rotation loop as CreateVerifiedPlayer.
                for (int attempt = 0; attempt < 3 && !gpLive; attempt++)
                {
                    gpKey = GpKeyFor(p);
                    gpInstId = ComputeInstanceId(profile, gpKey);
                    if (NodeLocatable(gpInstId) && !ChildStarted(gpInstId))
                    {
                        Log($"GP{p}: existing node {gpInstId} has no started HID child - evicting before create ({DescribeDevNode(gpInstId)})");
                        ForceEvictNode(gpInstId);
                    }
                    try
                    {
                        _gpCtrl[p - 1] = _ctx.CreateControllerAt(GpBaseIndex + p - 1, profile, gpKey);
                        _gpIsXInput[p - 1] = xinput;
                        _gpProfileNames[p - 1] = profile.ProductString; // [V57j] for the rename check
                        if (xinput) InitGpAxes(p - 1, profile);
                    }
                    catch (Exception ex)
                    {
                        Log($"GP{p}: create on key '{gpKey}' threw: {ex.Message} ({DescribeDevNode(gpInstId)})");
                        DrainSdkDiag("CREATE_GP_EX" + attempt);
                        DisposeGpCtrl(p, profile);
                        ForceEvictNode(gpInstId);
                        _gpRotCount[p - 1]++;
                        continue;
                    }
                    var vGp = VerifyDevice(profile, gpKey, 6000, attemptHeal: true);
                    gpInstId = vGp.InstId;
                    gpLive = vGp.Live;
                    if (!gpLive)
                    {
                        Log($"GP{p}: not started on key '{gpKey}' ({DescribeDevNode(gpInstId)}) - evicting and rotating gp identity key");
                        DrainSdkDiag("ROTATE_GP" + attempt);
                        DisposeGpCtrl(p, profile);
                        ForceEvictNode(gpInstId);
                        _gpRotCount[p - 1]++;
                    }
                }
                Log($"Created GP{p} ({(xinput ? "XInput" : "DInput")}, profile {profId}, index {GpBaseIndex + p - 1}) in {sw.ElapsedMilliseconds} ms (wiimote in GamePad mode). key={gpKey} instId={gpInstId} live={gpLive} {(gpInstId != null ? DescribeDevNode(gpInstId) : "")}");
                DrainSdkDiag("ACTIVATE_GP" + p);
                if (!gpLive)
                {
                    DisposeGpCtrl(p, profile);
                    ClearGpFrame(p - 1);
                    return "ERR NOT_STARTED_GP" + p;
                }
                return "OK ACTIVATED_GP" + p + (xinput ? "_XINPUT" : "_DINPUT");
            }
            catch (Exception ex)
            {
                Log($"ActivateGamepad GP{p} FAILED: {ex.Message}");
                try { DisposeGpCtrl(p, _ctx.GetProfile(xinput ? X360ProfileIds[p - 1] : GpProfileIds[p - 1])); } catch { try { _gpCtrl[p - 1]?.Dispose(); } catch { } _gpCtrl[p - 1] = null; }
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
            // [V83] + clean leftover node
            try { DisposeGpCtrl(p, _ctx.GetProfile(_gpIsXInput[p - 1] ? X360ProfileIds[p - 1] : GpProfileIds[p - 1])); } catch { }
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
        // [V83-UMDF2-ZOMBIE] clean the gp devnode too (disable+name if still live, else remove)
        HMProfile gp = null;
        try { gp = _ctx.GetProfile(_gpIsXInput[i] ? X360ProfileIds[i] : GpProfileIds[i]); } catch { }
        DisposeGpCtrl(i + 1, gp);
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
                HMProfile cp = null;
                try { cp = _ctx.GetProfile(_kbOnly[i] ? KbProfileIds[i] : ProfileIds[i]); } catch { }
                DisposePlayerCtrl(i + 1, cp); // [V83] + clean leftover node
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
                    // [V83-UMDF2-ZOMBIE] EN: periodic leftover sweep (~15 s). A vetoed removal
                    //     can leave a deferred-removal node that is invisible on the spot
                    //     (CM_Locate finds nothing, the dispose cleanup skips it silently) and
                    //     materializes seconds later as an "unknown device" (observed: P2's
                    //     node appeared only after the LAST player disconnected). This sweep
                    //     catches those. It runs under _gate so no device creation can race,
                    //     and only evicts nodes WITHOUT a started HID child - a healthy live
                    //     device is always kept. With no leftovers the cost is ~100 CM_Locate
                    //     calls (sub-millisecond each).
                    //     FR: balayage périodique des restes (~15 s). Une suppression refusée
                    //     peut laisser un nœud en retrait différé, invisible sur l'instant
                    //     (CM_Locate ne trouve rien, le nettoyage du dispose l'ignorait
                    //     silencieusement) qui se matérialise quelques secondes plus tard en
                    //     « périphérique inconnu » (observé : le nœud P2 est apparu seulement
                    //     après la déconnexion du DERNIER joueur). Ce balayage les attrape.
                    //     Il tourne sous _gate (aucune course avec une création) et n'évince
                    //     que les nœuds SANS fils HID démarré - un device sain est conservé.
                    if (tick % 400 == 0) // ~10 s backstop sweep (targeted re-checks handle the dispose case)
                        SweepLeftoverHmNodes();
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
                    HMProfile cp = null;
                    try { cp = _ctx.GetProfile(_kbOnly[i] ? KbProfileIds[i] : ProfileIds[i]); } catch { }
                    DisposePlayerCtrl(i + 1, cp); // [V83] + clean leftover node
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

    // ── [V83-UMDF2-VERIFY] verified create + heal chain + SDK diag mirror ──────

    // EN: Create the player's device and verify it REALLY works (HID child DN_STARTED).
    //     Heal chain: restart heal -> remove+recreate on the same key -> rotate the
    //     identity key (P<n> -> P<n>R) to bypass a wedged pending-removal devnode that
    //     PnP refuses to start children on until reboot (observed after a service
    //     reinstall: node LIVE, problem=0, HID child stuck without DN_STARTED, removal
    //     vetoed "in use by application"). A fresh node on the rotated key starts
    //     instantly, so the mouse comes back WITHOUT a Windows reboot. The dead wedged
    //     node has no started HID child, hence no HID interface: invisible to RawInput.
    //     FR: Crée le device du joueur et vérifie qu'il marche VRAIMENT (fils HID
    //     DN_STARTED). Chaîne de réparation : restart -> remove+recreate sur la même clé ->
    //     rotation de la clé d'identité (P<n> -> P<n>R) pour contourner un devnode bloqué
    //     en attente de suppression que PnP refuse de démarrer avant le reboot. Un nœud
    //     neuf sur clé rotatée démarre immédiatement : la souris revient SANS redémarrer
    //     Windows. Le nœud bloqué n'a pas de fils HID démarré, donc pas d'interface HID :
    //     invisible pour RawInput.
    private static string CreateVerifiedPlayer(int p, HMProfile profile, bool kbOnly, string okPrefix)
    {
        string key = KeyFor(p);
        string instId = null;
        bool live = false;
        var sw = Stopwatch.StartNew();
        // EN: Up to 3 attempts, rotating the identity key (P1 -> P1R -> P1RR) on any failure:
        //     a wedged/zombie node on one key never heals, but a FRESH node on a fresh key
        //     starts instantly (measured 546 ms, live=True) - so rotate and retry.
        //     FR: Jusqu'à 3 essais, en rotatant la clé d'identité (P1 -> P1R -> P1RR) à chaque
        //     échec : un nœud bloqué/zombie sur une clé ne guérit jamais, mais un nœud NEUF
        //     sur une clé neuve démarre instantanément (mesuré 546 ms, live=True) - on
        //     rotate et on réessaie.
        for (int attempt = 0; attempt < 3 && !live; attempt++)
        {
            key = KeyFor(p);
            instId = ComputeInstanceId(profile, key);
            // Pre-check: an existing node whose HID child is absent or not started is a
            // wedged pending-removal node - evict it BEFORE create so the SDK does a fresh
            // create instead of adopting it (and burning its 5s wait_started budget).
            if (NodeLocatable(instId) && !ChildStarted(instId))
            {
                Log($"P{p}: existing node {instId} has no started HID child - evicting before create ({DescribeDevNode(instId)})");
                ForceEvictNode(instId);
            }
            try
            {
                _ctrl[p - 1] = _ctx.CreateController(profile, key);
                _kbOnly[p - 1] = kbOnly;
            }
            catch (Exception ex)
            {
                // zombie (live node, Enum key gone -> "not owned") or rival: rotate
                Log($"P{p}: create on key '{key}' threw: {ex.Message} ({DescribeDevNode(instId)})");
                DrainSdkDiag("CREATE_EX" + attempt);
                DisposePlayerCtrl(p, profile);
                ForceEvictNode(instId);
                _rotCount[p - 1]++;
                continue;
            }
            var v = VerifyDevice(profile, key, 6000, attemptHeal: true);
            instId = v.InstId;
            live = v.Live;
            if (!live)
            {
                Log($"P{p}: not started on key '{key}' ({DescribeDevNode(instId)}) - evicting and rotating identity key to '{KeyFor(p)}R'");
                DrainSdkDiag("ROTATE" + attempt);
                DisposePlayerCtrl(p, profile);
                ForceEvictNode(instId);
                _rotCount[p - 1]++;
            }
        }
        Log($"Created controller P{p} (profile {profile.Id}) in {sw.ElapsedMilliseconds} ms (wiimote connected). key={key} instId={instId} live={live} {(instId != null ? DescribeDevNode(instId) : "")}");
        DrainSdkDiag("ACTIVATE_P" + p);
        if (!live)
        {
            DisposePlayerCtrl(p, profile);
            _kbOnly[p - 1] = false;
            return "ERR NOT_STARTED_P" + p;
        }
        return okPrefix + p;
    }

    private static bool NodeLocatable(string instId)
    {
        return CM_Locate_DevNodeW(out _, instId, 0) == 0 || CM_Locate_DevNodeW(out _, instId, 1) == 0;
    }

    private static bool ChildStarted(string instId)
    {
        try
        {
            string child = HIDMaestro.Internal.DeviceManager.GetHidChildId(instId);
            return child != null && HIDMaestro.Internal.DeviceManager.IsDeviceStarted(child);
        }
        catch { return false; }
    }

    // EN: After a dispose, the SDK teardown may delete the Enum key while the live node
    //     survives (removal vetoed) -> Code-1 "unknown device" zombie in Device Manager.
    //     Disable the leftover node so it shows as disabled instead, and so it is visibly
    //     ours-but-off. The next create rotates to a fresh key anyway.
    // FR: Après un dispose, le teardown du SDK peut supprimer la clé Enum alors que le
    //     nœud live survit (suppression vetoed) -> zombie Code 1 « périphérique inconnu ».
    //     On désactive le nœud restant pour qu'il apparaisse désactivé, pas inconnu. La
    //     prochaine création rotate de toute façon sur une clé neuve.
    // [V83-UMDF2-ZOMBIE] EN: After a dispose, a live node whose removal was vetoed cannot be
    //     hard-deleted until reboot; if its Enum key is gone it shows as an unnamed
    //     "unknown device" (Code 1) in Device Manager - one per wiimote disconnect. So: if the
    //     node is still LIVE, keep its Enum key, disable it (greyed out, keeps its driver+name)
    //     and write a friendly DeviceDesc; only uninstall+delete the key when the node is
    //     phantom-only (safe, invisible anyway).
    //     FR: Après un dispose, un nœud live dont la suppression a été refusée ne peut pas être
    //     supprimé avant le reboot ; si sa clé Enum est partie, il apparaît comme « périphérique
    //     inconnu » (code 1) sans nom - un par déconnexion de wiimote. Donc : si le nœud est
    //     encore LIVE, on garde sa clé Enum, on le désactive (grisé, garde pilote+nom) et on
    //     écrit un DeviceDesc lisible ; on ne supprime la clé que si le nœud est phantom.
    private static void CleanupNodeAfterDispose(HMProfile profile, string key, string tag)
    {
        try
        {
            if (profile == null) return;
            string instId = ComputeInstanceId(profile, key);
            if (!NodeLocatable(instId))
            {
                // [V83-UMDF2-ZOMBIE] EN: the SDK teardown removed the node "on the spot" but a
                //     vetoed removal is DEFERRED: the node is invisible now and materializes a
                //     few seconds later as a Code-1 "unknown device" (observed: P2's node
                //     appeared only after the last player disconnected). Schedule targeted
                //     re-checks so it is evicted as soon as it materializes.
                //     FR: le teardown SDK a retiré le nœud « sur place » mais une suppression
                //     refusée est DIFFÉRÉE : le nœud est invisible maintenant et se matérialise
                //     quelques secondes plus tard en « périphérique inconnu » code 1. On
                //     programme des re-vérifications ciblées pour l'évincer dès qu'il apparaît.
                ScheduleDeferredNodeRecheck(instId, tag);
                return;
            }
            //     DIF_REMOVE is vetoed (proven in the logs: "stillPresent=False" right after),
            //     so the leftover node really disappears without a reboot. Only the rare
            //     survivor falls back to disable + friendly name (greyed out, named).
            //     FR: évincer D'ABORD - CM_Disable+CM_Uninstall force la suppression même quand
            //     DIF_REMOVE est refusé (prouvé dans les logs : « stillPresent=False » juste
            //     après), donc le nœud restant disparaît vraiment sans reboot. Seul le rare
            //     survivant retombe sur disable + nom lisible (grisé).
            ForceEvictNode(instId);
            if (!NodeLocatable(instId))
            {
                Log($"{tag}: node {instId} removed cleanly");
                return;
            }
            if (CM_Locate_DevNodeW(out uint dn, instId, 0) == 0)
                CM_Disable_DevNode(dn, 0);
            WriteFriendlyName(instId, profile.ProductString);
            Log($"{tag}: node {instId} survived evict - disabled and named «{profile.ProductString}» (until next reboot)");
        }
        catch (Exception ex) { Log($"{tag}: CleanupNodeAfterDispose failed: {ex.Message}"); }
    }

    // [V83-UMDF2-ZOMBIE] EN: Targeted re-checks (3 s then 10 s) for a node that vanished at
    //     dispose but may materialize later (deferred/vetoed removal). Evicts it as soon as it
    //     appears, so the "unknown device" is visible only for a few seconds instead of until
    //     the next periodic sweep. A healthy re-appearance (started HID child) is kept.
    //     FR: Re-vérifications ciblées (3 s puis 10 s) d'un nœud disparu au dispose mais
    //     susceptible de se matérialiser plus tard (suppression différée/refusée). On l'évince
    //     dès son apparition : le « périphérique inconnu » ne reste visible que quelques
    //     secondes au lieu d'attendre le sweep périodique. Une réapparition saine (fils HID
    //     démarré) est conservée.
    private static void ScheduleDeferredNodeRecheck(string instId, string tag)
    {
        var t = new Thread(() =>
        {
            foreach (var delayMs in new[] { 3000, 10000 })
            {
                Thread.Sleep(delayMs);
                try
                {
                    if (!NodeLocatable(instId)) continue;
                    if (ChildStarted(instId)) return; // healthy again: keep it
                    Log($"{tag}: deferred node {instId} materialized - evicting");
                    ForceEvictNode(instId);
                    if (!NodeLocatable(instId))
                    {
                        Log($"{tag}: deferred node {instId} removed cleanly");
                        return;
                    }
                }
                catch { }
            }
        })
        { IsBackground = true, Name = "HmDeferredCheck" };
        t.Start();
    }

    private static void WriteFriendlyName(string instId, string name)
    {
        if (string.IsNullOrEmpty(name)) return;
        try
        {
            using var k = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                $@"SYSTEM\CurrentControlSet\Enum\{instId}", writable: true);
            if (k == null) return;
            k.SetValue("DeviceDesc", name, Microsoft.Win32.RegistryValueKind.String);
            k.SetValue("FriendlyName", name, Microsoft.Win32.RegistryValueKind.String);
        }
        catch { }
    }

    // [V83-UMDF2-ZOMBIE] EN: Sweep leftover HIDMaestro devnodes at startup. KERNEL enumeration
    //     (SetupDi DIGCF_PRESENT) - NOT registry keys: the zombied nodes have lost their Enum key
    //     (deleted by a previous teardown), so only the kernel knows them; CM_Locate finds them
    //     LIVE while a registry walk sees nothing (that is why the v5 sweep found 0 node).
    //     Identity pattern ROOT\VID_xxxx&PID_xxxx\HM_<16hex> (the reserved identity-token
    //     namespace); ownership is confirmed via SPDRP_HARDWAREID containing "HIDMaestro" when
    //     readable (issue #28: never touch foreign devices). Eviction is reliable:
    //     CM_Disable+CM_Uninstall forces removal even when DIF_REMOVE is vetoed.
    //     FR: Balayage des devnodes HIDMaestro restants au démarrage. Énumération KERNEL
    //     (SetupDi DIGCF_PRESENT) - PAS les clés de registre : les nœuds zombis ont perdu leur
    //     clé Enum (supprimée par un teardown précédent), seul le kernel les connaît ; CM_Locate
    //     les trouve LIVE alors qu'un parcours registre ne voit rien (pourquoi le sweep v5 n'a
    //     rien trouvé). Motif ROOT\VID_xxxx&PID_xxxx\HM_<16hex> (tokens d'identité réservés) ;
    //     propriété confirmée par SPDRP_HARDWAREID contenant « HIDMaestro » si lisible (issue #28 :
    //     jamais toucher un device étranger). L'éviction est fiable : CM_Disable+CM_Uninstall
    //     force la suppression même quand DIF_REMOVE est refusé.
    private static void SweepLeftoverHmNodes()
    {
        try
        {
            int cleaned = 0;
            foreach (var instId in EnumerateHmDevnodes())
            {
                // only evict DEAD nodes (no started HID child): never touch a healthy live
                // device (a still-running one from a previous host session is simply reused
                // by the next activation).
                if (ChildStarted(instId))
                {
                    Log($"sweep: node {instId} has a started HID child - keeping (healthy)");
                    continue;
                }
                Log($"sweep: leftover HM node {instId} - evicting");
                DrainSdkDiag("SWEEP_EVICT");
                ForceEvictNode(instId);
                if (NodeLocatable(instId) && CM_Locate_DevNodeW(out uint dn, instId, 0) == 0)
                {
                    CM_Disable_DevNode(dn, 0);
                    WriteFriendlyName(instId, "Wiimote4Guns virtual device (inactive)");
                    Log($"sweep: {instId} still live after evict - disabled and named");
                }
                cleaned++;
            }
            if (cleaned > 0) Log($"sweep: {cleaned} leftover HM node(s) processed");
        }
        catch (Exception ex) { Log("SweepLeftoverHmNodes failed: " + ex.Message); }
    }

    // EN: Enumerate the PRESENT devnodes matching HIDMaestro's identity namespace
    //     ROOT\VID_xxxx&PID_xxxx\HM_<16hex>, ownership-checked via SPDRP_HARDWAREID.
    //     Catches nodes whose Enum key was deleted (invisible to registry-only walks).
    //     FR: Énumère les devnodes PRÉSENTS du namespace d'identité HIDMaestro
    //     ROOT\VID_xxxx&PID_xxxx\HM_<16hex>, propriété vérifiée via SPDRP_HARDWAREID.
    //     Attrape les nœuds dont la clé Enum a été supprimée (invisibles au registre).
    private static List<string> EnumerateHmDevnodes()
    {
        var found = new List<string>();
        // pass 1: SetupDi (best effort - on some systems the call itself fails; then this
        // yields nothing and pass 2 still catches our nodes)
        try
        {
            foreach (var id in EnumerateHmDevnodesSetupDi())
                if (!found.Contains(id)) found.Add(id);
        }
        catch (Exception ex) { Log("sweep: SetupDi enumeration failed: " + ex.Message); }

        // pass 2: deterministic candidates computed from the identity keys we actually use
        // (P<n>, P<n>R, P<n>RR, P<n>RRR x mkb/kb/gp/x360 profiles + GPW/GPWX/GPWXX gamepad
        // keys), located with CM_Locate_DevNodeW - the CfgMgr32 calls this host already
        // relies on everywhere (proven working at activation time in the field logs).
        // The HM_<16hex> identity token is HIDMaestro's reserved namespace, so a located
        // candidate is provably ours (no foreign device can carry that token).
        // FR: candidats déterministes calculés depuis les clés d'identité réellement
        // utilisées, localisés via CM_Locate_DevNodeW - les appels CfgMgr32 déjà utilisés
        // partout par cet hôte (prouvés fonctionnels sur le terrain). Le token HM_<16hex>
        // est le namespace réservé de HIDMaestro : un candidat localisé est donc le nôtre.
        try
        {
            foreach (var id in CandidateHmDevnodes())
                if (!found.Contains(id)) found.Add(id);
        }
        catch (Exception ex) { Log("sweep: candidate enumeration failed: " + ex.Message); }

        return found;
    }

    private static System.Collections.Generic.IEnumerable<string> CandidateHmDevnodes()
    {
        string[] playerKeys = { "", "R", "RR", "RRR" };
        string[] gpKeys = { "", "X", "XX" };
        for (int p = 1; p <= PlayerCount; p++)
        {
            // mkb (V57e), kb-only (V57h) - identity key "P<n>" (+historic rotations)
            foreach (var profileSet in new[] { ProfileIds, KbProfileIds })
            {
                HMProfile prof = null;
                try { prof = _ctx?.GetProfile(profileSet[p - 1]); } catch { }
                if (prof == null) continue;
                foreach (var suffix in playerKeys)
                {
                    string id = ComputeInstanceId(prof, "P" + p + suffix);
                    if (NodeLocatable(id)) yield return id;
                }
            }
            // gamepad devices (DInput + XInput profiles) - identity key GPW<n> (+rotations)
            foreach (var gpSet in new[] { GpProfileIds, X360ProfileIds })
            {
                HMProfile prof = null;
                try { prof = _ctx?.GetProfile(gpSet[p - 1]); } catch { }
                if (prof == null) continue;
                foreach (var suffix in gpKeys)
                {
                    string id = ComputeInstanceId(prof, GpKeyPrefix + suffix + p);
                    if (NodeLocatable(id)) yield return id;
                }
            }
        }
    }

    private static List<string> EnumerateHmDevnodesSetupDi()
    {
        var found = new List<string>();
        const int DIGCF_PRESENT = 0x00000001;
        const int DIGCF_ALLCLASSES = 0x00000002;
        Guid emptyGuid = Guid.Empty;
        IntPtr dis = SetupDiGetClassDevsW(ref emptyGuid, IntPtr.Zero, IntPtr.Zero, DIGCF_PRESENT | DIGCF_ALLCLASSES);
        if (dis == IntPtr.Zero || dis == new IntPtr(-1)) return found;
        var sb = new StringBuilder(260);
        var rx = new System.Text.RegularExpressions.Regex(
            @"^ROOT\\VID_[0-9A-Fa-f]{4}&PID_[0-9A-Fa-f]{4}\\HM_[0-9A-Fa-f]{16}$");
        try
        {
            uint index = 0;
            while (true)
            {
                var did = new DeviceInfoData { cbSize = IntPtr.Size == 8 ? 32 : 28 };
                if (!SetupDiEnumDeviceInfo(dis, index++, ref did)) break; // ERROR_NO_MORE_ITEMS
                sb.Clear();
                if (!SetupDiGetDeviceInstanceIdW(dis, ref did, sb, (uint)sb.Capacity, out _)) continue;
                if (!rx.IsMatch(sb.ToString())) continue;
                // ownership: HardwareID must contain HIDMaestro (when the property is readable)
                byte[] hwBuf = new byte[1024];
                bool owned = true;
                if (SetupDiGetDeviceRegistryPropertyW(dis, ref did, 1 /*SPDRP_HARDWAREID*/, out _, hwBuf, (uint)hwBuf.Length, out uint hwLen)
                    && hwLen > 2)
                {
                    string hw = Encoding.Unicode.GetString(hwBuf, 0, (int)hwLen - 2);
                    owned = hw.IndexOf("HIDMaestro", StringComparison.OrdinalIgnoreCase) >= 0;
                }
                if (!owned) continue; // foreign device: never touch (issue #28)
                found.Add(sb.ToString());
            }
        }
        finally { SetupDiDestroyDeviceInfoList(dis); }
        return found;
    }

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern IntPtr SetupDiGetClassDevsW(ref Guid ClassGuid, IntPtr Enumerator, IntPtr hwndParent, int Flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiEnumDeviceInfo(IntPtr DeviceInfoSet, uint MemberIndex, ref DeviceInfoData DeviceInfoData);

    [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetupDiGetDeviceInstanceIdW(IntPtr DeviceInfoSet, ref DeviceInfoData DeviceInfoData,
        StringBuilder DeviceInstanceId, uint DeviceInstanceIdSize, out uint RequiredSize);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiGetDeviceRegistryPropertyW(IntPtr DeviceInfoSet, ref DeviceInfoData DeviceInfoData,
        uint Property, out uint PropertyRegDataType, byte[] PropertyBuffer, uint PropertyBufferSize, out uint RequiredSize);

    [DllImport("setupapi.dll", SetLastError = true)]
    private static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

    [StructLayout(LayoutKind.Sequential)]
    private struct DeviceInfoData
    {
        public int cbSize;
        public Guid ClassGuid;
        public uint DevInst;
        public IntPtr Reserved;
    }

    // EN: Dispose the player's controller AND clean its devnode (see CleanupNodeAfterDispose).
    // FR: Dispose le contrôleur du joueur ET nettoie son devnode (voir CleanupNodeAfterDispose).
    private static void DisposePlayerCtrl(int p, HMProfile profile)
    {
        try { _ctrl[p - 1]?.Dispose(); } catch { }
        _ctrl[p - 1] = null;
        CleanupNodeAfterDispose(profile, KeyFor(p), $"P{p}");
    }

    // EN: Dispose the player's gamepad controller AND clean its devnode.
    // FR: Dispose le contrôleur gamepad du joueur ET nettoie son devnode.
    private static void DisposeGpCtrl(int p, HMProfile profile)
    {
        try { _gpCtrl[p - 1]?.Dispose(); } catch { }
        _gpCtrl[p - 1] = null;
        CleanupNodeAfterDispose(profile, GpKeyFor(p), $"GP{p}");
    }

    [DllImport("CfgMgr32.dll")]
    private static extern uint CM_Disable_DevNode(uint dnDevInst, uint ulFlags);

    [DllImport("CfgMgr32.dll")]
    private static extern uint CM_Uninstall_DevNode(uint dnDevInst, uint ulFlags);

    // EN: Best-effort hard eviction of a wedged node: disable+uninstall the HID children
    //     and the parent (clears their Enum registry keys), then delete any surviving
    //     Enum key directly (HmHost runs as SYSTEM, so the delete is allowed). If an app
    //     still holds the live node open, the kernel node may linger until reboot, but
    //     with no started HID child it exposes no HID interface and stays invisible to
    //     RawInput - the rotated fresh node is the one that works.
    // FR: Éviction forcée au mieux d'un nœud bloqué : disable+uninstall des fils HID et du
    //     parent (vide leurs clés Enum), puis suppression directe de la clé Enum restante
    //     (HmHost tourne en SYSTEM, la suppression est autorisée). Si une app tient encore
    //     le nœud kernel ouvert, il peut persister jusqu'au reboot, mais sans fils HID
    //     démarré il n'expose aucune interface HID et reste invisible pour RawInput - c'est
    //     le nœud neuf rotaté qui fonctionne.
    private static void ForceEvictNode(string instId)
    {
        try
        {
            List<string> kids;
            try { kids = HIDMaestro.Internal.DeviceManager.GetAllHidChildIds(instId); }
            catch { kids = new List<string>(); }
            foreach (var c in kids)
            {
                if (CM_Locate_DevNodeW(out uint ci, c, 1) == 0)
                {
                    CM_Disable_DevNode(ci, 0);
                    CM_Uninstall_DevNode(ci, 0);
                }
            }
            if (CM_Locate_DevNodeW(out uint pi, instId, 1) == 0)
            {
                CM_Disable_DevNode(pi, 0);
                CM_Uninstall_DevNode(pi, 0);
            }
            Thread.Sleep(500);
            try
            {
                Microsoft.Win32.Registry.LocalMachine.DeleteSubKeyTree(
                    $@"SYSTEM\CurrentControlSet\Enum\{instId}", throwOnMissingSubKey: false);
            }
            catch (Exception ex) { Log($"ForceEvictNode registry delete {instId}: {ex.GetType().Name}"); }
            bool still = CM_Locate_DevNodeW(out _, instId, 1) == 0;
            Log($"ForceEvictNode({instId}) done, stillPresent(phantom)={still}");
        }
        catch (Exception ex) { Log($"ForceEvictNode({instId}) failed: {ex.Message}"); }
    }

    // ── post-create verification + SDK diag mirror ──────────
    // EN: The SDK's SetupController returns success even when the devnode never reaches
    //     DN_STARTED (the V75 adopt path can adopt a pending-removal node left by a
    //     force-killed prior session / a service reinstall, without rebinding the driver).
    //     HmHost verifies the real PnP state through the SDK's PUBLIC DeviceManager API
    //     and heals (restart, then remove + recreate by the caller) before reporting OK.
    // FR: SetupController du SDK répond succès même si le devnode n'atteint jamais
    //     DN_STARTED (le chemin d'adoption V75 peut adopter un nœud en attente de
    //     suppression laissé par une session tuée en force / une réinstall du service,
    //     sans rebind du pilote). HmHost vérifie l'état PnP réel via l'API PUBLIQUE
    //     DeviceManager du SDK et répare (restart, puis remove + recréation par l'appelant)
    //     avant de répondre OK.

    private static (string InstId, bool Live) VerifyDevice(HMProfile profile, string key, int budgetMs, bool attemptHeal)
    {
        string instId = ComputeInstanceId(profile, key);
        bool live = WaitDeviceLive(instId, budgetMs);
        if (!live && attemptHeal)
        {
            Log($"device not started ({DescribeDevNode(instId)}); trying RestartDevice heal");
            DrainSdkDiag("HEAL_RESTART");
            try { HIDMaestro.Internal.DeviceManager.RestartDevice(instId); }
            catch (Exception ex) { Log("RestartDevice ex: " + ex.Message); }
            live = WaitDeviceLive(instId, 8000);
            if (live) Log($"healed via RestartDevice: {instId}");
        }
        if (!live)
            Log($"device still not started: {DescribeDevNode(instId)}");
        DrainSdkDiag("VERIFY");
        return (instId, live);
    }

    private static bool WaitDeviceLive(string instId, int budgetMs)
    {
        // [V83-UMDF2-VERIFY] EN: liveness = the HID CHILD must be DN_STARTED (that is the
        //     interface mouhid/kbdhid bind to and RawInput sees). The parent alone being
        //     started is NOT enough: on an adopted pending-removal devnode the parent can
        //     be started while the child never starts (observed: step=4.wait_started
        //     burns its full 5000ms budget, mouse dead).
        //     FR: la vivacité = le fils HID doit être DN_STARTED (c'est l'interface sur
        //     laquelle mouhid/kbdhid se lient et que RawInput voit). Le parent seul démarré
        //     ne suffit PAS : sur un devnode adopté en attente de suppression, le parent
        //     peut être démarré pendant que le fils ne démarre jamais (observé :
        //     step=4.wait_started brûle tout son budget de 5000 ms, souris morte).
        var sw = Stopwatch.StartNew();
        while (true)
        {
            string child = null;
            bool childStarted = false;
            try { child = HIDMaestro.Internal.DeviceManager.GetHidChildId(instId); } catch { }
            if (child != null)
            {
                try { childStarted = HIDMaestro.Internal.DeviceManager.IsDeviceStarted(child); } catch { }
            }
            // [V83] require BOTH: the ROOT parent must be DN_STARTED (driver bound+started -
            // a driverless parent shows as Code 1 "unknown device") AND the HID child must
            // be DN_STARTED (the interface RawInput/mouhid actually use).
            if (ParentStarted(instId) && child != null && childStarted) return true;
            if (sw.ElapsedMilliseconds >= budgetMs) return false;
            Thread.Sleep(100);
        }
    }

    private const uint DN_STARTED_FLAG = 0x00000008;

    private static bool ParentStarted(string instId)
    {
        try
        {
            if (CM_Locate_DevNodeW(out uint dn, instId, 0) != 0) return false;
            return CM_Get_DevNode_Status(out uint st, out _, dn, 0) == 0 && (st & DN_STARTED_FLAG) != 0;
        }
        catch { return false; }
    }

    // EN: Mirrors HIDMaestro.Internal.DeviceIdentity: token = "HM_" + first 8 bytes
    //     (big-endian) of SHA256("HIDMaestro.DeviceIdentity.v1\n" + key), X16.
    //     Plain-HID enumerator is VID_xxxx&PID_xxxx (VID 045E adds &IG_00).
    // FR: Miroir de HIDMaestro.Internal.DeviceIdentity : token = « HM_ » + 8 premiers
    //     octets (big-endian) de SHA256("HIDMaestro.DeviceIdentity.v1\n" + key), en X16.
    //     Énumérateur HID simple = VID_xxxx&PID_xxxx (VID 045E ajoute &IG_00).
    private static string ComputeInstanceId(HMProfile profile, string key)
    {
        byte[] digest = System.Security.Cryptography.SHA256.HashData(
            Encoding.UTF8.GetBytes("HIDMaestro.DeviceIdentity.v1\n" + key));
        ulong h = System.Buffers.Binary.BinaryPrimitives.ReadUInt64BigEndian(digest);
        string token = "HM_" + h.ToString("X16");
        string vid = $"{profile.VendorId:X4}";
        string pid = $"{profile.ProductId:X4}";
        string enumerator = profile.VendorId == 0x045E
            ? $"VID_{vid}&PID_{pid}&IG_00"
            : $"VID_{vid}&PID_{pid}";
        return $@"ROOT\{enumerator}\{token}";
    }

    [DllImport("CfgMgr32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern uint CM_Locate_DevNodeW(out uint pdnDevInst, string pDeviceID, uint ulFlags);

    [DllImport("CfgMgr32.dll")]
    private static extern uint CM_Get_DevNode_Status(out uint pulStatus, out uint pulProblemNumber, uint dnDevInst, uint ulFlags);

    private static string DescribeDevNode(string instId)
    {
        try
        {
            uint rcLive = CM_Locate_DevNodeW(out uint dn, instId, 0);
            uint rcPhantom = CM_Locate_DevNodeW(out uint dnP, instId, 1);
            if (rcLive != 0 && rcPhantom != 0) return $"{instId}: ABSENT";
            uint devInst = rcLive == 0 ? dn : dnP;
            uint rc = CM_Get_DevNode_Status(out uint status, out uint problem, devInst, 0);
            string parent = $"{instId}: {(rcLive == 0 ? "LIVE" : "PHANTOM")} status=0x{status:X8} problem=0x{problem:X8} (rc=0x{rc:X8})";
            // [V83-UMDF2-VERIFY] also describe the HID child: that is the node whose
            // DN_STARTED actually matters for input (see WaitDeviceLive).
            string child = null;
            try { child = HIDMaestro.Internal.DeviceManager.GetHidChildId(instId); } catch { }
            if (child == null) return parent + " | no HID child";
            uint rcC = CM_Locate_DevNodeW(out uint dnC, child, 0);
            if (rcC != 0) return parent + $" | child {child}: NOT LIVE";
            uint rcCS = CM_Get_DevNode_Status(out uint stC, out uint prC, dnC, 0);
            return parent + $" | child {child}: LIVE status=0x{stC:X8} problem=0x{prC:X8} (rc=0x{rcCS:X8})";
        }
        catch (Exception ex) { return $"{instId}: describe failed ({ex.GetType().Name})"; }
    }

    private static long _diagOffset = -1;
    private static long _timingOffset = -1;

    private static long InitialLen(string path)
    {
        try { return File.Exists(path) ? new FileInfo(path).Length : 0; }
        catch { return 0; }
    }

    // EN: Mirror the SDK's %TEMP%\HIDMaestro diag/timing files into HmHost.log so a user
    //     log bundle is self-contained (HIDMAESTRO_DIAG=1 is set in Main before SDK init).
    // FR: Miroir les fichiers diag/timing du SDK dans HmHost.log pour un bundle de logs
    //     utilisateur autonome (HIDMAESTRO_DIAG=1 est posé dans Main avant l'init du SDK).
    private static void DrainSdkDiag(string tag)
    {
        try
        {
            string dir = Path.Combine(Path.GetTempPath(), "HIDMaestro");
            DrainOne(Path.Combine(dir, "teardown_diag.log"), ref _diagOffset, "sdk-diag", tag);
            DrainOne(Path.Combine(dir, "setup_timing.log"), ref _timingOffset, "sdk-timing", tag);
        }
        catch { }
    }

    private static void DrainOne(string path, ref long offset, string prefix, string tag)
    {
        try
        {
            if (!File.Exists(path)) return;
            if (offset < 0) offset = 0;
            byte[] data;
            long newOffset;
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                if (fs.Length <= offset) return;
                fs.Seek(offset, SeekOrigin.Begin);
                int len = (int)(fs.Length - offset);
                data = new byte[len];
                int read = 0;
                while (read < len)
                {
                    int r = fs.Read(data, read, len - read);
                    if (r <= 0) break;
                    read += r;
                }
                Array.Resize(ref data, read);
                newOffset = fs.Length;
            }
            string text = Encoding.UTF8.GetString(data);
            // only consume up to the last complete line; keep the partial tail for next drain
            int lastNl = text.LastIndexOf('\n');
            if (lastNl < 0) return;
            newOffset -= (text.Length - lastNl - 1);
            offset = newOffset;
            int start = 0;
            while (start <= lastNl)
            {
                int nl = text.IndexOf('\n', start);
                if (nl < 0 || nl > lastNl) break;
                string line = text.Substring(start, nl - start).TrimEnd('\r');
                if (line.Length > 0) Log($"[{prefix}:{tag}] {line}");
                start = nl + 1;
            }
        }
        catch { }
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
