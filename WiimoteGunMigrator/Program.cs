using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Threading;

namespace WiimoteGunMigrator
{
    /// <summary>
    /// [3.1.5.10] EN: update_W4G.exe — native self-updater for Wiimote4Guns.
    ///     Replaces the PowerShell WiimoteUpdate.ps1 approach: same flow but as a
    ///     standalone net48 exe (no PowerShell dependency, no quoting issues).
    ///     Called by WiimoteGun.exe with: update_W4G.exe "<download_url>" <pid>
    ///     Flow:
    ///       1. Wait for the calling app (PID) to exit.
    ///       2. Download the release .7z from the given URL.
    ///       3. Extract with 7za.exe (shipped with the app).
    ///       4. Apply with correct staging: top-level files → app dir (except the
    ///          running update_W4G.exe and the running service exe),
    ///          WiimoteGun.Service\ → update_service\ staging (HmHost included),
    ///          other folders → recursive merge.
    ///       5. Delete the legacy WiimoteUpdate.ps1 (no longer used).
    ///       6. Clean up stale .7z and .null files.
    ///       7. Restart WiimoteGun.exe with the current user account.
    ///     FR: update_W4G.exe — auto-updateur natif de Wiimote4Gans.
    ///     Remplace l'approche PowerShell WiimoteUpdate.ps1 : même flux mais en exe
    ///     net48 autonome (sans dépendance PowerShell, sans problèmes de guillemets).
    ///     Appelé par WiimoteGun.exe avec : update_W4G.exe "<url_téléchargement>" <pid>
    ///     Flux :
    ///       1. Attendre la sortie de l'app appelante (PID).
    ///       2. Télécharger le .7z de la release depuis l'URL donnée.
    ///       3. Extraire avec 7za.exe (livré avec l'app).
    ///       4. Appliquer avec le staging correct : fichiers racine → dossier app
    ///          (sauf update_W4G.exe en cours et l'exe du service en cours),
    ///          WiimoteGun.Service\ → staging update_service\ (HmHost inclus),
    ///          autres dossiers → fusion récursive.
    ///       5. Supprimer le WiimoteUpdate.ps1 obsolète (plus utilisé).
    ///       6. Nettoyer les fichiers .7z et .null résiduels.
    ///       7. Relancer WiimoteGun.exe avec le compte utilisateur en cours.
    /// </summary>
    internal static class Program
    {
        private const string SelfName = "update_W4G.exe";
        private const string ServiceExeName = "WiimoteGun.Service.exe";
        private const string AppExeName = "WiimoteGun.exe";
        private const string LegacyPs1 = "WiimoteUpdate.ps1";

