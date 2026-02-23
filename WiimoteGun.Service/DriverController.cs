using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;

namespace WiimoteGun.Service
{
    public static class DriverController
    {
        // Based on user logs and cleanup scripts, we match specific patterns for devcon
        private static readonly string[] PLAYER_ID_PATTERNS = new string[] 
        {
            "*VMULTIA*", // P1
            "*VMULTIB*", // P2
            "*VMULTIC*", // P3
            "*VMULTID*"  // P4
        };

        private static string LogFile = AppDomain.CurrentDomain.BaseDirectory + "WiimoteGunService.log";
        private static string DevConPath = AppDomain.CurrentDomain.BaseDirectory + "devcon.exe";
        
        // EN: Track which players have been enabled (to clean up after rescan)
        // FR: Suivre quels joueurs ont été activés (pour nettoyer après rescan)
        private static HashSet<int> _enabledPlayers = new HashSet<int>();
        
        // EN: Track which players have gamepads explicitly enabled
        // FR: Suivre quels joueurs ont les gamepads explicitement activés
        private static HashSet<int> _gamepadActivePlayers = new HashSet<int>();

        public static void Log(string message)
        {
            try
            {
                FileInfo fi = new FileInfo(LogFile);
                if (fi.Exists && fi.Length > 1536 * 1024) // 1.5 MB limit
                {
                    string backupFile = LogFile + ".bak";
                    if (File.Exists(backupFile)) File.Delete(backupFile);
                    File.Move(LogFile, backupFile);
                }
                File.AppendAllText(LogFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}");
            }
            catch { } 
        }
        
        /// <summary>
        /// EN: Reset the enabled players list (called when new client connects).
        /// FR: Réinitialiser la liste des joueurs activés (appelé quand un nouveau client se connecte).
        /// </summary>
        public static void ResetEnabledPlayers()
        {
            Log("ResetEnabledPlayers: Clearing enabled players and gamepad lists (new client registration or fresh session)");
            _enabledPlayers.Clear();
            _gamepadActivePlayers.Clear();
        }

        public static void EnablePlayer(int playerIndex)
        {
            CheckDevCon();
            Log($"EnablePlayer command received for P{playerIndex}");
            if (playerIndex < 1 || playerIndex > 4) return;

            // EN: Track this player as enabled / FR: Suivre ce joueur comme activé
            _enabledPlayers.Add(playerIndex);
            Log($"Enabled players: {string.Join(",", _enabledPlayers)}");

            // Check if driver is installed, if not, install it silently
            // Hybrid Strategy: P2, P3, P4 only install if P1 (VMultiA) is already present
            if (!IsDriverInstalled(playerIndex))
            {
                if (playerIndex > 1) 
                {
                    // Verify P1 is installed before proceeding
                    if (!IsDriverInstalled(1))
                    {
                        Log($"P1 (VMultiA) is not installed. Skipping auto-install for P{playerIndex}. P1 must be installed first via Setup.");
                        return; 
                    }
                }
                
                Log($"Driver for P{playerIndex} not found. Attempting silent installation...");
                InstallDriver(playerIndex);
            }
            
            // EN: Optimization - Only rescan if the mouse device (COL03) is missing.
            // FR: Optimisation - Ne rescanner que si le périphérique souris (COL03) est manquant.
            // Full rescan is heavy and can disconnect Bluetooth.
            char playerChar = (char)('A' + playerIndex - 1);
            string mousePattern = $"*VMULTI{playerChar}*COL03*";
            
            if (!IsDevicePresent(mousePattern))
            {
                // EN: Targeted optimization - Restart the specific Root device instead of a global rescan.
                // FR: Optimisation ciblée - Redémarrer le Root device spécifique plutôt qu'un rescan global.
                // This re-enumerates removed collections (like COL03) without disrupting the entire Bluetooth stack.
                char charLower = char.ToLower(playerChar);
                string rootHwId = $"ecologylab\\vmulti{charLower}";
                Log($"COL03 for P{playerIndex} not found. Restarting root device {rootHwId} to re-enumerate...");
                RunDevCon($"restart \"{rootHwId}\"");
                // Small delay to allow Windows to re-enumerate the device
                System.Threading.Thread.Sleep(500);
            }
            else
            {
                Log($"COL03 for P{playerIndex} already present. Skipping rescan.");
            }
            
            // Enable specific interfaces: Mouse (Col03), Control (Col08) and Keyboard (Col07/Col08)
            RunDevCon($"enable \"*VMULTI{playerChar}*COL03*\""); // Mouse
            RunDevCon($"enable \"*VMULTI{playerChar}*COL07*\""); // Message/Keyboard
            RunDevCon($"enable \"*VMULTI{playerChar}*COL08*\""); // Keyboard/Control (Essential for GamePad/Keyboard reports)
            RunDevCon($"enable \"*VMULTI{playerChar}*COL09*\""); // Control Interface Extension
            
            // EN: CRITICAL: Remove COL03 for players NOT in the enabled list
            // FR: CRITIQUE: Supprimer les COL03 pour les joueurs NON dans la liste activée
            // This prevents ghost cursors when rescan re-enables all devices
            Log("Cleaning up COL03 for non-enabled players after rescan...");
            for (int i = 1; i <= 4; i++)
            {
                if (!_enabledPlayers.Contains(i))
                {
                    char otherPlayerChar = (char)('A' + i - 1);
                    string pattern = $"*vmulti{otherPlayerChar}*COL03*";
                    if (IsDeviceEnabled(pattern))
                    {
                        Log($"Disabling COL03 for non-enabled P{i}");
                        RunDevCon($"disable \"{pattern}\"");
                    }
                }
            }
        }

