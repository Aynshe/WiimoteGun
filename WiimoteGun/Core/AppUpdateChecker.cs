using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;

namespace WiimoteGun.Core
{
    /// <summary>
    /// EN: [V56e] Checks GitHub for a newer Wiimote4Guns release.
    /// Source: https://github.com/Aynshe/WiimoteGun/releases — the release ASSET whose
    /// name starts with "Wiimote4Guns_" carries the version right after the "v"
    /// (e.g. "Wiimote4Guns_beta_v0.0.0.0" -> 0.0.0.0,
    ///      "Wiimote4Guns_beta_pre-release_v2.3.5.1.7z" -> 2.3.5.1).
    /// If that version is HIGHER than the running assembly version, an update is
    /// available. The check runs ONCE per session on a background thread; the result
    /// is cached and consumed by the startup tile notification and the Home indicator.
    /// FR: [V56e] Vérifie sur GitHub la disponibilité d'une release Wiimote4Guns plus
    /// récente. Source : https://github.com/Aynshe/WiimoteGun/releases — l'ASSET de
    /// release dont le nom commence par « Wiimote4Guns_ » porte la version juste après
    /// le « v » (ex. « Wiimote4Guns_beta_v0.0.0.0 » -> 0.0.0.0,
    /// « Wiimote4Guns_beta_pre-release_v2.3.5.1.7z » -> 2.3.5.1). Si cette version est
    /// SUPÉRIEURE à la version en cours d'exécution, une mise à jour est disponible.
    /// La vérification s'exécute UNE FOIS par session sur un thread d'arrière-plan ;
    /// le résultat est mis en cache et consommé par la notification tuile au démarrage
    /// et par le voyant de la page Home.
    /// </summary>
    public static class AppUpdateChecker
    {
        private const string ReleasesApiUrl = "https://api.github.com/repos/Aynshe/WiimoteGun/releases";
        private const string ReleasesPageUrl = "https://github.com/Aynshe/WiimoteGun/releases";
        private const string AssetPrefix = "Wiimote4Guns_";
        private const int HttpTimeoutMs = 10000;

        private static readonly object _lock = new object();
        private static bool _checkStarted;             // Once-per-session guard (EN/FR: Garde une fois par session)
        private static bool _checkCompleted;           // The check FINISHED (with a result or an error)
        private static bool _updateAvailable;
        private static bool _offline;                  // [V56f] The check FAILED (no internet / DNS / timeout)
        private static string _latestVersion;
        private static string _downloadUrl;
        private static string _releaseUrl = ReleasesPageUrl;
        private static string _error;

        /// <summary>EN: True once the check has FINISHED this session (result or error). FR: True dès que la vérification est TERMINÉE cette session (résultat ou erreur).</summary>
        public static bool HasChecked { get { lock (_lock) { return _checkCompleted; } } }

        /// <summary>EN: True if a newer release is available. FR: True si une release plus récente est disponible.</summary>
        public static bool UpdateAvailable { get { lock (_lock) { return _updateAvailable; } } }

        /// <summary>
        /// EN: [V56f] True when the check finished but could not reach GitHub (no internet,
        /// DNS failure, timeout, proxy error). The Home indicator then shows a distinct
        /// ORANGE "Offline" state instead of a misleading green "Up to date".
        /// FR: [V56f] True quand la vérification s'est terminée sans pouvoir joindre
        /// GitHub (pas d'internet, échec DNS, timeout, erreur proxy). Le voyant Home
        /// affiche alors un état « Offline » ORANGE distinct au lieu d'un « Up to date »
        /// vert trompeur.
        /// </summary>
        public static bool Offline { get { lock (_lock) { return _offline; } } }

        /// <summary>EN: The latest version string parsed from the asset name (e.g. "2.3.5.1"). FR: La chaîne de version la plus récente parsée depuis le nom de l'asset (ex. « 2.3.5.1 »).</summary>
        public static string LatestVersion { get { lock (_lock) { return _latestVersion; } } }

        /// <summary>EN: Direct download URL of the release asset (.7z). FR: URL de téléchargement direct de l'asset de release (.7z).</summary>
        public static string DownloadUrl { get { lock (_lock) { return _downloadUrl; } } }

        /// <summary>EN: GitHub releases page URL. FR: URL de la page des releases GitHub.</summary>
        public static string ReleaseUrl { get { lock (_lock) { return _releaseUrl; } } }

