using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace W4G_Setup
{
    /// <summary>
    /// [V61] EN: Setup wizard — Designer-compatible partial class.
    ///     Shows a system check zone (archive, RetroBat, runtime, installed version)
    ///     and pre-fills the install path with <RetroBat>\plugins\Wiimote4Guns when
    ///     detected. The path field IS the install directory (no plugins subfolder
    ///     computation). Bilingual FR/EN.
    ///     FR: Assistant d'installation — classe partielle compatible Designer.
    ///     Affiche une zone de checks système (archive, RetroBat, runtime, version
    ///     installée) et pré-remplit le chemin avec <RetroBat>\plugins\Wiimote4Guns
    ///     quand détecté. Le champ chemin EST le dossier d'installation (pas de
    ///     calcul de sous-dossier plugins). Bilingue FR/EN.
    /// </summary>
    internal partial class SetupForm : Form
    {
        public bool SetupCompleted { get; private set; }

        // [V61-SETUP-2] Cached latest GitHub release version (fetched ONCE, async, at opening)
        // (EN/FR: Version de la dernière release GitHub en cache — fetchée UNE FOIS, async,
        // à l'ouverture)
        private string _onlineVersion;
        private bool _onlineChecked;

        public SetupForm()
        {
            InitializeComponent();
            ApplyLanguage();
            WireEvents();
            InitializePathAndChecks();
        }

        // ========== Language ==========
        private void ApplyLanguage()
        {
            this.Text = "Wiimote4Guns Setup" + (Program.IsFrench ? " — Installation" : "");
            lblSubtitle.Text = Program.L(
                "RetroBat Plugin Setup Wizard",
                "Assistant d'installation du plugin RetroBat");
            lblPath.Text = Program.L("Installation folder:", "Dossier d'installation :");
            btnBrowse.Text = Program.L("Browse...", "Parcourir...");
            lblCheckTitle.Text = Program.L("System Check", "Vérification Système");
            lblLayout.Text = Program.L("LED Layout:", "Layout LED :");
            chkHybrid.Text = Program.L("Enable Hybrid PnP (alpha test)", "Activer Hybrid PnP (test alpha)");
            lblHybridInfo.Text = Program.L(
                "Adds distance and tilt compensation to the Wiimote Bar tracking.\n" +
                "Requires playing at the same distance where you calibrated.\n" +
                "Can be disabled later, or combined with edge extrapolation\n" +
                "(reduces the minimum distance at the cost of less precision\n" +
                "at screen edges).",
                "Ajoute la compensation de distance et d'inclinaison au tracking\n" +
                "de la barre Wiimote. Nécessite de jouer à la même distance que\n" +
                "la calibration. Peut être désactivé plus tard, ou couplé à\n" +
                "l'extrapolation de bord (réduit la distance minimale au prix\n" +
                "d'une précision moindre sur les côtés de l'écran.");
            btnInstall.Text = Program.L("Install", "Installer");
            btnFinish.Text = Program.L("Finish", "Terminer");
            lnkGitHub.Text = Program.L("GitHub: Wiimote4Gun", "GitHub : Wiimote4Gun");
            lnkRetroBat.Text = "www.retrobat.org";
        }

        // ========== Events ==========
        private void WireEvents()
        {
            btnBrowse.Click += (s, e) =>
            {
                using (var fbd = new FolderBrowserDialog())
                {
                    fbd.Description = Program.L(
                        "Select the installation folder",
                        "Sélectionnez le dossier d'installation");
                    if (fbd.ShowDialog() == DialogResult.OK)
                        txtInstallPath.Text = fbd.SelectedPath;
                }
            };

            txtInstallPath.TextChanged += (s, e) => UpdateChecks();
            cmbLayout.SelectedIndexChanged += (s, e) => UpdateHybridAvailability();
            cmbLayout.SelectedIndex = 0;
            btnInstall.Click += (s, e) => RunSetup();
            btnFinish.Click += btnFinish_Click;

            // [V61-SETUP-2] EN: fetch the latest GitHub release version ONCE when the form
            //     is shown (handle guaranteed) — the System Check zone then displays it
            //     and the local-archive comparison uses the cached value instead of a
            //     blocking synchronous GitHub call at every path change.
            //     FR: récupère UNE FOIS la version de la dernière release GitHub à
            //     l'affichage (handle garanti) — la zone System Check l'affiche alors et
            //     la comparaison d'archive utilise la valeur en cache au lieu d'un appel
            //     GitHub synchrone bloquant à chaque changement de chemin.
            this.Shown += (s, e) => StartOnlineVersionFetch();

            // [V61] Clickable links — open the default browser
            lnkGitHub.LinkClicked += (s, e) => OpenUrl("https://github.com/Aynshe/WiimoteGun");
            lnkRetroBat.LinkClicked += (s, e) => OpenUrl("https://www.retrobat.org");
        }

        /// <summary>
        /// [V61] EN: Launches Wiimote4Guns.exe from the installation folder when the
        ///     user clicks Finish / Terminer. Skips the launch if another instance is
        ///     already running (single-instance app).
        ///     FR: Lance Wiimote4Guns.exe depuis le dossier d'installation au clic sur
        ///     Finish / Terminer. Ignore le lancement si une autre instance tourne déjà
        ///     (app single-instance).
        /// </summary>
        private void btnFinish_Click(object sender, EventArgs e)
        {
            SetupCompleted = true;

            string dir = txtInstallPath.Text.Trim();
            string exe = Path.Combine(dir, "WiimoteGun.exe");

            if (!File.Exists(exe))
            {
                Program.Log("Finish clicked but WiimoteGun.exe not found in: " + dir);
                Close();
                return;
            }

            if (IsAnotherWiimoteGunRunning())
            {
                Program.Log("Finish clicked — WiimoteGun.exe is already running, not launching a second instance.");
                Close();
                return;
            }

            LaunchWiimoteGun();
            Close();
        }

        private bool IsAnotherWiimoteGunRunning()
        {
            try
            {
                var procs = System.Diagnostics.Process.GetProcessesByName("WiimoteGun");
                return procs != null && procs.Length > 0;
            }
            catch { return false; }
        }

        private void LaunchWiimoteGun()
        {
            string dir = txtInstallPath.Text.Trim();
            string exe = Path.Combine(dir, "WiimoteGun.exe");
            try
            {
                Program.Log("Launching WiimoteGun.exe: " + exe);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = exe,
                    WorkingDirectory = dir,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Program.Log("Failed to launch WiimoteGun.exe: " + ex.Message);
            }
        }

        private void OpenUrl(string url)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Program.Log("Failed to open URL " + url + ": " + ex.Message);
            }
        }

        // ========== Path init + System Check ==========
        private void InitializePathAndChecks()
        {
            // [V61] Pre-fill with <RetroBat>\plugins\Wiimote4Guns if detected
            string retroBat, err;
            if (Program.FindRetroBatPath(out retroBat, out err))
            {
                txtInstallPath.Text = Path.Combine(retroBat, "plugins", "Wiimote4Guns");
                Program.Log("Pre-filled install path: " + txtInstallPath.Text);
            }
            UpdateChecks();
        }

        private void UpdateChecks()
        {
            string installPath = txtInstallPath.Text.Trim();

            // --- Archive locale ---
            string localArchive = Program.FindLocalArchive();
            string localVersion = localArchive != null ? Program.GetArchiveVersion(localArchive) : null;
            if (localVersion != null)
            {
                // [V61-SETUP-2] EN: the online version comes from the CACHED async fetch —
                //     UpdateChecks never blocks on the GitHub API anymore (the old
                //     synchronous FindLatestRelease froze the UI at startup and at every
                //     path change). Until the fetch completes, show the version alone.
                //     FR: la version en ligne vient du fetch ASYNCHRONE en cache —
                //     UpdateChecks ne bloque plus jamais sur l'API GitHub (l'ancien
                //     FindLatestRelease synchrone gelait l'UI au démarrage et à chaque
                //     changement de chemin). En attendant le fetch, version seule.
                if (_onlineChecked && _onlineVersion != null &&
                    Program.CompareVersions(localVersion, _onlineVersion) < 0)
                {
                    SetCheck(chkArchive, Program.L(
                        "📦 Local archive: v" + localVersion + " (newer v" + _onlineVersion + " available online)",
                        "📦 Archive locale : v" + localVersion + " (v" + _onlineVersion + " plus récente disponible en ligne)"),
                        Color.FromArgb(255, 180, 0));
                }
                else if (!_onlineChecked)
                {
                    SetCheck(chkArchive, Program.L(
                        "📦 Local archive: v" + localVersion,
                        "📦 Archive locale : v" + localVersion),
                        Color.FromArgb(160, 160, 170));
                }
                else
                {
                    SetCheck(chkArchive, Program.L(
                        "📦 Local archive: v" + localVersion + " (up to date)",
                        "📦 Archive locale : v" + localVersion + " (à jour)"),
                        Color.FromArgb(0, 200, 100));
                }

                // Warning if archive < 3.1.6.0 (layout/hybrid not supported)
                if (Program.CompareVersions(localVersion, "3.1.6.0") < 0)
                {
                    chkLayoutWarning.Visible = true;
                    chkLayoutWarning.Text = Program.L(
                        "⚠ Layout and Hybrid PnP selection will NOT be applied\n" +
                        "   (local archive is older than v3.1.6.0)",
                        "⚠ Le choix du layout et Hybrid PnP ne sera PAS appliqué\n" +
                        "   (l'archive locale est antérieure à v3.1.6.0)");
                }
                else
                {
                    chkLayoutWarning.Visible = false;
                }
            }
            else
            {
                SetCheck(chkArchive, Program.L(
                    "📦 Local archive: not found (will download from GitHub)",
                    "📦 Archive locale : non trouvée (téléchargement depuis GitHub)"),
                    Color.FromArgb(140, 140, 150));
                chkLayoutWarning.Visible = false;
            }

            // --- Latest GitHub release [V61-SETUP-2] ---
            if (!_onlineChecked)
            {
                SetCheck(chkOnline, Program.L(
                    "☁ Latest GitHub release: checking...",
                    "☁ Dernière release GitHub : vérification..."),
                    Color.FromArgb(140, 140, 150));
            }
            else if (!string.IsNullOrEmpty(_onlineVersion))
            {
                SetCheck(chkOnline, Program.L(
                    "☁ Latest GitHub release: v" + _onlineVersion + " available",
                    "☁ Dernière release GitHub : v" + _onlineVersion + " disponible"),
                    Color.FromArgb(0, 200, 100));
            }
            else
            {
                SetCheck(chkOnline, Program.L(
                    "☁ Latest GitHub release: unavailable (offline?)",
                    "☁ Dernière release GitHub : indisponible (hors ligne ?)"),
                    Color.FromArgb(255, 180, 0));
            }

            // --- RetroBat ---
            string retroBatPath, retroBatErr;
            if (Program.FindRetroBatPath(out retroBatPath, out retroBatErr))
            {
                SetCheck(chkRetroBat, Program.L(
                    "🎮 RetroBat: detected (" + retroBatPath + ")",
                    "🎮 RetroBat : détecté (" + retroBatPath + ")"),
                    Color.FromArgb(0, 200, 100));
            }
            else
            {
                SetCheck(chkRetroBat, Program.L(
                    "🎮 RetroBat: not detected (standalone installation)",
                    "🎮 RetroBat : non détecté (installation standalone)"),
                    Color.FromArgb(140, 140, 150));
            }

            // --- Runtime .NET 10 ---
            if (Program.IsNet10Installed())
            {
                SetCheck(chkRuntime, Program.L(
                    "⚡ .NET 10 Runtime: installed",
                    "⚡ Runtime .NET 10 : installé"),
                    Color.FromArgb(0, 200, 100));
            }
            else
            {
                SetCheck(chkRuntime, Program.L(
                    "⚡ .NET 10 Runtime: NOT installed (will be proposed)",
                    "⚡ Runtime .NET 10 : NON installé (sera proposé)"),
                    Color.FromArgb(255, 140, 0));
            }

            // --- Wiimote4Guns installed version ---
            if (!string.IsNullOrEmpty(installPath) && Directory.Exists(installPath))
            {
                string installedVersion = Program.GetInstalledVersion(installPath);
                if (installedVersion != null)
                {
                    SetCheck(chkInstalled, Program.L(
                        "🎯 Wiimote4Guns: v" + installedVersion + " installed",
                        "🎯 Wiimote4Guns : v" + installedVersion + " installé"),
                        Color.FromArgb(0, 180, 255));
                }
                else
                {
                    SetCheck(chkInstalled, Program.L(
                        "🎯 Wiimote4Guns: not installed (fresh install)",
                        "🎯 Wiimote4Guns : non installé (nouvelle installation)"),
                        Color.FromArgb(140, 140, 150));
                }
            }
            else
            {
                SetCheck(chkInstalled, Program.L(
                    "🎯 Wiimote4Guns: select a folder to detect",
                    "🎯 Wiimote4Guns : sélectionnez un dossier pour détecter"),
                    Color.FromArgb(140, 140, 150));
            }
        }

        private void SetCheck(Label label, string text, Color color)
        {
            label.Text = text;
            label.ForeColor = color;
        }

        /// <summary>
        /// [V61-SETUP-2] EN: Fetches the latest GitHub release version ONCE, in the
        ///     background (ThreadPool): the UI never freezes on the GitHub API. The
        ///     System Check line shows "checking..." then the version (green) or
        ///     "unavailable" (orange, offline). The cached value also feeds the local
        ///     archive comparison in UpdateChecks. RunSetup keeps its own fresh
        ///     synchronous check at install time.
        ///     FR: Récupère UNE FOIS la version de la dernière release GitHub en
        ///     arrière-plan (ThreadPool) : l'UI ne gèle jamais sur l'API GitHub. La
        ///     ligne de check affiche « vérification... » puis la version (vert) ou
        ///     « indisponible » (orange, hors ligne). La valeur en cache alimente aussi
        ///     la comparaison d'archive locale dans UpdateChecks. RunSetup garde son
        ///     propre check synchrone frais au moment de l'installation.
        /// </summary>
        private void StartOnlineVersionFetch()
        {
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                string version = null;
                try
                {
                    string url, err;
                    if (Program.FindLatestRelease(out url, out err))
                        version = Program.GetArchiveVersion(url);
                }
                catch { }
                Program.Log("Latest GitHub release: " + (string.IsNullOrEmpty(version) ? "unavailable" : "v" + version));

                try
                {
                    if (IsDisposed) return;
                    BeginInvoke((Action)(() =>
                    {
                        _onlineVersion = version;
                        _onlineChecked = true;
                        UpdateChecks();
                    }));
                }
                catch { }
            });
        }

        // ========== Hybrid PnP availability ==========
        private void UpdateHybridAvailability()
        {
            string layout = cmbLayout.SelectedItem?.ToString() ?? "Wiimote Bar";
            bool hybridAvailable =
                layout.StartsWith("Wiimote Bar", StringComparison.OrdinalIgnoreCase) ||
                layout.StartsWith("Two Wiimote", StringComparison.OrdinalIgnoreCase);

            chkHybrid.Enabled = hybridAvailable;
            if (!hybridAvailable)
            {
                chkHybrid.Checked = false;
                chkHybrid.ForeColor = Color.FromArgb(100, 100, 110);
                lblHybridInfo.ForeColor = Color.FromArgb(90, 90, 100);
            }
            else
            {
                chkHybrid.ForeColor = Color.White;
                lblHybridInfo.ForeColor = Color.FromArgb(160, 160, 170);
            }
        }

        // ========== Setup logic ==========
        private void RunSetup()
        {
            string installDir = txtInstallPath.Text.Trim();
            if (string.IsNullOrEmpty(installDir) || !Directory.Exists(installDir))
            {
                MessageBox.Show(this,
                    Program.L("Please select a valid installation folder.",
                        "Veuillez sélectionner un dossier d'installation valide."),
                    "Wiimote4Guns Setup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string installedVersion = Program.GetInstalledVersion(installDir);
            btnInstall.Enabled = false;
            progress.Visible = true;
            Program.Log("=== Setup started — installDir: " + installDir + " ===");

            // Determine source (local or online)
            string sourcePath, archiveVersion;
            string localArchive = Program.FindLocalArchive();
            string localVersion = localArchive != null ? Program.GetArchiveVersion(localArchive) : null;

            if (localArchive != null)
            {
                string onlineUrl, onlineErr;
                bool onlineFound = Program.FindLatestRelease(out onlineUrl, out onlineErr);
                string onlineVersion = onlineFound ? Program.GetArchiveVersion(onlineUrl) : null;

                if (onlineFound && onlineVersion != null &&
                    Program.CompareVersions(localVersion, onlineVersion) < 0)
                {
                    var choice = MessageBox.Show(this,
                        Program.L(
                            "A local archive v" + localVersion + " was found.\n" +
                            "A newer version v" + onlineVersion + " is available online.\n\n" +
                            "[Yes] = Download the latest\n[No] = Use the local archive (offline)",
                            "Une archive locale v" + localVersion + " a été trouvée.\n" +
                            "Une version plus récente v" + onlineVersion + " est disponible en ligne.\n\n" +
                            "[Oui] = Télécharger la dernière\n[Non] = Utiliser l'archive locale (hors ligne)"),
                        Program.L("Update Available", "Mise à jour disponible"),
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (choice == DialogResult.Yes)
                    {
                        sourcePath = onlineUrl;
                        archiveVersion = onlineVersion;
                    }
                    else
                    {
                        sourcePath = localArchive;
                        archiveVersion = localVersion;
                    }
                }
                else
                {
                    sourcePath = localArchive;
                    archiveVersion = localVersion;
                }
            }
            else
            {
                string downloadUrl, error;
                if (!Program.FindLatestRelease(out downloadUrl, out error))
                {
                    ShowError("Failed to find release: " + error);
                    return;
                }
                sourcePath = downloadUrl;
                archiveVersion = Program.GetArchiveVersion(downloadUrl);
            }

            // Version comparison
            if (installedVersion != null && archiveVersion != null)
            {
                int cmp = Program.CompareVersions(installedVersion, archiveVersion);
                Program.Log("Comparison: installed=" + installedVersion + " archive=" + archiveVersion + " cmp=" + cmp);
                if (cmp >= 0)
                {
                    SetStatus(Program.L(
                        "Already up to date (v" + installedVersion + " ≥ v" + archiveVersion + ").",
                        "Déjà à jour (v" + installedVersion + " ≥ v" + archiveVersion + ")."),
                        Color.FromArgb(0, 200, 100));
                    progress.Visible = false;
                    btnInstall.Visible = false;
                    btnFinish.Visible = true;
                    btnFinish.Focus();
                    Program.CleanupTemp();
                    return;
                }
            }

            // [V61-FIX-4] EN: Close WiimoteGun.exe BEFORE extracting — the running app
            //     locks its own exe and every DLL it loaded (System.Buffers, System.Memory,
            //     WiimoteGun.exe...), so the update could not be applied (user report:
            //     all files skipped with "in use by another process").
            //     FR: Fermer WiimoteGun.exe AVANT d'extraire — l'app en cours verrouille
            //     son exe et toutes les DLL chargées, la mise à jour ne pouvait pas
            //     s'appliquer (rapport user : tous les fichiers sautés en « en cours
            //     d'utilisation par un autre processus »).
            progress.Value = 20;
            SetStatus(Program.L("Closing Wiimote4Guns...", "Fermeture de Wiimote4Guns..."));
            string closeError;
            if (!Program.CloseWiimoteGunApp(out closeError))
            {
                ShowError(Program.L(
                    "Could not close WiimoteGun.exe: " + closeError + "\nClose it manually and run the setup again.",
                    "Impossible de fermer WiimoteGun.exe : " + closeError + "\nFermez-le manuellement et relancez le setup."));
                return;
            }

            // Extract
            progress.Value = 25;
            SetStatus(Program.L("Extracting...", "Extraction..."));
            string extractError;
            if (!Program.DownloadAndExtract(sourcePath, installDir, out extractError))
            {
                ShowError("Extraction failed: " + extractError);
                return;
            }
            Program.Log("Extraction complete.");

            // .NET 10 runtime
            progress.Value = 50;
            SetStatus(Program.L("Checking .NET 10 Runtime...", "Vérification du Runtime .NET 10..."));
            if (!Program.IsNet10Installed())
            {
                var result = MessageBox.Show(this,
                    Program.L(
                        "The UMDF2 driver requires the .NET 10 Runtime (free).\n" +
                        "It is not installed.\n\nInstall it now? (~30 MB)",
                        "Le pilote UMDF2 nécessite le Runtime .NET 10 (gratuit).\n" +
                        "Il n'est pas installé.\n\nL'installer maintenant ? (~30 Mo)"),
                    ".NET 10 Runtime", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    SetStatus(Program.L("Installing .NET 10 Runtime...", "Installation du Runtime .NET 10..."));
                    string runtimeError;
                    if (!Program.InstallNet10Runtime(out runtimeError))
                    {
                        Program.Log("Runtime install failed: " + runtimeError);
                        MessageBox.Show(this,
                            Program.L("Runtime installation failed: " + runtimeError,
                                "Échec de l'installation du runtime : " + runtimeError),
                            ".NET 10 Runtime", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }

            // Settings (only if archive >= 3.1.6.0)
            progress.Value = 75;
            SetStatus(Program.L("Configuring settings...", "Configuration des paramètres..."));
            string layoutName = cmbLayout.SelectedItem?.ToString() ?? "Wiimote Bar";
            bool hybridPnp = chkHybrid.Checked && chkHybrid.Enabled;

            if (archiveVersion != null && Program.CompareVersions(archiveVersion, "3.1.6.0") >= 0)
            {
                string preError;
                if (!Program.RunPreSetup(installDir, layoutName, hybridPnp, out preError))
                {
                    Program.Log("PreSetup: " + preError);
                }
                else Program.Log("PreSetup OK (layout=" + layoutName + ", hybrid=" + hybridPnp + ")");
            }
            else
            {
                Program.Log("PreSetup skipped (archive < 3.1.6.0 or version unknown).");
            }

            // Done
            progress.Value = 100;
            SetStatus(Program.L(
                "Setup complete! Launch Wiimote4Guns to finish configuration.",
                "Installation terminée ! Lancez Wiimote4Guns pour terminer la configuration."),
                Color.FromArgb(0, 200, 100));
            btnInstall.Visible = false;
            btnFinish.Visible = true;
            btnFinish.Focus();
            Program.CleanupTemp();
            Program.Log("=== Setup completed ===");
        }

        private void SetStatus(string text) { SetStatus(text, Color.FromArgb(180, 180, 190)); }
        private void SetStatus(string text, Color color)
        {
            lblStatus.Text = text;
            lblStatus.ForeColor = color;
            this.Refresh();
            Application.DoEvents();
        }

        private void ShowError(string message)
        {
            Program.Log("ERROR: " + message);
            lblStatus.Text = message;
            lblStatus.ForeColor = Color.FromArgb(255, 80, 80);
            progress.Visible = false;
            btnInstall.Enabled = true;
            Program.CleanupTemp();
        }

        private void lblHybridInfo_Click(object sender, EventArgs e)
        {

        }
    }
}