        public static void DisablePlayer(int playerIndex)
        {
            CheckDevCon();
            Log($"DisablePlayer command received for P{playerIndex}");
             if (playerIndex < 1 || playerIndex > 4) return;

            // Only disable the devices we manage (Mouse & Keyboard), leaving others untouched
            char playerChar = (char)('A' + playerIndex - 1);
            RunDevCon($"disable \"*VMULTI{playerChar}*COL03*\""); // Mouse
            RunDevCon($"disable \"*VMULTI{playerChar}*COL07*\""); // Keyboard
        }

        private static bool IsDriverInstalled(int playerIndex)
        {
            if (playerIndex < 1 || playerIndex > 4) return false;
            char playerChar = (char)('A' + playerIndex - 1);
            string charLower = char.ToLower(playerChar).ToString();
            
            // Logic: Check if the Root device exists AND if it is properly enumerated (check for COL01 node)
            // If only Root exists but no children, the driver is likely stalled/broken
            
            // 1. Check Root
            string rootPattern = $"*vmulti{charLower}*";
            string rootOutput = RunDevConWithOutput($"find \"{rootPattern}\"");
            
            bool rootFound = rootOutput.Contains("matching device(s) found");
            
            if (!rootFound) return false;
            
            // 2. Check Child (COL01 is always present on this driver)
            // If Root is found but COL01 is missing, it's a broken install -> Return false to trigger Re-Install
            string childPattern = $"*vmulti{charLower}*COL01*";
            string childOutput = RunDevConWithOutput($"find \"{childPattern}\"");
            
            bool childFound = childOutput.Contains("matching device(s) found");
            
            if (rootFound && !childFound)
            {
                Log($"[Diagnostic] Root device found but Child COL01 missing for P{playerIndex}. Assuming broken install.");
                return false;
            }
            
            return true;
        }

