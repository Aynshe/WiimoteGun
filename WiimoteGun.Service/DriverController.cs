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

        public static void Log(string message)
        {
            try
            {
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
            _enabledPlayers.Clear();
            Log("ResetEnabledPlayers: Cleared enabled players list for new session");
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
            
            // EN: Rescan for hardware changes to re-enumerate removed devices (like COL03 after REMOVE_MOUSE_ALL)
            // FR: Rechercher les modifications matérielles pour ré-énumérer les périphériques supprimés
            // WARNING: This is a GLOBAL rescan - it will re-enable ALL removed COL03 devices!
            char playerChar = (char)('A' + playerIndex - 1);
            Log($"Rescanning hardware to re-enumerate removed devices for P{playerIndex}...");
            RunDevCon("rescan");
            
            // Small delay to allow Windows to re-enumerate the device
            System.Threading.Thread.Sleep(500);
            
            // Enable specific interfaces: Mouse (Col03) and Keyboard (Col07)
            RunDevCon($"enable \"*VMULTI{playerChar}*COL03*\""); // Mouse
            RunDevCon($"enable \"*VMULTI{playerChar}*COL07*\""); // Keyboard
            
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
                        Log($"Removing COL03 for non-enabled P{i}");
                        RunDevCon($"remove \"{pattern}\"");
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
                // Matching batch script: COL01, COL02, COL04, COL05, COL06, COL08, COL09
                string[] colsToDisable = { "01", "02", "04", "05", "06", "08", "09" };
                foreach (string col in colsToDisable)
                {
                    // Pattern: *vmultia*COL01*
                    string colPattern = $"*vmulti{charLower}*COL{col}*";
                    RunDevCon($"disable \"{colPattern}\"");
                }

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
        /// EN: Cleanup unwanted VMulti collections for all players (COL01, COL02, COL04, COL05, COL06).
        /// FR: Nettoyer les collections VMulti non désirées pour tous les joueurs (COL01, COL02, COL04, COL05, COL06).
        /// Called via IPC from WiimoteGun app when Wiimotes connect.
        /// </summary>
        public static void CleanupUnwantedCollections()
        {
            CheckDevCon();
            Log("CleanupUnwantedCollections command received");

            // EN: Collections to disable (same as Gunmote: Touch, Multi-touch, Gamepad, Config, Joystick)
            // FR: Collections à désactiver (comme Gunmote : Touch, Multi-touch, Gamepad, Config, Joystick)
            // Note: COL03 (Mouse), COL07 (Keyboard), COL08 (Control) are kept enabled
            string[] colsToDisable = { "01", "02", "04", "05", "06" };

            // EN: Process all 4 possible players / FR: Traiter les 4 joueurs possibles
            char[] players = { 'a', 'b', 'c', 'd' };

            foreach (char playerChar in players)
            {
                foreach (string col in colsToDisable)
                {
                    // Pattern: *vmultia*COL01*
                    string colPattern = $"*vmulti{playerChar}*COL{col}*";
                    RunDevCon($"disable \"{colPattern}\"");
                }
            }

            Log("CleanupUnwantedCollections completed");
        }

        /// <summary>
        /// EN: Remove (hide) COL03 mouse device for a specific player.
        /// FR: Supprimer (masquer) le périphérique souris COL03 pour un joueur spécifique.
        /// Only removes if the device is NOT already disabled.
        /// </summary>
        public static void RemoveMouseForPlayer(int playerIndex)
        {
            CheckDevCon();
            
            // EN: Remove from enabled players tracking / FR: Retirer du suivi des joueurs activés
            _enabledPlayers.Remove(playerIndex);
            Log($"Removed P{playerIndex} from enabled list. Now enabled: {string.Join(",", _enabledPlayers)}");
            
            char playerChar = GetPlayerChar(playerIndex);
            string pattern = $"*vmulti{playerChar}*COL03*";
            
            if (!IsDeviceEnabled(pattern))
            {
                Log($"RemoveMouseForPlayer P{playerIndex}: COL03 already disabled, skipping");
                return;
            }
            
            Log($"RemoveMouseForPlayer P{playerIndex}: {pattern}");
            RunDevCon($"remove \"{pattern}\"");
        }

        /// <summary>
        /// EN: Remove (hide) COL03 mouse device for all players.
        /// FR: Supprimer (masquer) le périphérique souris COL03 pour tous les joueurs.
        /// Only removes devices that are NOT already disabled.
        /// </summary>
        public static void RemoveMouseForAllPlayers()
        {
            CheckDevCon();
            
            // EN: Clear enabled players tracking / FR: Vider le suivi des joueurs activés
            _enabledPlayers.Clear();
            Log("RemoveMouseForAllPlayers: clearing enabled list and removing COL03 for all vmulti devices");
            char[] players = { 'a', 'b', 'c', 'd' };
            foreach (char playerChar in players)
            {
                string pattern = $"*vmulti{playerChar}*COL03*";
                if (IsDeviceEnabled(pattern))
                {
                    RunDevCon($"remove \"{pattern}\"");
                }
                else
                {
                    Log($"  vmulti{playerChar} COL03 already disabled, skipping");
                }
            }
            Log("RemoveMouseForAllPlayers completed");
        }

        /// <summary>
        /// EN: Remove (hide) COL03 mouse device for all players EXCEPT those connected.
        /// FR: Supprimer (masquer) COL03 pour tous les joueurs SAUF ceux connectés.
        /// Format: comma-separated player indexes (e.g., "1,2" for P1 and P2 connected)
        /// Only removes devices that are NOT already disabled.
        /// </summary>
        public static void RemoveMouseExceptPlayers(string connectedPlayersStr)
        {
            CheckDevCon();
            Log($"RemoveMouseExceptPlayers: connected={connectedPlayersStr}");

            // EN: Parse connected player indexes / FR: Parser les index des joueurs connectés
            var connected = new System.Collections.Generic.HashSet<int>();
            if (!string.IsNullOrWhiteSpace(connectedPlayersStr))
            {
                foreach (var part in connectedPlayersStr.Split(','))
                {
                    if (int.TryParse(part.Trim(), out int idx))
                        connected.Add(idx);
                }
            }

            // EN: Remove COL03 for players NOT in connected list (if device is enabled)
            // FR: Supprimer COL03 pour les joueurs NON dans la liste connectée (si device enabled)
            for (int i = 1; i <= 4; i++)
            {
                if (!connected.Contains(i))
                {
                    char playerChar = GetPlayerChar(i);
                    string pattern = $"*vmulti{playerChar}*COL03*";
                    if (IsDeviceEnabled(pattern))
                    {
                        RunDevCon($"remove \"{pattern}\"");
                    }
                    else
                    {
                        Log($"  vmulti{playerChar} COL03 already disabled, skipping");
                    }
                }
            }
            Log("RemoveMouseExceptPlayers completed");
        }

        /// <summary>
        /// EN: Check if a device matching the pattern is enabled (exists and not disabled).
        /// FR: Vérifier si un périphérique correspondant au pattern est activé (existe et non désactivé).
        /// Uses devcon status to check device state.
        /// </summary>
        private static bool IsDeviceEnabled(string pattern)
        {
            string output = RunDevConWithOutput($"status \"{pattern}\"");

            // EN: If output contains "running" or "started", device is enabled
            // FR: Si la sortie contient "running" ou "started", le device est activé
            // If output contains "disabled" or "No matching devices", device is not enabled
            bool isEnabled = output.IndexOf("running", StringComparison.OrdinalIgnoreCase) >= 0 ||
                             output.IndexOf("started", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isDisabled = output.IndexOf("disabled", StringComparison.OrdinalIgnoreCase) >= 0 ||
                              output.IndexOf("No matching devices", StringComparison.OrdinalIgnoreCase) >= 0;

            return isEnabled && !isDisabled;
        }

        /// <summary>
        /// EN: Get player character (a, b, c, d) from index (1-4).
        /// FR: Obtenir le caractère joueur (a, b, c, d) depuis l'index (1-4).
        /// </summary>
        private static char GetPlayerChar(int playerIndex)
        {
            switch (playerIndex)
            {
                case 1: return 'a';
                case 2: return 'b';
                case 3: return 'c';
                case 4: return 'd';
                default: return 'a';
            }
        }
    }
}
