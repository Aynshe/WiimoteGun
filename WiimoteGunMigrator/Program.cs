using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace WiimoteGunMigrator
{
    /// <summary>
    /// [3.1.0.1] EN: TRANSITIONAL MIGRATOR for the broken 3.1.0.0 installs.
    ///     Shipped as "WiimoteGun.exe" at the root of the transition archive
    ///     "Wiimote4Guns_beta_pre-release_v3.1.0.0.7z". The flow:
    ///       1. The OLD WiimoteUpdate.ps1 downloads the transition archive,
    ///          extracts it, copies the top-level exe files (the migrator
    ///          replaces WiimoteGun.exe) and RESTARTS "WiimoteGun.exe" - which
    ///          is now THIS migrator. NOTE: the old script may NOT copy the
    ///          nested .7z to the app dir (observed on v3.0.0.21: only the exe
    ///          and side files were copied), so the migrator cannot rely on
    ///          finding it on disk.
    ///       2. The migrator RE-DOWNLOADS the transition archive from GitHub
    ///          (same asset the old script downloaded), extracts it, and finds
    ///          the NESTED real-release .7z inside it.
    ///       3. Extracts the nested archive and applies its content with the
    ///          CORRECT compiled staging logic: top-level files -> app dir
    ///          (except the running migrator), top-level folders -> recursive
    ///          merge, WiimoteGun.Service\ (files AND subfolders, HmHost
    ///          INCLUDED) -> update_service\ staging, UpdateService.ps1 -> LIVE.
    ///       4. The migrator renames itself aside, copies the REAL
    ///          WiimoteGun.exe in place, starts it, and exits - the update
    ///          continues like a normal one (service prompt + HmHost deploy).
    ///     FR: MIGRATEUR DE TRANSITION pour les installs 3.1.0.0 cassées.
    ///     Livré en « WiimoteGun.exe » à la racine de l'archive de transition
    ///     « Wiimote4Guns_beta_pre-release_v3.1.0.0.7z ». Le flux :
    ///       1. L'ANCIEN WiimoteUpdate.ps1 télécharge l'archive de transition,
    ///          l'extrait, copie les fichiers exe racine (le migrateur remplace
    ///          WiimoteGun.exe) et RELANCE « WiimoteGun.exe » - qui est
    ///          désormais CE migrateur. NOTE : l'ancien script peut NE PAS
    ///          copier le .7z imbriqué vers l'app (observé sur v3.0.0.21 :
    ///          seuls l'exe et les fichiers annexes ont été copiés), donc le
    ///          migrateur ne peut pas compter sur le trouver sur disque.
    ///       2. Le migrateur RE-TÉLÉCHARGE l'archive de transition depuis
    ///          GitHub (le même asset que l'ancien script), l'extrait, et
    ///          trouve le .7z de la VRAIE release EMBARQUÉE dedans.
    ///       3. Extrait l'archive imbriquée et applique son contenu avec la
    ///          logique de staging CORRECTE compilée : fichiers racine ->
    ///          dossier de l'app (sauf le migrateur en cours), dossiers
    ///          racine -> fusion récursive, WiimoteGun.Service\ (fichiers ET
    ///          sous-dossiers, HmHost INCLUS) -> staging update_service\,
    ///          UpdateService.ps1 -> VIVANT.
    ///       4. Le migrateur se renomme à part, copie le VRAI WiimoteGun.exe
    ///          à sa place, le lance, et se termine - la mise à jour continue
    ///          comme une normale (prompt service + déploiement HmHost).
    /// </summary>
    internal static class Program
    {
        private const string ReleasesApiUrl = "https://api.github.com/repos/Aynshe/WiimoteGun/releases";
        private const string AssetPrefix = "Wiimote4Guns_";
        private const string SelfRenameSuffix = ".migrator-old.exe";

        private static int Main(string[] args)
        {
            try
            {
                Console.OutputEncoding = Encoding.UTF8;
                Console.WriteLine("=== [ WiimoteGun transition migrator ] ===");
                Console.WriteLine();

                // EN/FR: App dir = the migrator's own folder (it IS the app exe now)
                string appDir = AppDomain.CurrentDomain.BaseDirectory;

                // EN/FR: Clean up a stale renamed migrator from a previous run
                try
                {
                    string stale = Path.Combine(appDir, "WiimoteGun" + SelfRenameSuffix);
                    if (File.Exists(stale)) File.Delete(stale);
                }
                catch { }

                if (!Directory.Exists(Path.Combine(appDir, "WiimoteGun.Service")))
                {
                    Console.WriteLine("ERROR: this does not look like a Wiimote4Guns installation folder:");
                    Console.WriteLine("       " + appDir);
                    Pause();
                    return 1;
                }

                // [1/4] RE-DOWNLOAD the transition archive from GitHub (the old script
                //     did NOT copy the nested .7z to the app dir - observed on v3.0.0.21)
                Console.WriteLine("[1/4] Downloading the transition archive from GitHub...");
                string transitionUrl;
                if (!FindTransitionAsset(out transitionUrl))
                {
                    Console.WriteLine("ERROR: no Wiimote4Guns_*.7z asset found on the prerelease.");
                    Pause();
                    return 1;
                }

                string tempDir = Path.Combine(Path.GetTempPath(), "Wiimote4Guns_Migrator_" + Guid.NewGuid().ToString("N"));
                string transitionArchive = Path.Combine(tempDir, "transition" + Path.GetExtension(new Uri(transitionUrl).AbsolutePath));
                Directory.CreateDirectory(tempDir);
                try
                {
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    using (WebClient wc = new WebClient())
                    {
                        wc.Headers.Add("User-Agent", "Wiimote4Guns-Migrator");
                        wc.DownloadFile(transitionUrl, transitionArchive);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR: download failed: " + ex.Message);
                    Pause();
                    return 1;
                }

                // Extract the transition archive
                string transitionExtract = Path.Combine(tempDir, "transition_extracted");
                Directory.CreateDirectory(transitionExtract);
                if (!ExtractArchive(transitionArchive, transitionExtract, appDir))
                {
                    Console.WriteLine("ERROR: could not extract the transition archive.");
                    Pause();
                    return 1;
                }

                // Content root (single top folder -> descend)
                string transitionRoot = transitionExtract;
                string[] transEntries = Directory.GetFileSystemEntries(transitionExtract);
                if (transEntries.Length == 1 && Directory.Exists(transEntries[0]))
                    transitionRoot = transEntries[0];

                // Find the NESTED real-release .7z inside the extracted transition archive
                string nestedArchive = FindNestedArchiveInDir(transitionRoot);
                if (nestedArchive == null)
                {
                    Console.WriteLine("ERROR: no release .7z found inside the transition archive.");
                    Console.WriteLine("The transition archive must contain the real release .7z at its root.");
                    Pause();
                    return 1;
                }
                Console.WriteLine("Found nested release: " + Path.GetFileName(nestedArchive));

                // [2/4] Extract the NESTED real-release archive
                Console.WriteLine("[2/4] Extracting the real release...");
                string extractDir = Path.Combine(tempDir, "release_extracted");
                Directory.CreateDirectory(extractDir);
                if (!ExtractArchive(nestedArchive, extractDir, appDir))
                {
                    Console.WriteLine("ERROR: extraction failed (no 7za.exe next to the app,");
                    Console.WriteLine("       no 7-Zip installed, not a .zip archive).");
                    Pause();
                    return 1;
                }

                // EN/FR: Content root: the archive may create a folder named after itself -
                //     descend into it (same convention as WiimoteUpdate.ps1)
                string fixRoot = extractDir;
                string[] rootEntries = Directory.GetFileSystemEntries(extractDir);
                if (rootEntries.Length == 1 && Directory.Exists(rootEntries[0]))
                    fixRoot = rootEntries[0];

                // [3/4] Apply with the CORRECT staging logic (compiled - no script involved)
                Console.WriteLine("[3/4] Applying update (correct staging: HmHost included)...");
                int applied = 0;
                string stagingDir = Path.Combine(appDir, "WiimoteGun.Service", "update_service");

                foreach (string item in Directory.GetFileSystemEntries(fixRoot))
                {
                    string name = Path.GetFileName(item);
                    if (Directory.Exists(item))
                    {
                        if (name.Equals("WiimoteGun.Service", StringComparison.OrdinalIgnoreCase))
                        {
                            // EN/FR: Service folder -> FULL staging (files AND subfolders, HmHost included)
                            Directory.CreateDirectory(stagingDir);
                            foreach (string svcItem in Directory.GetFileSystemEntries(item))
                            {
                                string svcName = Path.GetFileName(svcItem);
                                if (Directory.Exists(svcItem))
                                {
                                    if (svcName.Equals("update_service", StringComparison.OrdinalIgnoreCase))
                                    {
                                        // EN/FR: The archive's own staging -> merge its content into the live staging
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
                            // EN/FR: App-level folder (1-patch_emu, WiimoteGunDriver, ...) -> recursive merge
                            MergeEntry(item, Path.Combine(appDir, name), ref applied);
                        }
                    }
                    else
                    {
                        // EN/FR: Top-level file -> app directory. The running migrator IS
                        //     WiimoteGun.exe: the real exe is handled by the rename trick
                        //     below, not by a blind copy (it would fail locked).
                        if (name.Equals("WiimoteGun.exe", StringComparison.OrdinalIgnoreCase))
                            continue;
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

                // EN/FR: The LIVE UpdateService.ps1 must be the new one NOW (the broken
                //     machines keep a pre-V57d copy that cannot deploy HmHost)
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

                // [4/4] Replace THIS binary with the real WiimoteGun.exe (rename trick)
                //     and start it - the update continues like a normal one.
                //     NOTE: the temp cleanup happens AFTER this step - the real
                //     WiimoteGun.exe lives inside the temp extraction dir.
                Console.WriteLine("[4/4] Installing the real WiimoteGun.exe and starting it...");
                string realExeSource = Path.Combine(fixRoot, "WiimoteGun.exe");
                bool realExeInstalled = false;
                if (File.Exists(realExeSource))
                {
                    try
                    {
                        string self = Process.GetCurrentProcess().MainModule.FileName;
                        string renamedSelf = Path.Combine(appDir, "WiimoteGun" + SelfRenameSuffix);
                        if (File.Exists(renamedSelf)) { try { File.Delete(renamedSelf); } catch { } }
                        File.Move(self, renamedSelf); // EN/FR: rename the RUNNING exe aside
                        File.Copy(realExeSource, Path.Combine(appDir, "WiimoteGun.exe"), true);
                        realExeInstalled = true;
                        Console.WriteLine("  real WiimoteGun.exe installed.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("  WARNING: could not install the real exe now: " + ex.Message);
                    }
                }
                else
                {
                    Console.WriteLine("  WARNING: no WiimoteGun.exe found in the extracted release.");
                }

                // EN/FR: Start the real app - the normal update flow takes over
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = Path.Combine(appDir, "WiimoteGun.exe"),
                        WorkingDirectory = appDir,
                        UseShellExecute = true
                    });
                    Console.WriteLine("WiimoteGun started - update continues like a normal one.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("WARNING: could not start WiimoteGun: " + ex.Message);
                }

                Console.WriteLine();

                // EN/FR: NOW it is safe to clean up the temp dir (the real exe has been
                //     copied to the app dir and the app has been started)
                try { Directory.Delete(tempDir, true); } catch { }

                // EN/FR: Clean up the transition leftovers in the APP dir: any .7z
                //     archive file and any .null info file the old update script may
                //     have copied there when it applied the transition archive.
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

                Console.WriteLine("DONE - " + applied + " items applied" + (realExeInstalled ? " + real app installed." : "."));
                Console.WriteLine("(EN) The app now offers the service update (Admin) to deploy the");
                Console.WriteLine("     HmHost driver next to the service.");
                Console.WriteLine("(FR) L'app propose maintenant la mise a jour service (Admin) pour");
                Console.WriteLine("     deployer le pilote HmHost a cote du service.");
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
        /// EN: Find the transition archive asset on the GitHub prerelease - the same
        ///     asset the old script downloaded (first "Wiimote4Guns_*.7z" from the
        ///     newest non-draft release). FR: Trouve l'asset de l'archive de
        ///     transition sur la prerelease GitHub - le même asset que l'ancien
        ///     script a téléchargé (premier « Wiimote4Guns_*.7z » de la release
        ///     non-brouillon la plus récente).
        /// </summary>
        private static bool FindTransitionAsset(out string assetUrl)
        {
            assetUrl = null;
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                string json;
                using (WebClient wc = new WebClient())
                {
                    wc.Headers.Add("User-Agent", "Wiimote4Guns-Migrator");
                    json = wc.DownloadString(ReleasesApiUrl);
                }
                if (string.IsNullOrEmpty(json)) return false;

                Regex assetRegex = new Regex(@"""browser_download_url""\s*:\s*""([^""]*Wiimote4Guns_[^""]*\.(?:7z|zip))""", RegexOptions.IgnoreCase);
                Match m = assetRegex.Match(json);
                if (m.Success)
                {
                    assetUrl = m.Groups[1].Value;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("WARNING: GitHub query failed: " + ex.Message);
            }
            return false;
        }

        /// <summary>
        /// EN: Find any .7z or .zip archive file inside the given directory (not
        ///     recursive - only at the root of the given folder). Used to find the
        ///     nested real-release .7z inside the extracted transition archive.
        ///     FR: Trouve tout fichier archive .7z ou .zip dans le dossier donné (pas
        ///     récursif - seulement à la racine du dossier). Utilisé pour trouver le
        ///     .7z de la vraie release imbriqué dans l'archive de transition extraite.
        /// </summary>
        private static string FindNestedArchiveInDir(string dir)
        {
            try
            {
                foreach (string file in Directory.GetFiles(dir, "*.7z"))
                {
                    return file;
                }
                foreach (string file in Directory.GetFiles(dir, "*.zip"))
                {
                    return file;
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// EN: Extract an archive with 7za.exe (shipped with the app), or system 7-Zip,
        ///     or .zip natively via ZipFile.
        ///     FR: Extrait une archive avec 7za.exe (livré avec l'app), ou 7-Zip
        ///     système, ou .zip nativement via ZipFile.
        /// </summary>
        private static bool ExtractArchive(string archivePath, string extractDir, string appDir)
        {
            // EN/FR: .zip - native extraction, no tool needed
            if (archivePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    ZipFile.ExtractToDirectory(archivePath, extractDir);
                    return true;
                }
                catch { return false; }
            }

            // EN/FR: .7z - need 7za.exe or 7-Zip
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