        /// <summary>EN: Last check error message, if any (diagnostics only). FR: Dernier message d'erreur de vérification, le cas échéant (diagnostic).</summary>
        public static string LastError { get { lock (_lock) { return _error; } } }

        /// <summary>
        /// EN: Start the update check once per session on a background thread.
        /// NEVER blocks the application: the HTTP requests carry their own timeouts
        /// (10s) and everything runs on an IsBackground thread. On connection failure
        /// the check simply completes with the Offline state.
        /// FR: Démarre la vérification de mise à jour une fois par session sur un
        /// thread d'arrière-plan. Ne bloque JAMAIS l'application : les requêtes HTTP
        /// portent leurs propres timeouts (10s) et tout s'exécute sur un thread
        /// IsBackground. En cas d'échec de connexion, la vérification se termine
        /// simplement dans l'état Offline.
        /// </summary>
        public static void BeginCheck()
        {
            lock (_lock)
            {
                if (_checkStarted) return;
                _checkStarted = true; // Once-per-session guard, even on failure
            }

            Thread t = new Thread(CheckWorker);
            t.IsBackground = true;
            t.Start();
        }

        private static void CheckWorker()
        {
            try
            {
                string json = DownloadString(ReleasesApiUrl);

                if (string.IsNullOrEmpty(json))
                {
                    lock (_lock) { _error = "GitHub API unreachable"; _offline = true; }
                    SimpleLogger.Instance.Warning("[AppUpdate] GitHub API unreachable (offline?) — check skipped.");
                    return;
                }

                WiimoteGun.Core.JsonValue root = JsonLite.Parse(json);
                if (root == null || root.Type != JsonValue.JsonType.Array || root.Items == null)
                {
                    lock (_lock) { _error = "Invalid JSON from GitHub API"; _offline = true; }
                    SimpleLogger.Instance.Warning("[AppUpdate] Invalid JSON from GitHub API.");
                    return;
                }

                // EN/FR: NOTE: /releases/latest returns 404 when all releases are
                // PRE-RELEASES — the /releases list is used instead (GitHub returns
                // them newest first). Take the first release carrying a valid asset.
                foreach (JsonValue release in root.Items)
                {
                    JsonValue draftVal = release.Get("draft");
                    if (draftVal != null && draftVal.Type == JsonValue.JsonType.Boolean && draftVal.Bool) continue; // Drafts excluded
                    JsonValue preVal = release.Get("prerelease");
                    bool prerelease = preVal != null && preVal.Type == JsonValue.JsonType.Boolean && preVal.Bool;

                    // EN/FR: Find the asset whose name starts with "Wiimote4Guns_"
                    string assetName = null, assetUrl = null;
                    JsonValue assets = release.Get("assets");
                    if (assets != null && assets.Type == JsonValue.JsonType.Array && assets.Items != null)
                    {
                        foreach (JsonValue asset in assets.Items)
                        {
                            string name = asset.GetString("name", "");
                            if (!name.StartsWith(AssetPrefix, StringComparison.OrdinalIgnoreCase)) continue;
                            // EN/FR: Only archive assets matter (skip .txt / .sha256 side files)
                            if (!name.EndsWith(".7z", StringComparison.OrdinalIgnoreCase) &&
                                !name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) continue;

                            assetName = name;
                            assetUrl = asset.GetString("browser_download_url", "");
                            break;
                        }
                    }

                    if (string.IsNullOrEmpty(assetName) || string.IsNullOrEmpty(assetUrl))
                    {
                        // EN/FR: This release has no valid asset (e.g. "Update_for_..." only) — try the next one
                        continue;
                    }

                    string htmlUrl = release.GetString("html_url", ReleasesPageUrl);

                    if (!TryParseVersionFromAssetName(assetName, out string latestStr))
                    {
                        lock (_lock)
                        {
                            _updateAvailable = false;
                            _releaseUrl = htmlUrl;
                            _error = "Could not parse version from asset name: " + assetName;
                        }
                        SimpleLogger.Instance.Warning("[AppUpdate] Could not parse version from asset name: " + assetName);
                        return;
                    }

                    Version latest = ParseVersion(latestStr);
                    Version current = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

                    bool update = latest > current;

                    lock (_lock)
                    {
                        _updateAvailable = update;
                        _latestVersion = latestStr;
                        _downloadUrl = assetUrl;
                        _releaseUrl = htmlUrl;
                    }

                    SimpleLogger.Instance.Info(string.Format("[AppUpdate] Check done: running v{0}, latest asset v{1} (prerelease={2}) -> {3}",
                        current, latestStr, prerelease, update ? "UPDATE AVAILABLE" : "up to date"));
                    return;
                }

                lock (_lock)
                {
                    _updateAvailable = false;
                    _releaseUrl = ReleasesPageUrl;
                    _error = "No Wiimote4Guns_ asset found in any release";
                }
                SimpleLogger.Instance.Info("[AppUpdate] No Wiimote4Guns_ asset found in any release.");
            }
            catch (System.Net.WebException)
            {
                // [V56f] No internet / DNS / timeout / proxy: OFFLINE state, never a fake "up to date"
                // (EN/FR: Pas d'internet / DNS / timeout / proxy : état OFFLINE, jamais un faux « up to date »)
                lock (_lock) { _error = "No internet connection"; _offline = true; }
                SimpleLogger.Instance.Warning("[AppUpdate] No internet connection — update check unavailable (app NOT blocked).");
            }
            catch (Exception ex)
            {
                lock (_lock) { _error = ex.Message; _offline = true; }
                SimpleLogger.Instance.Warning("[AppUpdate] Check failed: " + ex.Message);
            }
            finally
            {
                // [V56f] Mark the check COMPLETED on every exit path — HasChecked then
                // reflects a FINISHED check (result or error), not a started one.
                // (EN/FR: Marquer la vérification TERMINÉE sur tout chemin de sortie —
                // HasChecked reflète alors une vérification FINIE (résultat ou erreur),
                // pas une vérification démarrée.)
                lock (_lock) { _checkCompleted = true; }
            }
        }

