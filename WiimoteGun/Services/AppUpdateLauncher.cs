using System;
using System.Diagnostics;
using System.IO;

namespace WiimoteGun.Services
{
    /// <summary>
    /// EN: [V56e] Launches the direct self-update process for the WiimoteGun app.
    /// The heavy lifting is done by "WiimoteUpdate.ps1" (shipped next to the app and
    /// updatable by future releases): it waits for the app to exit, downloads the
    /// release .7z, extracts it (the archive creates a folder named after itself —
    /// only its CONTENT is used), copies everything into the app directory EXCEPT the
    /// running service files (those are routed to "WiimoteGun.Service\update_service"
    /// so the existing admin self-update mechanism applies them), then restarts
    /// WiimoteGun with the CURRENT USER account (never admin).
    /// The script runs as the invoking user (no elevation) in a visible console so
    /// the user sees the progress.
    /// FR: [V56e] Démarre le processus de mise à jour directe de l'app WiimoteGun.
    /// Le gros du travail est fait par « WiimoteUpdate.ps1 » (livré à côté de l'app et
    /// modifiable par les releases futures) : il attend la sortie de l'app, télécharge
    /// le .7z de la release, l'extrait (l'archive crée un dossier à son nom — seul son
    /// CONTENU est utilisé), copie tout dans le dossier de l'app SAUF les fichiers du
    /// service actif (routés vers « WiimoteGun.Service\update_service » afin que le
    /// mécanisme d'auto-update admin existant les applique), puis relance WiimoteGun
    /// avec le COMPTE UTILISATEUR en cours (jamais admin).
    /// Le script s'exécute sous l'utilisateur appelant (sans élévation) dans une
    /// console visible pour que l'utilisateur suive la progression.
    /// </summary>
    public static class AppUpdateLauncher
    {
        /// <summary>
        /// EN: Launch the update script and return true if it started. After a
        /// successful launch the app must unregister from the service (so the
        /// CrashWatchdog does not "restart" it mid-update) and exit.
        /// FR: Démarre le script de mise à jour et retourne true s'il a démarré.
        /// Après un lancement réussi, l'app doit se désenregistrer auprès du service
        /// (pour que le CrashWatchdog ne la « relance » pas en pleine mise à jour)
        /// puis sortir.
        /// </summary>
        public static bool Launch(string downloadUrl, out string error)
        {
            error = null;
            try
            {
                string appDir = AppDomain.CurrentDomain.BaseDirectory;

                // [V56h] Windows command-line quoting: BaseDirectory ends with a trailing
                // backslash — "C:\path\" makes the closing quote ESCAPED (\"), so the
                // whole "-WaitPid 12345" gets swallowed into the -AppDir value (the exact
                // bug seen in the first live update test). Trim it (app is never at a
                // drive root) AND double any trailing backslashes when quoting.
                // (EN/FR: Quotation en ligne de commande Windows : BaseDirectory finit
                // par un antislash — « C:\chemin\ » rend le guillemet final ÉCHAPPÉ (\"),
                // si bien que tout le « -WaitPid 12345 » est absorbé dans la valeur
                // -AppDir (le bug exact du premier test réel). On le retire (l'app n'est
                // jamais à la racine d'un lecteur) ET on double les antislashes finaux
                // lors de la mise entre guillemets.)
                if (appDir.Length > 3) appDir = appDir.TrimEnd('\\');

                string scriptPath = Path.Combine(appDir, "WiimoteUpdate.ps1");

                if (!File.Exists(scriptPath))
                {
                    error = "Update script not found: " + scriptPath;
                    return false;
                }

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = string.Format(
                        "-NoProfile -ExecutionPolicy Bypass -File {0} -DownloadUrl {1} -AppDir {2} -WaitPid {3}",
                        QuoteArg(scriptPath), QuoteArg(downloadUrl), QuoteArg(appDir), Process.GetCurrentProcess().Id),
                    // EN/FR: NO "runas" — the update runs with the current user account
                    UseShellExecute = true,   // New console window with progress
                    WorkingDirectory = appDir
                };

                Process.Start(psi);
                SimpleLogger.Instance.Info("[AppUpdate] WiimoteUpdate.ps1 launched — the app will now exit for update.");
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                SimpleLogger.Instance.Error("[AppUpdate] Failed to launch update script: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// EN: [V56h] Quote a command-line argument. Doubles any TRAILING backslashes
        /// before the closing quote: a single trailing backslash would escape the quote
        /// in Windows/PowerShell argument parsing and swallow the following arguments.
        /// FR: [V56h] Met un argument de ligne de commande entre guillemets. Double les
        /// antislashes FINAUX avant le guillemet fermant : un antislash final unique
        /// échapperait le guillemet dans le parsing Windows/PowerShell et absorberait
        /// les arguments suivants.
        /// </summary>
        private static string QuoteArg(string value)
        {
            if (string.IsNullOrEmpty(value)) return "\"\"";

            int end = value.Length;
            while (end > 0 && value[end - 1] == '\\') end--;
            int trailingBackslashes = value.Length - end;

            string core = trailingBackslashes > 0
                ? value.Substring(0, end) + new string('\\', trailingBackslashes * 2)
                : value;
            return "\"" + core + "\"";
        }
    }
}
