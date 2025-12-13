using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Linq;
using System.Text;
using System.ServiceProcess;
using WiimoteGun.Forms;
using WiimoteGun.UI.Legacy;
using WiimoteGun.UI;
using WiimoteGun.UI.Calibrate;

namespace WiimoteGun
{
    static class Program
    {
        // P/Invoke for robust process detection (EN/FR: P/Invoke pour détection processus robuste)
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool QueryFullProcessImageName(IntPtr hProcess, uint dwFlags, System.Text.StringBuilder lpExeName, ref uint lpdwSize);

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        static NotifyIcon _trayIcon;
        static WiimoteControllerManager _wiiMoteManager;
        public static WiimoteControllerManager WiiMoteManager { get { return _wiiMoteManager; } }
        static ApplicationContext _appContext;
        static Mutex _singleInstanceMutex;
        static IRVisualizerForm _irVisualizerForm; // Single instance of IR Visualizer (EN/FR: Instance unique IR Visualizer)
        static MessageWindow _messageWindow; // IPC window for -refresh command (EN/FR: Fenêtre IPC pour commande -refresh)
        static string _activeRemapProfile = null; // Active remap profile path (EN/FR: Chemin du profil remap actif)
        static bool _menuMode = false; // Menu mode flag for windowed overlay (EN/FR: Drapeau mode menu pour overlay fenêtré)

        [STAThread]
        static void Main(string[] args)
        {
            // Parse -remap argument first (EN/FR: Parser argument -remap d'abord)
            ParseRemapArgument(args);

            // Handle -restart argument: Wait for previous instance to exit (EN/FR: Gérer argument -restart)
            if (args.Length > 0 && (args[0].ToLower() == "-restart" || args[0].ToLower() == "/restart"))
            {
                SimpleLogger.Instance.Info("-restart argument detected, waiting for previous instance to exit...");
                // Wait a bit to ensure the previous instance has time to release the mutex
                System.Threading.Thread.Sleep(2000);
            }

            // Handle -refresh argument: Reload configuration in running instance (EN/FR: Gérer argument -refresh)
            if (args.Length > 0 && (args[0].ToLower() == "-refresh" || args[0].ToLower() == "/refresh"))
            {
                SimpleLogger.Instance.Info("-refresh argument detected, sending message to running instance");
                bool success = MessageWindow.SendRefreshToRunningInstance();
                
                if (success)
                {
                    SimpleLogger.Instance.Info("Refresh message sent successfully");
                    // No popup - silent operation (EN/FR: Pas de popup - opération silencieuse)
                    return; // Exit since we successfully sent refresh to existing instance
                }
                else
                {
                    SimpleLogger.Instance.Warning("Failed to send refresh message (no running instance?)");
                    // No instance running - continue to start normally
                    // (EN/FR: Aucune instance - continuer démarrage normal)
                    SimpleLogger.Instance.Info("No running instance found, starting new instance with arguments");
                    // Don't return - continue to start the application
                }
            }

            // Handle -remap argument: Try to send to running instance first (EN/FR: Gérer argument -remap : essayer d'envoyer à instance en cours d'abord)
            if (!string.IsNullOrEmpty(_activeRemapProfile))
            {
                SimpleLogger.Instance.Info($"-remap argument detected with profile: {_activeRemapProfile}, attempting to send to running instance");
                bool success = MessageWindow.SendRemapToRunningInstance(_activeRemapProfile);
                
                if (success)
                {
                    SimpleLogger.Instance.Info("Remap message sent successfully to running instance");
                    return; // Exit - remap sent to existing instance
                }
                else
                {
                    SimpleLogger.Instance.Info("No running instance found, will start new instance with remap profile");
                    // Continue to start new instance with the remap profile
                }
            }

            // Handle -menu argument: Try to send to running instance first (EN/FR: Gérer argument -menu : essayer d'envoyer à instance en cours d'abord)
            if (_menuMode)
            {
                SimpleLogger.Instance.Info("-menu argument detected, attempting to send to running instance");
                bool success = MessageWindow.SendMenuToRunningInstance();
                
                if (success)
                {
                    SimpleLogger.Instance.Info("Menu message sent successfully to running instance");
                    return; // Exit - message sent to existing instance
                }
                else
                {
                    SimpleLogger.Instance.Info("No running instance found, will start new instance with menu mode");
                    // Continue to start new instance and open windowed overlay
                }
            }

            // Handle driver installation commands (requires admin)
            if (args.Length > 0)
            {
                if (args[0] == "/installDrivers")
                {
                    InstallInterceptionDriver();
                    return;
                }
                if (args[0] == "/uninstallDrivers")
                {
                    UninstallInterceptionDriver();
                    return;
                }
                
                // VMulti1 (Pentablet)
                if (args[0] == "/installVMulti1")
                {
                    InstallVMultiDriver("VMulti1", "vmulti.inf");
                    return;
                }
                if (args[0] == "/uninstallVMulti1")
                {
                    UninstallVMultiDriver("VMulti1");
                    return;
                }

                // VMulti2 (vmultib)
                if (args[0] == "/installVMulti2")
                {
                    InstallVMultiDriver("VMulti2", "vmultib.inf");
                    return;
                }
                if (args[0] == "/uninstallVMulti2")
                {
                    UninstallVMultiDriver("VMulti2");
                    return;
                }

                // Player 1 (vmultia) - virtual1
                if (args[0] == "/installPlayer1")
                {
                    InstallVMultiDriver("virtual1", "vmultia.inf");
                    return;
                }
                if (args[0] == "/uninstallPlayer1")
                {
                    UninstallVMultiDriver("virtual1");
                    return;
                }

                // Player 2 (vmultib) - virtual2
                if (args[0] == "/installPlayer2")
                {
                    InstallVMultiDriver("virtual2", "vmultib.inf");
                    return;
                }
                if (args[0] == "/uninstallPlayer2")
                {
                    UninstallVMultiDriver("virtual2");
                    return;
                }

                // Player 3 (vmultic) - virtual3
                if (args[0] == "/installPlayer3")
                {
                    InstallVMultiDriver("virtual3", "vmultic.inf");
                    return;
                }
                if (args[0] == "/uninstallPlayer3")
                {
                    UninstallVMultiDriver("virtual3");
                    return;
                }

                // Player 4 (vmultid) - virtual4
                if (args[0] == "/installPlayer4")
                {
                    InstallVMultiDriver("virtual4", "vmultid.inf");
                    return;
                }
                if (args[0] == "/uninstallPlayer4")
                {
                    UninstallVMultiDriver("virtual4");
                    return;
                }
            }
            // (EN/FR: Gérer commandes installation driver - nécessite admin)
            if (args.Length > 0)
            {
                string firstArg = args[0].ToLower();
                
                // Only handle driver-related commands, not -remap
                // (EN/FR: Gérer uniquement commandes driver, pas -remap)
                if (firstArg == "/installdrivers" || firstArg == "-installdrivers" ||
                    firstArg == "/uninstalldrivers" || firstArg == "-uninstalldrivers")
                {
                    HandleDriverCommands(args);
                    return;
                }
            }

            bool createdNew;
            _singleInstanceMutex = new Mutex(true, "WiimoteGun {71916996-F0A0-434C-88CA-41A62B4F9E17}", out createdNew);
            if (!createdNew)
                return;

            SimpleLogger.Instance.Info("---------------------------------------------------------------");
            SimpleLogger.Instance.Info("WiimoteGun startup");

            // Update PATH environment variable if needed (EN/FR: Mettre à jour variable PATH si nécessaire)
            UpdatePathEnvironmentVariable();

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // First Run / Setup Checker
            if (WiimoteGun.Options.Instance.ShowSetupWizard)
            {
                if (!IsServiceInstalled("interception") || !IsServiceInstalled("WiimoteGunHelper"))
                {
                    using (var wizard = new Forms.SetupWizard())
                    {
                         wizard.ShowDialog();
                         // We don't block start even if they cancel, unless critical?
                         // User asked for "Skip" option, so we just run.
                    }
                }
            }

            _appContext = new ApplicationContext();
            _wiiMoteManager = new WiimoteControllerManager();
            _synchronizationContext = new WindowsFormsSynchronizationContext();

            // Create message window for IPC (EN/FR: Créer fenêtre message pour IPC)
            _messageWindow = new MessageWindow();
            _messageWindow.RefreshRequested += OnRefreshRequested;
            _messageWindow.RemapRequested += OnRemapRequested;
            _messageWindow.MenuRequested += OnMenuRequested;
            
            // Initialize Overlay (EN/FR: Initialiser Overlay)
            _profileOverlay = new ProfileOverlay(_menuMode);
            
            // Initialize Hotkey Manager (EN/FR: Initialiser gestionnaire hotkeys)
            HotkeyManager.Initialize();
            // Connect overlay state to hotkey manager (EN/FR: Connecter état overlay)
            HotkeyManager.IsOverlayOpen = () => _profileOverlay != null && _profileOverlay.Visible;
            
            WiiMoteController.OverlayRequested += (s, e) => 
            {
                _synchronizationContext.Post(_ => 
                {
                    // If overlay is in windowed mode, close it and open fullscreen (EN/FR: Si overlay est en mode fenêtré, le fermer et ouvrir plein écran)
                    if (_profileOverlay != null && _profileOverlay.IsWindowedMode)
                    {
                        _profileOverlay.Close();
                        _profileOverlay = new ProfileOverlay(windowedMode: false);
                        _profileOverlay.FormClosed += (sender, evtArgs) => _profileOverlay = null;
                        _profileOverlay.Show();
                    }
                    else if (_profileOverlay != null)
                    {
                        // Normal toggle for fullscreen overlay (EN/FR: Bascule normale pour overlay plein écran)
                        if (_profileOverlay.Visible)
                            _profileOverlay.Hide();
                        else
                            _profileOverlay.Show();
                    }
                    else
                    {
                        // Create new fullscreen overlay if null (EN/FR: Créer nouvel overlay plein écran si null)
                        _profileOverlay = new ProfileOverlay(windowedMode: false);
                        _profileOverlay.FormClosed += (sender, evtArgs) => _profileOverlay = null;
                        _profileOverlay.Show();
                    }
                }, null);
            };

            // Start Game Detection Timer (EN/FR: Démarrer Timer Détection Jeu)
            _gameDetectionTimer = new System.Threading.Timer(CheckForGameProcesses, null, 2000, 2000);

            InitializeTrayIcon();

            // Show welcome dialog on first run BEFORE starting message loop (EN/FR: Afficher le dialogue de bienvenue au premier lancement AVANT la boucle de messages)
            SimpleLogger.Instance.Info("Checking FirstRun flag: " + Options.Instance.FirstRun);
            
            if (Options.Instance.FirstRun)
            {
                SimpleLogger.Instance.Info("FirstRun is TRUE, showing welcome dialog");
                ShowWelcomeDialog();
                Options.Instance.FirstRun = false;
                Options.Instance.Save();
                SimpleLogger.Instance.Info("Welcome dialog shown, FirstRun set to false");
            }
            else
            {
                SimpleLogger.Instance.Info("FirstRun is FALSE, skipping welcome dialog");
            }

            // Auto-assign VMulti devices to Player 1 and 2 if option is enabled (EN/FR: Auto-assigner VMulti aux joueurs 1 et 2 si option activée)
            VMultiDeviceDetector.AutoAssignVMultiDevices();

            // CRITICAL: Apply remap profile AFTER Options loaded but BEFORE WiimoteControllerManager starts
            // (EN/FR: CRITIQUE : Appliquer profil remap APRÈS Options mais AVANT démarrage WiimoteControllerManager)
            // CRITICAL: Apply remap profile AFTER Options loaded but BEFORE WiimoteControllerManager starts
            // (EN/FR: CRITIQUE : Appliquer profil remap APRÈS Options mais AVANT démarrage WiimoteControllerManager)
            ApplyRemapProfile();

            // Open windowed overlay if requested via -menu argument (EN/FR: Ouvrir overlay fenêtré si demandé via argument -menu)
            if (_menuMode)
            {
                // Use timer to allow message loop to start first (EN/FR: Utiliser timer pour laisser boucle message démarrer)
                System.Threading.Timer menuTimer = null;
                menuTimer = new System.Threading.Timer(_ => 
                {
                    _appContext.MainForm?.Invoke((MethodInvoker)delegate
                    {
                        OpenWindowedOverlay();
                    });
                    menuTimer?.Dispose();
                }, null, 500, System.Threading.Timeout.Infinite);
            }
           
            Application.Run(_appContext);
        }