        /// <summary>
        /// EN: Extract the version from an asset name: the digits after the LAST "v"
        /// of the "Wiimote4Guns_..." prefix part (e.g. "Wiimote4Guns_beta_v0.0.0.0"
        /// -> "0.0.0.0", "Wiimote4Guns_beta_pre-release_v2.3.5.1.7z" -> "2.3.5.1").
        /// The archive extension is stripped FIRST (".7z" would otherwise merge into
        /// the version: "2.3.5.3.7") and the result is capped at 4 components
        /// (System.Version accepts at most major.minor.build.revision).
        /// FR: Extrait la version d'un nom d'asset : les chiffres après le DERNIER « v »
        /// de la partie préfixe « Wiimote4Guns_... » (ex.
        /// « Wiimote4Guns_beta_v0.0.0.0 » -> « 0.0.0.0 »,
        /// « Wiimote4Guns_beta_pre-release_v2.3.5.1.7z » -> « 2.3.5.1 »).
        /// L'extension d'archive est retirée AVANT (sinon « .7z » fusionne avec la
        /// version : « 2.3.5.3.7 ») et le résultat est plafonné à 4 composantes
        /// (System.Version accepte au plus major.minor.build.revision).
        /// </summary>
        public static bool TryParseVersionFromAssetName(string assetName, out string version)
        {
            version = null;
            if (string.IsNullOrEmpty(assetName)) return false;

            // EN/FR: Strip the archive extension first: ".7z" would otherwise be parsed
            // as an extra version component (".7" matches "\.\d+")
            string clean = Regex.Replace(assetName, @"\.(7z|zip)$", "", RegexOptions.IgnoreCase);

            MatchCollection matches = Regex.Matches(clean, @"_?v(\d+(?:\.\d+)*)", RegexOptions.IgnoreCase);
            if (matches.Count == 0) return false;

            // EN/FR: The LAST "v<number>" is the release version
            string raw = matches[matches.Count - 1].Groups[1].Value;

            // EN/FR: Cap at 4 components (System.Version limit)
            string[] parts = raw.Split('.');
            if (parts.Length > 4)
            {
                raw = string.Join(".", parts, 0, 4);
            }

            version = raw;
            return true;
        }

        private static Version ParseVersion(string s)
        {
            try { return new Version(s); }
            catch { return new Version(0, 0, 0, 0); }
        }

        private static string DownloadString(string url)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "GET";
            req.Timeout = HttpTimeoutMs;
            req.ReadWriteTimeout = HttpTimeoutMs;
            req.UserAgent = "Wiimote4Guns-Updater"; // GitHub requires a User-Agent
            req.Accept = "application/vnd.github+json";
            req.Proxy = WebRequest.GetSystemWebProxy();
            req.Proxy.Credentials = CredentialCache.DefaultCredentials;

            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            using (Stream stream = resp.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream, System.Text.Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
