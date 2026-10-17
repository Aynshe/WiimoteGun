using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace W4G_Setup
{
    /// <summary>
    /// [V61] EN: Standalone Wiimote4Guns setup wizard for RetroBat.
    ///     Downloads the latest GitHub release (or uses a local archive found
    ///     next to the setup exe — offline mode), extracts it to
    ///     <RetroBat>\plugins\Wiimote4Guns with the EMBEDDED 7za.exe, optionally
    ///     installs the .NET 10 runtime, lets the user pick an LED layout +
    ///     Hybrid PnP, then generates settings.cfg via WiimoteGun.exe -presetup.
    ///     Bilingual: FR if Windows is French, EN otherwise.
    ///     FR: Assistant d'installation autonome de Wiimote4Guns pour RetroBat.
    ///     Télécharge la dernière release GitHub (ou utilise une archive locale
    ///     trouvée à côté de l'exe du setup — mode offline), l'extrait vers
    ///     <RetroBat>\plugins\Wiimote4Guns avec le 7za.exe EMBARQUÉ, installe
    ///     optionnellement le runtime .NET 10, laisse l'utilisateur choisir un
    ///     layout LED + Hybrid PnP, puis génère settings.cfg via
    ///     WiimoteGun.exe -presetup. Bilingue : FR si Windows est français.
    /// </summary>
    internal static class Program
    {
        private const string ReleasesApiUrl = "https://api.github.com/repos/Aynshe/WiimoteGun/releases";
        private const string DotnetVersionFile = "https://builds.dotnet.microsoft.com/dotnet/Runtime/10.0/latest.version";
        private const string DotnetFeedBase = "https://builds.dotnet.microsoft.com/dotnet/Runtime";

        private static bool _isFrench;
        private static string _tempDir;
        private static string _sevenZipPath;
        private static string _logPath;

        [STAThread]
        static int Main(string[] args)
        {
            try
            {
                // Initialize the log file (next to the setup exe)
                _logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "W4G_Setup.log");
                try { File.WriteAllText(_logPath, "=== W4G_Setup started " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " ===\r\n"); }
                catch { }

                // Language detection
                try
                {
                    var ci = CultureInfo.CurrentUICulture;
                    _isFrench = ci != null && (ci.TwoLetterISOLanguageName == "fr" || ci.Name.StartsWith("fr"));
                }
                catch { _isFrench = false; }
                Log("Language: " + (_isFrench ? "FR" : "EN"));

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Extract the EMBEDDED 7za.exe to temp — must happen before any extraction
                if (!Extract7Zip())
                {
                    MessageBox.Show(
                        L("Failed to extract the embedded 7za.exe.", "Échec de l'extraction du 7za.exe embarqué."),
                        "Wiimote4Guns Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return 1;
                }

                using (var setupForm = new SetupForm())
                {
                    Application.Run(setupForm);
                    return setupForm.SetupCompleted ? 0 : 1;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Setup failed: " + ex.Message + "\n\n" +
                    "Échec de l'installation : " + ex.Message,
                    "Wiimote4Guns Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
        }

        // ========== Language helpers ==========
        public static bool IsFrench { get { return _isFrench; } }
        public static string L(string en, string fr) { return _isFrench ? fr : en; }

        // ========== Logging ==========
        public static void Log(string message)
        {
            try
            {
                string line = DateTime.Now.ToString("HH:mm:ss.fff") + "  " + message + "\r\n";
                File.AppendAllText(_logPath, line);
            }
            catch { }
        }

        // ========== Install dir resolution ==========
        /// <summary>
        /// EN: Resolves the install directory from the chosen path. If the chosen
        ///     path IS the registry-detected RetroBat path → plugins\Wiimote4Guns.
        ///     Otherwise → the chosen path is the install target (standalone).
        ///     FR: Résout le dossier d'installation depuis le chemin choisi. Si le
        ///     chemin EST le chemin RetroBat détecté au registre → plugins\Wiimote4Guns.
        ///     Sinon → le chemin choisi est la cible (standalone).
        /// </summary>
        public static string GetInstallDir(string chosenPath)
        {
            string retroBat, err;
            if (FindRetroBatPath(out retroBat, out err) &&
                string.Equals(chosenPath.TrimEnd('\\'), retroBat.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                return Path.Combine(chosenPath, "plugins", "Wiimote4Guns");
            return chosenPath;
        }

        // ========== Embedded 7za.exe ==========
        /// <summary>
        /// EN: Extracts the EMBEDDED 7za.exe resource to the temp directory.
        ///     Returns false if extraction fails. The extracted file is used for
        ///     all archive extraction — the setup is fully standalone.
        ///     FR: Extrait la ressource EMBARQUÉE 7za.exe vers le répertoire
        ///     temporaire. Renvoie false si l'échec. Le fichier extrait est
        ///     utilisé pour toute extraction d'archive — le setup est autonome.
        /// </summary>
        private static bool Extract7Zip()
        {
            try
            {
                _tempDir = Path.Combine(Path.GetTempPath(), "W4G_Setup_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(_tempDir);
                _sevenZipPath = Path.Combine(_tempDir, "7za.exe");

                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                // Try both common manifest resource names
                string[] possibleNames = { "W4G_Setup.7za.exe", "W4G_Setup.Resources.7za.exe", "7za.exe" };
                System.IO.Stream stream = null;
                foreach (string name in possibleNames)
                {
                    stream = assembly.GetManifestResourceStream(name);
                    if (stream != null) break;
                }

                if (stream == null)
                {
                    // List all resources for debugging
                    var all = assembly.GetManifestResourceNames();
                    foreach (string n in all) Console.WriteLine("Resource: " + n);
                    return false;
                }

                using (stream)
                using (var fs = new FileStream(_sevenZipPath, FileMode.Create, FileAccess.Write))
                {
                    stream.CopyTo(fs);
                }

                return File.Exists(_sevenZipPath) && new FileInfo(_sevenZipPath).Length > 100000;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Extract7Zip failed: " + ex.Message);
                return false;
            }
        }

        // ========== Offline mode: local archive detection ==========
        /// <summary>
        /// EN: Finds a Wiimote4Guns_*.7z or .zip archive next to the setup exe
        ///     (offline mode). Returns the path or null.
        ///     FR: Trouve une archive Wiimote4Guns_*.7z ou .zip à côté de l'exe
        ///     du setup (mode offline). Renvoie le chemin ou null.
        /// </summary>
        public static string FindLocalArchive()
        {
            try
            {
                string setupDir = AppDomain.CurrentDomain.BaseDirectory;
                var files = Directory.GetFiles(setupDir, "Wiimote4Guns_*.7z")
                    .Concat(Directory.GetFiles(setupDir, "Wiimote4Guns_*.zip"));
                var sorted = files.OrderByDescending(f => f).ToArray();
                return sorted.Length > 0 ? sorted[0] : null;
            }
            catch { return null; }
        }

        /// <summary>
        /// EN: Extracts the version from a Wiimote4Guns archive filename
        ///     (e.g., "Wiimote4Guns_beta_pre-release_v3.1.6.0.7z" → "3.1.6.0").
        ///     FR: Extrait la version d'un nom de fichier d'archive Wiimote4Guns
        ///     (ex. « Wiimote4Guns_beta_pre-release_v3.1.6.0.7z » → « 3.1.6.0 »).
        /// </summary>
        public static string GetArchiveVersion(string archivePath)
        {
            try
            {
                string name = Path.GetFileNameWithoutExtension(archivePath);
                var match = Regex.Match(name, @"_?v\.?(\d+(?:\.\d+)*)", RegexOptions.IgnoreCase);
                if (match.Success) return match.Groups[1].Value;
            }
            catch { }
            return null;
        }

        // ========== Installed version detection ==========
        /// <summary>
        /// EN: Reads the assembly version of the WiimoteGun.exe already installed in
        ///     the target directory. Returns null if not installed or unreadable.
        ///     FR: Lit la version d'assembly du WiimoteGun.exe déjà installé dans
        ///     le dossier cible. Renvoie null si absent ou illisible.
        /// </summary>
        public static string GetInstalledVersion(string installDir)
        {
            try
            {
                string exe = Path.Combine(installDir, "WiimoteGun.exe");
                if (!File.Exists(exe)) return null;
                var vi = FileVersionInfo.GetVersionInfo(exe);
                if (vi != null && !string.IsNullOrEmpty(vi.FileVersion))
                    return vi.FileVersion;
            }
            catch { }
            return null;
        }

        /// <summary>
        /// EN: Compares two version strings (e.g., "3.1.5.10" vs "3.1.6.0").
        ///     Returns: -1 if a < b, 0 if equal, 1 if a > b. Null-safe.
        ///     FR: Compare deux chaînes de version (ex. « 3.1.5.10 » vs « 3.1.6.0 »).
        ///     Renvoie : -1 si a < b, 0 si égal, 1 si a > b. Null-safe.
        /// </summary>
        public static int CompareVersions(string a, string b)
        {
            if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b)) return 0;
            if (string.IsNullOrEmpty(a)) return -1;
            if (string.IsNullOrEmpty(b)) return 1;

            var pa = a.Split('.');
            var pb = b.Split('.');
            int len = Math.Max(pa.Length, pb.Length);
            for (int i = 0; i < len; i++)
            {
                int va = i < pa.Length ? int.Parse(pa[i]) : 0;
                int vb = i < pb.Length ? int.Parse(pb[i]) : 0;
                if (va < vb) return -1;
                if (va > vb) return 1;
            }
            return 0;
        }

        // ========== RetroBat path detection ==========
        // [V61] EN: Uses the SAME registry key and value as WiimoteGun.exe
        //     (RemapProfileManager): HKCU\Software\RetroBat\LatestKnownInstallPath.
        //     FR: Utilise la MÊME clé registre et valeur que WiimoteGun.exe
        //     (RemapProfileManager) : HKCU\Software\RetroBat\LatestKnownInstallPath.
        private const string RetroBatRegKey = @"Software\RetroBat";
        private const string RetroBatRegValue = "LatestKnownInstallPath";

        public static bool FindRetroBatPath(out string path, out string error)
        {
            path = null; error = null;
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RetroBatRegKey))
                {
                    if (key != null)
                    {
                        var val = key.GetValue(RetroBatRegValue) as string;
                        if (!string.IsNullOrEmpty(val) && Directory.Exists(val))
                        {
                            path = val;
                            Log("RetroBat found in registry (HKCU\\" + RetroBatRegKey + "\\" + RetroBatRegValue + "): " + val);
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Registry read failed: " + ex.Message);
            }

            error = L("RetroBat not found in the registry.\nBrowse to your RetroBat folder, or select any folder for a standalone installation.",
                "RetroBat introuvable dans le registre.\nNaviguez vers votre dossier RetroBat, ou sélectionnez n'importe quel dossier pour une installation standalone.");
            return false;
        }

        // ========== GitHub latest release ==========
        public static bool FindLatestRelease(out string downloadUrl, out string error)
        {
            downloadUrl = null; error = null;
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                string json;
                using (var wc = new WebClient())
                {
                    wc.Headers.Add("User-Agent", "Wiimote4Guns-Setup");
                    json = wc.DownloadString(ReleasesApiUrl);
                }
                if (string.IsNullOrEmpty(json)) { error = "Empty GitHub response."; return false; }

                var assetRegex = new Regex(@"""browser_download_url""\s*:\s*""([^""]*Wiimote4Guns_[^""]*\.(?:7z|zip))""",
                    RegexOptions.IgnoreCase);
                var match = assetRegex.Match(json);
                if (match.Success)
                {
                    downloadUrl = match.Groups[1].Value;
                    return true;
                }
                error = "No Wiimote4Guns release asset found on GitHub.";
                return false;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        // ========== Download & extract ==========
        public static bool DownloadAndExtract(string downloadUrl, string targetDir, out string error)
        {
            error = null;
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                string archivePath;

                // [V61] EN: If the URL is actually a LOCAL FILE path (offline mode),
                //     use it directly instead of downloading.
                //     FR: Si l'URL est en fait un CHEMIN DE FICHIER LOCAL (mode offline),
                //     l'utiliser directement au lieu de télécharger.
                if (File.Exists(downloadUrl))
                {
                    archivePath = downloadUrl;
                }
                else
                {
                    string tempArchive = Path.Combine(_tempDir, "release" + Path.GetExtension(new Uri(downloadUrl).AbsolutePath));
                    using (var wc = new WebClient())
                    {
                        wc.Headers.Add("User-Agent", "Wiimote4Guns-Setup");
                        wc.DownloadFile(downloadUrl, tempArchive);
                    }
                    archivePath = tempArchive;
                }

                // Extract
                string extractDir = Path.Combine(_tempDir, "extracted");
                Directory.CreateDirectory(extractDir);

                if (archivePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    ZipFile.ExtractToDirectory(archivePath, extractDir);
                }
                else
                {
                    // .7z — use the EMBEDDED 7za.exe (extracted to temp at startup)
                    if (string.IsNullOrEmpty(_sevenZipPath) || !File.Exists(_sevenZipPath))
                    {
                        error = L("Embedded 7za.exe not available.", "Le 7za.exe embarqué n'est pas disponible.");
                        return false;
                    }
                    var psi = new ProcessStartInfo
                    {
                        FileName = _sevenZipPath,
                        Arguments = "x -y -o\"" + extractDir + "\" \"" + archivePath + "\"",
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true
                    };
                    using (Process p = Process.Start(psi))
                    {
                        p.StandardOutput.ReadToEnd();
                        p.WaitForExit(120000);
                        if (!p.HasExited || p.ExitCode != 0)
                        {
                            error = "7za extraction failed (exit code " + (p.HasExited ? p.ExitCode.ToString() : "timeout") + ").";
                            return false;
                        }
                    }
                }

                // Content root: descend if single top folder
                string contentRoot = extractDir;
                var entries = Directory.GetFileSystemEntries(extractDir);
                if (entries.Length == 1 && Directory.Exists(entries[0]))
                    contentRoot = entries[0];

                // Copy to target
                Directory.CreateDirectory(targetDir);
                foreach (var item in Directory.GetFileSystemEntries(contentRoot))
                {
                    string name = Path.GetFileName(item);
                    string target = Path.Combine(targetDir, name);
                    if (Directory.Exists(item))
                        CopyDirectory(item, target);
                    else
                        File.Copy(item, target, true);
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        // ========== .NET 10 runtime ==========
        public static bool IsNet10Installed()
        {
            string[] roots = {
                Environment.GetEnvironmentVariable("DOTNET_ROOT_X64"),
                Environment.GetEnvironmentVariable("DOTNET_ROOT"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet"),
            };
            foreach (string root in roots)
            {
                if (string.IsNullOrEmpty(root)) continue;
                try
                {
                    string path = Path.Combine(root, "shared", "Microsoft.NETCore.App");
                    if (Directory.Exists(path))
                    {
                        foreach (string dir in Directory.GetDirectories(path, "10.*"))
                        {
                            if (!dir.Contains("preview") && !dir.Contains("rc")) return true;
                        }
                    }
                }
                catch { }
            }
            return false;
        }

        public static bool InstallNet10Runtime(out string error)
        {
            error = null;
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                string versionText;
                using (var wc = new WebClient())
                {
                    wc.Headers.Add("User-Agent", "Wiimote4Guns-Setup");
                    versionText = wc.DownloadString(DotnetVersionFile).Trim();
                }
                string latestVersion = null;
                foreach (string line in versionText.Split('\n'))
                {
                    string trimmed = line.Trim();
                    if (!string.IsNullOrEmpty(trimmed)) latestVersion = trimmed;
                }
                if (string.IsNullOrEmpty(latestVersion)) { error = "Could not parse .NET 10 version."; return false; }

                string productVersion = latestVersion;
                try
                {
                    string pvUrl = DotnetFeedBase + "/" + latestVersion + "/productVersion.txt";
                    using (var wc = new WebClient())
                    {
                        wc.Headers.Add("User-Agent", "Wiimote4Guns-Setup");
                        string pv = wc.DownloadString(pvUrl).Trim();
                        if (!string.IsNullOrEmpty(pv)) productVersion = pv;
                    }
                }
                catch { }

                string installerUrl = DotnetFeedBase + "/" + latestVersion +
                    "/dotnet-runtime-" + productVersion + "-win-x64.exe";
                string tempInstaller = Path.Combine(_tempDir, "dotnet-runtime-" + latestVersion + "-win-x64.exe");
                using (var wc = new WebClient())
                {
                    wc.Headers.Add("User-Agent", "Wiimote4Guns-Setup");
                    wc.DownloadFile(installerUrl, tempInstaller);
                }

                var psi = new ProcessStartInfo
                {
                    FileName = tempInstaller,
                    Arguments = "/install /quiet /norestart",
                    UseShellExecute = true,
                    Verb = "runas"
                };
                using (Process installer = Process.Start(psi))
                {
                    if (installer == null) { error = "Failed to start runtime installer."; return false; }
                    if (!installer.WaitForExit(300000))
                    {
                        try { installer.Kill(); } catch { }
                        error = "Runtime installer timed out.";
                        return false;
                    }
                    if (installer.ExitCode != 0 && installer.ExitCode != 3010)
                    {
                        error = "Runtime installer exited with code " + installer.ExitCode + ".";
                        return false;
                    }
                }

                if (IsNet10Installed()) return true;

                error = L("Runtime installed but not detected. A Windows restart may be needed.",
                    "Runtime installé mais non détecté. Un redémarrage de Windows peut être nécessaire.");
                return false;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        // ========== Generate settings.cfg via WiimoteGun.exe -presetup ==========
        public static bool RunPreSetup(string installDir, string layoutName, bool hybridPnp, out string error)
        {
            error = null;
            try
            {
                string exe = Path.Combine(installDir, "WiimoteGun.exe");
                if (!File.Exists(exe))
                {
                    error = L("WiimoteGun.exe not found in the installation directory.",
                        "WiimoteGun.exe introuvable dans le dossier d'installation.");
                    return false;
                }

                string args = "-presetup -layout:\"" + layoutName + "\" -hybrid:" + (hybridPnp ? "true" : "false");
                var psi = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = args,
                    WorkingDirectory = installDir,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (Process p = Process.Start(psi))
                {
                    if (p == null) { error = "Failed to start WiimoteGun.exe -presetup."; return false; }
                    p.WaitForExit(15000);
                }

                string settingsPath = Path.Combine(installDir, "settings.cfg");
                if (!File.Exists(settingsPath))
                {
                    error = L("settings.cfg was not generated.",
                        "settings.cfg n'a pas été généré.");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        // ========== Cleanup ==========
        public static void CleanupTemp()
        {
            try { if (!string.IsNullOrEmpty(_tempDir) && Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true); }
            catch { }
        }

        // ========== Private helpers ==========
        private static void CopyDirectory(string source, string target)
        {
            Directory.CreateDirectory(target);
            foreach (string file in Directory.GetFiles(source))
                File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
            foreach (string dir in Directory.GetDirectories(source))
                CopyDirectory(dir, Path.Combine(target, Path.GetFileName(dir)));
        }
    }
}
