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
using System.Drawing;
using WiimoteGun.Forms;
using WiimoteGun.UI.Legacy;
using WiimoteGun.UI;
using WiimoteGun.UI.Calibrate;
using WiimoteGun.Core;

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

        [System.Runtime.InteropServices.DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")]
        private static extern int TimeBeginPeriod(int msec);

        [System.Runtime.InteropServices.DllImport("winmm.dll", EntryPoint = "timeEndPeriod")]
        private static extern int TimeEndPeriod(int msec);

        private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        static NotifyIcon _trayIcon;
        static WiimoteControllerManager _wiiMoteManager;
        public static WiimoteControllerManager WiiMoteManager { get { return _wiiMoteManager; } }
        static ApplicationContext _appContext;
        static Mutex _singleInstanceMutex;
        static IRVisualizerForm _irVisualizerForm; // Single instance of IR Visualizer (EN/FR: Instance unique IR Visualizer)
        static MessageWindow _messageWindow; // IPC window for -refresh command (EN/FR: Fenêtre IPC pour commande -refresh)
        static string _activeRemapProfile = null; // Active remap profile path (EN/FR: Chemin du profil remap actif)
        static string _activeGamePadProfile = null; // Active GamePad profile path (EN/FR: Chemin du profil GamePad actif)
        static bool _menuMode = false; // Menu mode flag for windowed overlay (EN/FR: Drapeau mode menu pour overlay fenêtré)
        
        static string _lastDetectedGame = null;
        static string _lastDetectedGamePath = null;
        static int _lastDetectedProcessId = 0;
        static string _autoLoadedGameExe = null;
        static string _autoLoadedGamePadExe = null;
        static bool _manualProfileOverride = false;
        static bool _manualGamePadProfileOverride = false;
        static bool _defaultProfileLoadAttempted = false;
        static System.Threading.Timer _gameDetectionTimer;
        static ProfileOverlay _profileOverlay;
        static WindowsFormsSynchronizationContext _synchronizationContext;
        static int _lastActiveScreenIndex = -1; // Last screen aimed by a Wiimote (EN/FR: Dernier écran visé par une Wiimote)

        public static int LastActiveScreenIndex { get { return _lastActiveScreenIndex; } set { _lastActiveScreenIndex = value; } }
        public static bool IsRestarting = false; // Flag to distinguish between exit and restart (EN/FR: Flag pour distinguer sortie et redémarrage)

        public static string LastDetectedGameName { get { return _lastDetectedGame; } }
        public static string LastDetectedGamePath { get { return _lastDetectedGamePath; } }

        [STAThread]
        static void Main(string[] args)
        {
            // Increase timer resolution for Virtual Polling (EN/FR: Augmenter résolution timer pour Polling Virtuel)
            TimeBeginPeriod(1);
            
            try 
            {
                MainExecution(args);
            }
            finally
            {
                TimeEndPeriod(1);
            }
        }

        static void MainExecution(string[] args)
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
                SimpleLogger.Instance.Info(string.Format("-remap argument detected with profile: {0}, attempting to send to running instance", _activeRemapProfile));
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
                     MessageBox.Show("Interception driver is no longer supported.", "WiimoteGun", MessageBoxButtons.OK, MessageBoxIcon.Information);
                     return;
                }
                if (args[0] == "/uninstallDrivers")
                {
                     MessageBox.Show("Interception driver is no longer supported.", "WiimoteGun", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            SimpleLogger.Instance.Info(string.Format("WiimoteGun startup (v{0})", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version));

            // EN: Register this process with the service for crash/exit monitoring EARLY
            // FR: Enregistrer ce processus auprès du service pour la surveillance crash/sortie PRÉCOCEMENT
            // Doing this early prevents race conditions during Hot Restart where Wiimotes connect
            // and enable themselves BEFORE the registration wipes the service state.
            ServiceClient.RegisterClient();

            // EN: Check for service updates (Stop -> Replace -> Start) if a newer version is packaged.
            // FR: Vérifier les mises à jour du service (Arrêt -> Remplacement -> Démarrage) si une version plus récente est incluse.
            ServiceClient.CheckAndPromptServiceUpdate();

            // Update PATH environment variable if needed (EN/FR: Mettre à jour variable PATH si nécessaire)
            UpdatePathEnvironmentVariable();

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool showOptionsAfterSetup = false;
            // First Run / Setup Checker
            if (WiimoteGun.Options.Instance.ShowSetupWizard)
            {
                using (var wizard = new Forms.SetupWizard())
                {
                    DialogResult res = wizard.ShowDialog();
                    if (res == DialogResult.OK || res == DialogResult.Ignore)
                    {
                        showOptionsAfterSetup = true;
                    }
                }
            }

            _appContext = new ApplicationContext();
            // Clean up unwanted VMulti collections (non-COL03) at startup
            // (EN/FR: Nettoyer collections VMulti non désirées (non-COL03) au démarrage)
            Core.VMultiDeviceCleanup.RemoveUnwantedCollections();
            
            // NOTE: Do NOT call RemoveAllMiceAtStartup() here!
            // (EN/FR: NE PAS appeler RemoveAllMiceAtStartup() ici!)
            // If Wiimotes are already connected via Bluetooth, REMOVE_MOUSE_ALL would kill their
            // devices before ENABLE_P* can complete. The deferred cleanup (ScheduleCleanupAfterWiimoteConnect)
            // will handle cleanup correctly using REMOVE_MOUSE_EXCEPT for connected players.

            _wiiMoteManager = new WiimoteControllerManager();
            _synchronizationContext = new WindowsFormsSynchronizationContext();

            // Create message window for IPC (EN/FR: Créer fenêtre message pour IPC)
            _messageWindow = new MessageWindow();
            _messageWindow.RefreshRequested += OnRefreshRequested;
            _messageWindow.RemapRequested += OnRemapRequested;
            _messageWindow.MenuRequested += OnMenuRequested;
            _messageWindow.DeviceChanged += (s, e) => _wiiMoteManager?.RefreshAllDInputIndices();
            
            // Initialize Overlay (EN/FR: Initialiser Overlay)
            _profileOverlay = new ProfileOverlay(_menuMode);
            
            // Initialize Hotkey Manager (EN/FR: Initialiser gestionnaire hotkeys)
            HotkeyManager.Initialize();
            // Connect overlay state to hotkey manager (EN/FR: Connecter état overlay)
            // Fix: Only block hotkeys in Windowed Mode if the form is actually Active/Focused
            // (EN/FR: Correction : Bloquer hotkeys en mode fenêtré SEULEMENT si la fenêtre est active)
            HotkeyManager.IsOverlayOpen = () => 
            {
                if (_profileOverlay == null || !_profileOverlay.Visible) return false;
                
                // If Windowed Mode (Menu), only block if it's the Active Form (Focused)
                // This allows hotkeys to work in-game even if the Menu window is open in the background
                if (_profileOverlay.IsWindowedMode)
                {
                    return Form.ActiveForm == _profileOverlay;
                }
                
                // Fullscreen Overlay always blocks inputs (it's a modal game menu)
                return true;
            };
            
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
                        PositionOverlayOnTargetScreen(_profileOverlay);
                        _profileOverlay.Show();
                    }
                    else if (_profileOverlay != null)
                    {
                        // Normal toggle for fullscreen overlay (EN/FR: Bascule normale pour overlay plein écran)
                        if (_profileOverlay.Visible)
                            _profileOverlay.Hide();
                        else
                        {
                            PositionOverlayOnTargetScreen(_profileOverlay);
                            _profileOverlay.Show();
                        }
                    }
                    else
                    {
                        // Create new fullscreen overlay if null (EN/FR: Créer nouvel overlay plein écran si null)
                        _profileOverlay = new ProfileOverlay(windowedMode: false);
                        _profileOverlay.FormClosed += (sender, evtArgs) => _profileOverlay = null;
                        PositionOverlayOnTargetScreen(_profileOverlay);
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
            
            // Enable persistent gamepads if option is enabled to stabilize DInput indices
            // (EN/FR: Activer les gamepads persistants si l'option est activée pour stabiliser les indices DInput)
            if (Options.Instance.PersistentGamePads && Options.Instance.EnableGamePadSwapMode)
            {
                SimpleLogger.Instance.Info("[Startup] Persistent GamePads enabled. Pre-enabling 4 gamepads...");
                for (int i = 1; i <= 4; i++)
                {
                    ServiceClient.EnableGamepad(i);
                }
            }
            // Cleanup redundant startup gamepads removal (already handled by RemoveUnwantedCollections or Service Registration)
            // (EN/FR: Suppression du nettoyage redondant des gamepads au démarrage)
            /* 
            else
            {
                // If persistent gamepads are disabled, ensure they are removed from the system at startup
                // (EN/FR: Si les gamepads persistants sont désactivés, s'assurer qu'ils sont retirés du système au démarrage)
                SimpleLogger.Instance.Info("[Startup] Persistent GamePads disabled or Global GamePad mode off. Cleaning up virtual gamepads...");
                for (int i = 1; i <= 4; i++)
                {
                    ServiceClient.RemoveGamepad(i);
                }
            }
            */

            // Clean up unwanted VMulti collections to prevent them from appearing in emulators
            // (EN/FR: Nettoyer les collections VMulti non désirées pour éviter qu'elles n'apparaissent dans les émulateurs)
            // Cleanup logic moved to before WiimoteManager initialization (line 240 approx)
            // (EN/FR: Logique de nettoyage déplacée avant l'initialisation de WiimoteManager)

            // Client registration moved to early startup (line ~225)
            // (EN/FR: Enregistrement client déplacé au démarrage précoce)
            
            // Deferred fallback cleanup: If no Wiimotes connect within 5 seconds, clean up all COL03 devices
            // (EN/FR: Nettoyage différé de secours : Si aucune Wiimote ne se connecte dans les 5 secondes, nettoyer tous les COL03)
            // This handles the case where WiimoteGun starts but no Wiimotes are present or they fail to connect.
            System.Threading.Tasks.Task.Delay(5000).ContinueWith(_ =>
            {
                if (_wiiMoteManager != null && _wiiMoteManager.ConnectedWiimotesCount == 0)
                {
                    SimpleLogger.Instance.Info("[Startup Fallback] No Wiimotes connected after 5 seconds. Cleaning up all COL03 devices.");
                    Core.VMultiDeviceCleanup.RemoveAllMiceAtStartup();
                }
                else
                {
                    int count = 0;
                    if (_wiiMoteManager != null) count = _wiiMoteManager.ConnectedWiimotesCount;
                    SimpleLogger.Instance.Info(string.Format("[Startup Fallback] {0} Wiimotes connected. Skipping full cleanup.", count));
                }
            });

            // CRITICAL: Apply remap profile AFTER Options loaded but BEFORE WiimoteControllerManager starts
            // (EN/FR: CRITIQUE : Appliquer profil remap APRÈS Options mais AVANT démarrage WiimoteControllerManager)
            // CRITICAL: Apply remap profile AFTER Options loaded but BEFORE WiimoteControllerManager starts
            // (EN/FR: CRITIQUE : Appliquer profil remap APRÈS Options mais AVANT démarrage WiimoteControllerManager)
            ApplyRemapProfile();

            // Initialize In-Game Offset Adjustment Overlay (EN/FR: Initialiser overlay ajustement offset en jeu)
            // The overlay is singleton and auto-shows/hides via WiiMoteController events
            var offsetOverlay = WiimoteGun.UI.Modern.Forms.OffsetAdjustmentOverlay.Instance;
            offsetOverlay.Initialize(); // Subscribe to events on UI thread (EN/FR: S'abonner aux événements sur thread UI)

            // Open windowed overlay if requested via -menu argument or after SetupWizard
            // (EN/FR: Ouvrir l'overlay fenêtré si demandé via argument -menu ou après le SetupWizard)
            if (_menuMode || showOptionsAfterSetup)
            {
                // Use timer to allow message loop to start first (EN/FR: Utiliser timer pour laisser boucle message démarrer)
                System.Threading.Timer menuTimer = null;
                menuTimer = new System.Threading.Timer(_ => 
                {
                    PostToUIThread(() =>
                    {
                        OpenWindowedOverlay();
                    });
                    menuTimer?.Dispose();
                }, null, 500, System.Threading.Timeout.Infinite);
            }
            // Ensure cleanup happens on any application exit (EN/FR: Assurer le nettoyage lors de toute sortie de l'application)
            Application.ApplicationExit += OnApplicationExit;
           
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
            
            // Interception commands removed
            if (command == "/installdrivers" || command == "/uninstalldrivers")
            {
                 MessageBox.Show("Interception driver is no longer supported. Please use VMulti.", "WiimoteGun", MessageBoxButtons.OK, MessageBoxIcon.Information);
                 return;
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
            _trayIcon.Text = "Wiimote4Guns";
            _trayIcon.DoubleClick += (s, e) => OpenWindowedOverlay();

            var menuItems = new MenuItem[]
            {
                new MenuItem("&Options", (s, e) => OpenWindowedOverlay()),
                new MenuItem("-"),
                new MenuItem("&About", OnShowAbout),
                new MenuItem("-"),
                new MenuItem("&Exit", OnExitClicked)
            };

            _trayIcon.ContextMenu = new ContextMenu(menuItems);
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
            Application.Exit();
        }

        private static void OnApplicationExit(object sender, EventArgs e)
        {
            // EN: Unregister client to prevent Service from accidentally cleaning up devices on slow exit/restart
            // FR: Désinscrire le client pour éviter que le Service ne nettoie accidentellement les pilotes lors d'une fermeture lente ou d'un redémarrage
            try
            {
                ServiceClient.UnregisterClient(IsRestarting);
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Error unregistering IPC client: {ex.Message}");
            }

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
        }


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
                    _activeRemapProfile = args[i + 1]?.Replace('\\', '/');
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
                
                // EN: Always attempt to load default GamePad profile when a remap profile (mouse) is loaded
                // FR: Toujours essayer de charger le profil GamePad par défaut quand un profil remap (souris) est chargé
                RevertToDefaultGamePadProfile();
                
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
                SimpleLogger.Instance.Info("No remap profile found, reverting to Factory Default (Mouse/IR)");
                
                // EN: Force factory default mappings if no .remap file exists
                // FR: Forcer les mappings d'usine si aucun fichier .remap n'existe
                ApplyProfileToOptions(RemapProfile.GetFactoryDefault());
                
                // EN: Also try to load default GamePad profile
                // FR: Essayer aussi de charger le profil GamePad par défaut
                RevertToDefaultGamePadProfile();
                
                return false;
            }
        }

        /// <summary>
        /// EN: Apply a GamePad profile to current Options instance.
        /// FR: Appliquer un profil GamePad à l'instance Options actuelle.
        /// </summary>
        public static void ApplyGamePadProfileToOptions(GamePadProfile profile)
        {
            if (profile == null) return;
            
            Options.Instance.P1GamePadMappings.CopyFrom(profile.P1Mappings);
            Options.Instance.P2GamePadMappings.CopyFrom(profile.P2Mappings);
            Options.Instance.P3GamePadMappings.CopyFrom(profile.P3Mappings);
            Options.Instance.P4GamePadMappings.CopyFrom(profile.P4Mappings);
            
            SimpleLogger.Instance.Info($"Applied GamePad profile: {profile.ProfileName}");
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

                // Apply Hotkeys to Options AND HotkeyManager (EN/FR: Appliquer Hotkeys aux Options ET HotkeyManager)
                // P1
                var hotkeysP1 = profile.P1Hotkeys ?? new List<Hotkey>();
                Options.Instance.HotkeyProfileP1 = new HotkeyProfile(1) { Hotkeys = hotkeysP1.Select(h => h.Clone()).ToList() };
                HotkeyManager.SetProfile(1, Options.Instance.HotkeyProfileP1);

                // P2
                var hotkeysP2 = profile.P2Hotkeys ?? new List<Hotkey>();
                Options.Instance.HotkeyProfileP2 = new HotkeyProfile(2) { Hotkeys = hotkeysP2.Select(h => h.Clone()).ToList() };
                HotkeyManager.SetProfile(2, Options.Instance.HotkeyProfileP2);

                // P3
                var hotkeysP3 = profile.P3Hotkeys ?? new List<Hotkey>();
                Options.Instance.HotkeyProfileP3 = new HotkeyProfile(3) { Hotkeys = hotkeysP3.Select(h => h.Clone()).ToList() };
                HotkeyManager.SetProfile(3, Options.Instance.HotkeyProfileP3);

                // P4
                var hotkeysP4 = profile.P4Hotkeys ?? new List<Hotkey>();
                Options.Instance.HotkeyProfileP4 = new HotkeyProfile(4) { Hotkeys = hotkeysP4.Select(h => h.Clone()).ToList() };
                HotkeyManager.SetProfile(4, Options.Instance.HotkeyProfileP4);
                
                // CRITICAL: Clear active hotkey state to prevent stuck modification
                // (EN/FR: CRITIQUE : Effacer état hotkey actif pour éviter blocage)
                HotkeyManager.ClearActiveState();

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

        /// <summary>
        /// Get active gamepad profile name (EN/FR: Obtenir le nom du profil gamepad actif)
        /// </summary>
        public static string GetActiveGamePadProfileName()
        {
            if (!string.IsNullOrEmpty(_activeGamePadProfile))
            {
                return System.IO.Path.GetFileNameWithoutExtension(_activeGamePadProfile);
            }
            return "Default";
        }

        // Overlay and Auto-load fields (EN/FR: Champs Overlay et Auto-load)
        // Overlay and Auto-load fields are now defined at the top of the class

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
                    // ApplyRemapProfile handles the logic and connected count check
                    // (EN/FR: ApplyRemapProfile gère la logique et le check du nombre de connectés)
                    ApplyRemapProfile();
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
                        _lastDetectedGamePath = exePath; // Store path (EN/FR: Sauvegarder chemin)
                        _lastDetectedProcessId = (int)processId;
                        
                        // Pass full path for strict matching (EN/FR: Passer chemin complet pour correspondance stricte)
                        string profilePath = GameProfileMappingManager.GetProfileForGame(exeName, exePath);
                        bool hasMapping = !string.IsNullOrEmpty(profilePath);

                        // Prevent auto-load if editing (EN/FR: Empêcher chargement auto si édition en cours)
                        if (_profileOverlay != null && _profileOverlay.IsEditing)
                        {
                            SimpleLogger.Instance.Info($"Checking game: {exeName} - Editing active, skipping profile switch.");
                            return; 
                        }

                        // Only apply size filters if no mapping exists
                        // (EN/FR: Appliquer filtres taille seulement si aucun mapping n'existe)
                        if (!hasMapping && !isGameLikeSize && !isLargeWindow && !isClassicGameResolution)
                        {
                            // EN: If focus lost to non-game and we had something auto-loaded, REVERT.
                            // FR: Si perte de focus vers un non-jeu et qu'on avait un autoload, REVENIR.
                            if (_autoLoadedGameExe != null || _autoLoadedGamePadExe != null)
                            {
                                SimpleLogger.Instance.Info($"Focus lost to non-game/unmapped window: {exeName} (Size: {windowWidth}x{windowHeight}). Reverting profile...");
                                _synchronizationContext.Post(_ => 
                                {
                                     // Reset tracked state before revert (EN/FR: Reset état avant réversion)
                                    _autoLoadedGameExe = null;
                                    _autoLoadedGamePadExe = null;
                                    _lastDetectedGame = exeName;
                                    _lastDetectedProcessId = (int)processId;
                                    _lastDetectedGamePath = exePath;
                                    _manualProfileOverride = false;
                                    _manualGamePadProfileOverride = false;
                                    RevertToDefaultProfile();
                                }, null);
                            }
                            else
                            {
                                // Just update tracking so we don't spam checking the same non-game window
                                // (EN/FR: Juste mettre à jour le suivi pour ne pas scanner en boucle la même fenêtre non-jeu)
                                _lastDetectedGame = exeName;
                                _lastDetectedGamePath = exePath;
                                _lastDetectedProcessId = (int)processId;
                            }
                            return;
                        }
                        
                        SimpleLogger.Instance.Info($"Foreground changed to: {exeName} ({windowWidth}x{windowHeight}, Screen {screenIndex}: {screenWidth}x{screenHeight}, Coverage: {widthCoverage*100:F0}%x{heightCoverage*100:F0}%, Path: {exePath})");

                        // =========================================================================================
                        // MOUSE REMAP PROFILE LOGIC
                        // =========================================================================================

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

                        // =========================================================================================
                        // GAMEPAD PROFILE LOGIC (EN/FR: LOGIQUE PROFIL GAMEPAD)
                        // =========================================================================================
                        
                        string gamePadProfilePath = GameProfileMappingManager.GetGamePadProfileForGame(exeName, exePath);
                        bool hasGamePadMapping = !string.IsNullOrEmpty(gamePadProfilePath);

                        if (hasGamePadMapping)
                        {
                            SimpleLogger.Instance.Info($"Found GamePad mapping: {exeName} -> {gamePadProfilePath}");
                            
                            if (!_manualGamePadProfileOverride && _autoLoadedGamePadExe != exeName)
                            {
                                 _synchronizationContext.Post(_ => 
                                {
                                    SimpleLogger.Instance.Info($"Auto-loading GamePad profile for {exeName}: {gamePadProfilePath}");
                                    LoadGamePadProfileHot(gamePadProfilePath);
                                    _autoLoadedGamePadExe = exeName;
                                    string profileName = System.IO.Path.GetFileNameWithoutExtension(gamePadProfilePath);
                                    Notify($"GamePad Profile auto-loaded for {exeName}: {profileName}");
                                }, null);
                            }
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
                    if (_autoLoadedGameExe != null || _autoLoadedGamePadExe != null || _lastDetectedProcessId != 0)
                    {
                        _synchronizationContext.Post(_ =>
                        {
                            string oldGame = _lastDetectedGame;
                            _autoLoadedGameExe = null;
                            _autoLoadedGamePadExe = null;
                            _lastDetectedProcessId = 0;
                            _lastDetectedGame = null;
                            _lastDetectedGamePath = null;
                            _manualProfileOverride = false; // Reset override for next game
                            _manualGamePadProfileOverride = false;
                            
                            // Only revert if we are NOT editing (EN/FR: Revenir seulement si pas en édition)
                            if (_profileOverlay != null && _profileOverlay.IsEditing)
                            {
                                SimpleLogger.Instance.Info($"Process {oldGame} exited, but Editing active - NOT reverting profile.");
                            }
                            else
                            {
                                SimpleLogger.Instance.Info($"Process {oldGame} exited, reverting to default profile");
                                RevertToDefaultProfile();
                                // RevertToDefaultGamePadProfile is called inside RevertToDefaultProfile
                            }
                        }, null);
                    }
                }
            }
        }

        public static void LoadRemapProfileHot(string profilePath, bool isManualLoad = false)
        {
            try
            {
                SimpleLogger.Instance.Info($"Hot loading profile: {profilePath} (Manual: {isManualLoad})");
                // Normalize path to forward slashes for internal consistency (EN/FR: Normaliser pour cohérence interne)
                _activeRemapProfile = profilePath?.Replace('\\', '/');
                
                // Force Apply
                if (ApplyRemapProfile())
                {
                    Options.Instance.Save();
                }
                

                // If manual load, set override flag
                // (EN/FR: Si chargement manuel, définir flag)
                if (isManualLoad)
                {
                    _manualProfileOverride = true;
                }
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
                
                // EN: ApplyRemapProfile now handles both Mouse/IR and GamePad default profiles
                // FR: ApplyRemapProfile gère maintenant les profils par défaut Mouse/IR et GamePad
                ApplyRemapProfile();
                
                Options.Instance.Save();
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Failed to revert profile: {ex.Message}");
            }
        }
        
        public static void LoadGamePadProfileHot(string profilePath, bool isManualLoad = false)
        {
            try
            {
                SimpleLogger.Instance.Info($"Hot loading GamePad profile: {profilePath} (Manual: {isManualLoad})");
                _activeGamePadProfile = profilePath?.Replace('\\', '/');
                
                var profile = RemapProfileManager.LoadGamePadProfile(profilePath);
                if (profile != null)
                {
                    ApplyGamePadProfileToOptions(profile);
                    
                    // Note: We don't have a specific "Apply" method for GamePads because they are polled directly from Options
                    // but we should ensure saving if needed, or just keep in memory until exit?
                    // Better to save to ensure consistency.
                    Options.Instance.Save();
                    
                    if (isManualLoad) _manualGamePadProfileOverride = true;
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Failed to hot load GamePad profile: {ex.Message}");
            }
        }

        public static void RevertToDefaultGamePadProfile()
        {
            try
            {
                _activeGamePadProfile = null;
                var profile = RemapProfileManager.LoadDefaultGamePadProfile();
                if (profile != null)
                {
                    SimpleLogger.Instance.Info("Reverting to default GamePad profile from: default.remap");
                    ApplyGamePadProfileToOptions(profile);
                    Options.Instance.Save();
                }
                else
                {
                    SimpleLogger.Instance.Info("No default.remap (GamePad) found, reverting to Factory Default");
                    ApplyGamePadProfileToOptions(new GamePadProfile { ProfileName = "Factory Default" });
                    Options.Instance.Save();
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Failed to revert GamePad profile: {ex.Message}");
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
            PositionOverlayOnTargetScreen(_profileOverlay);
            _profileOverlay.Show(); // Non-modal show (EN/FR: Affichage non modal)
            _profileOverlay.Activate(); // Bring to front (EN/FR: Mettre au premier plan)
        }

        /// <summary>
        /// Centers the form on the last aimed screen or current MonitorId screen.
        /// (EN/FR: Centre le formulaire sur le dernier écran visé ou celui du MonitorId actuel.)
        /// </summary>
        private static void PositionOverlayOnTargetScreen(Form form)
        {
            if (form == null) return;

            int targetIndex = _lastActiveScreenIndex;
            if (targetIndex < 0) targetIndex = Options.Instance.MonitorId;

            if (targetIndex >= 0 && targetIndex < Screen.AllScreens.Length)
            {
                Screen target = Screen.AllScreens[targetIndex];
                SimpleLogger.Instance.Info(string.Format("Positioning overlay on Screen {0} ({1})", targetIndex, target.DeviceName));

                form.StartPosition = FormStartPosition.Manual;
                form.Location = new Point(
                    target.Bounds.Left + (target.Bounds.Width - form.Width) / 2,
                    target.Bounds.Top + (target.Bounds.Height - form.Height) / 2
                );
            }
            else
            {
                form.StartPosition = FormStartPosition.CenterScreen;
            }
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
