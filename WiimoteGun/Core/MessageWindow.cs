using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WiimoteGun
{
    /// <summary>
    /// Hidden window for receiving inter-process messages (EN/FR: Fenêtre cachée pour recevoir messages inter-processus)
    /// </summary>
    internal class MessageWindow : NativeWindow, IDisposable
    {
        // Custom message IDs (EN/FR: IDs de messages personnalisés)
        public const int WM_REFRESH_CONFIG = 0x8001; // WM_USER + 1
        public const int WM_REMAP = 0x8002;          // WM_USER + 2
        public const int WM_MENU = 0x8003;           // WM_USER + 3
        
        // System message IDs (EN/FR: IDs de messages système)
        public const int WM_DEVICECHANGE = 0x0219;
        public const int DBT_DEVNODES_CHANGED = 0x0007;

        // Window class name for finding the window (EN/FR: Nom de classe pour trouver la fenêtre)
        private const string WINDOW_CLASS_NAME = "WiimoteGunMessageWindow_{71916996-F0A0-434C-88CA-41A62B4F9E17}";

        public event EventHandler RefreshRequested;
        public event EventHandler<RemapRequestedEventArgs> RemapRequested;
        public event EventHandler MenuRequested;
        public event EventHandler DeviceChanged; // Triggered on hotplug (EN/FR: Déclenché au hotplug)

        public MessageWindow()
        {
            CreateParams cp = new CreateParams
            {
                Caption = WINDOW_CLASS_NAME,
                // Don't specify ClassName - let Windows create default class (EN/FR: Ne pas spécifier ClassName - laisser Windows créer classe par défaut)
                Style = 0, // Hidden window
                ExStyle = 0
            };

            CreateHandle(cp);
            SimpleLogger.Instance.Info($"MessageWindow created with handle: {Handle}");
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_REFRESH_CONFIG)
            {
                SimpleLogger.Instance.Info("WM_REFRESH_CONFIG received, triggering refresh event");
                RefreshRequested?.Invoke(this, EventArgs.Empty);
                m.Result = (IntPtr)1; // Signal success
                return;
            }
            else if (m.Msg == WM_REMAP)
            {
                SimpleLogger.Instance.Info("WM_REMAP received, reading profile path from temp file");
                
                try
                {
                    // Read profile path from temporary file (EN/FR: Lire chemin profil depuis fichier temporaire)
                    string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "WiimoteGun_RemapProfile.tmp");
                    if (System.IO.File.Exists(tempFile))
                    {
                        string profilePath = System.IO.File.ReadAllText(tempFile);
                        SimpleLogger.Instance.Info($"Remap profile path from IPC: {profilePath}");
                        
                        RemapRequested?.Invoke(this, new RemapRequestedEventArgs(profilePath));
                        m.Result = (IntPtr)1; // Signal success
                        
                        // Clean up temp file (EN/FR: Nettoyer fichier temporaire)
                        System.IO.File.Delete(tempFile);
                    }
                    else
                    {
                        SimpleLogger.Instance.Error("WM_REMAP received but temp file not found");
                        m.Result = (IntPtr)0; // Signal failure
                    }
                }
                catch (Exception ex)
                {
                    SimpleLogger.Instance.Error($"Error processing WM_REMAP: {ex.Message}");
                    m.Result = (IntPtr)0; // Signal failure
                }
                return;
            }
            else if (m.Msg == WM_MENU)
            {
                SimpleLogger.Instance.Info("WM_MENU received, triggering menu event");
                MenuRequested?.Invoke(this, EventArgs.Empty);
                m.Result = (IntPtr)1; // Signal success
                return;
            }
            else if (m.Msg == WM_DEVICECHANGE)
            {
                // DBT_DEVNODES_CHANGED is enough for general controller plug/unplug (EN/FR: DBT_DEVNODES_CHANGED suffit pour branchement/débranchement)
                if ((int)m.WParam == DBT_DEVNODES_CHANGED)
                {
                    SimpleLogger.Instance.Debug("WM_DEVICECHANGE (DBT_DEVNODES_CHANGED) received, triggering device changed event");
                    DeviceChanged?.Invoke(this, EventArgs.Empty);
                }
            }

            base.WndProc(ref m);
        }

        public void Dispose()
        {
            DestroyHandle();
        }

        // Static method to find and message a running instance (EN/FR: Méthode statique pour trouver et envoyer message)
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        public static bool SendRefreshToRunningInstance()
        {
            // Find window by caption only (class is null for default NativeWindow) (EN/FR: Trouver fenêtre par caption uniquement)
            IntPtr hWnd = FindWindow(null, WINDOW_CLASS_NAME);
            if (hWnd == IntPtr.Zero)
            {
                SimpleLogger.Instance.Warning("No running WiimoteGun instance found");
                return false;
            }

            SimpleLogger.Instance.Info($"Found running instance at handle: {hWnd}, sending refresh message");
            IntPtr result = SendMessage(hWnd, WM_REFRESH_CONFIG,IntPtr.Zero, IntPtr.Zero);
            return result == (IntPtr)1;
        }

        public static bool SendRemapToRunningInstance(string profilePath)
        {
            // Find window by caption only (EN/FR: Trouver fenêtre par caption uniquement)
            IntPtr hWnd = FindWindow(null, WINDOW_CLASS_NAME);
            if (hWnd == IntPtr.Zero)
            {
                SimpleLogger.Instance.Warning("No running WiimoteGun instance found for remap");
                return false;
            }

            try
            {
                // Write profile path to temporary file (EN/FR: Écrire chemin profil dans fichier temporaire)
                string tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "WiimoteGun_RemapProfile.tmp");
                System.IO.File.WriteAllText(tempFile, profilePath);
                
                SimpleLogger.Instance.Info($"Found running instance at handle: {hWnd}, sending remap message with profile: {profilePath}");
                IntPtr result = SendMessage(hWnd, WM_REMAP, IntPtr.Zero, IntPtr.Zero);
                return result == (IntPtr)1;
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"Failed to send remap message: {ex.Message}");
                return false;
            }
        }

        public static bool SendMenuToRunningInstance()
        {
            // Find window by caption only (EN/FR: Trouver fenêtre par caption uniquement)
            IntPtr hWnd = FindWindow(null, WINDOW_CLASS_NAME);
            if (hWnd == IntPtr.Zero)
            {
                SimpleLogger.Instance.Warning("No running WiimoteGun instance found for menu");
                return false;
            }

            SimpleLogger.Instance.Info($"Found running instance at handle: {hWnd}, sending menu message");
            IntPtr result = SendMessage(hWnd, WM_MENU, IntPtr.Zero, IntPtr.Zero);
            return result == (IntPtr)1;
        }
    }

    /// <summary>
    /// Event args for remap requested event (EN/FR: Arguments pour événement remap demandé)
    /// </summary>
    public class RemapRequestedEventArgs : EventArgs
    {
        public string ProfilePath { get; }

        public RemapRequestedEventArgs(string profilePath)
        {
            ProfilePath = profilePath;
        }
    }
}