        private static void InstallDriver(int playerIndex)
        {
            try 
            {
                // Determine paths
                // Structure: WiimoteGun.Service\WiimoteGunDriver\virtualX\vmultiY.inf
                // playerIndex 1 -> virtual1 -> vmultia.inf -> ecologylab\vmultia
                string baseDir = AppDomain.CurrentDomain.BaseDirectory; // Service dir
                string folderName = $"virtual{playerIndex}";
                char playerChar = (char)('A' + playerIndex - 1);
                string charLower = char.ToLower(playerChar).ToString();
                string infName = $"vmulti{charLower}.inf";
                string hwId = $"ecologylab\\vmulti{charLower}"; // FIXED: Correct HWID per user request

                string driverDir = Path.Combine(baseDir, "WiimoteGunDriver", folderName);
                string infPath = Path.Combine(driverDir, infName);

                if (!File.Exists(infPath))
                {
                    Log($"ERROR: Driver INF not found at {infPath}");
                    return;
                }

                Log($"Installing driver P{playerIndex} from {infPath} with HWID {hwId}...");

                // Command: devcon /r install "Path\To.inf" hwid
                // /r = reboot if needed (suppressed if possible, but good practice to include if script has it)
                string args = $"/r install \"{infPath}\" {hwId}";
                
                // Use the devcon in the driver folder if possible, by passing driverDir
                RunDevCon(args, driverDir);
                
                Log($"Driver P{playerIndex} installed. Running cleanup...");

                // Cleanup: Disable unused columns to avoid interference/ghost devices
                // Matching batch script: COL01, COL02, COL04, COL05, COL08, COL09 (Exclude 06 - Gamepad managed via Remove)
                // EN: COL06 (Gamepad) is removed separately to avoid disable-loop crashes
                // FR: COL06 (Gamepad) est supprimé séparément pour éviter les crashs de boucle disable
                string[] colsToDisable = { "01", "02", "04", "05", "08", "09" };
                foreach (string col in colsToDisable)
                {
                    // Pattern: *vmultia*COL01*
                    string colPattern = $"*vmulti{charLower}*COL{col}*";
                    RunDevCon($"disable \"{colPattern}\"");
                }

                // Explicitly DISABLE Gamepad (06) to hide it by default
                string gamepadPattern = $"*vmulti{charLower}*COL06*";
                RunDevCon($"disable \"{gamepadPattern}\"");

                Log($"Driver P{playerIndex} installation and cleanup finished.");
            }
            catch (Exception ex)
            {
                Log($"ERROR during driver installation: {ex.Message}");
            }
        }

        private static void CheckDevCon()
        {
            if (!File.Exists(DevConPath))
            {
                Log($"ERROR: devcon.exe not found at {DevConPath}");
            }
        }

        private static void RunDevCon(string args, string workingDir = null)
        {
            try
            {
                // Verify working dir or default to BaseDirectory
                string devConToUse = DevConPath;
                if (!string.IsNullOrEmpty(workingDir) && File.Exists(Path.Combine(workingDir, "devcon.exe")))
                {
                    devConToUse = Path.Combine(workingDir, "devcon.exe");
                }

                Log($"Running: {devConToUse} {args}");
                ProcessStartInfo psi = new ProcessStartInfo(devConToUse, args);
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true; // Capture Error too
                if (!string.IsNullOrEmpty(workingDir)) psi.WorkingDirectory = workingDir;

                Process p = Process.Start(psi);
                
                string output = p.StandardOutput.ReadToEnd();
                string error = p.StandardError.ReadToEnd();
                
                p.WaitForExit(20000); // Increased timeout for install (20s)

                if (!string.IsNullOrWhiteSpace(output)) Log($"[DevCon Output]: {output.Trim()}");
                if (!string.IsNullOrWhiteSpace(error)) Log($"[DevCon Error]: {error.Trim()}");
            }
            catch (Exception ex)
            {
                Log($"Error running devcon: {ex.Message}");
            }
        }

        private static string RunDevConWithOutput(string args)
        {
             try
            {
                Log($"Running: {DevConPath} {args}");
                ProcessStartInfo psi = new ProcessStartInfo(DevConPath, args);
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                Process p = Process.Start(psi);
                
                string output = p.StandardOutput.ReadToEnd();
                string error = p.StandardError.ReadToEnd();
                
                p.WaitForExit(5000);
                
                // Always log output for find commands/status to debug
                if (!string.IsNullOrWhiteSpace(output)) Log($"[DevCon Output]: {output.Trim()}");
                if (!string.IsNullOrWhiteSpace(error)) Log($"[DevCon Error]: {error.Trim()}");
                
                return output + Environment.NewLine + error;
            }
            catch (Exception ex)
            {
                Log($"Error running devcon: {ex.Message}");
                return "Error";
            }
        }

