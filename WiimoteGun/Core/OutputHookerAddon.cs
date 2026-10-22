using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;

namespace WiimoteGun.Core
{
    /// <summary>
    /// EN: [V62-OutputTab] Manages the bundled OutputHooker addon: detects the installed version,
    ///     checks the latest GitHub release (asset "OutputHooker_v*.zip"), downloads + extracts its
    ///     CONTENT into &lt;app&gt;/addons/OutputHooker (without overwriting the user's OutputHooker.ini),
    ///     kills a running OutputHooker.exe before replacing it, edits the [OutputHooker] ini options,
    ///     and can auto-start/stop it with Wiimote connections.
    /// FR: [V62-OutputTab] Gère l'addon OutputHooker embarque : version installee, derniere release
    ///     GitHub (asset "OutputHooker_v*.zip"), telechargement + extraction du CONTENU vers
    ///     &lt;app&gt;/addons/OutputHooker (sans ecraser OutputHooker.ini), fermeture de OutputHooker.exe
    ///     avant remplacement, edition des options [OutputHooker], et demarrage/arret auto selon les Wiimotes.
    /// </summary>
    public static class OutputHookerAddon
    {
        private const string PROCESS_NAME = "OutputHooker";
        private const string RELEASES_API = "https://api.github.com/repos/PolybiusExtreme/OutputHooker/releases";