        private static int Main(string[] args)
        {
            try
            {
                Console.OutputEncoding = Encoding.UTF8;
                Console.Title = "Wiimote4Guns Update";
                Console.WriteLine("=== [ Wiimote4Guns native updater ] ===");
                Console.WriteLine();

                // EN/FR: App dir = our own folder (we live next to the app)
                string appDir = AppDomain.CurrentDomain.BaseDirectory;

                // [V60-FIX-4] EN: Clean up a stale renamed updater from a previous run
                try
                {
                    string stale = Path.Combine(appDir, SelfName + ".old");
                    if (File.Exists(stale)) { try { File.Delete(stale); Console.WriteLine("  cleanup: " + SelfName + ".old"); } catch { } }
                }
                catch { }

                // [0] Parse args: "<download_url>" [pid]
                if (args.Length < 1)
                {
                    Console.WriteLine("ERROR: usage: " + SelfName + " <download_url> [pid]");
                    Console.WriteLine("       The download URL is provided by WiimoteGun.exe.");
                    Pause();
                    return 1;
                }
                string downloadUrl = args[0];
                int waitPid = 0;
                if (args.Length >= 2 && !int.TryParse(args[1], out waitPid)) waitPid = 0;

                // Sanity check: this must be a Wiimote4Guns install folder
                if (!Directory.Exists(Path.Combine(appDir, "WiimoteGun.Service")))
                {
                    Console.WriteLine("ERROR: this does not look like a Wiimote4Guns installation folder:");
                    Console.WriteLine("       " + appDir);
                    Pause();
                    return 1;
                }

                // [1/5] Wait for the calling app to exit (it must release its exe)
                if (waitPid > 0)
                {
                    Console.WriteLine("[1/5] Waiting for WiimoteGun.exe (PID " + waitPid + ") to exit...");
                    try
                    {
                        Process appProc = Process.GetProcessById(waitPid);
                        if (appProc != null && !appProc.WaitForExit(30000))
                        {
                            Console.WriteLine("WARNING: app did not exit within 30s — killing it.");
                            try { appProc.Kill(); appProc.WaitForExit(5000); } catch { }
                        }
                        else
                        {
                            Console.WriteLine("  App exited.");
                        }
                    }
                    catch (ArgumentException)
                    {
                        Console.WriteLine("  App already exited.");
                    }
                }
                else
                {
                    Console.WriteLine("[1/5] No PID to wait for — proceeding.");
                }

                // [2/5] Download the release archive
                Console.WriteLine("[2/5] Downloading update...");
                string tempDir = Path.Combine(Path.GetTempPath(), "W4G_Update_" + Guid.NewGuid().ToString("N"));
                string archivePath = Path.Combine(tempDir, "release.7z");
                Directory.CreateDirectory(tempDir);
                try
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    using (WebClient wc = new WebClient())
                    {
                        wc.Headers.Add("User-Agent", "Wiimote4Guns-Updater");
                        wc.DownloadFile(downloadUrl, archivePath);
                    }
                    Console.WriteLine("  Downloaded: " + Path.GetFileName(downloadUrl));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR: download failed: " + ex.Message);
                    Pause();
                    return 1;
                }

                // [3/5] Extract with 7za.exe (or native .zip)
                Console.WriteLine("[3/5] Extracting...");
                string extractDir = Path.Combine(tempDir, "extracted");
                Directory.CreateDirectory(extractDir);
                if (!ExtractArchive(archivePath, extractDir, appDir))
                {
                    Console.WriteLine("ERROR: extraction failed (no 7za.exe next to the app,");
                    Console.WriteLine("       no 7-Zip installed, not a .zip archive).");
                    Pause();
                    return 1;
                }

                // Content root: the archive may create a folder named after itself
                string contentRoot = extractDir;
                string[] rootEntries = Directory.GetFileSystemEntries(extractDir);
                if (rootEntries.Length == 1 && Directory.Exists(rootEntries[0]))
                    contentRoot = rootEntries[0];

                // [4/5] Apply with correct staging
                Console.WriteLine("[4/5] Applying update...");
                int applied = 0;
                string stagingDir = Path.Combine(appDir, "WiimoteGun.Service", "update_service");

                foreach (string item in Directory.GetFileSystemEntries(contentRoot))
                {
                    string name = Path.GetFileName(item);
                    if (Directory.Exists(item))
                    {
                        if (name.Equals("WiimoteGun.Service", StringComparison.OrdinalIgnoreCase))
                        {
                            // Service folder → FULL staging (files AND subfolders, HmHost included)
                            // The service exe is RUNNING — it cannot be overwritten directly.
                            // It will be updated by the app at startup (existing mechanism).
                            Directory.CreateDirectory(stagingDir);
                            foreach (string svcItem in Directory.GetFileSystemEntries(item))
                            {
                                string svcName = Path.GetFileName(svcItem);
                                if (Directory.Exists(svcItem))
                                {
                                    if (svcName.Equals("update_service", StringComparison.OrdinalIgnoreCase))
                                    {
                                        foreach (string nested in Directory.GetFileSystemEntries(svcItem))
                                            MergeEntry(nested, Path.Combine(stagingDir, Path.GetFileName(nested)), ref applied);
                                    }
                                    else
                                    {
                                        MergeEntry(svcItem, Path.Combine(stagingDir, svcName), ref applied);
                                    }
                                }
                                else
                                {
                                    File.Copy(svcItem, Path.Combine(stagingDir, svcName), true);
                                    applied++;
                                    Console.WriteLine("  staged: " + svcName);
                                }
                            }
                        }
                        else
                        {
                            // App-level folder → recursive merge
                            MergeEntry(item, Path.Combine(appDir, name), ref applied);
                        }
                    }
                    else
                    {
                        // Top-level file → app directory
                        // EXCEPT: the running service exe (goes to staging anyway)
                        if (name.Equals(ServiceExeName, StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine("  skipped (running service): " + name);
                            continue;
                        }
                        // [V60-FIX-4] EN: The UPDATER itself — use the RENAME TRICK:
                        //     1. Rename the RUNNING update_W4G.exe to .old
                        //     2. Copy the new update_W4G.exe in its place
                        //     3. The .old file is cleaned up on the next run / app start
                        //     This replaces the old "skip" which LOST the new version
                        //     (the temp dir was deleted at the end).
                        //     FR: L'UPDATEUR lui-même — rename trick :
                        //     1. Renommer le update_W4G.exe EN COURS en .old
                        //     2. Copier le nouveau update_W4G.exe à sa place
                        //     3. Le fichier .old est nettoyé au prochain run / démarrage app
                        //     Ceci remplace l'ancien « skip » qui PERDAIT la nouvelle
                        //     version (le temp dir était supprimé à la fin).
                        if (name.Equals(SelfName, StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                string self = Process.GetCurrentProcess().MainModule.FileName;
                                string renamedSelf = Path.Combine(appDir, SelfName + ".old");
                                if (File.Exists(renamedSelf)) { try { File.Delete(renamedSelf); } catch { } }
                                File.Move(self, renamedSelf);
                                File.Copy(item, Path.Combine(appDir, SelfName), true);
                                applied++;
                                Console.WriteLine("  updater updated (rename trick): " + name);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("  WARNING: could not update the updater: " + ex.Message);
                                Console.WriteLine("  (the current version handles this update;");
                                Console.WriteLine("   the new version will apply on the next update cycle)");
                            }
                            continue;
                        }
                        try
                        {
                            File.Copy(item, Path.Combine(appDir, name), true);
                            applied++;
                            Console.WriteLine("  updated: " + name);
                        }
                        catch
                        {
                            Console.WriteLine("  skipped (locked): " + name);
                        }
                    }
                }

                // Refresh the LIVE UpdateService.ps1 (staged copy → live)
                try
                {
                    string stagedScript = Path.Combine(stagingDir, "UpdateService.ps1");
                    string liveScript = Path.Combine(appDir, "WiimoteGun.Service", "UpdateService.ps1");
                    if (File.Exists(stagedScript))
                    {
                        File.Copy(stagedScript, liveScript, true);
                        Console.WriteLine("  live UpdateService.ps1 refreshed.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("  WARNING: could not refresh UpdateService.ps1: " + ex.Message);
                }

                // [5/5] Cleanup + restart
                Console.WriteLine("[5/5] Cleanup and restart...");

                // Delete the legacy WiimoteUpdate.ps1 (replaced by update_W4G.exe)
                try
                {
                    string ps1 = Path.Combine(appDir, LegacyPs1);
                    if (File.Exists(ps1))
                    {
                        File.Delete(ps1);
                        Console.WriteLine("  deleted legacy " + LegacyPs1);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("  WARNING: could not delete " + LegacyPs1 + ": " + ex.Message);
                }

                // Clean up stale .7z and .null files in the app dir
                try
                {
                    foreach (string f in Directory.GetFiles(appDir, "*.7z"))
                    {
                        File.Delete(f);
                        Console.WriteLine("  cleanup: " + Path.GetFileName(f));
                    }
                    foreach (string f in Directory.GetFiles(appDir, "*.null"))
                    {
                        File.Delete(f);
                        Console.WriteLine("  cleanup: " + Path.GetFileName(f));
                    }
                }
                catch { }

                // Restart WiimoteGun.exe (current user, never admin)
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = Path.Combine(appDir, AppExeName),
                        WorkingDirectory = appDir,
                        UseShellExecute = true
                    });
                    Console.WriteLine("  " + AppExeName + " restarted.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("WARNING: could not start " + AppExeName + ": " + ex.Message);
                    Console.WriteLine("         Please start it manually from:");
                    Console.WriteLine("         " + Path.Combine(appDir, AppExeName));
                }

                // NOW safe to clean up temp (app exe already copied)
                try { Directory.Delete(tempDir, true); } catch { }

                Console.WriteLine();
                Console.WriteLine("DONE - " + applied + " items applied.");
                Console.WriteLine("(EN) The app will offer the service update (Admin) if needed.");
                Console.WriteLine("(FR) L'app proposera la mise a jour du service (Admin) si besoin.");
                Pause();
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("CRITICAL ERROR: " + ex.Message);
                Pause();
                return 1;
            }
        }

        /// <summary>
        /// EN: Extract an archive with 7za.exe (shipped with the app), or system 7-Zip,
        ///     or .zip natively via ZipFile.
        /// </summary>
        private static bool ExtractArchive(string archivePath, string extractDir, string appDir)
        {
            if (archivePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    ZipFile.ExtractToDirectory(archivePath, extractDir);
                    return true;
                }
                catch { return false; }
            }

            string sevenZip = null;
            string local = Path.Combine(appDir, "7za.exe");
            if (File.Exists(local)) sevenZip = local;
            if (sevenZip == null)
            {
                foreach (string name in new[] { "7z.exe", "7za.exe" })
                {
                    string pathOnPath = FindOnPath(name);
                    if (pathOnPath != null) { sevenZip = pathOnPath; break; }
                }
            }
            if (sevenZip == null)
            {
                foreach (string p in new[] { @"C:\Program Files\7-Zip\7z.exe", @"C:\Program Files (x86)\7-Zip\7z.exe" })
                {
                    if (File.Exists(p)) { sevenZip = p; break; }
                }
            }
            if (sevenZip == null) return false;

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = sevenZip,
                    Arguments = "x -y -o\"" + extractDir + "\" \"" + archivePath + "\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };
                using (Process p = Process.Start(psi))
                {
                    p.StandardOutput.ReadToEnd();
                    p.WaitForExit(120000);
                    return p.HasExited && p.ExitCode == 0;
                }
            }
            catch { return false; }
        }

        // EN/FR: Merge a file OR a directory into its target (recursive, overwrites).
        private static void MergeEntry(string source, string target, ref int count)
        {
            try
            {
                if (Directory.Exists(source))
                {
                    Directory.CreateDirectory(target);
                    foreach (string entry in Directory.GetFileSystemEntries(source))
                        MergeEntry(entry, Path.Combine(target, Path.GetFileName(entry)), ref count);
                }
                else
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(target));
                    File.Copy(source, target, true);
                    count++;
                }
            }
            catch
            {
                // EN/FR: Locked file - keep going
            }
        }

        private static string FindOnPath(string name)
        {
            try
            {
                string pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
                foreach (string dir in pathEnv.Split(';'))
                {
                    if (string.IsNullOrEmpty(dir)) continue;
                    string full = Path.Combine(dir.Trim('"'), name);
                    if (File.Exists(full)) return full;
                }
            }
            catch { }
            return null;
        }

        private static void Pause()
        {
            Console.WriteLine();
            Console.Write("Press any key to close... (Appuyez sur une touche pour fermer)");
            try { Console.ReadKey(true); } catch { }
        }
    }
}
