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

        public static void Log(string message)
        {
            try
            {
                File.AppendAllText(LogFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}");
            }
            catch { } 
        }

        public static void EnablePlayer(int playerIndex)
        {
            CheckDevCon();
            Log($"EnablePlayer command received for P{playerIndex}");
            if (playerIndex < 1 || playerIndex > 4) return;

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
            
            // Enable specific interfaces: Mouse (Col03) and Keyboard (Col07)
            // This acts as an auto-cleanup: we don't enable the unused interfaces
            char playerChar = (char)('A' + playerIndex - 1);
            RunDevCon($"enable \"*VMULTI{playerChar}*COL03*\""); // Mouse
            RunDevCon($"enable \"*VMULTI{playerChar}*COL07*\""); // Keyboard
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
    }
}