        /// <summary>
        /// EN: Cleanup unwanted VMulti collections for all players (COL01, COL02, COL04, COL05).
        /// FR: Nettoyer les collections VMulti non désirées pour tous les joueurs (COL01, COL02, COL04, COL05).
        /// Called via IPC from WiimoteGun app when Wiimotes connect.
        /// </summary>

        public static void CleanupUnwantedCollections()
        {
            CheckDevCon();
            Log("CleanupUnwantedCollections command received");

            // EN: Collections to disable (same as Gunmote: Touch, Multi-touch, Config, Joystick)
            // FR: Collections à désactiver (comme Gunmote : Touch, Multi-touch, Config, Joystick)
            string[] colsToDisable = { "01", "02", "04", "05" }; // Excluded "06"

            // EN: Process all 4 possible players / FR: Traiter les 4 joueurs possibles
            for (int i = 1; i <= 4; i++)
            {
                char playerChar = GetPlayerChar(i);

                foreach (string col in colsToDisable)
                {
                    string colPattern = $"*vmulti{playerChar}*COL{col}*";
                    RunDevCon($"disable \"{colPattern}\"");
                }

                bool isEnabled = _enabledPlayers.Contains(i);

                // Gamepad (COL06) Logic:
                // Keep if explicitly enabled (via ENABLE_GAMEPAD_PX), Remove if disabled/disconnected
                // (EN/FR: Garder si explicitement activé, Supprimer sinon)
                string gamepadPattern = $"*vmulti{playerChar}*COL06*";
                bool isGamepadDesired = _gamepadActivePlayers.Contains(i);

                if (!isEnabled || !isGamepadDesired)
                {
                    if (IsDeviceEnabled(gamepadPattern))
                    {
                        Log($"Cleanup: Disabling COL06 for inactive/undesired P{i}");
                        RunDevCon($"disable \"{gamepadPattern}\"");
                    }
                }
                
                // Mouse (COL03) Logic (Restored):
                // Keep if enabled, Remove if disabled/disconnected
                // This replaces removed Update logic
                string mousePattern = $"*vmulti{playerChar}*COL03*";
                if (!isEnabled)
                {
                     if (IsDeviceEnabled(mousePattern))
                     {
                         Log($"Cleanup: Disabling COL03 for inactive P{i}");
                         RunDevCon($"disable \"{mousePattern}\"");
                     }
                }
            }

            Log("CleanupUnwantedCollections completed");
        }

        public static void RemoveMouseForAllPlayers()
        {
            CheckDevCon();
            Log("RemoveMouseForAllPlayers command received");
            for (int i = 1; i <= 4; i++)
            {
                RemoveMouseForPlayer(i);
            }
        }

        public static void RemoveMouseForPlayer(int playerIndex)
        {
            char playerChar = GetPlayerChar(playerIndex);
            string pattern = $"*vmulti{playerChar}*COL03*";
            if (IsDeviceEnabled(pattern))
            {
                Log($"Disabling Mouse (COL03) for P{playerIndex}");
                RunDevCon($"disable \"{pattern}\"");
            }
        }

        public static void RemoveMouseExceptPlayers(string playersStr)
        {
            CheckDevCon();
            Log($"RemoveMouseExceptPlayers received: {playersStr}");
            
            HashSet<int> keepPlayers = new HashSet<int>();
            if (!string.IsNullOrEmpty(playersStr))
            {
                string[] parts = playersStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string part in parts)
                {
                    if (int.TryParse(part.Trim(), out int pId))
                    {
                        keepPlayers.Add(pId);
                    }
                }
            }

            for (int i = 1; i <= 4; i++)
            {
                if (!keepPlayers.Contains(i))
                {
                    RemoveMouseForPlayer(i);
                }
            }
        }

