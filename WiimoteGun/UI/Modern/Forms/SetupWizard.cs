using System;
using System.Drawing;
using System.Windows.Forms;
using System.ServiceProcess;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace WiimoteGun.Forms
{
    public partial class SetupWizard : Form
    {
        public SetupWizard()
        {
            InitializeComponent();
            // [V57n] EN: Global UI zoom at creation (bounds, fonts, screen clamping).
            //     FR: Zoom UI global à la création (bornes, polices, bornage écran).
            WiimoteGun.UI.UiScaler.ApplyForm(this);
            if (!this.DesignMode) CheckComponents();
        }

        // EN/FR: Event Handlers for Designer compatibility (Gestionnaires d'événements pour compatibilité Designer)
        private void btnInstallService_Click(object sender, EventArgs e) { ManageService(install: true); }
        private void btnUninstallService_Click(object sender, EventArgs e) { ManageService(install: false); }
        private void btnInstallVMulti_Click(object sender, EventArgs e)
        {
            // [V57k] EN: The install button is unlocked only by the opt-in checkbox, and a
            //     bilingual confirmation must be accepted before the legacy driver installs.
            //     FR: Le bouton d'installation n'est déverrouillé que par la case d'adhésion,
            //     et une confirmation bilingue doit être acceptée avant l'installation du
            //     pilote legacy.
            if (!chkUnlockVMulti.Checked)
                return;
            if (!ShowVMultiInstallConfirmation())
            {
                // EN/FR: The user backed out of the confirmation - keep the checkbox but abort.
                WiimoteGun.Options.Instance.Save();
                return;
            }
            ManageVMulti(install: true);
        }
        private void btnUninstallVMulti_Click(object sender, EventArgs e) { ManageVMulti(install: false); }
        private void btnContinue_Click(object sender, EventArgs e) { this.DialogResult = DialogResult.OK; this.Close(); }
        private void btnSkip_Click(object sender, EventArgs e) { this.DialogResult = DialogResult.Ignore; this.Close(); }
        private void chkDontShowAgain_CheckedChanged(object sender, EventArgs e)
        {
            WiimoteGun.Options.Instance.ShowSetupWizard = !chkDontShowAgain.Checked;
            WiimoteGun.Options.Instance.Save();
        }
        private void btnReCheck_Click(object sender, EventArgs e) { CheckComponents(); }

        // [V57k] EN: Opt-in checkbox in front of the install button - the legacy VMulti
        //     driver install stays LOCKED until the user explicitly checks it.
        //     FR: Case d'adhésion devant le bouton d'installation - l'installation du
        //     pilote legacy VMulti reste VERROUILLÉE tant que l'utilisateur ne la coche pas.
        private void chkUnlockVMulti_CheckedChanged(object sender, EventArgs e)
        {
            UpdateVMultiInstallGate(isVMultiInstalled: IsVMultiInstalled());
        }

        // [V57k] EN: The install button is enabled ONLY when the opt-in checkbox is checked
        //     AND the driver is not already installed; its label tells the lock state.
        //     FR: Le bouton d'installation n'est activé QUE si la case d'adhésion est cochée
        //     ET que le pilote n'est pas déjà installé ; son libellé indique l'état du
        //     verrouillage.
        private void UpdateVMultiInstallGate(bool isVMultiInstalled)
        {
            bool unlocked = chkUnlockVMulti.Checked && !isVMultiInstalled;
            btnInstallVMulti.Enabled = unlocked;
            btnInstallVMulti.BackColor = unlocked ? Color.FromArgb(0, 122, 204) : Color.Gray;
            btnInstallVMulti.Text = isVMultiInstalled ? "Installed" : (chkUnlockVMulti.Checked ? "Install Driver" : "Install (locked)");
        }


        private void CheckComponents()
        {
            // WiimoteGun Service Check
            bool isServiceInstalled = IsServiceInstalled("WiimoteGunHelper"); 
            if (!isServiceInstalled) isServiceInstalled = IsServiceInstalled("WiimoteGunService");

            if (isServiceInstalled)
            {
                lblServiceStatus.Text = "✓ Installed";
                lblServiceStatus.ForeColor = Color.LightGreen;
                btnInstallService.Enabled = false;
                btnInstallService.Text = "Installed";
                btnInstallService.BackColor = Color.Gray;
                btnUninstallService.Enabled = true;

            }
            else
            {
                lblServiceStatus.Text = "❌ Not Installed";
                lblServiceStatus.ForeColor = Color.Red;
                btnInstallService.Enabled = true;
                btnInstallService.BackColor = Color.FromArgb(0, 122, 204);
                btnUninstallService.Enabled = false;
            }

            // VMulti Check - [V57k] EN: The legacy driver is now OPTIONAL (the new
            //     'RawInput (UMDF2)' mode is the recommended default). Not installed is
            //     therefore NOT an error state anymore.
            //     FR: Le pilote legacy est désormais OPTIONNEL (le nouveau mode
            //     « RawInput (UMDF2) » est le défaut recommandé). Non installé n'est
            //     donc PLUS un état d'erreur.
            bool isVMultiInstalled = IsVMultiInstalled();
            if (isVMultiInstalled)
            {
                lblVMultiStatus.Text = "✓ Installed (legacy)";
                lblVMultiStatus.ForeColor = Color.LightGreen;
                btnUninstallVMulti.Enabled = true;
            }
            else
            {
                lblVMultiStatus.Text = "○ Not installed (optional)";
                lblVMultiStatus.ForeColor = Color.Orange;
                btnUninstallVMulti.Enabled = false;
            }
            UpdateVMultiInstallGate(isVMultiInstalled);

            // [V57k] EN: Only the SERVICE is required to continue. The VMulti driver is
            //     optional - the new UMDF2 mode installs silently through the service.
            //     FR: Seul le SERVICE est requis pour continuer. Le pilote VMulti est
            //     optionnel - le nouveau mode UMDF2 s'installe silencieusement via le
            //     service.
            if (isServiceInstalled)
            {
                btnContinue.Enabled = true;
                btnContinue.BackColor = Color.FromArgb(0, 150, 0); // Green
                btnSkip.Visible = false; // Hide skip when the required service is installed
            }
            else
            {
                btnContinue.Enabled = false;
                btnContinue.BackColor = Color.Gray;
                btnSkip.Visible = true;
            }

            // [V57k] EN: New-user default - when the service is installed and the legacy
            //     VMulti driver is NOT, the recommended 'RawInput (UMDF2)' mode is selected
            //     automatically (a saved 'RawInput (VMulti)' mode would target a driver
            //     that is not installed). Also applies when the wizard is re-shown after
            //     the post-3.0.0.24 service update.
            //     FR: Défaut nouveau utilisateur - quand le service est installé et que le
            //     pilote legacy VMulti ne l'est PAS, le mode recommandé « RawInput (UMDF2) »
            //     est sélectionné automatiquement (un mode « RawInput (VMulti) » sauvegardé
            //     ciblerait un pilote non installé). S'applique aussi quand le wizard est
            //     ré-affiché après la mise à jour du service post-3.0.0.24.
            if (isServiceInstalled && !isVMultiInstalled &&
                WiimoteGun.Options.Instance.DefaultMouseMode == MouseMode.RawInput)
            {
                WiimoteGun.Options.Instance.DefaultMouseMode = MouseMode.RawInputUmdf;
                WiimoteGun.Options.Instance.Save();
                lblVMultiStatus.Text = "○ Not installed -> UMDF2 selected";
                SimpleLogger.Instance.Info("[V57k] Setup Wizard: VMulti not installed - 'RawInput (UMDF2)' selected as the default input mode.");
            }
        }

        private bool IsServiceInstalled(string serviceName)
        {
            try
            {
                return ServiceController.GetServices().Any(s => s.ServiceName.Equals(serviceName, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        }

        private void ManageService(bool install)
        {
            try
            {
                // Dynamic path in subfolder WiimoteGun.Service
                string servicePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WiimoteGun.Service", "WiimoteGun.Service.exe");
                
                if (!File.Exists(servicePath))
                {
                    servicePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WiimoteGun.Service.exe");
                }

                if (!File.Exists(servicePath))
                {
                    MessageBox.Show(string.Format("Service executable not found at:\n{0}", servicePath), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string args = install ? "-install" : "-uninstall";

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = servicePath,
                    Arguments = args,
                    Verb = "runas",
                    UseShellExecute = true
                };
                Process.Start(psi).WaitForExit();
                
                string action = install ? "installed" : "uninstalled";
                MessageBox.Show(this, $"Service {action} successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                if (install)
                {
                    try { 
                        using (ServiceController sc = new ServiceController("WiimoteGunHelper")) 
                        {
                            if (sc.Status != ServiceControllerStatus.Running) sc.Start(); 
                        }
                    } catch {}
                }

                CheckComponents();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, string.Format("Operation failed: {0}", ex.Message), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool IsVMultiInstalled()
        {
            // EN/FR: Natively detect if any vmulti driver is installed without devcon.exe
            // (EN/FR: Détecter nativement si un pilote vmulti est installé sans devcon.exe)
            return VMultiDeviceDetector.IsAnyVMultiInstalled();
        }

        // [V57k] EN: Bilingual confirmation shown before installing the LEGACY VMulti
        //     driver (the install button is already unlocked by the opt-in checkbox).
        //     A small button toggles FR/EN for the whole dialog - texts AND buttons.
        //     Returns true only when the user explicitly confirms the install.
        //     FR: Confirmation bilingue affichée avant l'installation du pilote LEGACY
        //     VMulti (le bouton est déjà déverrouillé par la case d'adhésion). Un petit
        //     bouton bascule FR/EN pour tout le dialogue - textes ET boutons. Renvoie
        //     vrai seulement si l'utilisateur confirme explicitement l'installation.
        private bool ShowVMultiInstallConfirmation()
        {
            // (EN/FR: FR first - toggle button switches to EN)
            string frText =
                "Le NOUVEAU pilote « RawInput (UMDF2) » est disponible, fonctionne de la même manière " +
                "et ne devrait PAS être bloqué par Microsoft à compter de Windows 11 26H02, " +
                "qui va déprécier les pilotes signés avec d'anciens certificats.\r\n\r\n" +
                "Le nouveau pilote est actuellement EN PHASE DE TEST.\r\n\r\n" +
                "Le pilote VMulti (legacy) sera à terme supprimé, ou disponible uniquement pour " +
                "Windows 10 et les Windows 11 antérieurs à 26H02.\r\n\r\n" +
                "Voulez-vous vraiment installer VMulti ?";
            string enText =
                "The NEW 'RawInput (UMDF2)' driver is available, works the same way, " +
                "and should NOT be blocked by Microsoft starting with Windows 11 26H02, " +
                "which will deprecate drivers signed with legacy certificates.\r\n\r\n" +
                "The new driver is currently IN TESTING PHASE.\r\n\r\n" +
                "The VMulti (legacy) driver will eventually be removed, or only available on " +
                "Windows 10 and Windows 11 versions below 26H02.\r\n\r\n" +
                "Do you really want to install VMulti?";

            bool fr = true; // (EN/FR: default language, toggled by the small button)
            bool confirmed = false;

            using (var dialog = new Form())
            {
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                dialog.ShowInTaskbar = false;
                dialog.StartPosition = FormStartPosition.CenterParent;
                dialog.ClientSize = new Size(580, 320);
                dialog.BackColor = Color.FromArgb(30, 30, 30);
                dialog.ForeColor = Color.White;
                dialog.Text = "VMulti Driver — Confirmation";

                var lbl = new Label
                {
                    Location = new Point(20, 45),
                    Size = new Size(540, 210),
                    Text = frText,
                    Font = new Font("Segoe UI", 10F),
                    ForeColor = Color.White
                };

                var btnLang = new Button
                {
                    // (EN/FR: small language toggle - shows the OTHER language's code)
                    Text = "EN",
                    Location = new Point(495, 10),
                    Size = new Size(65, 26),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(60, 60, 60),
                    ForeColor = Color.White,
                    Cursor = Cursors.Hand
                };

                var btnInstall = new Button
                {
                    Text = "Installer VMulti",
                    Location = new Point(155, 270),
                    Size = new Size(150, 34),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(0, 122, 204),
                    ForeColor = Color.White,
                    Cursor = Cursors.Hand
                };

                var btnCancel = new Button
                {
                    Text = "Annuler",
                    Location = new Point(330, 270),
                    Size = new Size(100, 34),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(60, 60, 60),
                    ForeColor = Color.White,
                    Cursor = Cursors.Hand
                };

                btnLang.Click += (s, e) =>
                {
                    fr = !fr;
                    lbl.Text = fr ? frText : enText;
                    btnLang.Text = fr ? "EN" : "FR";
                    btnInstall.Text = fr ? "Installer VMulti" : "Install VMulti";
                    btnCancel.Text = fr ? "Annuler" : "Cancel";
                };
                btnInstall.Click += (s, e) => { confirmed = true; dialog.Close(); };
                btnCancel.Click += (s, e) => dialog.Close();

                dialog.Controls.Add(lbl);
                dialog.Controls.Add(btnLang);
                dialog.Controls.Add(btnInstall);
                dialog.Controls.Add(btnCancel);

                dialog.ShowDialog(this);
            }

            return confirmed;
        }

        private void ManageVMulti(bool install)
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                // Service path: \WiimoteGun.Service\WiimoteGunDriver
                string driverBaseDir = Path.Combine(baseDir, "WiimoteGun.Service", "WiimoteGunDriver");
                
                if (!Directory.Exists(driverBaseDir))
                {
                    // Fallback to local
                    string localDriver = Path.Combine(baseDir, "WiimoteGunDriver");
                    if (Directory.Exists(localDriver)) driverBaseDir = localDriver;
                }

                if (!Directory.Exists(driverBaseDir))
                {
                    MessageBox.Show(this, $"Driver files not found at:\n{driverBaseDir}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Create Master Batch Script
                string tempDir = Path.Combine(Path.GetTempPath(), "WiimoteGunVMultiInstall");
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                Directory.CreateDirectory(tempDir);

                string batchContent = "";
                string batchName = install ? "install_master.bat" : "uninstall_master.bat";

                if (install)
                {
                    // Install ALL 4 drivers (virtual1-4)
                    string virtual1Dir = Path.Combine(driverBaseDir, "virtual1");
                    string devconPath = Path.Combine(virtual1Dir, "devcon.exe");
                    
                    if (!File.Exists(devconPath))
                    {
                         MessageBox.Show(this, "devcon.exe missing in virtual1 folder", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                         return;
                    }
                    
                    // Copy devcon to temp for execution context
                    File.Copy(devconPath, Path.Combine(tempDir, "devcon.exe"));

                    // Build Install Commands for ALL 4 drivers
                    batchContent = "@echo off\n";
                    batchContent += $"cd /d \"{tempDir}\"\n\n";
                    
                    var drivers = new[] { 
                        new { dir = "virtual1", inf = "vmultia.inf", hwid = "ecologylab\\vmultia", name = "VMultiA (Player 1)" },
                        new { dir = "virtual2", inf = "vmultib.inf", hwid = "ecologylab\\vmultib", name = "VMultiB (Player 2)" },
                        new { dir = "virtual3", inf = "vmultic.inf", hwid = "ecologylab\\vmultic", name = "VMultiC (Player 3)" },
                        new { dir = "virtual4", inf = "vmultid.inf", hwid = "ecologylab\\vmultid", name = "VMultiD (Player 4)" }
                    };
                    
                    foreach (var driver in drivers)
                    {
                        string virtualDir = Path.Combine(driverBaseDir, driver.dir);
                        string infPath = Path.Combine(virtualDir, driver.inf);
                        string charLower = driver.inf.Replace("vmulti", "").Replace(".inf", "");
                        
                        batchContent += $"echo Installing {driver.name}...\n";
                        batchContent += $"devcon.exe /r install \"{infPath}\" {driver.hwid}\n";
                        batchContent += "echo.\n";
                        batchContent += $"echo Disabling unused devices for {charLower}...\n";
                        // Disable unwanted HID collections to avoid clutter and conflicts
                        // (EN/FR: Désactiver collections HID inutiles pour éviter conflits)
                        batchContent += $"devcon.exe disable \"*vmulti{charLower}*COL01*\"\n";
                        batchContent += $"devcon.exe disable \"*vmulti{charLower}*COL02*\"\n";
                        batchContent += $"devcon.exe disable \"*vmulti{charLower}*COL03*\"\n"; // Disable system mouse collection (Col03) for initial installation (EN/FR: Désactiver la collection souris système (Col03) pour l'installation initiale)
                        batchContent += $"devcon.exe disable \"*vmulti{charLower}*COL04*\"\n";
                        batchContent += $"devcon.exe disable \"*vmulti{charLower}*COL05*\"\n";
                        batchContent += $"devcon.exe disable \"*vmulti{charLower}*COL06*\"\n";
                        // EN: Keep COL08 and COL09 for Keyboard and Gamepad Control (FR: Garder COL08 et COL09 pour Clavier et Contrôle Gamepad)
                        // batchContent += $"devcon.exe disable \"*vmulti{charLower}*COL08*\"\n";
                        // batchContent += $"devcon.exe disable \"*vmulti{charLower}*COL09*\"\n";
                        batchContent += "echo.\n\n";
                    }
                    
                    batchContent += "echo All drivers installed!\n";
                    batchContent += "timeout /t 3\n";
                }
                else
                {
                    // Uninstall ALL Logic
                    string virtual1Dir = Path.Combine(driverBaseDir, "virtual1");
                    
                    // Copy devcon and DIFxCmd to temp
                    string devconPath = Path.Combine(virtual1Dir, "devcon.exe");
                    string difxPath = Path.Combine(virtual1Dir, "DIFxCmd.exe");
                    
                    if (File.Exists(devconPath)) File.Copy(devconPath, Path.Combine(tempDir, "devcon.exe"));
                    if (File.Exists(difxPath)) File.Copy(difxPath, Path.Combine(tempDir, "DIFxCmd.exe"));
                    
                    // CRITICAL: Copy INF files to temp so DIFxCmd can find them
                    var infFiles = new[] { "vmultia.inf", "vmultib.inf", "vmultic.inf", "vmultid.inf" };
                    var virtualDirs = new[] { "virtual1", "virtual2", "virtual3", "virtual4" };
                    
                    for (int i = 0; i < infFiles.Length; i++)
                    {
                        string sourceInf = Path.Combine(driverBaseDir, virtualDirs[i], infFiles[i]);
                        string destInf = Path.Combine(tempDir, infFiles[i]);
                        if (File.Exists(sourceInf))
                        {
                            File.Copy(sourceInf, destInf, true);
                        }
                    }
                    
                    batchContent = $@"
@echo off
cd /d ""{tempDir}""
echo Uninstalling ALL VMulti Drivers...

echo Removing vmulti devices...
devcon.exe remove ""*vmulti*""

echo Removing INF files...
if exist DIFxCmd.exe (
    DIFxCmd.exe /u vmultia.inf
    DIFxCmd.exe /u vmultib.inf
    DIFxCmd.exe /u vmultic.inf
    DIFxCmd.exe /u vmultid.inf
)

echo.
echo Uninstall Complete.
timeout /t 3
";
                }

                string batchPath = Path.Combine(tempDir, batchName);
                File.WriteAllText(batchPath, batchContent);

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = batchPath,
                    Verb = "runas",
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Normal // Show console so user sees progress/errors
                };
                Process.Start(psi).WaitForExit();
                
                // Give Windows time to register the new devices (EN/FR: Laisser le temps à Windows d'enregistrer les nouveaux périphériques)
                // Give Windows time to register/deregister the devices (EN/FR: Laisser le temps à Windows d'enregistrer/désenregistrer les périphériques)
                System.Threading.Thread.Sleep(2000); // 2 seconds for device registration/deregistration
                
                string msg = install ? "Installation logic Executed." : "Uninstallation logic Executed.";
                MessageBox.Show(this, msg + " Please check console output if it appeared.", "Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                CheckComponents();
            }
            catch (Exception ex)
            {
               MessageBox.Show(this, $"Operation failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
