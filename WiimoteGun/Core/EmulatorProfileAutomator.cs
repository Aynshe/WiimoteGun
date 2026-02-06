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

        public static void UpdateProfiles(IEnumerable<WiiMoteController> controllers)
        {
            try
            {
                var controllerList = controllers.ToList();
                var dinputIndices = new Dictionary<int, int>(); // playerIndex -> dinputIndex (1-based)

                foreach (var c in controllerList)
                {
                    if (c.DInputIndex > 0)
                    {
                        dinputIndices[c.PlayerIndex] = c.DInputIndex;
                    }
                }

                if (dinputIndices.Count == 0)
                {
                    SimpleLogger.Instance.Info("[ProfileAutomator] No virtual gamepads detected with DInput indices. Skipping profile updates.");
                    return;
                }

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

                if (emulatorRoots.Count == 0)
                {
                    SimpleLogger.Instance.Warning("[ProfileAutomator] RetroBat 'emulators' folder not found via registry.");
                    return;
                }

                foreach (var root in emulatorRoots)
                {
                    UpdateDuckStationProfiles(root, dinputIndices);
                    UpdatePCSX2Profiles(root, dinputIndices);
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error("[ProfileAutomator] Error updating profiles: " + ex.Message);
            }
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

        private static void UpdateDuckStationProfiles(string emulatorRoot, Dictionary<int, int> dinputIndices)
        {
            string profileDir = Path.Combine(emulatorRoot, @"duckstation\inputprofiles");
            if (!Directory.Exists(profileDir)) return;

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

        private static void UpdatePCSX2Profiles(string emulatorRoot, Dictionary<int, int> dinputIndices)
        {
            string profileDir = Path.Combine(emulatorRoot, @"pcsx2\inputprofiles");
            if (!Directory.Exists(profileDir)) return;

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

        private static string UpdateIniContent(string content, string emulator, Dictionary<int, int> dinputIndices)
        {
            string[] lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            StringBuilder sb = new StringBuilder();
            
            string currentSection = "";
            bool inInputSources = false;
            int currentPlayerIndex = 0;
            bool sectionIsGunType = false;
            List<string> sectionLines = new List<string>();

            // EN: Helper to process and write the buffered section lines
            // FR: Aide pour traiter et écrire les lignes de section bufférisées
            Action flushSection = () =>
            {
                if (string.IsNullOrEmpty(currentSection)) return;

                foreach (var line in sectionLines)
                {
                    if (sectionIsGunType && currentPlayerIndex > 0 && dinputIndices.ContainsKey(currentPlayerIndex))
                    {
                        int targetDInputIndex = dinputIndices[currentPlayerIndex];
                        // EN: Both PCSX2 and DuckStation use 0-based indexing for DirectInput (Joy 1 = DInput-0)
                        // FR: PCSX2 et DuckStation utilisent tous deux des index basés sur 0 (Joy 1 = DInput-0)
                        int displayIndex = targetDInputIndex - 1;
                        sb.AppendLine(DInputRegex.Replace(line, "DInput-" + displayIndex));
                    }
                    else if (inInputSources && line.Trim().StartsWith("DInput"))
                    {
                        sb.AppendLine("DInput = true");
                    }
                    else
                    {
                        sb.AppendLine(line);
                    }
                }
                sectionLines.Clear();
            };

            for (int i = 0; i < lines.Length; i++)
            {
                string originalLine = lines[i];
                string trimmedLine = originalLine.Trim();

                if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
                {
                    flushSection();

                    currentSection = trimmedLine.Substring(1, trimmedLine.Length - 2);
                    inInputSources = (currentSection == "InputSources");
                    currentPlayerIndex = 0;
                    sectionIsGunType = false;

                    if (emulator == "DuckStation" && currentSection.StartsWith("Pad"))
                    {
                        int.TryParse(currentSection.Substring(3), out currentPlayerIndex);
                    }
                    else if (emulator == "PCSX2" && currentSection.StartsWith("USB"))
                    {
                        int.TryParse(currentSection.Substring(3), out currentPlayerIndex);
                    }

                    sb.AppendLine(originalLine);
                    continue;
                }

                // Identify if this section is for a lightgun/guncon
                // (EN/FR: Identifier si cette section est pour un lightgun/guncon)
                if (currentPlayerIndex > 0 && trimmedLine.StartsWith("Type"))
                {
                    string typeValue = trimmedLine.Split('=').Last().Trim();
                    if (typeValue.Equals("GunCon", StringComparison.OrdinalIgnoreCase) || 
                        typeValue.Equals("Justifier", StringComparison.OrdinalIgnoreCase) ||
                        typeValue.Equals("guncon2", StringComparison.OrdinalIgnoreCase))
                    {
                        sectionIsGunType = true;
                    }
                }

                sectionLines.Add(originalLine);
            }

            flushSection();

            return sb.ToString().TrimEnd() + Environment.NewLine;
        }
    }
}