        private static void HandleDriverCommands(string[] args)
        {
            if (!OptionsForm.IsAdministrator())
            {
                MessageBox.Show("These commands must be run with administrator privileges.", "WiimoteGun", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string command = args[0].ToLower();
            if (command == "/installdrivers" || command == "/uninstalldrivers")
            {
                try
                {
                    // Interception installer path (EN/FR: Chemin de l'installateur Interception)
                    string installerFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WiimoteGunDriver", "command line installer");
                    string installerPath = "";
                    
                    if (command == "/installdrivers")
                    {
                        installerPath = Path.Combine(installerFolder, "install-interception.exe");
                        SimpleLogger.Instance.Info("Running Interception driver install: " + installerPath);
                    }
                    else // /uninstalldrivers
                    {
                        installerPath = Path.Combine(installerFolder, "install-interception.exe");
                        SimpleLogger.Instance.Info("Running Interception driver uninstall: " + installerPath);
                    }

                    if (!File.Exists(installerPath))
                    {
                        MessageBox.Show("Interception installer not found:\n\n" + installerPath + "\n\n" +
                                        "Please ensure the WiimoteGunDriver folder is complete.",
                                        "Installer Missing", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    var process = new Process();
                    process.StartInfo.FileName = installerPath;
                    process.StartInfo.WorkingDirectory = installerFolder;
                    process.StartInfo.UseShellExecute = true; // Required for elevation prompt
                    process.StartInfo.Verb = "runas"; // Request elevation
                    
                    // Add /install argument for installation, /uninstall for uninstallation (EN/FR: Ajouter l'argument /install ou /uninstall)
                    if (command == "/uninstalldrivers")
                    {
                        process.StartInfo.Arguments = "/uninstall";
                    }
                    else
                    {
                        process.StartInfo.Arguments = "/install";
                    }

                    process.Start();
                    process.WaitForExit();
                    
                    string resultMsg = command == "/installdrivers"
                        ? "Interception driver installation completed.\n\nYou MUST restart your PC for changes to take effect."
                        : "Interception driver uninstallation completed.\n\nYou MUST restart your PC for changes to take effect.";
                    
                    SimpleLogger.Instance.Info(resultMsg);
                    MessageBox.Show(resultMsg, "WiimoteGun Driver Management", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    SimpleLogger.Instance.Error("Driver operation failed: " + ex.ToString());
                    MessageBox.Show("Driver operation failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var ex = e.ExceptionObject as Exception;
            if (ex != null)
            {
                SimpleLogger.Instance.Error("Unhandled Exception : " + ex.ToString());
                MessageBox.Show("Unhandled Exception : " + ex.ToString());
            }
        }

        private static void InitializeTrayIcon()
        {
            _trayIcon = new NotifyIcon();
            _trayIcon.Icon = Properties.Resources.gray;
            _trayIcon.Visible = true;
            _trayIcon.Text = "Wiimote Gun";
            _trayIcon.DoubleClick += (s, e) => OpenWindowedOverlay();

            var menuItems = new MenuItem[]
            {
                new MenuItem("&Button Mapping", OnShowButtonMapping),
                new MenuItem("&IR Visualizer", OnShowIRVisualizer),
                new MenuItem("&Assign Wiimotes", (s,e) => {}) { Name = "AssignWiimotes" }, // Placeholder
                new MenuItem("&Options", OnShowOptions),
                new MenuItem("&About", OnShowAbout),
                new MenuItem("-"),
                new MenuItem("&Exit", OnExitClicked)
            };

            _trayIcon.ContextMenu = new ContextMenu(menuItems);
            _trayIcon.ContextMenu.Popup += OnContextMenuPopup;
        }

        private static void OnContextMenuPopup(object sender, EventArgs e)
        {
            var menu = sender as ContextMenu;
            if (menu == null) return;

            // Find the Assign Wiimotes menu item
            MenuItem assignMenu = null;
            foreach (MenuItem item in menu.MenuItems)
            {
                if (item.Name == "AssignWiimotes")
                {
                    assignMenu = item;
                    break;
                }
            }

            if (assignMenu == null) return;

            assignMenu.MenuItems.Clear();

            // Create submenus for P1 to P4
            for (int i = 1; i <= 4; i++)
            {
                var playerMenu = new MenuItem($"Player {i}");
                PopulatePlayerMenu(playerMenu, i);
                assignMenu.MenuItems.Add(playerMenu);
            }
        }

        private static void PopulatePlayerMenu(MenuItem playerMenu, int playerIndex)
        {
            var controllers = _wiiMoteManager.GetControllers();
            string currentPreferred = "";
            switch (playerIndex)
            {
                case 1: currentPreferred = Options.Instance.PreferredMacP1; break;
                case 2: currentPreferred = Options.Instance.PreferredMacP2; break;
                case 3: currentPreferred = Options.Instance.PreferredMacP3; break;
                case 4: currentPreferred = Options.Instance.PreferredMacP4; break;
            }

            // Add "None" option
            var noneItem = new MenuItem("None (Auto)");
            noneItem.Checked = string.IsNullOrEmpty(currentPreferred);
            noneItem.Click += (s, e) => SetPreferredWiimote(playerIndex, "");
            playerMenu.MenuItems.Add(noneItem);
            playerMenu.MenuItems.Add("-");

            if (!controllers.Any())
            {
                var noDevItem = new MenuItem("No Wiimotes connected");
                noDevItem.Enabled = false;
                playerMenu.MenuItems.Add(noDevItem);
                return;
            }

            foreach (var controller in controllers)
            {
                string mac = controller.Wiimote.Address.ToString();
                string label = $"Wiimote {mac}";
                
                // Indicate if currently active for this player
                if (controller.PlayerIndex == playerIndex)
                    label += " (Active)";

                var item = new MenuItem(label);
                item.Checked = (mac == currentPreferred);
                item.Click += (s, e) => SetPreferredWiimote(playerIndex, mac);
                playerMenu.MenuItems.Add(item);
            }

            // Add separator and Mouse Device submenu (EN/FR: Ajouter séparateur et sous-menu Souris)
            playerMenu.MenuItems.Add("-");
            var mouseSubmenu = new MenuItem("🖱️ Mouse Device");
            PopulateMouseDeviceMenu(mouseSubmenu, playerIndex);
            playerMenu.MenuItems.Add(mouseSubmenu);
            
            // Add Keyboard Device submenu (EN/FR: Ajouter sous-menu Clavier)
            var keyboardSubmenu = new MenuItem("⌨️ Keyboard Device");
            PopulateKeyboardDeviceMenu(keyboardSubmenu, playerIndex);
            playerMenu.MenuItems.Add(keyboardSubmenu);
            
            // Add Rumble Settings submenu (EN/FR: Ajouter sous-menu Paramètres Vibration)
            var rumbleSubmenu = new MenuItem("💥 Rumble Settings");
            PopulateRumbleMenu(rumbleSubmenu, playerIndex);
            playerMenu.MenuItems.Add(rumbleSubmenu);
        }

        private static void PopulateMouseDeviceMenu(MenuItem mouseMenu, int playerIndex)
        {
            // Check if VMulti Auto-Lock is active for this player (EN/FR: Vérifier si Auto-Lock VMulti est actif pour ce joueur)
            if (VMultiDeviceDetector.ShouldLockPlayerDevices(playerIndex))
            {
                var lockItem = new MenuItem($"🔒 Locked to VMulti Player {playerIndex}");
                lockItem.Enabled = false;
                mouseMenu.MenuItems.Add(lockItem);
                return;
            }

            string currentPreferred = Options.Instance.GetPreferredMouseId(playerIndex);

            // Add "None (Auto)" option (EN/FR: Ajouter option "Aucune (Auto)")
            var noneItem = new MenuItem("None (Auto)");
            noneItem.Checked = string.IsNullOrEmpty(currentPreferred);
            noneItem.Click += (s, e) => SetPreferredMouse(playerIndex, "");
            mouseMenu.MenuItems.Add(noneItem);
            mouseMenu.MenuItems.Add("-");

            // Scan for mice using Interception (EN/FR: Scanner les souris avec Interception)
            var detectedMice = new System.Collections.Generic.List<(int deviceId, string hardwareId)>();
            
            try
            {
                var context = WiimoteGun.Interception.InterceptionDriver.interception_create_context();
                if (context != IntPtr.Zero)
                {
                    for (int i = 11; i <= 20; i++)
                    {
                        if (WiimoteGun.Interception.InterceptionDriver.interception_is_mouse(i) != 0)
                        {
                            // Use char array instead of StringBuilder for better marshalling
                            byte[] buffer = new byte[1000];
                            uint result = WiimoteGun.Interception.InterceptionDriver.interception_get_hardware_id(context, i, buffer, (uint)buffer.Length);
                            
                            if (result > 0)
                            {
                                // Convert byte array to string (UTF-16LE encoding for wide chars)
                                // Result is the number of characters, so multiply by 2 for bytes, but cap at buffer length
                                int byteCount = Math.Min((int)result * 2, buffer.Length);
                                string hardwareId = System.Text.Encoding.Unicode.GetString(buffer, 0, byteCount);
                                
                                // Remove all NULL characters (0x00) to avoid XML serialization errors
                                // (EN/FR: Supprimer tous les caractères NULL pour éviter erreurs XML)
                                hardwareId = hardwareId.Replace("\0", "").Trim();
                                
                                if (!string.IsNullOrEmpty(hardwareId))
                                {
                                    SimpleLogger.Instance.Info($"[MENU SCAN] Mouse {i} Hardware ID: {hardwareId}");
                                    detectedMice.Add((i, hardwareId));
                                }
                            }
                        }
                    }
                    WiimoteGun.Interception.InterceptionDriver.interception_destroy_context(context);
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Error scanning mice: {ex.Message}");
            }

            if (detectedMice.Count == 0)
            {
                var noDevItem = new MenuItem("No mice detected");
                    noDevItem.Enabled = false;
                mouseMenu.MenuItems.Add(noDevItem);
                return;
            }

            // Get currently active mouse for this player (EN/FR: Récupérer souris active pour ce joueur)
            int activeMouseId = -1;
            var playerController = _wiiMoteManager?.GetControllers().FirstOrDefault(c => c.PlayerIndex == playerIndex);
            if (playerController?.VirtualMouse is VirtualInterceptionMouse interceptionMouse)
            {
                activeMouseId = interceptionMouse.MouseDeviceId;
            }

            // Build list of mice already assigned to other players (EN/FR: Liste souris assignées à d'autres joueurs)
            var assignedMice = new System.Collections.Generic.HashSet<string>();
            for (int p = 1; p <= 4; p++)
            {
                if (p == playerIndex) continue; // Skip current player
                string otherPreferred = Options.Instance.GetPreferredMouseId(p);
                if (!string.IsNullOrEmpty(otherPreferred))
                {
                    assignedMice.Add(otherPreferred);
                }
            }

            // Check for duplicates (same VID/PID) to decide if we need unique Instance Path
            var vidPidGroups = new System.Collections.Generic.Dictionary<string, int>();
            foreach (var (deviceId, hardwareId) in detectedMice)
            {
                var vidPidResult = DeviceHelper.ExtractVidPid(hardwareId);
                string vid = vidPidResult.vid;
                string pid = vidPidResult.pid;
                if (vid != null)
                {
                    string vidPidKey = pid != null ? $"VID_{vid}&PID_{pid}" : $"VID_{vid}";
                    if (!vidPidGroups.ContainsKey(vidPidKey))
                        vidPidGroups[vidPidKey] = 0;
                    vidPidGroups[vidPidKey]++;
                }
            }

            // Add each detected mouse (EN/FR: Ajouter chaque souris détectée)
            foreach (var (deviceId, hardwareId) in detectedMice)
            {
                SimpleLogger.Instance.Info($"[MOUSE DETECTION] Device {deviceId} - Hardware ID: {hardwareId}");
                
                var vidPidResult = DeviceHelper.ExtractVidPid(hardwareId);
                string vid = vidPidResult.vid;
                string pid = vidPidResult.pid;
                
                SimpleLogger.Instance.Info($"[MOUSE DETECTION] Device {deviceId} - Extracted VID: {vid}, PID: {pid}");
                
                if (vid == null)
                {
                    // Fallback for unrecognized format
                    var item = new MenuItem($"Mouse {deviceId} (Unknown)");
                    item.Click += (s, e) => SetPreferredMouse(playerIndex, hardwareId);
                    mouseMenu.MenuItems.Add(item);
                    continue;
                }

                string vidPidKey = pid != null ? $"VID_{vid}&PID_{pid}" : $"VID_{vid}";
                bool isDuplicate = vidPidGroups.ContainsKey(vidPidKey) && vidPidGroups[vidPidKey] > 1;

                // Get friendly name (EN/FR: Récupérer nom commercial)
                // Pass hardwareId (full path) to enable robust VMulti detection
                string friendlyName = DeviceHelper.GetDeviceFriendlyName(vidPidKey, hardwareId);
                
                SimpleLogger.Instance.Info($"[MOUSE DETECTION] Device {deviceId} - Friendly name: {friendlyName ?? "NOT FOUND"}");
                
                // Build display name (EN/FR: Construire nom d'affichage)
                string displayName;
                if (!string.IsNullOrEmpty(friendlyName))
                {
                    displayName = $"{friendlyName}";
                    if (isDuplicate)
                        displayName += $" #{deviceId}"; // Add ID if duplicate
                }
                else
                {
                    displayName = $"Mouse {deviceId} ({vidPidKey})";
                }

                // Add "(Active)" if this is the currently active mouse for this player
                if (deviceId == activeMouseId)
                {
                    displayName += " (Active)";
                }

                // Determine what to save (EN/FR: Déterminer quoi sauvegarder)
                string identifierToSave = isDuplicate ? hardwareId : vidPidKey;

                // Check if already assigned to another player
                bool isAssignedToOther = assignedMice.Contains(identifierToSave) || 
                                          assignedMice.Any(assigned => hardwareId.IndexOf(assigned, StringComparison.OrdinalIgnoreCase) >= 0);

                var menuItem = new MenuItem(displayName);
                menuItem.Checked = !string.IsNullOrEmpty(currentPreferred) && 
                                   (hardwareId.IndexOf(currentPreferred, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                    currentPreferred.IndexOf(vidPidKey, StringComparison.OrdinalIgnoreCase) >= 0);
                menuItem.Enabled = !isAssignedToOther; // Disable if assigned to another player
                menuItem.Click += (s, e) => SetPreferredMouse(playerIndex, identifierToSave);
                mouseMenu.MenuItems.Add(menuItem);
            }
        }

        private static void PopulateKeyboardDeviceMenu(MenuItem keyboardMenu, int playerIndex)
        {
            // Check if VMulti Auto-Lock is active for this player (EN/FR: Vérifier si Auto-Lock VMulti est actif pour ce joueur)
            if (VMultiDeviceDetector.ShouldLockPlayerDevices(playerIndex))
            {
                var lockItem = new MenuItem($"🔒 Locked to VMulti Player {playerIndex}");
                lockItem.Enabled = false;
                keyboardMenu.MenuItems.Add(lockItem);
                return;
            }

            string currentPreferred = Options.Instance.GetPreferredKeyboardId(playerIndex);

            // Add "None (Auto)" option (EN/FR: Ajouter option "Aucun (Auto)")
            var noneItem = new MenuItem("None (Auto)");
            noneItem.Checked = string.IsNullOrEmpty(currentPreferred);
            noneItem.Click += (s, e) => SetPreferredKeyboard(playerIndex, "");
            keyboardMenu.MenuItems.Add(noneItem);
            keyboardMenu.MenuItems.Add("-");

            // Get all controllers to check which keyboards are currently active (EN/FR: Obtenir tous les contrôleurs pour vérifier quels claviers sont actifs)
            var controllers = _wiiMoteManager?.GetControllers();
            if (controllers == null || !controllers.Any())
            {
                var noDevItem = new MenuItem("No controllers connected");
                noDevItem.Enabled = false;
                keyboardMenu.MenuItems.Add(noDevItem);
                return;
            }

            // Get list of keyboards already assigned to other players (EN/FR: Liste des claviers déjà assignés)
            var assignedKeyboards = new List<string>();
            foreach (int p in new[] { 1, 2, 3, 4 })
            {
                if (p == playerIndex) continue;
                string otherPreferred = Options.Instance.GetPreferredKeyboardId(p);
                if (!string.IsNullOrEmpty(otherPreferred))
                    assignedKeyboards.Add(otherPreferred);
            }

            // Get available keyboards with names and hardware IDs (EN/FR: Obtenir claviers avec noms et IDs matériels)
            var availableKeyboards = VirtualInterceptionKeyboard.GetAvailableKeyboardsWithNames();
            var hardwareIds = new System.Collections.Generic.Dictionary<int, string>();
            
            // Get hardware ID for each keyboard (EN/FR: Récupérer ID matériel pour chaque clavier)
            foreach (int deviceId in availableKeyboards.Keys)
            {
                string hwId = VirtualInterceptionKeyboard.GetKeyboardHardwareId(deviceId);
                if (!string.IsNullOrEmpty(hwId))
                    hardwareIds[deviceId] = hwId;
            }
            
            if (availableKeyboards.Count == 0)
            {
                var noKbdItem = new MenuItem("No keyboards detected");
                noKbdItem.Enabled = false;
                keyboardMenu.MenuItems.Add(noKbdItem);
            }

            foreach (var kvp in availableKeyboards)
            {
                int deviceId = kvp.Key;
                string displayName = kvp.Value;
                
                // Get hardware ID for this keyboard (EN/FR: Récupérer ID matériel pour ce clavier)
                string hardwareId = hardwareIds.ContainsKey(deviceId) ? hardwareIds[deviceId] : null;
                if (string.IsNullOrEmpty(hardwareId))
                    continue; // Skip keyboards without valid hardware ID

                // Check if this keyboard is currently active for this player (EN/FR: Vérifier si ce clavier est actif pour ce joueur)
                var controller = controllers.FirstOrDefault(c => c.PlayerIndex == playerIndex);
                bool isActive = false;
                if (controller != null && controller.VirtualJoy is VirtualInterceptionKeyboard kbd)
                {
                     if (kbd.KeyboardDeviceId == deviceId)
                        isActive = true;
                }
                
                if (isActive)
                    displayName += " (Active)";

                // Check if assigned to another player (EN/FR: Vérifier si assigné à un autre joueur)
                bool isAssignedToOther = assignedKeyboards.Contains(hardwareId);

                var menuItem = new MenuItem(displayName);
                menuItem.Checked = !string.IsNullOrEmpty(currentPreferred) && currentPreferred == hardwareId;
                menuItem.Enabled = !isAssignedToOther; // Disable if assigned to another player
                menuItem.Click += (s, e) => SetPreferredKeyboard(playerIndex, hardwareId);
                keyboardMenu.MenuItems.Add(menuItem);
            }
        }

        private static void SetPreferredKeyboard(int playerIndex, string deviceId)
        {
            Options.Instance.SetPreferredKeyboardId(playerIndex, deviceId);
            SimpleLogger.Instance.Info($"Set preferred keyboard for Player {playerIndex}: {(string.IsNullOrEmpty(deviceId) ? "Auto" : deviceId)}");
            
            // Refresh keyboard assignment for active controller (EN/FR: Rafraîchir l'assignation clavier pour contrôleur actif)
            var controllers = _wiiMoteManager.GetControllers();
            var controller = controllers.FirstOrDefault(c => c.PlayerIndex == playerIndex);
            if (controller != null && controller.VirtualJoy is VirtualInterceptionKeyboard kbd)
            {
                kbd.RefreshDeviceId();
                SimpleLogger.Instance.Info($"Refreshed keyboard device assignment for Player {playerIndex}");
            }
        }

        private static void SetPreferredMouse(int playerIndex, string hardwareId)
        {
            Options.Instance.SetPreferredMouseId(playerIndex, hardwareId);
            SimpleLogger.Instance.Info($"Set preferred mouse for Player {playerIndex}: {(string.IsNullOrEmpty(hardwareId) ? "Auto" : hardwareId)}");
            
            // Refresh mouse assignment for all controllers (EN/FR: Rafraîchir l'assignation souris pour tous les contrôleurs)
            VirtualInterceptionMouse.RefreshDevices();
            SimpleLogger.Instance.Info($"Refreshed mouse device assignments");
        }

        private static void PopulateRumbleMenu(MenuItem rumbleMenu, int playerIndex)
        {
            // Enable Rumble checkbox (EN/FR: Case à cocher Activer Vibration)
            var enableItem = new MenuItem("✓ Enable Weapon Rumble");
            enableItem.Checked = Options.Instance.GetEnableWeaponRumble(playerIndex);
            enableItem.Click += (s, e) =>
            {
                bool newValue = !Options.Instance.GetEnableWeaponRumble(playerIndex);
                switch (playerIndex)
                {
                    case 1: Options.Instance.EnableWeaponRumble_P1 = newValue; break;
                    case 2: Options.Instance.EnableWeaponRumble_P2 = newValue; break;
                    case 3: Options.Instance.EnableWeaponRumble_P3 = newValue; break;
                    case 4: Options.Instance.EnableWeaponRumble_P4 = newValue; break;
                }
                Options.Instance.Save();
            };
            rumbleMenu.MenuItems.Add(enableItem);
            
            // Continuous Fire checkbox (EN/FR: Case à cocher Tir Continu)
            var continuousItem = new MenuItem("✓ Continuous Fire Rumble");
            continuousItem.Checked = Options.Instance.GetAllowContinuousRumble(playerIndex);
            continuousItem.Click += (s, e) =>
            {
                bool newValue = !Options.Instance.GetAllowContinuousRumble(playerIndex);
                switch (playerIndex)
                {
                    case 1: Options.Instance.AllowContinuousRumble_P1 = newValue; break;
                    case 2: Options.Instance.AllowContinuousRumble_P2 = newValue; break;
                    case 3: Options.Instance.AllowContinuousRumble_P3 = newValue; break;
                    case 4: Options.Instance.AllowContinuousRumble_P4 = newValue; break;
                }
                Options.Instance.Save();
            };
            rumbleMenu.MenuItems.Add(continuousItem);
            
            rumbleMenu.MenuItems.Add("-");
            
            // Intensity submenu (EN/FR: Sous-menu Intensité)
            var intensityMenu = new MenuItem("🔊 Intensity");
            int currentIntensity = Options.Instance.GetRumbleIntensity(playerIndex);
            foreach (int intensity in new[] { 55, 75, 100 })
            {
                var item = new MenuItem($"{intensity}%");
                item.Checked = (currentIntensity == intensity);
                item.Click += (s, e) =>
                {
                    switch (playerIndex)
                    {
                        case 1: Options.Instance.RumbleIntensity_P1 = intensity; break;
                        case 2: Options.Instance.RumbleIntensity_P2 = intensity; break;
                        case 3: Options.Instance.RumbleIntensity_P3 = intensity; break;
                        case 4: Options.Instance.RumbleIntensity_P4 = intensity; break;
                    }
                    Options.Instance.Save();
                };
                intensityMenu.MenuItems.Add(item);
            }
            rumbleMenu.MenuItems.Add(intensityMenu);
            
            // Duration submenu (EN/FR: Sous-menu Durée)
            var durationMenu = new MenuItem("⏱️ Duration");
            int currentDuration = Options.Instance.GetRumbleDurationMs(playerIndex);
            foreach (int duration in new[] { 40, 60, 80, 100, 150, 200 })
            {
                var item = new MenuItem($"{duration}ms");
                item.Checked = (currentDuration == duration);
                item.Click += (s, e) =>
                {
                    switch (playerIndex)
                    {
                        case 1: Options.Instance.RumbleDurationMs_P1 = duration; break;
                        case 2: Options.Instance.RumbleDurationMs_P2 = duration; break;
                        case 3: Options.Instance.RumbleDurationMs_P3 = duration; break;
                        case 4: Options.Instance.RumbleDurationMs_P4 = duration; break;
                    }
                    Options.Instance.Save();
                };
                durationMenu.MenuItems.Add(item);
            }
            rumbleMenu.MenuItems.Add(durationMenu);
            
            // Repetition Interval submenu (EN/FR: Sous-menu Intervalle Répétition)
            var repetitionMenu = new MenuItem("🔄 Repetition Interval");
            int currentRepetition = Options.Instance.GetRumbleRepetitionMs(playerIndex);
            foreach (int repetition in new[] { 50, 75, 100, 150, 200, 300 })
            {
                var item = new MenuItem($"{repetition}ms");
                item.Checked = (currentRepetition == repetition);
                item.Click += (s, e) =>
                {
                    switch (playerIndex)
                    {
                        case 1: Options.Instance.RumbleRepetitionMs_P1 = repetition; break;
                        case 2: Options.Instance.RumbleRepetitionMs_P2 = repetition; break;
                        case 3: Options.Instance.RumbleRepetitionMs_P3 = repetition; break;
                        case 4: Options.Instance.RumbleRepetitionMs_P4 = repetition; break;
                    }
                    Options.Instance.Save();
                };
                repetitionMenu.MenuItems.Add(item);
            }
            rumbleMenu.MenuItems.Add(repetitionMenu);
        }


        private static void SetPreferredWiimote(int playerIndex, string mac)
        {
            switch (playerIndex)
            {
                case 1: Options.Instance.PreferredMacP1 = mac; break;
                case 2: Options.Instance.PreferredMacP2 = mac; break;
                case 3: Options.Instance.PreferredMacP3 = mac; break;
                case 4: Options.Instance.PreferredMacP4 = mac; break;
            }
            Options.Instance.Save();
            
            string msg = string.IsNullOrEmpty(mac) 
                ? $"Player {playerIndex} preference cleared." 
                : $"Player {playerIndex} assigned to Wiimote {mac}.";
            
            msg += "\n\nChanges will take effect on next connection/restart.";
            MessageBox.Show(msg, "Wiimote Assignment", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static void SetConnectedState(bool connected)
        {
            PostToUIThread(() =>
            {
                _trayIcon.Icon = connected ? Properties.Resources.green : Properties.Resources.gray;
            });
        }

        private static void OnShowAbout(object sender, EventArgs e)
        {
            using (var frm = new AboutBox())
                frm.ShowDialog();
        }

        private static void OnShowOptions(object sender, EventArgs e)
        {
            using (var frm = new OptionsForm())
                frm.ShowDialog();
        }

        private static void OnShowButtonMapping(object sender, EventArgs e)
        {
            using (var frm = new MappingForm())
                frm.ShowDialog();
        }

        private static void OnShowIRVisualizer(object sender, EventArgs e)
        {
            // Single instance pattern: Only one IR Visualizer window at a time (EN/FR: Une seule fenêtre IR Visualizer à la fois)
            if (_irVisualizerForm != null && !_irVisualizerForm.IsDisposed)
            {
                // Bring existing window to front (EN/FR: Ramener fenêtre existante au premier plan)
                _irVisualizerForm.Activate();
                _irVisualizerForm.BringToFront();
                if (_irVisualizerForm.WindowState == FormWindowState.Minimized)
                {
                    _irVisualizerForm.WindowState = FormWindowState.Normal;
                }
                return;
            }

            // Create new instance and track it (EN/FR: Créer nouvelle instance et la suivre)
            _irVisualizerForm = new IRVisualizerForm();
            _irVisualizerForm.FormClosed += (s, args) => { _irVisualizerForm = null; }; // Clear reference when closed
            _irVisualizerForm.Show();
        }

        private static void OnRefreshRequested(object sender, EventArgs e)
        {
            SimpleLogger.Instance.Info("Configuration refresh requested via IPC");
            PostToUIThread(RefreshConfiguration);
        }

        private static void RefreshConfiguration()
        {
            try
            {
                SimpleLogger.Instance.Info("Configuration refresh requested - restarting application...");
                
                // Reload configuration from XML first (EN/FR: Recharger config depuis XML d'abord)
                Options.Load();
                SimpleLogger.Instance.Info("Configuration reloaded from file");

                // Get current executable path (EN/FR: Obtenir chemin executable actuel)
                string exePath = Application.ExecutablePath;
                
                // Build restart arguments (EN/FR: Construire arguments de redémarrage)
                string arguments = "";
                if (!string.IsNullOrEmpty(_activeRemapProfile))
                {
                    arguments = $"-remap \"{_activeRemapProfile}\"";
                    SimpleLogger.Instance.Info($"Restarting with remap profile: {_activeRemapProfile}");
                }
                
                // Create temporary batch file for restart (EN/FR: Créer fichier batch temporaire pour redémarrage)
                string tempBat = Path.Combine(Path.GetTempPath(), $"WiimoteGun_Restart_{Guid.NewGuid()}.bat");
                
                try
                {
                    using (StreamWriter writer = new StreamWriter(tempBat))
                    {
                        writer.WriteLine("@echo off");
                        writer.WriteLine(":: WiimoteGun Restart Script (EN/FR: Script redémarrage WiimoteGun)");
                        writer.WriteLine();
                        
                        // Get process name for waiting
                        string processName = Path.GetFileNameWithoutExtension(exePath);
                        
                        writer.WriteLine(":: Wait for WiimoteGun process to exit (EN/FR: Attendre sortie processus WiimoteGun)");
                        writer.WriteLine(":WAIT_LOOP");
                        writer.WriteLine($"tasklist /FI \"IMAGENAME eq {processName}.exe\" 2>NUL | find /I /N \"{processName}.exe\">NUL");
                        writer.WriteLine("if \"%ERRORLEVEL%\"==\"0\" (");
                        writer.WriteLine("    timeout /t 1 /nobreak >nul");
                        writer.WriteLine("    goto WAIT_LOOP");
                        writer.WriteLine(")");
                        writer.WriteLine();
                        writer.WriteLine(":: Extra safety delay (EN/FR: Délai sécurité supplémentaire)");
                        writer.WriteLine("timeout /t 1 /nobreak >nul");
                        writer.WriteLine();
                        writer.WriteLine(":: Start new WiimoteGun instance (EN/FR: Démarrer nouvelle instance)");
                        
                        // Use cd to change to exe directory to ensure wiimotegun command works
                        string exeDir = Path.GetDirectoryName(exePath);
                        writer.WriteLine($"cd /d \"{exeDir}\"");
                        
                        if (string.IsNullOrEmpty(arguments))
                        {
                            writer.WriteLine($"start \"\" \"{processName}.exe\"");
                        }
                        else
                        {
                            writer.WriteLine($"start \"\" \"{processName}.exe\" {arguments}");
                        }
                        
                        writer.WriteLine();
                        writer.WriteLine(":: Self-delete batch file (EN/FR: Auto-suppression du fichier batch)");
                        writer.WriteLine($"del /f /q \"{tempBat}\"");
                    }
                    
                    SimpleLogger.Instance.Info($"Created restart batch file: {tempBat}");
                    
                    // Start batch file hidden (EN/FR: Lancer fichier batch masqué)
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = tempBat,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        UseShellExecute = false
                    };
                    
                    Process.Start(psi);
                    SimpleLogger.Instance.Info("Restart batch file started");
                }
                catch (Exception batEx)
                {
                    SimpleLogger.Instance.Error($"Failed to create/start restart batch: {batEx.Message}");
                    
                    // Fallback to legacy direct restart (EN/FR: Repli sur redémarrage direct)
                    SimpleLogger.Instance.Info("Falling back to direct restart method");
                    if (string.IsNullOrEmpty(arguments))
                        Process.Start(exePath);
                    else
                        Process.Start(exePath, arguments);
                }
                
                // CRITICAL: Release mutex before exiting (EN/FR: CRITIQUE: Libérer mutex avant sortie)
                if (_singleInstanceMutex != null)
                {
                    _singleInstanceMutex.ReleaseMutex();
                    _singleInstanceMutex.Dispose();
                    _singleInstanceMutex = null;
                    SimpleLogger.Instance.Info("Mutex released");
                }
                
                SimpleLogger.Instance.Info("Exiting current instance for restart");
                
                // Exit current instance (EN/FR: Quitter instance actuelle)
                Application.Exit();
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Configuration refresh/restart failed: {ex.Message}");
                Notify($"Restart failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle remap profile load request from IPC (EN/FR: Gérer demande chargement profil remap depuis IPC)
        /// </summary>
        private static void OnRemapRequested(object sender, RemapRequestedEventArgs e)
        {
            try
            {
                SimpleLogger.Instance.Info($"OnRemapRequested called with profile: {e.ProfilePath}");
                
                // Set active remap profile and reload (EN/FR: Définir profil remap actif et recharger)
                _activeRemapProfile = e.ProfilePath;
                
                // Apply remap profile to current running instance (EN/FR: Appliquer profil remap à l'instance en cours)
                if (ApplyRemapProfile())
                {
                    // Save to settings so it persists (EN/FR: Sauvegarder pour persister)
                    Options.Instance.Save();
                    
                    SimpleLogger.Instance.Info($"Remap profile '{e.ProfilePath}' applied successfully to running instance");
                    // Notification is already handled inside ApplyRemapProfile (EN/FR: Notification déjà gérée dans ApplyRemapProfile)
                }
                else
                {
                    SimpleLogger.Instance.Error($"Failed to apply remap profile: {e.ProfilePath}");
                    Notify($"Failed to load remap profile: {System.IO.Path.GetFileName(e.ProfilePath)}");
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Failed to apply remap profile: {ex.Message}");
                Notify($"Failed to load remap profile: {ex.Message}");
            }
        }
         
        private static void OnExitClicked(object sender, EventArgs e)
        {
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }

            if (_wiiMoteManager != null)
            {
                _wiiMoteManager.Dispose();
                _wiiMoteManager = null;
            }

            Application.Exit();
        }

        private static SynchronizationContext _synchronizationContext;

        public static SynchronizationContext SynchronizationContext
        {
            get { return _synchronizationContext; }
        }


        public static void Notify(string text)
        {
            if (!Options.Instance.ShowNotifications || string.IsNullOrEmpty(text))
                return;

            PostToUIThread(() =>
            {
                var frm = new NotifyForm();
                frm.UpdateState(text);
                frm.Show();
            });
        }

        public static void PostToUIThread(System.Action a)
        {
            try
            {
                if (_synchronizationContext == null)
                    return;

                _synchronizationContext.Post(state =>
                {
                    try
                    {
                        a();
                    }
                    catch { }
                }, null);
            }
            catch { }
        }

        private static void ShowWelcomeDialog()
        {
            using (var dialog = new WelcomeDialog())
            {
                dialog.ShowDialog();
            }
        }

        /// <summary>
        /// Update PATH environment variable to include WiimoteGun directory
        /// (EN/FR: Mettre à jour variable PATH pour inclure dossier WiimoteGun)
        /// </summary>
        private static void UpdatePathEnvironmentVariable()
        {
            try
            {
                // Get current executable directory (EN/FR: Obtenir dossier exécutable actuel)
                string exeDir = System.IO.Path.GetDirectoryName(Application.ExecutablePath);
                
                // Get current user PATH variable (EN/FR: Obtenir variable PATH utilisateur actuel)
                string currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? "";
                
                // Check if WiimoteGun directory is already in PATH and if it's the correct one
                // (EN/FR: Vérifier si dossier WiimoteGun est déjà dans PATH et s'il est correct)
                string[] pathDirs = currentPath.Split(';');
                bool alreadyInPath = pathDirs.Any(dir => dir.Equals(exeDir, StringComparison.OrdinalIgnoreCase));
                
                // Find old WiimoteGun paths to remove (different directory)
                // (EN/FR: Trouver anciens chemins WiimoteGun à supprimer)
                var oldWiimoteGunPaths = pathDirs.Where(dir => 
                    dir.IndexOf("WiimoteGun", StringComparison.OrdinalIgnoreCase) >= 0 && 
                    !dir.Equals(exeDir, StringComparison.OrdinalIgnoreCase)
                ).ToList();
                
                if (alreadyInPath && oldWiimoteGunPaths.Count == 0)
                {
                    SimpleLogger.Instance.Info($"PATH already contains current WiimoteGun directory: {exeDir}");
                    return;
                }
                
                // Try to update PATH (may fail without admin on some systems)
                // (EN/FR: Tenter mise à jour PATH - peut échouer sans admin)
                try
                {
                    // Remove old WiimoteGun paths (EN/FR: Supprimer anciens chemins WiimoteGun)
                    if (oldWiimoteGunPaths.Count > 0)
                    {
                        foreach (var oldPath in oldWiimoteGunPaths)
                        {
                            SimpleLogger.Instance.Info($"Removing old WiimoteGun path from PATH: {oldPath}");
                            currentPath = currentPath.Replace(oldPath + ";", "").Replace(";" + oldPath, "").Replace(oldPath, "");
                        }
                    }
                    
                    // Add current directory to PATH if not already present (EN/FR: Ajouter dossier actuel si absent)
                    if (!alreadyInPath)
                    {
                        // Clean up any double semicolons (EN/FR: Nettoyer doubles points-virgules)
                        currentPath = currentPath.TrimEnd(';');
                        
                        // Add new path (EN/FR: Ajouter nouveau chemin)
                        string newPath = string.IsNullOrEmpty(currentPath) 
                            ? exeDir 
                            : currentPath + ";" + exeDir;
                        
                        Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.User);
                        SimpleLogger.Instance.Info($"Added WiimoteGun directory to user PATH: {exeDir}");
                        SimpleLogger.Instance.Info("Note: New PATH will be available in new processes after this session");
                    }
                    else
                    {
                        // Just update to remove old paths (EN/FR: Juste mettre à jour pour supprimer anciens chemins)
                        currentPath = currentPath.TrimEnd(';');
                        Environment.SetEnvironmentVariable("PATH", currentPath, EnvironmentVariableTarget.User);
                        SimpleLogger.Instance.Info("Updated PATH to remove old WiimoteGun directories");
                    }
                }
                catch (System.Security.SecurityException secEx)
                {
                    // Permission denied - silently skip PATH update
                    // (EN/FR: Permission refusée - ignorer silencieusement la mise à jour PATH)
                    SimpleLogger.Instance.Warning($"Insufficient permissions to update PATH: {secEx.Message}");
                    SimpleLogger.Instance.Info("PATH update skipped - WiimoteGun will still function normally");
                }
                catch (UnauthorizedAccessException authEx)
                {
                    // Access denied - silently skip PATH update
                    // (EN/FR: Accès refusé - ignorer silencieusement la mise à jour PATH)
                    SimpleLogger.Instance.Warning($"Access denied when updating PATH: {authEx.Message}");
                    SimpleLogger.Instance.Info("PATH update skipped - WiimoteGun will still function normally");
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Failed to update PATH environment variable: {ex.Message}");
                // Don't show error to user - PATH update is not critical for operation
                // (EN/FR: Ne pas afficher erreur - mise à jour PATH non critique)
            }
        }

        /// <summary>
        /// Parse -remap and -menu arguments from command line
        /// (EN/FR: Parser arguments -remap et -menu de la ligne de commande)
        /// </summary>
        private static void ParseRemapArgument(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                // Check for -remap argument (EN/FR: Vérifier argument -remap)
                if ((args[i].ToLower() == "-remap" || args[i].ToLower() == "/remap") && i < args.Length - 1)
                {
                    _activeRemapProfile = args[i + 1];
                    SimpleLogger.Instance.Info($"Remap profile argument detected: {_activeRemapProfile}");
                }
                
                // Check for -menu argument (EN/FR: Vérifier argument -menu)
                if (args[i].ToLower() == "-menu" || args[i].ToLower() == "/menu")
                {
                    _menuMode = true;
                    SimpleLogger.Instance.Info("Menu mode argument detected");
                }
            }
        }

        /// <summary>
        /// Load and apply remap profile based on priority
        /// Priority: -remap argument > default.remap > settings.cfg
        /// (EN/FR: Charger et appliquer profil remap selon priorité)
        /// </summary>
        public static bool ApplyRemapProfile()
        {
            SimpleLogger.Instance.Info("ApplyRemapProfile() called");
            SimpleLogger.Instance.Info($"_activeRemapProfile = '{_activeRemapProfile ?? "null"}'");
            
            RemapProfile profile = null;

            // Priority 1: -remap argument (EN/FR: Priorité 1 : argument -remap)
            if (!string.IsNullOrEmpty(_activeRemapProfile))
            {
                SimpleLogger.Instance.Info($"Loading remap profile from argument: {_activeRemapProfile}");
                
                try
                {
                    profile = RemapProfileManager.LoadProfile(_activeRemapProfile);
                    
                    if (profile != null)
                    {
                        SimpleLogger.Instance.Info($"Remap profile '{profile.ProfileName}' loaded successfully from argument");
                    }
                    else
                    {
                        SimpleLogger.Instance.Error($"Failed to load remap profile: {_activeRemapProfile} (LoadProfile returned null)");
                    }
                }
                catch (Exception ex)
                {
                    SimpleLogger.Instance.Error($"Exception loading remap profile: {ex.Message}");
                    SimpleLogger.Instance.Error($"Stack trace: {ex.StackTrace}");
                }
            }
            // Priority 2: default.remap (EN/FR: Priorité 2 : default.remap)
            else
            {
                // Check if Wiimotes are connected before loading default profile
                // (EN/FR: Vérifier si Wiimotes connectées avant chargement profil par défaut)
                if (_wiiMoteManager != null && _wiiMoteManager.ConnectedWiimotesCount == 0)
                {
                    SimpleLogger.Instance.Info("No Wiimotes connected, deferring default.remap load");
                    return false;
                }

                SimpleLogger.Instance.Info("No -remap argument, checking for default.remap");
                profile = RemapProfileManager.LoadDefaultProfile();
                
                // Mark default load as attempted so we don't retry endlessly in loop if it fails/doesn't exist
                // (EN/FR: Marquer comme tenté pour éviter boucle infinie si échec/inexistant)
                _defaultProfileLoadAttempted = true;
                
                if (profile != null)
                {
                    SimpleLogger.Instance.Info("Loaded default.remap");
                    // Set active profile name for tracking (EN/FR: Définir nom profil actif pour suivi)
                    _activeRemapProfile = "default.remap";
                }
                else
                {
                    SimpleLogger.Instance.Info("No default.remap found");
                }
            }

            // Apply profile if loaded (EN/FR: Appliquer le profil si chargé)
            if (profile != null)
            {
                SimpleLogger.Instance.Info($"Applying remap profile: {profile.ProfileName}");
                ApplyProfileToOptions(profile);
                
                // Notify user that profile was loaded (EN/FR: Notifier l'utilisateur du chargement)
                // Delay notification to avoid overlap with Wiimote connection notifications
                // (EN/FR: Retarder notification pour éviter chevauchement avec connexion Wiimote)
                string profileName = profile.ProfileName;
                string source = !string.IsNullOrEmpty(_activeRemapProfile) ? 
                    Path.GetFileNameWithoutExtension(_activeRemapProfile) : "default.remap";
                
                System.Threading.Timer notifyTimer = null;
                notifyTimer = new System.Threading.Timer(_ =>
                {
                    try
                    {
                        Notify($"Remap profile loaded: {profileName}");
                    }
                    catch { }
                    finally
                    {
                        notifyTimer?.Dispose();
                    }
                }, null, 2000, System.Threading.Timeout.Infinite); // 2 second delay
                
                return true;
            }
            else
            {
                SimpleLogger.Instance.Info("No remap profile loaded, using settings.cfg mappings");
                return false;
            }
        }

        /// <summary>
        /// Apply remap profile to current Options instance
        /// (EN/FR: Appliquer le profil remap à l'instance Options actuelle)
        /// </summary>
        private static void ApplyProfileToOptions(RemapProfile profile)
        {
            try
            {
                if (profile.P1Mappings != null)
                    Options.Instance.P1Mappings.CopyFrom(profile.P1Mappings);
                if (profile.P2Mappings != null)
                    Options.Instance.P2Mappings.CopyFrom(profile.P2Mappings);
                if (profile.P3Mappings != null)
                    Options.Instance.P3Mappings.CopyFrom(profile.P3Mappings);
                if (profile.P4Mappings != null)
                    Options.Instance.P4Mappings.CopyFrom(profile.P4Mappings);

                SimpleLogger.Instance.Info($"Applied remap profile: {profile.ProfileName}");
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Failed to apply remap profile: {ex.Message}");
            }
        }

        /// <summary>
        /// Get active remap profile path (for passing to -refresh)
        /// (EN/FR: Obtenir le chemin du profil remap actif)
        /// </summary>
        public static string GetActiveRemapProfile()
        {
            return _activeRemapProfile;
        }
        // Overlay and Auto-load fields (EN/FR: Champs Overlay et Auto-load)
        private static ProfileOverlay _profileOverlay;
        private static System.Threading.Timer _gameDetectionTimer;
        private static string _lastDetectedGame = null;

        static bool _manualProfileOverride = false; // Flag to prevent auto-load when user manually loaded a profile (EN/FR: Flag pour empêcher auto-load si chargement manuel)
        static string _autoLoadedGameExe = ""; // Track which game triggered auto-load (EN/FR: Suivre quel jeu a déclenché auto-load)
        static bool _defaultProfileLoadAttempted = false; // Track if we tried to load default profile (EN/FR: Suivre si tentative chargement profil défaut)
        private static int _lastDetectedProcessId = 0; // Track process ID instead of just exe name

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
        
        [DllImport("user32.dll")]
        private static extern bool IsZoomed(IntPtr hWnd);
        
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        
        private static bool IsFullscreen(IntPtr hWnd)
        {
            // Check if window is maximized or covers entire screen (EN/FR: Vérifier si maximisé ou plein écran)
            try
            {
                // Get window rectangle
                RECT windowRect;
                if (!GetWindowRect(hWnd, out windowRect))
                    return false;
                
                // Get screen dimensions
                var screen = System.Windows.Forms.Screen.FromHandle(hWnd);
                int screenWidth = screen.Bounds.Width;
                int screenHeight = screen.Bounds.Height;
                
                // Check if window covers the entire screen
                bool coversScreen = (windowRect.Left <= 0 && windowRect.Top <= 0 && 
                                   windowRect.Right >= screenWidth && windowRect.Bottom >= screenHeight);
                
                return coversScreen || IsZoomed(hWnd);
            }
            catch
            {
                return false;
            }
        }

        private static void CheckForGameProcesses(object state)
        {
            try
            {
                // Skip if no Wiimotes connected (EN/FR: Ignorer si aucune Wiimote connectée)
                if (_wiiMoteManager == null || _wiiMoteManager.ConnectedWiimotesCount == 0)
                    return;

                // Deferred default profile load (EN/FR: Chargement différé profil par défaut)
                if (string.IsNullOrEmpty(_activeRemapProfile) && !_manualProfileOverride && !_defaultProfileLoadAttempted)
                {
                    _defaultProfileLoadAttempted = true;
                    var profile = RemapProfileManager.LoadDefaultProfile();
                    if (profile != null)
                    {
                        SimpleLogger.Instance.Info("Loading deferred default.remap");
                        ApplyProfileToOptions(profile);
                        _activeRemapProfile = "default.remap";
                        
                        // Notify user (EN/FR: Notifier utilisateur)
                        _synchronizationContext.Post(_ => Notify("Default profile loaded"), null);
                    }
                }

                IntPtr hwnd = GetForegroundWindow();
                if (hwnd == IntPtr.Zero)
                {
                    // No foreground window, check if tracked process still exists
                    CheckTrackedProcessExit();
                    return;
                }

                uint processId;
                GetWindowThreadProcessId(hwnd, out processId);
                
                // Check if tracked process still exists (EN/FR: Vérifier si processus suivi existe toujours)
                if (_lastDetectedProcessId != 0 && _lastDetectedProcessId != processId)
                {
                    CheckTrackedProcessExit();
                }
                
                // Check if window size matches game-like resolution (EN/FR: Vérifier si taille fenêtre correspond à résolution jeu)
                // Use configured screen from Options (EN/FR: Utiliser écran configuré depuis Options)
                RECT windowRect;
                if (!GetWindowRect(hwnd, out windowRect))
                    return;
                    
                int windowWidth = windowRect.Right - windowRect.Left;
                int windowHeight = windowRect.Bottom - windowRect.Top;
                
                // Get configured screen index from Options (EN/FR: Obtenir index écran depuis Options)
                int screenIndex = Options.Instance.MonitorId;
                var screens = System.Windows.Forms.Screen.AllScreens;
                
                // Validate screen index (EN/FR: Valider index écran)
                if (screenIndex < 0 || screenIndex >= screens.Length)
                    screenIndex = 0; // Fallback to primary screen
                
                var targetScreen = screens[screenIndex];
                int screenWidth = targetScreen.Bounds.Width;
                int screenHeight = targetScreen.Bounds.Height;
                
                // Calculate coverage percentage (EN/FR: Calculer pourcentage couverture)
                // Allow 10% margin for borders/taskbars (EN/FR: Autoriser marge 10% pour bordures/barre tâches)
                float widthCoverage = (float)windowWidth / screenWidth;
                float heightCoverage = (float)windowHeight / screenHeight;
                
                // Consider it a game if window covers at least 85% of screen in both dimensions
                // (EN/FR: Considérer comme jeu si fenêtre couvre au moins 85% écran dans les deux dimensions)
                bool isGameLikeSize = widthCoverage >= 0.85f && heightCoverage >= 0.85f;
                
                // Also accept if window is very large (>= 1280x720) even if not matching screen
                // (EN/FR: Accepter aussi si fenêtre très grande même si pas taille écran)
                bool isLargeWindow = windowWidth >= 1280 && windowHeight >= 720;
                
                // Accept classic/retro game resolutions (640x480, 800x600, 1024x768, etc.)
                // (EN/FR: Accepter résolutions jeux classiques/rétro)
                bool isClassicGameResolution = windowWidth >= 640 && windowHeight >= 480 && 
                                               (windowWidth <= 1024 || windowHeight <= 768);
                
                string exePath = null;
                string exeName = null;

                try
                {
                    var process = System.Diagnostics.Process.GetProcessById((int)processId);
                    exePath = process.MainModule.FileName;
                    exeName = System.IO.Path.GetFileName(exePath);
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    // Access denied - try alternative method (QueryFullProcessImageName)
                    // (EN/FR: Accès refusé - essayer méthode alternative)
                    IntPtr hProcess = IntPtr.Zero;
                    try
                    {
                        hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
                        if (hProcess != IntPtr.Zero)
                        {
                            System.Text.StringBuilder buffer = new System.Text.StringBuilder(1024);
                            uint size = (uint)buffer.Capacity;
                            
                            if (QueryFullProcessImageName(hProcess, 0, buffer, ref size))
                            {
                                exePath = buffer.ToString();
                                exeName = System.IO.Path.GetFileName(exePath);
                            }
                        }
                    }
                    finally
                    {
                        if (hProcess != IntPtr.Zero)
                            CloseHandle(hProcess);
                    }
                }
                catch (InvalidOperationException)
                {
                    // Process exited
                    return;
                }
                catch (Exception)
                {
                    // Other errors
                    return;
                }


                if (!string.IsNullOrEmpty(exeName))
                {
                    // Ignore WiimoteGun itself
                    if (exeName.Equals("WiimoteGun.exe", StringComparison.OrdinalIgnoreCase))
                        return;

                    // Check if this is a new game (EN/FR: Vérifier si nouveau jeu)
                    if (exeName != _lastDetectedGame || _lastDetectedProcessId != processId)
                    {
                        _lastDetectedGame = exeName;
                        _lastDetectedProcessId = (int)processId;
                        
                        // Pass full path for strict matching (EN/FR: Passer chemin complet pour correspondance stricte)
                        string profilePath = GameProfileMappingManager.GetProfileForGame(exeName, exePath);
                        bool hasMapping = !string.IsNullOrEmpty(profilePath);

                        // Only apply size filters if no mapping exists
                        // (EN/FR: Appliquer filtres taille seulement si aucun mapping n'existe)
                        if (!hasMapping && !isGameLikeSize && !isLargeWindow && !isClassicGameResolution)
                        {
                            // Not a game-sized window and not mapped, skip (EN/FR: Pas taille jeu et pas mappé, ignorer)
                            return;
                        }
                        
                        SimpleLogger.Instance.Info($"Foreground changed to: {exeName} ({windowWidth}x{windowHeight}, Screen {screenIndex}: {screenWidth}x{screenHeight}, Coverage: {widthCoverage*100:F0}%x{heightCoverage*100:F0}%, Path: {exePath})");

                        if (hasMapping)
                        {
                            SimpleLogger.Instance.Info($"Found profile mapping: {exeName} -> {profilePath}");
                            // Found a mapping, auto-load it if not manually overridden
                            // (EN/FR: Mapping trouvé, chargement auto si pas remplacement manuel)
                            if (!_manualProfileOverride && _autoLoadedGameExe != exeName)
                            {
                                _synchronizationContext.Post(_ => 
                                {
                                    SimpleLogger.Instance.Info($"Auto-loading profile for {exeName}: {profilePath}");
                                    LoadRemapProfileHot(profilePath);
                                    _autoLoadedGameExe = exeName;
                                    Notify($"Profile auto-loaded for {exeName}");
                                }, null);
                            }
                            else
                            {
                                if (_manualProfileOverride)
                                {
                                    SimpleLogger.Instance.Info($"Skipping auto-load: Manual override active");
                                    // Notify user that auto-load is locked until restart (EN/FR: Notifier utilisateur que auto-load est verrouillé jusqu'au redémarrage)
                                    Notify("Profile manually loaded - Auto-load locked until WiimoteGun restart");
                                }
                                if (_autoLoadedGameExe == exeName)
                                    SimpleLogger.Instance.Info($"Skipping auto-load: Already loaded for {exeName}");
                            }
                        }
                        else
                        {
                            SimpleLogger.Instance.Info($"No profile mapping found for: {exeName} [{exePath}]");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Error in CheckForGameProcesses: {ex.Message}");
            }
        }
        
        private static void CheckTrackedProcessExit()
        {
            // Check if the tracked process has exited (EN/FR: Vérifier si processus suivi a quitté)
            if (_lastDetectedProcessId != 0)
            {
                try
                {
                    var process = System.Diagnostics.Process.GetProcessById(_lastDetectedProcessId);
                    // Process still exists, do nothing
                }
                catch
                {
                    // Process has exited, revert to default (EN/FR: Processus terminé, revenir par défaut)
                    if (_autoLoadedGameExe != null)
                    {
                        _synchronizationContext.Post(_ =>
                        {
                            SimpleLogger.Instance.Info($"Game {_autoLoadedGameExe} exited, reverting to default profile");
                            RevertToDefaultProfile();
                            _autoLoadedGameExe = null;
                            _lastDetectedProcessId = 0;
                            _lastDetectedGame = null;
                            _manualProfileOverride = false; // Reset override for next game
                        }, null);
                    }
                }
            }
        }

        public static void LoadRemapProfileHot(string profilePath, bool isManualLoad = false)
        {
            try
            {
                _activeRemapProfile = profilePath;
                ApplyRemapProfile();
                Options.Instance.Save();
                
                // If manual load, set override flag to prevent auto-reload
                // (EN/FR: Si chargement manuel, définir flag pour éviter rechargement auto)
                if (isManualLoad)
                {
                    _manualProfileOverride = true;
                }
                // Notification is handled in ApplyRemapProfile
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Failed to hot load profile: {ex.Message}");
            }
        }

        public static void RevertToDefaultProfile()
        {
            try
            {
                _activeRemapProfile = null; // Will cause ApplyRemapProfile to load default.remap or settings.cfg
                ApplyRemapProfile();
                Options.Instance.Save();
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Failed to revert profile: {ex.Message}");
            }
        }
        
        public static void SetManualProfileOverride()
        {
            // Called when user manually loads a profile from overlay
            // (EN/FR: Appelé quand utilisateur charge manuellement un profil)
            _manualProfileOverride = true;
        }
        private static void OpenWindowedOverlay()
        {
            // Close existing overlay if open (EN/FR: Fermer overlay existant si ouvert)
            if (_profileOverlay != null && !_profileOverlay.IsDisposed)
            {
                _profileOverlay.Close();
                _profileOverlay = null;
            }
            
            // Create new overlay in windowed mode (EN/FR: Créer nouvel overlay en mode fenêtré)
            SimpleLogger.Instance.Info("Opening ProfileOverlay in windowed mode");
            _profileOverlay = new ProfileOverlay(windowedMode: true);
            _profileOverlay.FormClosed += (s, e) => _profileOverlay = null;
            _profileOverlay.Show(); // Non-modal show (EN/FR: Affichage non modal)
            _profileOverlay.Activate(); // Bring to front (EN/FR: Mettre au premier plan)
        }

        private static void OnMenuRequested(object sender, EventArgs e)
        {
            // Handle IPC menu request (EN/FR: Gérer demande menu IPC)
            // Must run on UI thread (EN/FR: Doit s'exécuter sur thread UI)
            if (_synchronizationContext != null)
            {
                _synchronizationContext.Post(_ => 
                {
                    OpenWindowedOverlay();
                }, null);
            }
            else
            {
                // Fallback (should not happen if initialized correctly)
                OpenWindowedOverlay();
            }
        }
        private static void InstallInterceptionDriver()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string installerPath = Path.Combine(baseDir, "install-interception.exe");
                string workingDir = baseDir;

                if (!File.Exists(installerPath))
                {
                    // Try looking in Interception folder
                    installerPath = Path.Combine(baseDir, "Interception", "install-interception.exe");
                    workingDir = Path.Combine(baseDir, "Interception");
                }

                if (!File.Exists(installerPath))
                {
                    MessageBox.Show($"Interception installer not found: {installerPath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Copy to Temp to avoid Mapped Drive issues (Z:\ is not visible to Admin)
                string tempDir = Path.Combine(Path.GetTempPath(), "WiimoteGun_Interception");
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                CopyDirectory(workingDir, tempDir, true);

                string tempInstallerPath = Path.Combine(tempDir, Path.GetFileName(installerPath));

                var psi = new ProcessStartInfo
                {
                    FileName = tempInstallerPath,
                    WorkingDirectory = tempDir,
                    Arguments = "/install",
                    UseShellExecute = true,
                    Verb = "runas"
                };

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error installing Interception: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void UninstallInterceptionDriver()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string installerPath = Path.Combine(baseDir, "install-interception.exe");
                string workingDir = baseDir;

                if (!File.Exists(installerPath))
                {
                    // Try looking in Interception folder
                    installerPath = Path.Combine(baseDir, "Interception", "install-interception.exe");
                    workingDir = Path.Combine(baseDir, "Interception");
                }

                if (!File.Exists(installerPath))
                {
                    MessageBox.Show($"Interception installer not found: {installerPath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Copy to Temp
                string tempDir = Path.Combine(Path.GetTempPath(), "WiimoteGun_Interception");
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                CopyDirectory(workingDir, tempDir, true);

                string tempInstallerPath = Path.Combine(tempDir, Path.GetFileName(installerPath));

                var psi = new ProcessStartInfo
                {
                    FileName = tempInstallerPath,
                    WorkingDirectory = tempDir,
                    Arguments = "/uninstall",
                    UseShellExecute = true,
                    Verb = "runas"
                };

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error uninstalling Interception: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void InstallVMultiDriver(string driverDirName, string infFileName)
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string sourceDriverDir = Path.Combine(baseDir, "WiimoteGunDriver", driverDirName);
                string driverPath = Path.Combine(sourceDriverDir, infFileName);

                if (!File.Exists(driverPath))
                {
                    MessageBox.Show($"Driver file not found: {driverPath}\n\nPlease copy all driver files to WiimoteGunDriver/{driverDirName}/", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Verify required tools exist
                string devconPath = Path.Combine(sourceDriverDir, "devcon.exe");
                string difxPath = Path.Combine(sourceDriverDir, "DIFxCmd.exe");
                
                if (!File.Exists(devconPath) || !File.Exists(difxPath))
                {
                    MessageBox.Show($"Required tools (devcon.exe, DIFxCmd.exe) not found in {sourceDriverDir}\n\nPlease copy ALL files from the original driver folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Copy to Temp to ensure files are accessible
                string tempDir = Path.Combine(Path.GetTempPath(), "WiimoteGun_VMulti_" + driverDirName);
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                CopyDirectory(sourceDriverDir, tempDir, true);

                // Build Master Batch Script
                // We read the content of the existing .bat files (stripping their admin checks)
                // This ensures we execute EXACTLY what the user provided in the driver folder
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("@echo off");
                
                // NO UAC Check needed in Master Batch - we are already elevated by C# (Verb="runas")
                // (EN/FR: Pas de check UAC - déjà élevé par C#)
                
                sb.AppendLine($"title WiimoteGun - {driverDirName} Installation");
                sb.AppendLine("color 0A");
                sb.AppendLine("echo ===============================================================================");
                sb.AppendLine($"echo   WiimoteGun - {driverDirName} Installation");
                sb.AppendLine("echo ===============================================================================");
                sb.AppendLine("echo.");

                // 1. UNINSTALL (Read remove_hiddriver.bat)
                string removeScriptPath = Path.Combine(sourceDriverDir, "remove_hiddriver.bat");
                if (File.Exists(removeScriptPath))
                {
                    sb.AppendLine($"echo [Step 1/3] Uninstalling previous {driverDirName}...");
                    sb.AppendLine("echo -----------------------------------------------------------");
                    sb.AppendLine(GetBatchContentWithoutAdmin(removeScriptPath));
                    sb.AppendLine("echo.");
                }

                // 2. INSTALL (Read install_hiddriver.bat)
                string installScriptPath = Path.Combine(sourceDriverDir, "install_hiddriver.bat");
                if (File.Exists(installScriptPath))
                {
                    sb.AppendLine($"echo [Step 2/3] Installing new {driverDirName}...");
                    sb.AppendLine("echo -----------------------------------------------------------");
                    sb.AppendLine(GetBatchContentWithoutAdmin(installScriptPath));
                    sb.AppendLine("if %errorlevel% neq 0 ( color 0C & echo [ERROR] Installation failed! & pause & exit /b %errorlevel% )");
                    sb.AppendLine("echo.");
                }

                // 3. CLEANUP (Read Cleanup-virtualX.bat)
                string cleanupScriptPath = Path.Combine(sourceDriverDir, $"Cleanup-{driverDirName}.bat");
                if (File.Exists(cleanupScriptPath))
                {
                    sb.AppendLine($"echo [Step 3/3] Cleaning up unused devices...");
                    sb.AppendLine("echo -----------------------------------------------------------");
                    sb.AppendLine(GetBatchContentWithoutAdmin(cleanupScriptPath));
                    sb.AppendLine("echo.");
                }

                sb.AppendLine("echo ===============================================================================");
                sb.AppendLine("echo   [SUCCESS] Installation and Cleanup Complete!");
                sb.AppendLine("echo ===============================================================================");
                sb.AppendLine("pause");

                string masterBatchPath = Path.Combine(tempDir, "install_complete.bat");
                File.WriteAllText(masterBatchPath, sb.ToString());

                // Run the master batch script
                var psi = new ProcessStartInfo
                {
                    FileName = masterBatchPath,
                    UseShellExecute = true,
                    Verb = "runas", // Ensure admin rights for this master launch
                    WorkingDirectory = tempDir
                };

                Process.Start(psi);
                
                // We don't need to show a MessageBox here because the batch script has a pause and shows success
                // But we can show a small notification that the process started
                // MessageBox.Show($"{driverDirName} installation started.", "WiimoteGun", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void UninstallVMultiDriver(string driverDirName)
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string sourceDriverDir = Path.Combine(baseDir, "WiimoteGunDriver", driverDirName);
                string removeScriptPath = Path.Combine(sourceDriverDir, "remove_hiddriver.bat");

                if (!File.Exists(removeScriptPath))
                {
                    MessageBox.Show($"Remove script not found: {removeScriptPath}\n\nPlease ensure the WiimoteGunDriver/{driverDirName} folder is complete.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Copy to Temp to ensure files are accessible
                string tempDir = Path.Combine(Path.GetTempPath(), "WiimoteGun_VMulti_Uninstall_" + driverDirName);
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                CopyDirectory(sourceDriverDir, tempDir, true);

                // Build Uninstall Batch Script
                // We read the content of the existing .bat files (stripping their admin checks)
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("@echo off");
                
                // NO UAC Check needed in Master Batch - we are already elevated by C# (Verb="runas")
                // (EN/FR: Pas de check UAC - déjà élevé par C#)

                sb.AppendLine($"title WiimoteGun - Uninstall {driverDirName}");
                sb.AppendLine("color 0A");
                sb.AppendLine($"echo Uninstalling {driverDirName}...");
                sb.AppendLine("echo.");
                sb.AppendLine("echo [Step 1/1] Running remove script...");
                sb.AppendLine("echo -----------------------------------------------------------");
                sb.AppendLine(GetBatchContentWithoutAdmin(removeScriptPath));
                sb.AppendLine("echo.");
                sb.AppendLine("echo ===============================================================================");
                sb.AppendLine("echo   [SUCCESS] Uninstallation Complete!");
                sb.AppendLine("echo ===============================================================================");
                sb.AppendLine("pause");

                string tempBatchPath = Path.Combine(tempDir, "uninstall_complete.bat");
                File.WriteAllText(tempBatchPath, sb.ToString());

                // Run the batch script
                var psi = new ProcessStartInfo
                {
                    FileName = tempBatchPath,
                    WorkingDirectory = tempDir,
                    UseShellExecute = true,
                    Verb = "runas"
                };

                Process.Start(psi);
                
                // MessageBox.Show($"{driverDirName} uninstallation started.", "WiimoteGun", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Helper to read batch file content, stripping Admin headers and pauses (EN/FR: Lire contenu batch sans admin/pauses)
        /// </summary>
        private static string GetBatchContentWithoutAdmin(string batchPath)
        {
            if (!File.Exists(batchPath)) return "";

            var lines = File.ReadAllLines(batchPath);
            var content = new StringBuilder();
            
            // Find the separator line that ends the admin block
            int separatorIndex = -1;
            for(int i = 0; i < Math.Min(lines.Length, 30); i++)
            {
                if (lines[i].Trim().StartsWith(":--------------------------------------"))
                {
                    separatorIndex = i;
                    break;
                }
            }

            for (int i = 0; i < lines.Length; i++)
            {
                // If we found a separator, skip everything up to and including it
                if (separatorIndex >= 0 && i <= separatorIndex) continue;

                var line = lines[i];
                // Remove pauses so we control flow in master script
                if (line.Trim().Equals("pause", StringComparison.OrdinalIgnoreCase) || 
                    line.Trim().Equals("@pause", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                content.AppendLine(line);
            }
            return content.ToString();
        }

        private static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
        {
            var dir = new DirectoryInfo(sourceDir);
            if (!dir.Exists) throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

            Directory.CreateDirectory(destinationDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath, true);
            }

            if (recursive)
            {
                foreach (DirectoryInfo subDir in dir.GetDirectories())
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }
        private static bool IsServiceInstalled(string serviceName)
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
    }
}
