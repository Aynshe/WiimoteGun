using System;
using System.Diagnostics;
using System.IO;

namespace WiimoteGun.Services
{
    /// <summary>
    /// EN: [3.1.5.10] Launches the native self-updater "update_W4G.exe" for Wiimote4Guns.
    ///     Replaces the PowerShell approach (WiimoteUpdate.ps1): the native exe has no
    ///     PowerShell dependency, no command-line quoting issues, and handles the
    ///     complete update flow: wait for app exit, download, extract (7za.exe), apply
    ///     with service staging, delete the legacy ps1, restart.
    ///     FR: [3.1.5.10] Démarre l'auto-updateur natif « update_W4G.exe » de Wiimote4Guns.
    ///     Remplace l'approche PowerShell (WiimoteUpdate.ps1) : l'exe natif n'a aucune
    ///     dépendance PowerShell, aucun problème de guillemets en ligne de commande, et
    ///     gère le flux complet : attente de la sortie de l'app, téléchargement,
    ///     extraction (7za.exe), application avec staging du service, suppression du ps1
    ///     obsolète, relance.
    /// </summary>
    public static class AppUpdateLauncher
    {
        /// <summary>
        /// EN: Launch update_W4G.exe and return true if it started. After a successful
        ///     launch the app must unregister from the service and exit.
        ///     FR: Démarre update_W4G.exe et retourne true s'il a démarré. Après un
        ///     lancement réussi, l'app doit se désenregistrer du service puis sortir.
        /// </summary>
        public static bool Launch(string downloadUrl, out string error)
        {
            error = null;
            try
            {
                string appDir = AppDomain.CurrentDomain.BaseDirectory;

                string updaterPath = Path.Combine(appDir, "update_W4G.exe");
                if (!File.Exists(updaterPath))
                {
                    error = "Native updater not found: " + updaterPath;
                    return false;
                }

                // EN/FR: Simple arguments — no PowerShell quoting issues: "<url>" <pid>
                //     The updater derives the app dir from its own location.
                string args = "\"" + downloadUrl + "\" " + Process.GetCurrentProcess().Id;

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = updaterPath,
                    Arguments = args,
                    // EN/FR: NO "runas" — the update runs with the current user account.
                    //     UseShellExecute=true → the console exe shows its own window.
                    UseShellExecute = true,
                    WorkingDirectory = appDir
                };

                Process.Start(psi);
                SimpleLogger.Instance.Info("[AppUpdate] update_W4G.exe launched (native updater) — the app will now exit for update.");
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                SimpleLogger.Instance.Error("[AppUpdate] Failed to launch update_W4G.exe: " + ex.Message);
                return false;
            }
        }
    }
}
