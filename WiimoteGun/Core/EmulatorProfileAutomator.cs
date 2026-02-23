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
                bool anyGamePadActive = false;

                foreach (var c in controllerList)
                {
                    // Filter active gamepads: Only controllers in GamePad mode with a valid DInput index 
                    // (EN/FR: Filtrer gamepads actifs : Uniquement contrôleurs en mode GamePad avec index DInput valide)
                    if (c.DInputIndex > 0 && (c.Mode == WiiMoteMode.GamePad || c.Mode == WiiMoteMode.GamePad43 || c.Mode == WiiMoteMode.GamePadFPS))
                    {
                        dinputIndices[c.PlayerIndex] = c.DInputIndex;
                        anyGamePadActive = true;
                    }
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
                    if (dsProfileDir != null) UpdateDuckStationProfiles(dsProfileDir, dinputIndices);

                    string ps2ProfileDir = FindEmulatorSubDir(root, "pcsx2", "inputprofiles");
                    if (ps2ProfileDir != null) UpdatePCSX2Profiles(ps2ProfileDir, dinputIndices);

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
                            GenerateMissingDolphinSettings(dolphinSettingsDir, dolphinProfilesDir);
                            UpdateDolphinGameSettings(dolphinSettingsDir, anyGamePadActive);
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

            var files = Directory.GetFiles(settingsDir, "*.in*")
                .Where(f => f.EndsWith(".ini") || f.EndsWith(".ini-wiimotegun"));

            foreach (var file in files)
            {
                try
                {
                    string content = File.ReadAllText(file);
                    // EN: Only touch files that have [Controls] and reference our wiimotegun profiles
                    // FR: Uniquement toucher les fichiers qui ont [Controls] et référencent nos profils wiimotegun
                    if (!content.Contains("[Controls]") || !content.Contains("-wiimotegun")) continue;

                    string fileName = Path.GetFileName(file);
                    string dir = Path.GetDirectoryName(file);
                    string newPath = null;

                    if (anyGamePadActive)
                    {
                        // EN: GamePad mode active -> Unmask our custom settings (Reveals them to Dolphin)
                        // FR: Mode GamePad actif -> Démasquer nos paramètres (Rend le fichier visible par Dolphin)
                        if (fileName.EndsWith("-wiimotegun"))
                        {
                            newPath = Path.Combine(dir, fileName.Replace("-wiimotegun", ""));
                        }
                    }
                    else
                    {
                        // EN: Wiimote mode active -> Mask our custom settings (Hides them from Dolphin)
                        // FR: Mode Wiimote actif -> Masquer nos paramètres (Cache le fichier pour Dolphin)
                        if (fileName.EndsWith(".ini"))
                        {
                            newPath = file + "-wiimotegun";
                        }
                    }

                    if (newPath != null)
                    {
                        if (File.Exists(newPath)) File.Delete(newPath);
                        File.Move(file, newPath);
                        SimpleLogger.Instance.Debug(string.Format("[ProfileAutomator] Renamed Dolphin game setting: {0} -> {1}", fileName, Path.GetFileName(newPath)));
                    }
                }
                catch (Exception ex)
                {
                    SimpleLogger.Instance.Error(string.Format("[ProfileAutomator] Error managing Dolphin game setting {0}: {1}", file, ex.Message));
                }
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

        private static void UpdateDuckStationProfiles(string profileDir, Dictionary<int, int> dinputIndices)
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
                    string updatedContent = UpdateIniContent(content, "DuckStation", dinputIndices);

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

        private static void UpdatePCSX2Profiles(string profileDir, Dictionary<int, int> dinputIndices)
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
                    string updatedContent = UpdateIniContent(content, "PCSX2", dinputIndices);

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

            // Search for all relevant variants including corrupted ones (EN/FR: Chercher toutes les variantes incluant celles corrompues)
            var files = Directory.GetFiles(settingsDir, "*.in*")
                .Where(f => f.EndsWith(".ini") || f.EndsWith(".ini-wiimotegun") || f.EndsWith(".in") || f.EndsWith(".in-wiimotegun"));

            foreach (var file in files)
            {
                try
                {
                    string content = File.ReadAllText(file);
                    if (!content.Contains("InputProfileName") && !content.Contains("InputProfile")) continue;
                    if (!content.Contains("-wiimotegun")) continue;

                    // Identify the base name and extension (fix previous .in bug)
                    string fileName = Path.GetFileName(file);
                    string dir = Path.GetDirectoryName(file);
                    string newPath = null;

                    if (anyGamePadActive)
                    {
                        // PCSX2/DuckStation: Restore (Unmask) to .ini in GamePad mode
                        if (fileName.EndsWith("-wiimotegun") || fileName.EndsWith(".in"))
                        {
                            string restoredName = fileName.Replace("-wiimotegun", "");
                            if (restoredName.EndsWith(".in")) restoredName += "i"; // Fix corrupted .in -> .ini
                            newPath = Path.Combine(dir, restoredName);
                        }
                    }
                    else
                    {
                        // PCSX2/DuckStation: Mask with -wiimotegun in Wiimote/Mouse mode
                        if (fileName.EndsWith(".ini"))
                        {
                            newPath = file + "-wiimotegun";
                        }
                    }

                    if (newPath != null)
                    {
                        if (File.Exists(newPath)) File.Delete(newPath);
                        File.Move(file, newPath);
                        SimpleLogger.Instance.Debug(string.Format("[ProfileAutomator] Renamed {0} game setting: {1} -> {2}", emuName, fileName, Path.GetFileName(newPath)));
                    }
                }
                catch (Exception ex)
                {
                    SimpleLogger.Instance.Error(string.Format("[ProfileAutomator] Error managing {0} game setting {1}: {2}", emuName, file, ex.Message));
                }
            }
        }

        private static string UpdateIniContent(string content, string emulator, Dictionary<int, int> dinputIndices)
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
            // We assume dinputIndices only contains active virtual gamepads
            var gamepadPlayers = dinputIndices.Keys.OrderBy(k => k).ToList();

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
