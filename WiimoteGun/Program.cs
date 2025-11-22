using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Linq;

namespace WiimoteGun
{
    static class Program
    {
        static NotifyIcon _trayIcon;
        static WiimoteControllerManager _wiiMoteManager;
        static ApplicationContext _appContext;
        static Mutex _singleInstanceMutex;

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                HandleDriverCommands(args);
                return;
            }

            bool createdNew;
            _singleInstanceMutex = new Mutex(true, "WiimoteGun {71916996-F0A0-434C-88CA-41A62B4F9E17}", out createdNew);
            if (!createdNew)
                return;

            SimpleLogger.Instance.Info("---------------------------------------------------------------");
            SimpleLogger.Instance.Info("WiimoteGun startup");

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            _appContext = new ApplicationContext();
            _wiiMoteManager = new WiimoteControllerManager();
            _synchronizationContext = new WindowsFormsSynchronizationContext();

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
            // Non-modal to allow interaction with other windows if needed, 
            // but using ShowDialog is also fine if we want to block tray interaction.
            // Let's use Show() but keep a reference if we wanted single instance, 
            // but here using() block implies modal or short lived. 
            // The user requested "open the window", implying it stays open.
            // ShowDialog() blocks the UI thread which might block Wiimote events if they are dispatched on UI thread?
            // Wiimote events are usually on their own thread or threadpool.
            // However, Program.cs Main runs Application.Run(_appContext).
            // If we use ShowDialog, it runs its own message loop.
            
            // Better to just show it.
            var frm = new IRVisualizerForm();
            frm.Show();
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
            if (!Options.Instance.ShowNotifications)
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
    }
}
