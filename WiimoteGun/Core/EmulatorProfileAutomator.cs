using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using WiimoteGun;

namespace WiimoteGun.Core
{
    /// <summary>
    /// EN/FR: Automatically updates emulator DirectInput indices in profile files.
    /// Met à jour automatiquement les index DirectInput des émulateurs dans les fichiers de profil.
    /// </summary>
    public static class EmulatorProfileAutomator
    {
        private static readonly Regex DInputRegex = new Regex(@"DInput-(\d+)", RegexOptions.Compiled);

        // [V57g] EN: Full DInput binding token regex: captures the source ("DInput-<n>")
        //     and the sub-binding ("Button<N>" / "<sign>Axis<N>") so an XInput player's
        //     gun section can be rewritten to "XInput-<slot>/<translated-binding>".
        //     FR: Regex du token de binding DInput complet : capture la source
        //     (« DInput-<n> ») et le sous-binding (« Button<N> » / « <signe>Axis<N> »)
        //     pour réécrire la section gun d'un joueur XInput en
        //     « XInput-<slot>/<binding-traduit> ».
        private static readonly Regex DInputBindingRegex = new Regex(@"DInput-\d+(/[-+\w]+)", RegexOptions.Compiled);

        // [V57g] EN: DInput -> XInput binding-name translation (vmulti DInput Button<N> is
        //     vmulti bit N; the XInput token names follow each emulator's XInput source
        //     enumeration). TO VERIFY ON TARGET (plan P5.5): bind one guncon2 input BY
        //     HAND in DuckStation/PCSX2 with a physical XInput pad, read the profile the
        //     emulator writes, and adjust these tables if the tokens differ - they are
        //     the single point of correction. DuckStation: buttons follow the XINPUT_GAMEPAD
        //     wButtons order (Button0=DPadUp..Button13=Y), triggers are analog axes
        //     (Axis4=LT, Axis5=RT), sticks Axis0..Axis3 = LX/LY/RX/RY (same positional
        //     layout as the DInput profile, so the IR-driven relative axes keep working).
        //     FR: Traduction des noms de bindings DInput -> XInput (le Button<N> DInput
        //     vmulti est le bit N vmulti ; les tokens XInput suivent l'énumération de la
        //     source XInput de chaque émulateur). À VÉRIFIER SUR CIBLE (plan P5.5) :
        //     binder UNE entrée guncon2 À LA MAIN dans DuckStation/PCSX2 avec une manette
        //     XInput physique, lire le profil écrit par l'émulateur, et corriger ces
        //     tables si les tokens diffèrent - elles sont le point de correction unique.
        //     DuckStation : les boutons suivent l'ordre wButtons de XINPUT_GAMEPAD
        //     (Button0=DPadUp..Button13=Y), les gâchettes sont des axes analogiques
        //     (Axis4=LT, Axis5=RT), les sticks Axis0..Axis3 = LX/LY/RX/RY (même layout
        //     positionnel que le profil DInput, les axes relatifs pilotés par l'IR
        //     continuent donc de fonctionner).
        private static readonly Dictionary<string, string> DuckStationXInputMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // vmulti DInput button bit -> DuckStation XInput button token
            // (EN/FR: bit bouton DInput vmulti -> token bouton XInput DuckStation)
            { "Button0",  "Button10" },  // A          (wButtons idx 10)
            { "Button1",  "Button11" },  // B          (idx 11) - guncon2 Trigger (wiimote B)
            { "Button2",  "Button12" },  // X          (idx 12)
            { "Button3",  "Button13" },  // Y          (idx 13)
            { "Button4",  "Button8"  },  // LB         (idx 8)
            { "Button5",  "Button9"  },  // RB         (idx 9)
            { "Button6",  "+Axis4"   },  // LT digital -> analog trigger pull (Axis4)
            { "Button7",  "+Axis5"   },  // RT digital -> analog trigger pull (Axis5)
            { "Button8",  "Button5"  },  // Back       (idx 5)
            { "Button9",  "Button4"  },  // Start      (idx 4)
            { "Button10", "Button6"  },  // L3         (idx 6)
            { "Button11", "Button7"  },  // R3         (idx 7)
            { "Button12", "Button0"  },  // DPadUp     (idx 0)
            { "Button13", "Button1"  },  // DPadDown   (idx 1)
            { "Button14", "Button2"  },  // DPadLeft   (idx 2)
            { "Button15", "Button3"  },  // DPadRight  (idx 3)
            // axes: same positional layout (LX, LY, RX, RY)
            // (EN/FR: axes : même layout positionnel (LX, LY, RX, RY))
            { "Axis0", "Axis0" },
            { "Axis1", "Axis1" },
            { "Axis2", "Axis2" },
            { "Axis3", "Axis3" },
        };

