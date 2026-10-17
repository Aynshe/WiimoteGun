using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows.Forms;

namespace WiimoteGun.Core
{
    /// <summary>
    /// [V60-FIX-2] EN: Downloads and installs the .NET 10 runtime (x64) when the
    ///     SetupWizard's START is clicked and the runtime is missing. The flow:
    ///       1. Check if a usable .NET 10 runtime is already present (DotNetRuntimeChecker).
    ///       2. If not, show a bilingual prompt explaining it's required for the
    ///          UMDF2 driver (HmHost).
    ///       3. If the user agrees, download the latest .NET 10 runtime installer
    ///          from Microsoft's builds feed, run it elevated, and wait for completion.
    ///       4. If the download or installation fails (URL broken, network down),
    ///          open the official .NET 10 download page in the browser so the user
    ///          can download and install manually.
    ///       5. After the flow, verify the runtime is now installed.
    ///     FR: Télécharge et installe le runtime .NET 10 (x64) quand on clique START
    ///     dans le SetupWizard et que le runtime est absent. Le flux :
    ///       1. Vérifier si un runtime .NET 10 utilisable est déjà présent.
    ///       2. Sinon, afficher un prompt bilingue expliquant qu'il est requis pour
    ///          le pilote UMDF2 (HmHost).
    ///       3. Si l'utilisateur accepte, télécharger le dernier installateur du
    ///          runtime .NET 10 depuis le feed Microsoft, le lancer en élevé, et
    ///          attendre la fin.
    ///       4. Si le téléchargement ou l'installation échoue (URL HS, réseau down),
    ///          ouvrir la page officielle de téléchargement .NET 10 dans le navigateur
    ///          pour que l'utilisateur télécharge et installe manuellement.
    ///       5. Après le flux, vérifier que le runtime est maintenant installé.
    /// </summary>
    public static class Net10RuntimeInstaller
    {
        private const string VersionFileUrl = "https://builds.dotnet.microsoft.com/dotnet/Runtime/10.0/latest.version";
        private const string DownloadPageUrl = "https://dotnet.microsoft.com/download/dotnet/10.0";
        private const string BuildsFeedBase = "https://builds.dotnet.microsoft.com/dotnet/Runtime";