        /// <summary>
        /// EN: Enable Col06 gamepad device for a specific player.
        /// FR: Activer le périphérique gamepad Col06 pour un joueur spécifique.
        /// </summary>
        public static void EnableGamepadForPlayer(int playerIndex)
        {
            CheckDevCon();
            // Ensure player and gamepad are marked as enabled
            _enabledPlayers.Add(playerIndex);
            _gamepadActivePlayers.Add(playerIndex);

            char playerChar = GetPlayerChar(playerIndex);
            string pattern = $"*vmulti{playerChar}*COL06*";

            if (IsDeviceEnabled(pattern))
            {
                Log($"EnableGamepadForPlayer P{playerIndex}: COL06 already enabled, skipping");
                CleanupUnwantedCollections(); // Run cleanup even if already enabled to ensure others are clean
                return;
            }

            Log($"EnableGamepadForPlayer P{playerIndex}: {pattern}");
            
            // Try enable (fast path, if it was disabled or just exists)
            RunDevCon($"enable \"{pattern}\"");
            
            // Check if successful
            System.Threading.Thread.Sleep(200);
            if (IsDeviceEnabled(pattern))
            {
                 Log($"EnableGamepadForPlayer P{playerIndex}: Successfully enabled.");
                 CleanupUnwantedCollections();
                 return;
            }

            // If failed, it might be removed/missing. We MUST Rescan.
            Log($"EnableGamepadForPlayer P{playerIndex}: Enable failed (not found?), triggering Rescan...");
            RunDevCon("rescan");
            System.Threading.Thread.Sleep(500);

            // After rescan, it should be enabled by default (since we removed it from cleanup list)
            // Or we check and enable again just to be sure
            if (!IsDeviceEnabled(pattern))
            {
                RunDevCon($"enable \"{pattern}\"");
            }

            // Run global cleanup to remove ghosts
            CleanupUnwantedCollections();
        }

        public static void RemoveGamepadForAllPlayers()
        {
            CheckDevCon();
            Log("RemoveGamepadForAllPlayers command received");
            for (int i = 1; i <= 4; i++)
            {
                RemoveGamepadForPlayer(i);
            }
        }

        /// <summary>
        /// EN: Remove (hide) Col06 gamepad device for a specific player.
        /// FR: Supprimer (masquer) le périphérique gamepad Col06 pour un joueur spécifique.
        /// </summary>
        public static void RemoveGamepadForPlayer(int playerIndex)
        {
            CheckDevCon();
            Log($"RemoveGamepadForPlayer P{playerIndex}: Marking as disabled.");
            
            // Remove from gamepad active list
            _gamepadActivePlayers.Remove(playerIndex);
            
            // CRITICAL: Do NOT remove from enabled list, as this would also kill the Mouse (COL03) during Cleanup!
            // _enabledPlayers.Remove(playerIndex);

            char playerChar = GetPlayerChar(playerIndex);
            string pattern = $"*vmulti{playerChar}*COL06*";

            // Also explicitly disable right now
            if (IsDeviceEnabled(pattern))
            {
                Log($"RemoveGamepadForPlayer P{playerIndex}: DISABLING {pattern}");
                RunDevCon($"disable \"{pattern}\"");
            }
            
            // Also run cleanup to catch COL03 if it was left over
            CleanupUnwantedCollections();
        }

        private static char GetPlayerChar(int playerIndex)
        {
            if (playerIndex < 1) return 'A';
            return (char)('A' + playerIndex - 1);
        }

        private static bool IsDeviceEnabled(string pattern)
        {
            string output = RunDevConWithOutput($"status \"{pattern}\"");
            return output.IndexOf("Driver is running", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsDevicePresent(string pattern)
        {
            // EN: Find if device exists at all (even if disabled)
            // FR: Vérifier si le périphérique existe (même s'il est désactivé)
            string output = RunDevConWithOutput($"find \"{pattern}\"");
            return output.IndexOf("matching device(s) found", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