        // [V57g] PCSX2: best-knowledge tokens pending the same on-target verification.
        //     (EN/FR: PCSX2 : tokens au mieux de nos connaissances, en attente de la
        //     même vérification sur cible.)
        private static readonly Dictionary<string, string> Pcsx2XInputMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Button0",  "Button10" },  // A
            { "Button1",  "Button11" },  // B - guncon2 Trigger
            { "Button2",  "Button12" },  // X
            { "Button3",  "Button13" },  // Y
            { "Button4",  "Button8"  },  // LB
            { "Button5",  "Button9"  },  // RB
            { "Button6",  "+Axis4"   },  // LT digital -> analog
            { "Button7",  "+Axis5"   },  // RT digital -> analog
            { "Button8",  "Button5"  },  // Back/Select
            { "Button9",  "Button4"  },  // Start
            { "Button10", "Button6"  },  // L3
            { "Button11", "Button7"  },  // R3
            { "Button12", "Button0"  },  // DPadUp
            { "Button13", "Button1"  },  // DPadDown
            { "Button14", "Button2"  },  // DPadLeft
            { "Button15", "Button3"  },  // DPadRight
            { "Axis0", "Axis0" },
            { "Axis1", "Axis1" },
            { "Axis2", "Axis2" },
            { "Axis3", "Axis3" },
        };

        // EN: Mapping dictionary GameId -> FriendlyName for Dolphin profiles
        // FR: Dictionnaire de correspondance GameId -> Nom convivial pour les profils Dolphin
        private static readonly Dictionary<string, string> DolphinGameMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "E72JAF", "STARBLADE" },
            { "E78JAF", "SOLVALOU" },
            { "R2VE01", "SinPunishment" },
            { "R5IE4Q", "TOYSTORYMania" },
            { "R8LE20", "CHICKENBLASTER" },
            { "R8XE52", "JURASSIC" },
            { "R8XZ52", "TopShotDinosaurHunter" },
            { "RBUE08", "REUmbrella" },
            { "RCJE8P", "CONDUIT" },
            { "SC2E8P", "CONDUIT_SC2E8P" },
            { "RD2E41", "RedSteel2" },
            { "REDE41", "REDSTEEL" },
            { "RGDEA4", "TARGETTERROR" },
            { "RGSE8P", "GHOSTSQUAD" },
            { "RHAE01", "WiiPlay" },
            { "RHDE8P", "HOTD23" },
            { "RHOE8P", "HOTDOverkill" },
            { "RL6E69", "NERF_ELITE" },
            { "RM2E69", "MedalOfHonorHeroes2" },
            { "RMRE5Z", "COCOTOMC" },
            { "RNKE69", "NERF" },
            { "RQ5E5G", "MadDogMcCree" },
            { "RQ7E20", "MARTIANPANIC" },
            { "RQPZ52", "CABELASMBH" },
            { "RRBE41", "RaymanRavingRabbids" },
            { "RY2E41", "RaymanRavingRabbids2" },
            { "RY3E41", "RaymanRavingRabbids_TV" },
            { "RZJE69", "DEADSPACE" },
            { "RZPE01", "LINKCROSSBOW" },
            { "S3AE5G", "ATTACKMOVIES" },
            { "SBDE08", "REDarkside" },
            { "SBHEFP", "RemingtonBIRD" },
            { "SBSEFP", "RemingtonNA" },
            { "SJUE20", "DINOSTRIKE" },
            { "SN2E69", "NERF_NstrikeDoubleBlastBundle" },
            { "SRKEFP", "RemingtonALASKA" },
            { "SS7EFP", "RemingtonAFRICA" },
            { "ST5E52", "Transformers" },
            { "ST9E52", "TopShotArcade" },
            { "STDEFP", "RELOAD" },
            { "SUVE52", "CABELASHunts2013" },
            { "SW7EVN", "GUNSLINGERS" },
            { "SW9EVN", "WickedMonstersBlast" },
            { "W6BE01", "ECOShooter" },
            { "W9BEZJ", "BIGTOWN" },
            { "WB4EGL", "WILDWESTGUNS" },
            { "WCREHW", "CARNIVALKING" },
            { "WFAEJS", "FASTDRAWSD" },
            { "WHFETY", "HeavyFireSO" },
            { "WSUE18", "SHOOTANDO" },
            { "WZPERZ", "ZOMBIEPANIC" }
        };

        public static void UpdateProfiles(IEnumerable<WiiMoteController> controllers)
        {
            try
            {
                var controllerList = controllers.ToList();
                var dinputIndices = new Dictionary<int, int>(); // playerIndex -> dinputIndex (1-based)
                var xinputSlots = new Dictionary<int, int>();  // [V57g] playerIndex -> XInput slot (0-based)
                bool anyGamePadActive = false;
                bool anyXInputActive = false; // [V57g]

                foreach (var c in controllerList)
                {
                    bool isGamePadMode = (c.Mode == WiiMoteMode.GamePad || c.Mode == WiiMoteMode.GamePad43 || c.Mode == WiiMoteMode.GamePadFPS);
                    if (!isGamePadMode)
                        continue;

                    // Filter active gamepads: Only controllers in GamePad mode with a valid DInput index
                    // (EN/FR: Filtrer gamepads actifs : Uniquement contrôleurs en mode GamePad avec index DInput valide)
                    if (c.DInputIndex > 0)
                    {
                        dinputIndices[c.PlayerIndex] = c.DInputIndex;
                        anyGamePadActive = true;
                    }

                    // [V57g] EN: XInput players (UMDF2 XUSB slot detected, or ViGEm in the
                    //     VMulti mode) count as active gamepads too - DuckStation/PCSX2 gun
                    //     profiles must NOT be inhibited for them. The slot (not the player
                    //     number) is what the bindings address.
                    //     FR: Les joueurs XInput (slot XUSB UMDF2 détecté, ou ViGEm en mode
                    //     VMulti) comptent aussi comme gamepads actifs - les profils gun
                    //     DuckStation/PCSX2 ne doivent PAS être inhibés pour eux. Le slot
                    //     (pas le numéro de joueur) est ce que les bindings adressent.
                    if (c.UsesXInputOutput && c.XInputSlot >= 0)
                    {
                        xinputSlots[c.PlayerIndex] = c.XInputSlot;
                        anyGamePadActive = true;
                        anyXInputActive = true;
                    }
                }

                // [V57g] EN: Dolphin's emulated-wiimote backend is DInput-only: an XInput
                //     device (UMDF2 XUSB or ViGEm) cannot be bound by the generated Wiimote
                //     profiles, so they stay inhibited for XInput players - said explicitly.
                //     FR: Le backend wiimote émulée de Dolphin est DInput uniquement : un
                //     device XInput (XUSB UMDF2 ou ViGEm) ne peut pas être bindé par les
                //     profils Wiimote générés, ils restent donc inhibés pour les joueurs
                //     XInput - dit explicitement.
                if (anyXInputActive)
                {
                    SimpleLogger.Instance.Info("[ProfileAutomator] XInput mode: Dolphin emulated-wiimote profiles cannot use XInput devices (DInput backend) - left inhibited.");
                }

                // Removed early return to allow "cleanup" (tagging) in Mouse mode
                // (EN/FR: Suppression du retour anticipé pour permettre le nettoyage en mode Souris)

                // EN: Get RetroBat registry path (preferred)
                // FR: Obtenir le chemin RetroBat du registre (préféré)
                string retroBatPath = RemapProfileManager.GetRetroBatPath();
                List<string> emulatorRoots = new List<string>();

                if (!string.IsNullOrEmpty(retroBatPath))
                {
                    string emuPath = Path.Combine(retroBatPath, "emulators");
                    if (Directory.Exists(emuPath))
                    {
                        emulatorRoots.Add(emuPath);
                        
                        // EN: Check if it's a symlink/junction for user visibility
                        // FR: Vérifier si c'est un symlink/junction pour la visibilité utilisateur
                        try {
                            FileAttributes attr = File.GetAttributes(emuPath);
                            if ((attr & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint) {
                                SimpleLogger.Instance.Info(string.Format("[ProfileAutomator] Found RetroBat emulators root (Link/Junction): {0}", emuPath));
                            } else {
                                SimpleLogger.Instance.Info(string.Format("[ProfileAutomator] Found RetroBat emulators root: {0}", emuPath));
                            }
                        } catch {
                            SimpleLogger.Instance.Info(string.Format("[ProfileAutomator] Found RetroBat emulators root: {0}", emuPath));
                        }
                    }
                }

                // Standalone / Manual paths (EN/FR: Chemins autonomes / manuels)
                if (!string.IsNullOrEmpty(Options.Instance.PCSX2Path) && Directory.Exists(Options.Instance.PCSX2Path))
                {
                    emulatorRoots.Add(Options.Instance.PCSX2Path);
                    SimpleLogger.Instance.Info(string.Format("[ProfileAutomator] Adding manual PCSX2 path: {0}", Options.Instance.PCSX2Path));
                }

                if (!string.IsNullOrEmpty(Options.Instance.DuckStationPath) && Directory.Exists(Options.Instance.DuckStationPath))
                {
                    emulatorRoots.Add(Options.Instance.DuckStationPath);
                    SimpleLogger.Instance.Info(string.Format("[ProfileAutomator] Adding manual DuckStation path: {0}", Options.Instance.DuckStationPath));
                }

                if (!string.IsNullOrEmpty(Options.Instance.DolphinPath) && Directory.Exists(Options.Instance.DolphinPath))
                {
                    emulatorRoots.Add(Options.Instance.DolphinPath);
                    SimpleLogger.Instance.Info(string.Format("[ProfileAutomator] Adding manual Dolphin path: {0}", Options.Instance.DolphinPath));
                }

                if (!string.IsNullOrEmpty(Options.Instance.CemuPath) && Directory.Exists(Options.Instance.CemuPath))
                {
                    emulatorRoots.Add(Options.Instance.CemuPath);
                    SimpleLogger.Instance.Info(string.Format("[ProfileAutomator] Adding manual Cemu path: {0}", Options.Instance.CemuPath));
                }

                if (!emulatorRoots.Any())
                {
                    SimpleLogger.Instance.Warning("[ProfileAutomator] No emulator folders found. Skipping profile updates.");
                    return;
                }

                if (!anyGamePadActive)
                {
                    SimpleLogger.Instance.Info("[ProfileAutomator] No virtual gamepads detected (Mouse mode or no active GamePad). Proceeding with inhibition tags.");
                }

                foreach (var root in emulatorRoots)
                {
                    // EN: Check both <root>\<emuName>\<dir> and <root>\<dir> for Standalone support.
                    // FR: Vérifier à la fois <root>\<emuName>\<dir> et <root>\<dir> pour le support Standalone.
                    
                    // Update input profiles (EN/FR: Mettre à jour les profils d'entrée)
                    string dsProfileDir = FindEmulatorSubDir(root, "duckstation", "inputprofiles");
                    if (dsProfileDir != null) UpdateDuckStationProfiles(dsProfileDir, dinputIndices, xinputSlots);

                    string ps2ProfileDir = FindEmulatorSubDir(root, "pcsx2", "inputprofiles");
                    if (ps2ProfileDir != null) UpdatePCSX2Profiles(ps2ProfileDir, dinputIndices, xinputSlots);

                    // Update game settings (EN/FR: Mettre à jour les paramètres de jeu)
                    string dsSettingsDir = FindEmulatorSubDir(root, "duckstation", "gamesettings");
                    if (dsSettingsDir != null) UpdateDuckStationGameSettings(dsSettingsDir, anyGamePadActive);

                    string ps2SettingsDir = FindEmulatorSubDir(root, "pcsx2", "gamesettings");
                    if (ps2SettingsDir != null) UpdatePCSX2GameSettings(ps2SettingsDir, anyGamePadActive);

                    // Dolphin Support (EN/FR: Support Dolphin)
                    string dolphinConfigDir = FindEmulatorSubDir(root, "dolphin-emu", Path.Combine("User", "Config"));
                    if (dolphinConfigDir != null)
                    {
                        try
                        {
                            string dolphinUserDir = Directory.GetParent(dolphinConfigDir).FullName;
                            string dolphinProfilesDir = Path.Combine(dolphinUserDir, "Config", "Profiles", "Wiimote");
                            string dolphinSettingsDir = Path.Combine(dolphinUserDir, "GameSettings");

                            // EN: Ensure directories exist
                            // FR: S'assurer que les dossiers existent
                            if (!Directory.Exists(dolphinProfilesDir)) Directory.CreateDirectory(dolphinProfilesDir);
                            if (!Directory.Exists(dolphinSettingsDir)) Directory.CreateDirectory(dolphinSettingsDir);

                            UpdateDolphinProfiles(dolphinConfigDir, dolphinProfilesDir, dinputIndices);
                            // [V57h] EN: Patch the `Device = DInput/0/<name> HID` line of
                            //     EVERY *-wiimotegun.ini (hard-coded templates AND the
                            //     profiles the user created from them) to the gamepad
                            //     device name of the selected driver mode - re-run on every
                            //     UpdateProfiles so a mode change is always reflected.
                            //     FR: Patche la ligne « Device = DInput/0/<nom> HID » de
                            //     TOUS les *-wiimotegun.ini (templates codés en dur ET
                            //     profils créés par l'utilisateur) vers le nom de device
                            //     gamepad du mode pilote sélectionné - rejoué à chaque
                            //     UpdateProfiles pour qu'un changement de mode soit
                            //     toujours répercuté.
                            PatchDolphinDeviceNames(dolphinProfilesDir);
                            GenerateMissingDolphinSettings(dolphinSettingsDir, dolphinProfilesDir);
                            // [V57h] EN: Dolphin masks follow the DINPUT players ONLY: the
                            //     emulated-wiimote backend cannot bind an XInput device
                            //     (UMDF2 XUSB or ViGEm), so an XInput-only player must NOT
                            //     unmask Dolphin profiles pointing at a DInput device that
                            //     does not exist in that mode (matches the explicit V57g log).
                            //     FR: Le masquage Dolphin suit UNIQUEMENT les joueurs DINPUT :
                            //     le backend wiimote émulée ne peut pas binder un device
                            //     XInput (XUSB UMDF2 ou ViGEm), un joueur XInput seul ne
                            //     doit donc PAS démasquer des profils Dolphin pointant un
                            //     device DInput inexistant dans ce mode (cohérent avec le
                            //     log V57g explicite).
                            UpdateDolphinGameSettings(dolphinSettingsDir, dinputIndices.Count > 0);

                            // [V57q] EN: Register the Wiimote4Guns DInput gamepads in ES's
                            //     es_input.cfg (text-level idempotent patch, one-time backup,
                            //     never touches existing entries) so EmulationStation
                            //     detects them automatically.
                            //     FR: Enregistre les gamepads DInput Wiimote4Guns dans l'
                            //     es_input.cfg d'ES (patch texte idempotent, sauvegarde
                            //     unique, ne touche jamais les entrées existantes) pour
                            //     qu'EmulationStation les détecte automatiquement.
                            EsInputConfigurator.EnsureGamepadEntries();
                        }
                        catch (Exception dex)
                        {
                            SimpleLogger.Instance.Error("[ProfileAutomator] Error in Dolphin support block: " + dex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error("[ProfileAutomator] Error updating profiles: " + ex.Message);
            }
        }

        private static void GenerateMissingDolphinSettings(string settingsDir, string profilesDir)
        {
            try
            {
                SimpleLogger.Instance.Info(string.Format("[ProfileAutomator] Generating missing Dolphin settings for {0} games from hardcoded mapping.", DolphinGameMappings.Count));

                foreach (var mapping in DolphinGameMappings)
                {
                    string gameId = mapping.Key;
                    string gamePrefix = mapping.Value;

                    string userFilePath = Path.Combine(settingsDir, gameId + ".ini");
                    string userFilePathCrt = userFilePath + "-wiimotegun";

                    // EN: Check if file already exists in either state
                    // FR: Vérifier si le fichier existe déjà (actif ou masqué)
                    if (File.Exists(userFilePath) || File.Exists(userFilePathCrt))
                    {
                        continue;
                    }

                    try
                    {
                        // EN: Generate GameSettings content from template
                        // FR: Générer le contenu de GameSettings à partir du template
                        string content = string.Format(DOLPHIN_GAME_SETTINGS_TEMPLATE, gamePrefix);
                        File.WriteAllText(userFilePath, content, Encoding.UTF8);
                        SimpleLogger.Instance.Info(string.Format("[ProfileAutomator] Generated GameSettings for: {0} (ID: {1})", gamePrefix, gameId));

                        // EN/FR: [V55p] Register the generated file in the tag memory:
                        // managed from birth (mask in Mouse mode, unmask in GamePad mode).
                        TagMemoryStore.Register("Dolphin", settingsDir, gameId + ".ini", TagMemoryStore.StateActive);

                        // EN: Generate game-specific profiles
                        // FR: Générer les profils spécifiques au jeu
                        CreateDolphinWiimoteProfiles(profilesDir, gamePrefix);
                    }
                    catch (Exception ex)
                    {
                        SimpleLogger.Instance.Error(string.Format("[ProfileAutomator] Error generating Dolphin settings for ID {0}: {1}", gameId, ex.Message));
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error("[ProfileAutomator] Error in GenerateMissingDolphinSettings: " + ex.Message);
            }
        }

        private static void CreateDolphinWiimoteProfiles(string profilesDir, string prefix)
        {
            string[] suffixes = { "a", "b", "c", "d" };
            for (int i = 1; i <= 4; i++)
            {
                string fileName = string.IsNullOrEmpty(prefix) 
                    ? string.Format("P{0}-wiimotegun.ini", i)
                    : string.Format("{0}_P{1}-wiimotegun.ini", prefix, i);
                
                string profilePath = Path.Combine(profilesDir, fileName);
                if (!File.Exists(profilePath))
                {
                    try
                    {
                        string template = DOLPHIN_WIIMOTE_TEMPLATE.Replace("{suffix}", suffixes[i - 1]);
                        File.WriteAllText(profilePath, template, Encoding.UTF8);
                        SimpleLogger.Instance.Info(string.Format("[ProfileAutomator] Created Dolphin profile: {0}", fileName));
                    }
                    catch (Exception ex)
                    {
                        SimpleLogger.Instance.Error(string.Format("[ProfileAutomator] Error creating Dolphin profile {0}: {1}", fileName, ex.Message));
                    }
                }
            }
        }

        // [V57h] EN: Patch the `Device = DInput/0/<name> HID` line of EVERY *-wiimotegun.ini
        //     in the Dolphin Wiimote profiles folder - the hard-coded templates AND the
        //     profiles the user created from them (any file matching the pattern). The
        //     device name follows the DRIVER MODE selected by the user:
        //       - RawInput (VMulti):  DInput/0/vmulti{a|b|c|d} HID   (per-player suffix)
        //       - RawInput (UMDF2):   DInput/0/GamePad Wiimote4Guns P<n> HID
        //     The pass is IDEMPOTENT and re-runs on every UpdateProfiles (device changes,
        //     mode switches): a mode change is always reflected, back and forth.
        //     FR: Patche la ligne « Device = DInput/0/<nom> HID » de TOUS les
        //     *-wiimotegun.ini du dossier de profils Wiimote de Dolphin - les templates
        //     codés en dur ET les profils créés par l'utilisateur (tout fichier matchant
        //     le motif). Le nom du device suit le MODE PILOTE sélectionné par
        //     l'utilisateur :
        //       - RawInput (VMulti) :  DInput/0/vmulti{a|b|c|d} HID   (suffixe par joueur)
        //       - RawInput (UMDF2)  :  DInput/0/GamePad Wiimote4Guns P<n> HID
        //     La passe est IDEMPOTENTE et rejouée à chaque UpdateProfiles (changements de
        //     devices, de mode) : un changement de mode est toujours répercuté, dans un
        //     sens comme dans l'autre.
        private static void PatchDolphinDeviceNames(string profilesDir)
        {
            try
            {
                if (!Directory.Exists(profilesDir))
                    return;

                bool umdf2 = Options.Instance.DefaultMouseMode == MouseMode.RawInputUmdf;
                string[] files = Directory.GetFiles(profilesDir, "*-wiimotegun.ini");
                if (files.Length == 0)
                    return;

                var deviceRegex = new Regex(@"^Device\s*=\s*DInput/0/(\S.*?)\s*HID\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
                int patched = 0;

                foreach (string file in files)
                {
                    try
                    {
                        // EN: Derive the player from the file name: "P<n>-wiimotegun.ini"
                        //     or "<prefix>_P<n>-wiimotegun.ini" (CreateDolphinWiimoteProfiles
                        //     naming; user copies keep it).
                        //     FR: Déduit le joueur du nom de fichier : « P<n>-wiimotegun.ini »
                        //     ou « <prefix>_P<n>-wiimotegun.ini » (nommage de
                        //     CreateDolphinWiimoteProfiles ; les copies utilisateur le
                        //     conservent).
                        Match m = Regex.Match(Path.GetFileName(file), @"P(\d)-wiimotegun\.ini$", RegexOptions.IgnoreCase);
                        if (!m.Success)
                            continue;
                        int player = int.Parse(m.Groups[1].Value);
                        if (player < 1 || player > 4)
                            continue;

                        string desired = umdf2
                            ? "GamePad Wiimote4Guns P" + player
                            : "vmulti" + "abcd"[player - 1];

                        string[] lines = File.ReadAllLines(file);
                        bool changed = false;
                        for (int i = 0; i < lines.Length; i++)
                        {
                            Match dm = deviceRegex.Match(lines[i].Trim());
                            if (!dm.Success)
                                continue;
                            string currentName = dm.Groups[1].Value.Trim();
                            if (!string.Equals(currentName, desired, StringComparison.Ordinal))
                            {
                                lines[i] = "Device = DInput/0/" + desired + " HID";
                                changed = true;
                            }
                        }

                        if (changed)
                        {
                            File.WriteAllLines(file, lines, Encoding.UTF8);
                            patched++;
                            SimpleLogger.Instance.Info(string.Format(
                                "[ProfileAutomator] Dolphin device name patched: {0} -> {1} [{2} mode]",
                                Path.GetFileName(file), desired, umdf2 ? "UMDF2" : "VMulti"));
                        }
                    }
                    catch (Exception ex)
                    {
                        SimpleLogger.Instance.Error(string.Format("[ProfileAutomator] Error patching Dolphin device name in {0}: {1}", file, ex.Message));
                    }
                }

                if (patched > 0)
                    SimpleLogger.Instance.Info(string.Format("[ProfileAutomator] Dolphin Device lines patched in {0} profile(s) for the {1} driver mode.", patched, umdf2 ? "UMDF2 (GamePad Wiimote4Guns Pn)" : "VMulti (vmultia..d)"));
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error("[ProfileAutomator] PatchDolphinDeviceNames error: " + ex.Message);
            }
        }

        private static void UpdateDolphinProfiles(string configDir, string profilesDir, Dictionary<int, int> dinputIndices)
        {
            // EN: Create generic profiles (FR: Créer les profils génériques)
            CreateDolphinWiimoteProfiles(profilesDir, "");

            // EN: Update WiimoteNew.ini to Source=1 (Emulated Wiimote) for all 4 sections
            // FR: Mettre à jour WiimoteNew.ini sur Source=1 (Wiimote émulée) pour les 4 sections
            string configFile = Path.Combine(configDir, "WiimoteNew.ini");
            if (File.Exists(configFile))
            {
                try
                {
                    string[] lines = File.ReadAllLines(configFile);
                    var sectionSequence = new List<string>();
                    var sectionsLines = new Dictionary<string, List<string>>();
                    string currentSection = "Header";
                    sectionSequence.Add(currentSection);
                    sectionsLines[currentSection] = new List<string>();
                    
                    foreach (var line in lines)
                    {
                        string trimmed = line.Trim();
                        if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                        {
                            currentSection = trimmed.Substring(1, trimmed.Length - 2);
                            if (!sectionsLines.ContainsKey(currentSection))
                            {
                                sectionsLines[currentSection] = new List<string>();
                                sectionSequence.Add(currentSection);
                            }
                        }
                        sectionsLines[currentSection].Add(line);
                    }

                    bool changed = false;
                    for (int i = 1; i <= 4; i++)
                    {
                        string sectionName = "Wiimote" + i;
                        if (!sectionsLines.ContainsKey(sectionName))
                        {
                            sectionsLines[sectionName] = new List<string> { "[" + sectionName + "]", "Source = 1" };
                            sectionSequence.Add(sectionName);
                            changed = true;
                        }
                        else
                        {
                            bool hasSource = false;
                            for (int j = 0; j < sectionsLines[sectionName].Count; j++)
                            {
                                if (sectionsLines[sectionName][j].Trim().StartsWith("Source ="))
                                {
                                    hasSource = true;
                                    if (sectionsLines[sectionName][j].Trim() != "Source = 1")
                                    {
                                        sectionsLines[sectionName][j] = "Source = 1";
                                        changed = true;
                                    }
                                }
                            }
                            if (!hasSource)
                            {
                                sectionsLines[sectionName].Add("Source = 1");
                                changed = true;
                            }
                        }
                    }

                    if (changed)
                    {
                        List<string> finalLines = new List<string>();
                        foreach (var section in sectionSequence)
                        {
                            finalLines.AddRange(sectionsLines[section]);
                        }
                        File.WriteAllLines(configFile, finalLines, Encoding.UTF8);
                        SimpleLogger.Instance.Info("[ProfileAutomator] Updated WiimoteNew.ini: ensured Source=1 in sections Wiimote1-4");
                    }
                }
                catch (Exception ex)
                {
                    SimpleLogger.Instance.Error("[ProfileAutomator] Error updating WiimoteNew.ini: " + ex.Message);
                }
            }
        }

        private static void UpdateDolphinGameSettings(string settingsDir, bool anyGamePadActive)
        {
            if (!Directory.Exists(settingsDir)) return;

            // EN/FR: [V55q] Memory-driven mask/unmask: only profiles REGISTERED in the
            // tag memory (detected tagged at least once) are managed. Everything
            // else — including untagged files with valid wiimotegun content — is
            // ignored forever.
            var files = Directory.GetFiles(settingsDir, "*.in*")
                .Where(f => f.EndsWith(".ini") || f.EndsWith(".ini-wiimotegun"));

            foreach (var file in files)
            {
                ProcessGameSettingsFile(file, anyGamePadActive, "Dolphin", IsValidDolphinGameSettingsContent);
            }

            // EN/FR: [V55p] Prune memory entries whose file was deleted by the user (scanned dir only)
            TagMemoryStore.PruneDirectory("Dolphin", settingsDir);
        }

        /// <summary>
        /// EN: [V55p] Validates a Dolphin GameSettings content: must have [Controls]
        /// and reference a -wiimotegun profile.
        /// FR: [V55p] Valide le contenu d'un GameSettings Dolphin : doit avoir
        /// [Controls] et référencer un profil -wiimotegun.
        /// </summary>
        private static bool IsValidDolphinGameSettingsContent(string content)
        {
            if (string.IsNullOrEmpty(content)) return false;
            if (!content.Contains("[Controls]")) return false;
            return content.Contains("-wiimotegun");
        }

        /// <summary>
        /// EN: [V55p] Validates a PCSX2/DuckStation game settings content: must
        /// reference an input profile and a -wiimotegun profile.
        /// FR: [V55p] Valide le contenu d'un paramètre de jeu PCSX2/DuckStation :
        /// doit référencer un profil d'entrée et un profil -wiimotegun.
        /// </summary>
        private static bool IsValidPcsxDuckStationGameSettingsContent(string content)
        {
            if (string.IsNullOrEmpty(content)) return false;
            if (!content.Contains("InputProfileName") && !content.Contains("InputProfile")) return false;
            return content.Contains("-wiimotegun");
        }

        /// <summary>
        /// EN: [V55p] Computes the ACTIVE (untagged) file name from any current
        /// state: plain ".ini", masked ".ini-wiimotegun", or the corrupted ".in"
        /// / ".in-wiimotegun" variants produced by earlier builds (fallback repair kept).
        /// FR: [V55p] Calcule le nom de fichier ACTIF (sans tag) depuis n'importe
        /// quel état courant : ".ini" simple, masqué ".ini-wiimotegun", ou les
        /// variantes corrompues ".in" / ".in-wiimotegun" des builds précédents
        /// (réparation de repli conservée).
        /// </summary>
        private static string ComputeActiveGameSettingsName(string fileName)
        {
            string name = fileName;
            if (name.EndsWith("-wiimotegun", StringComparison.OrdinalIgnoreCase))
            {
                name = name.Substring(0, name.Length - "-wiimotegun".Length);
            }
            if (name.EndsWith(".in", StringComparison.OrdinalIgnoreCase))
            {
                name += "i"; // Repair corrupted ".in" -> ".ini" (EN/FR: Réparer ".in" corrompu -> ".ini")
            }
            return name;
        }

        /// <summary>
        /// EN: [V55p/V55q] Masks/unmasks ONE game settings file, driven by the persistent
        /// tag memory (TagMemoryStore):
        /// 1. Registered profile -> managed WITHOUT content check (the memory is
        ///    the source of truth, robust to content edits/rewrites). This is the
        ///    startup crash-safety: a managed file left untagged by a crash while
        ///    in GamePad mode is re-masked in Wiimote/Mouse mode.
        /// 2. Unregistered profile -> enters the memory ONLY when DETECTED TAGGED
        ///    (it received the tag at least once: from WiimoteGun's own mask cycle
        ///    of a previous build, or manually by the user), after a content
        ///    safety check.
        /// 3. Anything else (untagged, never registered) -> IGNORED FOREVER: no
        ///    tag added, no removal attempted, whatever its content.
        /// GamePad mode active -> unmask (rename to ".ini") so the emulator loads
        /// the profile for the launched game automatically. Wiimote/Mouse mode ->
        /// mask (rename to ".ini-wiimotegun") to hide it from the emulator.
        /// FR: [V55p/V55q] Masque/démasque UN fichier de paramètres de jeu, piloté
        /// par la mémoire persistante des tags (TagMemoryStore) :
        /// 1. Profil enregistré -> géré SANS vérification de contenu (la mémoire
        ///    est la source de vérité, robuste aux éditions/réécritures). C'est la
        ///    sécurité anti-crash au démarrage : un fichier géré laissé sans tag
        ///    par un crash en mode GamePad est re-masqué en mode Wiimote/Souris.
        /// 2. Profil non enregistré -> entre en mémoire UNIQUEMENT s'il est
        ///    DÉTECTÉ TAGGÉ (il a reçu le tag au moins une fois : d'un cycle de
        ///    masquage WiimoteGun d'une version précédente, ou manuellement par
        ///    l'utilisateur), après une vérification de sécurité du contenu.
        /// 3. Tout le reste (non taggé, jamais enregistré) -> IGNORÉ POUR
        ///    TOUJOURS : aucun tag ajouté, aucun retrait tenté, quel que soit
        ///    son contenu.
        /// Mode GamePad actif -> démasquer (renommage en ".ini") pour que
        /// l'émulateur charge le profil du jeu lancé automatiquement. Mode
        /// Wiimote/Souris -> masquer (renommage en ".ini-wiimotegun") pour le
        /// cacher à l'émulateur.
        /// </summary>
        private static void ProcessGameSettingsFile(string file, bool anyGamePadActive, string emulatorKey, Func<string, bool> contentValidator)
        {
            try
            {
                // EN/FR: Vanished between enumeration and processing -> nothing to do
                if (!File.Exists(file)) return;

                string fileName = Path.GetFileName(file);
                string dir = Path.GetDirectoryName(file);
                string activeName = ComputeActiveGameSettingsName(fileName);
                string maskedName = activeName + "-wiimotegun";
                bool isTagged = fileName.EndsWith("-wiimotegun", StringComparison.OrdinalIgnoreCase);

                // EN/FR: [V55p] STEP 1 - Managed check from the persistent memory
                bool managed = TagMemoryStore.IsRegistered(emulatorKey, dir, activeName);

                if (!managed)
                {
                    // EN/FR: [V55q] STEP 2 - Registration gate: ONLY a file DETECTED
                    // TAGGED enters the memory (it received the tag at least once:
                    // from WiimoteGun's own mask cycle of a previous build, or
                    // manually by the user), after a content safety check.
                    // An UNTAGGED file is NEVER registered and NEVER touched,
                    // whatever its content: no tag added, no removal attempted.
                    // It stays ignored until it has received the tag at least once.
                    if (!isTagged) return;

                    string content;
                    try { content = File.ReadAllText(file); }
                    catch { return; } // Unreadable -> leave untouched (fallback safety)
                    if (!contentValidator(content)) return;

                    // EN/FR: A detected-tagged file is by definition in its MASKED state
                    managed = TagMemoryStore.Register(emulatorKey, dir, activeName, TagMemoryStore.StateMasked);
                }

                // EN/FR: [V55p] STEP 3 - Enforce the target state. The memory is
                // authoritative: managed files are masked/unmasked WITHOUT re-reading
                // their content, so an edited/rewritten file keeps being managed.
                string newPath = null;
                if (anyGamePadActive)
                {
                    // EN/FR: GamePad mode -> unmask (also repairs corrupted ".in")
                    if (!fileName.Equals(activeName, StringComparison.OrdinalIgnoreCase))
                    {
                        newPath = Path.Combine(dir, activeName);
                    }
                }
                else
                {
                    // EN/FR: Wiimote/Mouse mode -> mask (also masks a corrupted ".in")
                    if (!fileName.Equals(maskedName, StringComparison.OrdinalIgnoreCase))
                    {
                        newPath = Path.Combine(dir, maskedName);
                    }
                }

                if (newPath != null)
                {
                    if (File.Exists(newPath)) File.Delete(newPath);
                    File.Move(file, newPath);
                    SimpleLogger.Instance.Debug(string.Format("[ProfileAutomator] Renamed {0} game setting: {1} -> {2}", emulatorKey, fileName, Path.GetFileName(newPath)));
                }

                // EN/FR: [V55p] Keep the recorded state coherent (no-op when unchanged)
                TagMemoryStore.SetState(emulatorKey, dir, activeName,
                    anyGamePadActive ? TagMemoryStore.StateActive : TagMemoryStore.StateMasked);
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error(string.Format("[ProfileAutomator] Error managing {0} game setting {1}: {2}", emulatorKey, file, ex.Message));
            }
        }


        private static string FindEmulatorSubDir(string root, string emuSubName, string targetDir)
        {
            // 1. Try standard RetroBat structure: root\emuSubName\targetDir
            string path1 = Path.Combine(root, emuSubName, targetDir);
            if (Directory.Exists(path1)) return path1;

            // 2. Try direct structure (if root IS already the emulator folder): root\targetDir
            string path2 = Path.Combine(root, targetDir);
            if (Directory.Exists(path2)) return path2;

            return null;
        }

        private const string DUCKSTATION_TEMPLATE = @"[ControllerPorts]
UseProfileHotkeyBindings = true
MultitapMode = Disabled

[InputSources]
DInput = true
XInput = false
RawInput = false
SDL = false

[Pad1]
Type = GunCon
Up = Keyboard/Up
Right = Keyboard/Right
Down = Keyboard/Down
Left = Keyboard/Left
Triangle = Keyboard/I
Circle = Keyboard/L
Cross = Keyboard/K
Square = Keyboard/J
Select = Keyboard/Backspace
Start = Keyboard/Return
L1 = Keyboard/Q
R1 = Keyboard/E
L2 = Keyboard/1
R2 = Keyboard/3
L3 = Keyboard/2
R3 = Keyboard/4
LLeft = Keyboard/A
LRight = Keyboard/D
LDown = Keyboard/S
LUp = Keyboard/W
RLeft = Keyboard/F
RRight = Keyboard/H
RDown = Keyboard/G
RUp = Keyboard/T
LargeMotorVibrationBias = 5
RelativeUp = DInput-2/-Axis3
RelativeLeft = DInput-2/-Axis2
RelativeRight = DInput-2/+Axis2
RelativeDown = DInput-2/+Axis3
Trigger = DInput-2/Button1
ShootOffscreen = DInput-2/Button0
A = DInput-2/Button9
B = DInput-2/Button8
Pointer = DInput-2

[Pad2]
Type = GunCon
Pointer = DInput-0
Trigger = DInput-0/Button1
ShootOffscreen = DInput-0/Button0
A = DInput-0/Button8
B = DInput-0/Button9
RelativeLeft = DInput-0/-Axis2
RelativeRight = DInput-0/+Axis2
RelativeUp = DInput-0/-Axis3
RelativeDown = DInput-0/+Axis3
Start = DInput-0/Button9
Back = DInput-0/Button8

[Hotkeys]
OpenPauseMenu = Keyboard/Escape
Screenshot = Keyboard/F10
TogglePause = Keyboard/Space
ToggleFullscreen = Keyboard/F11
FastForward = Keyboard/Tab
LoadSelectedSaveState = Keyboard/F1
SaveSelectedSaveState = Keyboard/F2
SelectPreviousSaveStateSlot = Keyboard/F3
SelectNextSaveStateSlot = Keyboard/F4
";


        private const string DOLPHIN_GAME_SETTINGS_TEMPLATE = @"[Controls]
WiimoteSource1 = 1
WiimoteSource2 = 1
WiimoteSource3 = 1
WiimoteSource4 = 1
WiimoteProfile1 = {0}_P1-wiimotegun
WiimoteProfile2 = {0}_P2-wiimotegun
WiimoteProfile3 = {0}_P3-wiimotegun
WiimoteProfile4 = {0}_P4-wiimotegun
";

        private const string DOLPHIN_WIIMOTE_TEMPLATE = @"[Profile]
Device = DInput/0/vmulti{suffix} HID
Buttons/A = `Button 0`
Buttons/B = `Button 1`
Buttons/1 = `Button 2`
Buttons/2 = `Button 3`
Buttons/- = `Button 8`
Buttons/+ = `Button 9`
Buttons/Home = Back
D-Pad/Up = `Button 12`
D-Pad/Down = `Button 13`
D-Pad/Left = `Button 14`
D-Pad/Right = `Button 15`
IR/Up = `Axis Yr-`
IR/Down = `Axis Yr+`
IR/Left = `Axis Xr-`
IR/Right = `Axis Xr+`
IR/Calibration = 100.00 101.96 108.24 120.27 141.42 120.27 108.24 101.96 100.00 101.96 108.24 120.27 139.19 120.27 108.24 101.96 100.00 101.96 108.24 120.27 141.42 120.27 108.24 101.96 100.00 101.96 108.24 120.27 141.42 120.27 108.24 101.96
Tilt/Dead Zone = 15.
Swing/Dead Zone = 15.
IMUGyroscope/Dead Zone = 15.
Extension = Nunchuk
Nunchuk/Buttons/C = `Button 4`
Nunchuk/Buttons/Z = `Button 6`
Nunchuk/Stick/Dead Zone = 15.
Nunchuk/Stick/Up = `Axis Y-`
Nunchuk/Stick/Down = `Axis Y+`
Nunchuk/Stick/Left = `Axis X-`
Nunchuk/Stick/Right = `Axis X+`
Nunchuk/Stick/Calibration = 100.00 98.68 100.39 105.02 112.47 104.93 102.26 99.94 100.00 98.09 99.41 103.53 109.77 102.08 98.74 97.42 100.00 95.29 95.96 99.08 104.44 97.44 95.21 95.71 100.00 96.10 97.27 100.24 107.24 100.96 98.09 97.83
Nunchuk/Tilt/Dead Zone = 15.
Nunchuk/Swing/Dead Zone = 15.
Classic/Left Stick/Dead Zone = 15.
Classic/Right Stick/Dead Zone = 15.
";

        private const string PCSX2_TEMPLATE = @"[Pad]
UseProfileHotkeyBindings = true
MultitapPort1 = false
MultitapPort2 = false
PointerXScale = 8
PointerYScale = 8

[InputSources]
Keyboard = true
Mouse = true
SDL = false
XInput = false
SDLControllerEnhancedMode = false
SDLPS5PlayerLED = false
SDLRawInput = false
DInput = true

[Pad1]
Type = DualShock2
Right = Keyboard/Right
Down = Keyboard/Down
Left = Keyboard/Left
Triangle = Keyboard/I
Circle = Keyboard/L
Cross = Keyboard/K
Square = Keyboard/J
Select = Keyboard/Backspace
Start = Keyboard/Return
L2 = Pointer-0/LeftButton
R1 = Keyboard/E
R2 = Keyboard/3
L3 = Keyboard/2
R3 = Keyboard/4
LUp = Keyboard/W
LRight = Keyboard/D
LDown = Keyboard/S
LLeft = Keyboard/A
RUp = Keyboard/T
RRight = Keyboard/H
RDown = Keyboard/G
RLeft = Keyboard/F
AxisScale = 1.33
LargeMotorScale = 1
SmallMotorScale = 1
InvertL = 0
InvertR = 0
Deadzone = 0
ButtonDeadzone = 0
PressureModifier = 0.5

[Pad2]
Type = None

[Pad3]
Type = None

[Pad4]
Type = None

[Pad5]
Type = None

[Pad6]
Type = None

[Pad7]
Type = None

[Pad8]
Type = None

[Hotkeys]
OpenPauseMenu = Keyboard/Escape
TogglePause = Keyboard/Space
ToggleFullscreen = Keyboard/Alt & Keyboard/Return
ToggleFrameLimit = Keyboard/F4
ToggleTurbo = Keyboard/Tab
ToggleSlowMotion = Keyboard/Shift & Keyboard/Backtab
HoldTurbo = Keyboard/Period
InputRecToggleMode = Keyboard/Shift & Keyboard/R
PreviousSaveStateSlot = Keyboard/Shift & Keyboard/F2
NextSaveStateSlot = Keyboard/F2
SaveStateToSlot = Keyboard/F1
LoadStateFromSlot = Keyboard/F3
Screenshot = Keyboard/F8
GSDumpSingleFrame = Keyboard/Shift & Keyboard/F8
GSDumpMultiFrame = Keyboard/Control & Keyboard/Shift & Keyboard/F8
ToggleSoftwareRendering = Keyboard/F9
CycleAspectRatio = Keyboard/F6
ToggleMipmapMode = Keyboard/Insert
CycleInterlaceMode = Keyboard/F5

[USB1]
Type = guncon2
guncon2_Trigger = DInput-2/Button1
guncon2_ShootOffscreen = DInput-2/Button0
guncon2_Recalibrate = DInput-2/Button2
guncon2_A = DInput-2/Button6
guncon2_B = DInput-2/Button3
guncon2_C = DInput-2/Button4
guncon2_Select = DInput-2/Button8
guncon2_Start = DInput-2/Button9
guncon2_RelativeUp = DInput-2/-Axis3
guncon2_RelativeDown = DInput-2/+Axis3
guncon2_RelativeLeft = DInput-2/-Axis2
guncon2_RelativeRight = DInput-2/+Axis2
guncon2_Up = DInput-2/-Axis1
guncon2_Down = DInput-2/+Axis1
guncon2_Left = DInput-2/-Axis0
guncon2_Right = DInput-2/+Axis0

[USB2]
Type = guncon2
guncon2_ShootOffscreen = DInput-0/Button0
guncon2_Recalibrate = DInput-0/Button2
guncon2_A = DInput-0/Button6
guncon2_B = DInput-0/Button3
guncon2_C = DInput-0/Button4
guncon2_Select = DInput-0/Button8
guncon2_Start = DInput-0/Button9
guncon2_RelativeUp = DInput-0/-Axis3
guncon2_RelativeDown = DInput-0/+Axis3
guncon2_RelativeLeft = DInput-0/-Axis2
guncon2_RelativeRight = DInput-0/+Axis2
guncon2_Down = DInput-0/+Axis1
guncon2_Left = DInput-0/-Axis0
guncon2_Right = DInput-0/+Axis0
guncon2_Up = DInput-0/-Axis1
guncon2_Trigger = DInput-0/Button1

[UI]
EnableMouseMapping = false
";

        private static void UpdateDuckStationProfiles(string profileDir, Dictionary<int, int> dinputIndices, Dictionary<int, int> xinputSlots)
        {

            string defaultProfile = Path.Combine(profileDir, "gamepad-wiimotegun.ini");
            if (!File.Exists(defaultProfile))
            {
                try
                {
                    File.WriteAllText(defaultProfile, DUCKSTATION_TEMPLATE, Encoding.UTF8);
                    SimpleLogger.Instance.Info("[ProfileAutomator] Created default DuckStation profile: gamepad-wiimotegun.ini");
                }
                catch (Exception ex)
                {
                    SimpleLogger.Instance.Error("[ProfileAutomator] Error creating DuckStation profile: " + ex.Message);
                }
            }

            var files = Directory.GetFiles(profileDir, "*-wiimotegun.ini");
            foreach (var file in files)
            {
                try
                {
                    string content = File.ReadAllText(file);
                    string updatedContent = UpdateIniContent(content, "DuckStation", dinputIndices, xinputSlots);

                    if (content != updatedContent)
                    {
                        File.WriteAllText(file, updatedContent, Encoding.UTF8);
                        SimpleLogger.Instance.Info(string.Format("[ProfileAutomator] Updated DuckStation profile: {0}", Path.GetFileName(file)));
                    }
                }
                catch (Exception ex)
                {
                    SimpleLogger.Instance.Error(string.Format("[ProfileAutomator] Error updating DuckStation profile {0}: {1}", file, ex.Message));
                }
            }
        }

        private static void UpdatePCSX2Profiles(string profileDir, Dictionary<int, int> dinputIndices, Dictionary<int, int> xinputSlots)
        {

            string defaultProfile = Path.Combine(profileDir, "gamepad-wiimotegun.ini");
            if (!File.Exists(defaultProfile))
            {
                try
                {
                    File.WriteAllText(defaultProfile, PCSX2_TEMPLATE, Encoding.UTF8);
                    SimpleLogger.Instance.Info("[ProfileAutomator] Created default PCSX2 profile: gamepad-wiimotegun.ini");
                }
                catch (Exception ex)
                {
                    SimpleLogger.Instance.Error("[ProfileAutomator] Error creating PCSX2 profile: " + ex.Message);
                }
            }

            var files = Directory.GetFiles(profileDir, "*-wiimotegun.ini");
            foreach (var file in files)
            {
                try
                {
                    string content = File.ReadAllText(file);
                    string updatedContent = UpdateIniContent(content, "PCSX2", dinputIndices, xinputSlots);

                    if (content != updatedContent)
                    {
                        File.WriteAllText(file, updatedContent, Encoding.UTF8);
                        SimpleLogger.Instance.Info(string.Format("[ProfileAutomator] Updated PCSX2 profile: {0}", Path.GetFileName(file)));
                    }
                }
                catch (Exception ex)
                {
                    SimpleLogger.Instance.Error(string.Format("[ProfileAutomator] Error updating PCSX2 profile {0}: {1}", file, ex.Message));
                }
            }
        }

        private static void UpdateDuckStationGameSettings(string settingsDir, bool anyGamePadActive)
        {
            UpdateGameSettingsInternal(settingsDir, anyGamePadActive, "DuckStation");
        }

        private static void UpdatePCSX2GameSettings(string settingsDir, bool anyGamePadActive)
        {
            UpdateGameSettingsInternal(settingsDir, anyGamePadActive, "PCSX2");
        }

        private static void UpdateGameSettingsInternal(string settingsDir, bool anyGamePadActive, string emuName)
        {
            if (!Directory.Exists(settingsDir)) return;

            // EN/FR: [V55p] Search for all relevant variants including corrupted ones
            var files = Directory.GetFiles(settingsDir, "*.in*")
                .Where(f => f.EndsWith(".ini") || f.EndsWith(".ini-wiimotegun") || f.EndsWith(".in") || f.EndsWith(".in-wiimotegun"));

            foreach (var file in files)
            {
                // EN/FR: [V55p] Memory-driven mask/unmask (see ProcessGameSettingsFile)
                ProcessGameSettingsFile(file, anyGamePadActive, emuName, IsValidPcsxDuckStationGameSettingsContent);
            }

            // EN/FR: [V55p] Prune memory entries whose file was deleted by the user (scanned dir only)
            TagMemoryStore.PruneDirectory(emuName, settingsDir);
        }

        private static string UpdateIniContent(string content, string emulator, Dictionary<int, int> dinputIndices, Dictionary<int, int> xinputSlots)
        {
            string[] lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            // First pass: Analyze sections to identify gun types (EN/FR: Première passe : Analyser les sections)
            var sectionTypes = new Dictionary<string, string>(); // Section -> Type value
            string currentSection = "";
            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    currentSection = trimmed.Substring(1, trimmed.Length - 2);
                }
                else if (!string.IsNullOrEmpty(currentSection) && trimmed.StartsWith("Type"))
                {
                    sectionTypes[currentSection] = trimmed.Split('=').Last().Trim();
                }
            }

            // Identify active gamepad players (EN/FR: Identifier les joueurs gamepad actifs)
            // [V57g] EN: The active list is the UNION of DInput and XInput players - a gun
            //     section must never be inhibited just because its player chose XInput.
            //     FR: La liste active est l'UNION des joueurs DInput et XInput - une
            //     section gun ne doit jamais être inhibée parce que son joueur a choisi
            //     XInput.
            var gamepadPlayers = dinputIndices.Keys
                .Union(xinputSlots.Keys)
                .OrderBy(k => k)
                .ToList();
            var xinputMap = (emulator == "DuckStation") ? DuckStationXInputMap : Pcsx2XInputMap;

            StringBuilder sb = new StringBuilder();
            currentSection = "";
            bool inInputSources = false;
            int sectionPlayerIdx = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                string originalLine = lines[i];
                string trimmedLine = originalLine.Trim();

                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                    inInputSources = (currentSection == "InputSources");
                    sectionPlayerIdx = 0;

                    if (emulator == "DuckStation" && currentSection.StartsWith("Pad"))
                        int.TryParse(currentSection.Substring(3), out sectionPlayerIdx);
                    else if (emulator == "PCSX2" && currentSection.StartsWith("USB"))
                        int.TryParse(currentSection.Substring(3), out sectionPlayerIdx);

                    sb.AppendLine(originalLine);
                    continue;
                }

                if (inInputSources && trimmedLine.StartsWith("DInput"))
                {
                    sb.AppendLine("DInput = true");
                    continue;
                }

                // [V57g] EN: Enable the XInput source whenever at least one player is an
                //     XInput output, so the rewritten bindings can resolve. DInput stays
                //     enabled for physical pads.
                //     FR: Active la source XInput dès qu'au moins un joueur est une
                //     sortie XInput, pour que les bindings réécrits se résolvent. DInput
                //     reste activé pour les manettes physiques.
                if (inInputSources && xinputSlots.Count > 0 && trimmedLine.StartsWith("XInput"))
                {
                    sb.AppendLine("XInput = true");
                    continue;
                }

                if (sectionPlayerIdx > 0 && trimmedLine.StartsWith("Type"))
                {
                    string typeValue = sectionTypes.ContainsKey(currentSection) ? sectionTypes[currentSection] : "";
                    string normalizedType = typeValue.Replace("-wiimotegun", "");
                    bool isGun = normalizedType.Equals("GunCon", StringComparison.OrdinalIgnoreCase) ||
                                 normalizedType.Equals("Justifier", StringComparison.OrdinalIgnoreCase) ||
                                 normalizedType.Equals("guncon2", StringComparison.OrdinalIgnoreCase);

                    if (isGun)
                    {
                        bool shouldBeActive = false;

                        if (gamepadPlayers.Count == 1)
                        {
                            int activeP = gamepadPlayers[0];
                            // Redirection logic (EN/FR: Logique de redirection)
                            shouldBeActive = (sectionPlayerIdx == activeP);

                            // Check for P1=none, P2=active redirection
                            if (!shouldBeActive)
                            {
                                string p1Section = (emulator == "DuckStation") ? "Pad1" : "USB1";
                                bool p1IsNone = !sectionTypes.ContainsKey(p1Section) || sectionTypes[p1Section].Equals("none", StringComparison.OrdinalIgnoreCase);
                                if (p1IsNone && sectionPlayerIdx == 2) shouldBeActive = true;
                            }
                        }
                        else if (gamepadPlayers.Count >= 2)
                        {
                            // Multiple players: active if port is in our active gamepad list
                            shouldBeActive = gamepadPlayers.Contains(sectionPlayerIdx);
                        }

                        if (shouldBeActive)
                            sb.AppendLine(trimmedLine.Replace("-wiimotegun", "")); // Active: Ensure no tag
                        else
                            sb.AppendLine(trimmedLine.EndsWith("-wiimotegun") ? originalLine : originalLine + "-wiimotegun"); // Inhibit: Add tag if missing

                        continue;
                    }
                }

                // Process DInput-X replacements
                if (sectionPlayerIdx > 0 && DInputRegex.IsMatch(trimmedLine))
                {
                    string typeValue = sectionTypes.ContainsKey(currentSection) ? sectionTypes[currentSection] : "";
                    string normalizedType = typeValue.Replace("-wiimotegun", "");

                    bool isGun = normalizedType.Equals("GunCon", StringComparison.OrdinalIgnoreCase) ||
                                 normalizedType.Equals("Justifier", StringComparison.OrdinalIgnoreCase) ||
                                 normalizedType.Equals("guncon2", StringComparison.OrdinalIgnoreCase);

                    if (isGun)
                    {
                        int targetPlayer = sectionPlayerIdx;
                        if (gamepadPlayers.Count == 1)
                        {
                            // Redirection for single player (EN/FR: Redirection pour joueur unique)
                            targetPlayer = gamepadPlayers[0];
                        }

                        // [V57g] EN: XInput player - rewrite the WHOLE binding token
                        //     ("DInput-<n>/<sub>" -> "XInput-<slot>/<translated sub>") using
                        //     the emulator's translation table. DInput players keep the
                        //     existing index rewrite.
                        //     FR: Joueur XInput - réécrit le token de binding COMPLET
                        //     (« DInput-<n>/<sub> » -> « XInput-<slot>/<sub traduit> »)
                        //     via la table de traduction de l'émulateur. Les joueurs
                        //     DInput conservent la réécriture d'index existante.
                        if (xinputSlots.ContainsKey(targetPlayer))
                        {
                            int slot = xinputSlots[targetPlayer];
                            string rewritten = DInputBindingRegex.Replace(originalLine, m =>
                            {
                                string sub = m.Groups[1].Value; // "/ButtonN" or "/-AxisN"
                                string core = sub.TrimStart('/');
                                char sign = core[0];
                                if (sign == '-' || sign == '+')
                                {
                                    core = core.Substring(1);
                                    string mapped;
                                    if (xinputMap.TryGetValue(core, out mapped))
                                        return "XInput-" + slot + "/" + sign + mapped;
                                    // A signed axis entry that maps to a signed axis keeps
                                    // its direction; an unsigned mapping (e.g. analog
                                    // trigger "+Axis4") overrides the sign.
                                    // (EN/FR: Une entrée d'axe signée mappée sur un axe
                                    // signé garde sa direction ; un mappage non signé (ex.
                                    // gâchette analogique « +Axis4 ») prime sur le signe.)
                                    return "XInput-" + slot + "/" + sign + core;
                                }
                                return "XInput-" + slot + "/" + (xinputMap.TryGetValue(core, out string mappedBtn) ? mappedBtn : core);
                            });
                            sb.AppendLine(rewritten);
                            continue;
                        }

                        if (dinputIndices.ContainsKey(targetPlayer))
                        {
                            int displayIndex = dinputIndices[targetPlayer] - 1;
                            sb.AppendLine(DInputRegex.Replace(originalLine, "DInput-" + displayIndex));
                            continue;
                        }
                    }
                }

                sb.AppendLine(originalLine);
            }

            return sb.ToString().TrimEnd() + Environment.NewLine;
        }
    }
}