        /// <summary>
        /// EN: The complete install flow. Returns true when a usable .NET 10 runtime
        ///     is present on the machine after this call (was already installed, or
        ///     was successfully downloaded and installed). Returns false when the
        ///     user declined or the installation failed.
        ///     FR: Le flux d'installation complet. Renvoie true quand un runtime
        ///     .NET 10 utilisable est présent sur la machine après cet appel (déjà
        ///     installé, ou téléchargé et installé avec succès). Renvoie false quand
        ///     l'utilisateur a refusé ou que l'installation a échoué.
        /// </summary>
        public static bool EnsureRuntimeInstalled(IWin32Window owner)
        {
            // Already installed — nothing to do
            if (DotNetRuntimeChecker.IsNet10RuntimeInstalled())
            {
                SimpleLogger.Instance.Info("[V60] .NET 10 runtime already installed — no action needed.");
                return true;
            }

            // Prompt the user
            string prompt =
                "The UMDF2 virtual input driver requires the .NET 10 Runtime (free, from Microsoft).\n" +
                "It is not installed on this machine.\n\n" +
                "Install it now? (the download is about 30 MB)\n\n" +
                "--------------------------------\n\n" +
                "Le pilote d'entrée virtuelle UMDF2 nécessite le Runtime .NET 10 (gratuit, de Microsoft).\n" +
                "Il n'est pas installé sur cette machine.\n\n" +
                "L'installer maintenant ? (le téléchargement fait environ 30 Mo)";

            DialogResult choice = MessageBox.Show(owner, prompt,
                ".NET 10 Runtime — Wiimote4Guns Setup",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (choice != DialogResult.Yes)
            {
                SimpleLogger.Instance.Info("[V60] User declined the .NET 10 runtime install.");
                return false;
            }

            // [V60-FIX-2b] EN: Show a progress dialog while downloading + installing —
            //     the wizard's START button is locked during this (modal call), and the
            //     user needs to know WHAT is happening and WHEN it finishes (user report).
            //     FR: Afficher un dialogue de progression pendant le téléchargement +
            //     installation — le bouton START du wizard est verrouillé pendant cet
            //     appel modal, et l'utilisateur doit savoir CE QUI se passe et QUAND
            //     c'est terminé (rapport user).
            using (var progress = new Form())
            {
                progress.Text = ".NET 10 Runtime — Wiimote4Guns Setup";
                progress.Size = new System.Drawing.Size(480, 160);
                progress.StartPosition = FormStartPosition.CenterParent;
                progress.FormBorderStyle = FormBorderStyle.FixedDialog;
                progress.MaximizeBox = false;
                progress.MinimizeBox = false;
                progress.ControlBox = false; // No close button — the user can't skip
                progress.TopMost = true;

                var lblProgress = new System.Windows.Forms.Label
                {
                    Text = "Downloading .NET 10 Runtime (~30 MB)...\n" +
                           "Please wait — the installer will run next.\n\n" +
                           "Téléchargement du Runtime .NET 10 (~30 Mo)...\n" +
                           "Veuillez patienter — l'installateur va démarrer.",
                    Dock = DockStyle.Fill,
                    TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                    Font = new System.Drawing.Font("Segoe UI", 10f)
                };
                progress.Controls.Add(lblProgress);
                progress.Show(owner);

                bool result = DownloadAndInstall(owner);
                progress.Close();

                if (result)
                {
                    SimpleLogger.Instance.Info("[V60] .NET 10 runtime installed successfully.");
                    return true;
                }
            } // using progress

            // Download/install failed — open the download page for manual install
            SimpleLogger.Instance.Warning("[V60] Automatic .NET 10 runtime install failed — opening the download page.");
            MessageBox.Show(owner,
                "The automatic download failed.\n" +
                "The official download page will open in your browser.\n" +
                "Download '.NET Runtime 10.0.x — Windows — x64' and install it,\n" +
                "then restart Wiimote4Guns.\n\n" +
                "--------------------------------\n\n" +
                "Le téléchargement automatique a échoué.\n" +
                "La page officielle de téléchargement va s'ouvrir dans votre navigateur.\n" +
                "Téléchargez « .NET Runtime 10.0.x — Windows — x64 » et installez-le,\n" +
                "puis relancez Wiimote4Guns.",
                ".NET 10 Runtime — Wiimote4Guns Setup",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = DownloadPageUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error("[V60] Failed to open the download page: " + ex.Message);
            }

            return false;
        }

        /// <summary>
        /// EN: Download the latest .NET 10 runtime installer and run it elevated.
        ///     Returns true when the installer completed successfully AND the runtime
        ///     is now detected on the machine.
        ///     FR: Télécharge le dernier installateur du runtime .NET 10 et le lance
        ///     en élevé. Renvoie true quand l'installateur s'est terminé avec succès
        ///     ET que le runtime est maintenant détecté sur la machine.
        /// </summary>
        private static bool DownloadAndInstall(IWin32Window owner)
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                // [1] Get the latest version number from the version file
                //     Format: "# <commit_hash>\n# <version>" — last line is the version.
                string versionText;
                using (var wc = new WebClient())
                {
                    wc.Headers.Add("User-Agent", "Wiimote4Guns-Setup");
                    versionText = wc.DownloadString(VersionFileUrl).Trim();
                }

                // Parse: last non-empty line is the version (e.g. "10.0.12")
                string latestVersion = null;
                foreach (string line in versionText.Split('\n'))
                {
                    string trimmed = line.Trim();
                    if (!string.IsNullOrEmpty(trimmed)) latestVersion = trimmed;
                }

                if (string.IsNullOrEmpty(latestVersion))
                {
                    SimpleLogger.Instance.Warning("[V60] Could not parse the latest .NET 10 version from the version file.");
                    return false;
                }

                SimpleLogger.Instance.Info("[V60] Latest .NET 10 runtime version: " + latestVersion);

                // [2] Get the product version (may differ from the specific version)
                string productVersion = latestVersion;
                try
                {
                    string productVersionUrl = BuildsFeedBase + "/" + latestVersion + "/productVersion.txt";
                    using (var wc = new WebClient())
                    {
                        wc.Headers.Add("User-Agent", "Wiimote4Guns-Setup");
                        string pv = wc.DownloadString(productVersionUrl).Trim();
                        if (!string.IsNullOrEmpty(pv)) productVersion = pv;
                    }
                }
                catch
                {
                    // EN/FR: productVersion.txt not available — use the specific version
                }

                // [3] Construct the installer URL (.exe)
                string installerUrl = BuildsFeedBase + "/" + latestVersion +
                    "/dotnet-runtime-" + productVersion + "-win-x64.exe";

                SimpleLogger.Instance.Info("[V60] Downloading .NET 10 runtime installer: " + installerUrl);

                // [4] Download the installer to a temp file
                string tempInstaller = Path.Combine(Path.GetTempPath(),
                    "dotnet-runtime-" + latestVersion + "-win-x64.exe");
                using (var wc = new WebClient())
                {
                    wc.Headers.Add("User-Agent", "Wiimote4Guns-Setup");
                    wc.DownloadFile(installerUrl, tempInstaller);
                }

                if (!File.Exists(tempInstaller) || new FileInfo(tempInstaller).Length < 1000000)
                {
                    SimpleLogger.Instance.Warning("[V60] Downloaded installer is suspiciously small or missing.");
                    return false;
                }

                SimpleLogger.Instance.Info("[V60] Installer downloaded (" + new FileInfo(tempInstaller).Length + " bytes). Running...");

                // [5] Run the installer elevated (UAC prompt will appear)
                //     /install /quiet /norestart = silent install, no reboot prompt
                var psi = new ProcessStartInfo
                {
                    FileName = tempInstaller,
                    Arguments = "/install /quiet /norestart",
                    UseShellExecute = true, // Required for elevation
                    Verb = "runas"          // Trigger UAC
                };

                using (Process installer = Process.Start(psi))
                {
                    if (installer == null)
                    {
                        SimpleLogger.Instance.Warning("[V60] Failed to start the runtime installer.");
                        return false;
                    }

                    // Wait up to 5 minutes (the installer can be slow on old machines)
                    if (!installer.WaitForExit(300000))
                    {
                        SimpleLogger.Instance.Warning("[V60] Runtime installer timed out (5 min).");
                        try { installer.Kill(); } catch { }
                        return false;
                    }

                    if (installer.ExitCode != 0)
                    {
                        SimpleLogger.Instance.Warning("[V60] Runtime installer exited with code " + installer.ExitCode + " (0 = success, 3010 = success+reboot needed).");
                        // Exit code 3010 = success but reboot needed — treat as OK
                        if (installer.ExitCode != 0 && installer.ExitCode != 3010)
                            return false;
                    }
                }

                // [6] Verify the runtime is now installed
                if (DotNetRuntimeChecker.IsNet10RuntimeInstalled())
                {
                    SimpleLogger.Instance.Info("[V60] .NET 10 runtime verified after install.");
                    try { File.Delete(tempInstaller); } catch { }
                    return true;
                }

                SimpleLogger.Instance.Warning("[V60] Runtime installer completed but the runtime is not detected — a Windows restart may be needed.");
                return false;
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error("[V60] .NET 10 runtime install failed: " + ex.Message);
                return false;
            }
        }
    }
}
