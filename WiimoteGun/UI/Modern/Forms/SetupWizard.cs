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
            if (!this.DesignMode) CheckComponents();
        }

        // EN/FR: Event Handlers for Designer compatibility (Gestionnaires d'événements pour compatibilité Designer)
        private void btnInstallService_Click(object sender, EventArgs e) { ManageService(install: true); }
        private void btnUninstallService_Click(object sender, EventArgs e) { ManageService(install: false); }
        private void btnInstallVMulti_Click(object sender, EventArgs e) { ManageVMulti(install: true); }
        private void btnUninstallVMulti_Click(object sender, EventArgs e) { ManageVMulti(install: false); }
        private void btnContinue_Click(object sender, EventArgs e) { this.DialogResult = DialogResult.OK; this.Close(); }
        private void btnSkip_Click(object sender, EventArgs e) { this.DialogResult = DialogResult.Ignore; this.Close(); }
        private void chkDontShowAgain_CheckedChanged(object sender, EventArgs e)
        {
            WiimoteGun.Options.Instance.ShowSetupWizard = !chkDontShowAgain.Checked;
            WiimoteGun.Options.Instance.Save();
        }
        private void btnReCheck_Click(object sender, EventArgs e) { CheckComponents(); }


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

            // VMulti Check
            bool isVMultiInstalled = IsVMultiInstalled();
            if (isVMultiInstalled)
            {
                lblVMultiStatus.Text = "✓ Installed";
                lblVMultiStatus.ForeColor = Color.LightGreen;
                btnInstallVMulti.Enabled = false;
                btnInstallVMulti.Text = "Installed";
                btnInstallVMulti.BackColor = Color.Gray;
                btnUninstallVMulti.Enabled = true;
            }
            else
            {
                lblVMultiStatus.Text = "❌ Not Installed";
                lblVMultiStatus.ForeColor = Color.Red;
                btnInstallVMulti.Enabled = true;
                btnInstallVMulti.BackColor = Color.FromArgb(0, 122, 204);
                btnUninstallVMulti.Enabled = false;
            }

            if (isServiceInstalled && isVMultiInstalled)
            {
                btnContinue.Enabled = true;
                btnContinue.BackColor = Color.FromArgb(0, 150, 0); // Green
                btnSkip.Visible = false; // Hide skip if fully installed
            }
            else
            {
                btnContinue.Enabled = false;
                btnContinue.BackColor = Color.Gray;
                btnSkip.Visible = true;
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
            // Check if ANY vmulti device exists (same logic as Service uses)
            try {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string devconPath = Path.Combine(baseDir, "WiimoteGun.Service", "WiimoteGunDriver", "virtual1", "devcon.exe");
                if (!File.Exists(devconPath))
                     devconPath = Path.Combine(baseDir, "WiimoteGunDriver", "virtual1", "devcon.exe"); // Fallback

                if (!File.Exists(devconPath)) return false; 

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = devconPath,
                    Arguments = "find \"*vmulti*\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };
                
                var p = Process.Start(psi);
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit(2000);
                
                // At least one vmulti device found means drivers are installed
                return output.Contains("matching device(s) found");
            } catch { return false; }
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