        public static string AddonsRoot { get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "addons"); } }
        public static string HookerDir { get { return Path.Combine(AddonsRoot, "OutputHooker"); } }
        public static string HookerExe { get { return Path.Combine(HookerDir, "OutputHooker.exe"); } }
        public static string HookerIni { get { return Path.Combine(HookerDir, "OutputHooker.ini"); } }
        public static string SevenZipPath { get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "7za.exe"); } }

        private static Process _started; // process launched by us

        public static bool IsInstalled { get { return File.Exists(HookerExe); } }

        /// <summary>EN: installed version (FileVersion of OutputHooker.exe), or null. FR: version installee.</summary>
        public static string InstalledVersion()
        {
            try { if (File.Exists(HookerExe)) { var vi = FileVersionInfo.GetVersionInfo(HookerExe); return (vi == null || string.IsNullOrEmpty(vi.FileVersion)) ? null : vi.FileVersion; } }
            catch { }
            return null;
        }

        /// <summary>
        /// EN: [V62-Output] numeric version comparison - the installed FileVersion (e.g. "1.7.0.0")
        /// and the GitHub asset version (e.g. "1.7.0") do NOT have the same number of components, so
        /// a string comparison always fails and wrongly triggers a reinstall.
        /// FR: [V62-Output] comparaison numérique des versions - la version installée (ex "1.7.0.0")
        /// et la version de l'asset GitHub (ex "1.7.0") n'ont pas le même nombre de composantes, donc
        /// une comparaison de chaînes échoue toujours et provoque une réinstallation à tort.
        /// </summary>
        public static bool IsUpToDate(string installed, string latest)
        {
            if (string.IsNullOrEmpty(installed) || string.IsNullOrEmpty(latest)) return false;
            try { return new Version(installed) >= new Version(latest); }
            catch { return string.Equals(installed, latest, StringComparison.OrdinalIgnoreCase); }
        }

        /// <summary>
        /// EN: [V62-Output] page of the latest GitHub release. Falls back to the releases list
        /// when the version is unknown. FR: page de la dernière release GitHub - repli sur la liste
        /// des releases quand la version est inconnue.
        /// </summary>
        public static string LatestReleaseUrl(string version)
        {
            if (!string.IsNullOrEmpty(version))
                return "https://github.com/PolybiusExtreme/OutputHooker/releases/tag/v" + version;
            return "https://github.com/PolybiusExtreme/OutputHooker/releases";
        }

        public static bool IsRunning()
        {
            try { return Process.GetProcessesByName(PROCESS_NAME).Length > 0; } catch { return false; }
        }

        public static void KillRunning()
        {
            try
            {
                foreach (var p in Process.GetProcessesByName(PROCESS_NAME))
                {
                    try { p.Kill(); p.WaitForExit(3000); } catch { }
                }
            }
            catch { }
        }

        /// <summary>EN: latest GitHub release asset "OutputHooker_v&lt;digits&gt;.zip" -> (version, url). FR: derniere release GitHub.</summary>
        public static bool TryGetLatest(out string version, out string url)
        {
            version = null; url = null;
            try
            {
                using (var wc = new WebClient())
                {
                    wc.Headers.Add("User-Agent", "Wiimote4Guns");
                    string json = wc.DownloadString(RELEASES_API);
                    // find the first asset name OutputHooker_vX.zip, then the next browser_download_url after it
                    int idx = json.IndexOf("OutputHooker_v", StringComparison.OrdinalIgnoreCase);
                    if (idx < 0) return false;
                    int z = json.IndexOf(".zip", idx, StringComparison.OrdinalIgnoreCase);
                    if (z < 0) return false;
                    version = json.Substring(idx + "OutputHooker_v".Length, z - (idx + "OutputHooker_v".Length)).Trim();
                    int u = json.IndexOf("browser_download_url", z, StringComparison.OrdinalIgnoreCase);
                    if (u < 0) return false;
                    int q1 = json.IndexOf('"', json.IndexOf(':', u) + 1);
                    int q2 = json.IndexOf('"', q1 + 1);
                    if (q1 < 0 || q2 < 0) return false;
                    url = json.Substring(q1 + 1, q2 - q1 - 1);
                    return !string.IsNullOrEmpty(url);
                }
            }
            catch { return false; }
        }

        /// <summary>EN: download the given url + extract its CONTENT into HookerDir (keeps the ini). FR: telecharge + extrait le contenu.</summary>
        public static bool DownloadAndInstall(string url, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(url)) { error = "no url"; return false; }
            string tmpZip = null, tmpDir = null;
            try
            {
                Directory.CreateDirectory(AddonsRoot);
                tmpZip = Path.Combine(Path.GetTempPath(), "OutputHooker_" + Guid.NewGuid().ToString("N") + ".zip");
                using (var wc = new WebClient()) { wc.Headers.Add("User-Agent", "Wiimote4Guns"); wc.DownloadFile(url, tmpZip); }

                KillRunning(); // close a running instance before replacing files

                tmpDir = Path.Combine(Path.GetTempPath(), "OutputHooker_x_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tmpDir);
                if (!Extract(tmpZip, tmpDir, out error)) return false;

                // the archive contains a top-level "OutputHooker" folder -> copy its CONTENT
                string src = Path.Combine(tmpDir, "OutputHooker");
                if (!Directory.Exists(src)) src = tmpDir; // fallback: content at the archive root
                CopyTreeKeepIni(src, HookerDir, src);
                return true;
            }
            catch (Exception ex) { error = ex.Message; return false; }
            finally
            {
                try { if (tmpZip != null && File.Exists(tmpZip)) File.Delete(tmpZip); } catch { }
                try { if (tmpDir != null && Directory.Exists(tmpDir)) Directory.Delete(tmpDir, true); } catch { }
            }
        }

        private static bool Extract(string zip, string outDir, out string error)
        {
            error = null;
            string seven = SevenZipPath;
            if (!File.Exists(seven)) { error = "7za.exe not found"; return false; }
            try
            {
                var psi = new ProcessStartInfo(seven, "x -y -o\"" + outDir + "\" \"" + zip + "\"")
                { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true };
                var p = Process.Start(psi); if (p != null) { p.WaitForExit(120000); }
                return true;
            }
            catch (Exception ex) { error = ex.Message; return false; }
        }

        // EN/FR: recursive copy. NEVER overwrite an EXISTING top-level OutputHooker.ini OR any existing
        // file under the "ini\" sub-tree (the user's default.ini and per-game scripts live there).
        private static void CopyTreeKeepIni(string src, string dst, string root)
        {
            Directory.CreateDirectory(dst);
            foreach (var f in Directory.GetFiles(src))
            {
                string target = Path.Combine(dst, Path.GetFileName(f));
                string rel = src.Length > root.Length ? src.Substring(root.Length).TrimStart('\\', '/').Replace('\\', '/') : "";
                bool underIni = rel.StartsWith("ini/", StringComparison.OrdinalIgnoreCase);
                bool isTopIni = rel.Length == 0 && string.Equals(Path.GetFileName(f), "OutputHooker.ini", StringComparison.OrdinalIgnoreCase);
                if ((underIni || isTopIni) && File.Exists(target)) continue; // keep the user's ini / game scripts
                try { File.Copy(f, target, true); } catch { }
            }
            foreach (var d in Directory.GetDirectories(src))
            {
                // EN/FR: NEVER touch the "ini" sub-tree (default.ini + per-game scripts) - it holds
                // the user's OutputHooker game mappings, which the archive would overwrite.
                if (Path.GetFileName(d).Equals("ini", StringComparison.OrdinalIgnoreCase))
                {
                    // create the folder if missing so a fresh install still has the ini structure
                    try { Directory.CreateDirectory(Path.Combine(dst, "ini")); } catch { }
                    continue;
                }
                CopyTreeKeepIni(d, Path.Combine(dst, Path.GetFileName(d)), root);
            }
        }

        // ---- ini [OutputHooker] options (only these 3 keys) ----
        public static void ReadIniOptions(out bool notifOnNew, out bool addToDefault, out bool startMinimized)
        {
            notifOnNew = true; addToDefault = true; startMinimized = true; // defaults
            try
            {
                if (!File.Exists(HookerIni)) return;
                foreach (var raw in File.ReadAllLines(HookerIni))
                {
                    string line = (raw ?? "").Trim();
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string k = line.Substring(0, eq).Trim();
                    string v = line.Substring(eq + 1).Trim();
                    bool b; bool.TryParse(v, out b);
                    switch (k)
                    {
                        case "NotificationOnNewOutputs": notifOnNew = b; break;
                        case "AddNewOutputsToDefaultINI": addToDefault = b; break;
                        case "StartMinimized": startMinimized = b; break;
                    }
                }
            }
            catch { }
        }

        public static void WriteIniOptions(bool notifOnNew, bool addToDefault, bool startMinimized)
        {
            try
            {
                Directory.CreateDirectory(HookerDir);
                if (!File.Exists(HookerIni)) { File.WriteAllText(HookerIni, "[OutputHooker]\r\n"); }
                var lines = new System.Collections.Generic.List<string>(File.ReadAllLines(HookerIni));
                SetOrAdd(lines, "NotificationOnNewOutputs", notifOnNew);
                SetOrAdd(lines, "AddNewOutputsToDefaultINI", addToDefault);
                SetOrAdd(lines, "StartMinimized", startMinimized);
                File.WriteAllLines(HookerIni, lines.ToArray());
            }
            catch { }
        }

        private static void SetOrAdd(System.Collections.Generic.List<string> lines, string key, bool value)
        {
            string newLine = key + "=" + (value ? "true" : "false");
            for (int i = 0; i < lines.Count; i++)
            {
                string t = (lines[i] ?? "").Trim();
                int eq = t.IndexOf('=');
                if (eq > 0 && t.Substring(0, eq).Trim().Equals(key, StringComparison.OrdinalIgnoreCase)) { lines[i] = newLine; return; }
            }
            if (lines.Count == 0 || !lines[0].Trim().StartsWith("[")) lines.Insert(0, "[OutputHooker]");
            lines.Add(newLine);
        }

        // ---- auto start / stop ----
        public static void Start()
        {
            try
            {
                if (!IsInstalled || IsRunning()) return;
                var p = Process.Start(HookerExe);
                _started = p;
            }
            catch { }
        }

        public static void Stop()
        {
            try
            {
                if (!IsRunning()) return;
                KillRunning();
                _started = null;
            }
            catch { }
        }
    }
}
