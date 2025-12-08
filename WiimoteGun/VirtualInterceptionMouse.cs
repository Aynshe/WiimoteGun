using System;
using WiimoteGun.Interception;

namespace WiimoteGun
{
    /// <summary>
    /// Virtual mouse using Interception driver - supports multiple independent instances
    /// FR: Souris virtuelle utilisant le pilote Interception - supporte plusieurs instances indépendantes
    /// </summary>
    class VirtualInterceptionMouse : IVirtualMouse
    {
        private static IntPtr _context;
        private static int _instanceCount = 0;
        private static readonly object _lock = new object();
        private static System.Collections.Generic.List<VirtualInterceptionMouse> _instances = new System.Collections.Generic.List<VirtualInterceptionMouse>();
        private static System.Collections.Generic.List<int> _availableMice = new System.Collections.Generic.List<int>();
        
        private int _mouseDevice;
        private int _playerId;
        private string _deviceName;

        // Public property to access current mouse device ID (EN/FR: Propriété publique pour accéder à l'ID souris actuel)
        public int MouseDeviceId => _mouseDevice;

        // Screen resolution for absolute positioning (EN/FR: Résolution d'écran pour positionnement absolu)
        private const int SCREEN_WIDTH = 65535;
        private const int SCREEN_HEIGHT = 65535;

        // Current mouse state (EN/FR: État actuel de la souris)
        private bool _leftButtonPressed = false;
        private bool _rightButtonPressed = false;
        private bool _middleButtonPressed = false;

        // Delegate for weapon rumble trigger (EN/FR: Délégué pour déclenchement vibration arme)
        public Action<bool> OnLeftMouseButtonChanged;

        public VirtualInterceptionMouse(int playerIndex, string uniqueId)
        {
            _playerId = playerIndex;
            _deviceName = string.Format("WiimoteGun_{0}_P{1}", uniqueId, playerIndex);
            
            lock (_lock)
            {
                _instances.Add(this);

                // Initialize Interception context on first instance (EN/FR: Initialiser le contexte Interception à la première instance)
                if (_context == IntPtr.Zero)
                {
                    _context = InterceptionDriver.interception_create_context();
                    if (_context == IntPtr.Zero)
                    {
                        throw new Exception("Failed to create Interception context. Is the driver installed?");
                    }
                    SimpleLogger.Instance.Info("Interception context created successfully");
                    ScanMice();
                }

                _instanceCount++;
                
                UpdateDeviceId();
            }
        }

        public static void RefreshDevices()
        {
            lock (_lock)
            {
                SimpleLogger.Instance.Info("Refreshing Interception Mouse Devices...");
                ScanMice();
                foreach (var instance in _instances)
                {
                    instance.UpdateDeviceId();
                }
            }
        }

        private static void ScanMice()
        {
            _availableMice.Clear();
            // Mice are usually 11-20
            for (int i = 11; i <= 20; i++)
            {
                if (InterceptionDriver.interception_is_mouse(i) != 0)
                {
                    _availableMice.Add(i);
                    SimpleLogger.Instance.Info($"Detected Mouse Device ID: {i}");
                }
            }
            SimpleLogger.Instance.Info($"Using {_availableMice.Count} mice for up to 4 players");
        }

        private void UpdateDeviceId()
        {
            // Check for preferred Hardware ID (VID/PID) (EN/FR: Vérifier ID Matériel préféré)
            string preferredId = Options.Instance.GetPreferredMouseId(_playerId);
            bool assigned = false;

            if (!string.IsNullOrEmpty(preferredId) && _availableMice.Count > 0)
            {
                foreach (int deviceId in _availableMice)
                {
                    byte[] buffer = new byte[1000];
                    uint result = InterceptionDriver.interception_get_hardware_id(_context, deviceId, buffer, (uint)buffer.Length);
                    if (result > 0)
                    {
                        int byteCount = Math.Min((int)result * 2, buffer.Length);
                        string hardwareId = System.Text.Encoding.Unicode.GetString(buffer, 0, byteCount).TrimEnd('\0');
                        
                        // Use DeviceHelper for partial/fuzzy matching (EN/FR: Utiliser DeviceHelper pour matching partiel)
                        if (DeviceHelper.IsHardwareIdMatch(preferredId, hardwareId))
                        {
                            _mouseDevice = deviceId;
                            SimpleLogger.Instance.Info($"[MOUSE BINDING] Player {_playerId} bound to Hardware ID '{preferredId}' → Device ID {_mouseDevice}");
                            SimpleLogger.Instance.Debug($"[MOUSE BINDING] Matched: '{preferredId}' ≈ '{hardwareId}'");
                            assigned = true;
                            break;
                        }
                    }
                }
                
                if (!assigned)
                {
                    SimpleLogger.Instance.Warning($"[MOUSE BINDING] Player {_playerId} preferred Hardware ID '{preferredId}' NOT FOUND. Falling back to default assignment.");
                }
            }

            if (!assigned)
            {
                // Assign device ID based on player index and available mice (EN/FR: Assigner l'ID selon le joueur et les souris disponibles)
                if (_availableMice.Count > 0)
                {
                    int mouseIndex = (_playerId - 1) % _availableMice.Count;
                    _mouseDevice = _availableMice[mouseIndex];
                    SimpleLogger.Instance.Info($"Assigned Mouse Device ID {_mouseDevice} to Player {_playerId}");
                }
                else
                {
                    // Fallback to hardcoded ID if no mice detected
                    _mouseDevice = 10 + _playerId;
                    SimpleLogger.Instance.Warning($"No mice detected via Interception. Defaulting Player {_playerId} to Device ID {_mouseDevice}.");
                }
            }
        }

        /// <summary>
        /// Update mouse position and button states (EN/FR: Mettre à jour la position et les boutons de la souris)
        /// </summary>
        public void UpdateMouse(int x, int y, bool leftButton, bool rightButton, bool middleButton, bool moveCursor = true)
        {
            if (_context == IntPtr.Zero)
                return;

            try
            {
                // Handle button state changes (EN/FR: Gérer les changements d'état des boutons)
                SendButtonEvents(leftButton, rightButton, middleButton);

                // Send mouse movement in absolute coordinates only if requested (EN/FR: Envoyer le mouvement seulement si demandé)
                if (moveCursor)
                {
                    SendMouseMove(x, y);
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error(string.Format("Failed to update Interception mouse P{0}: {1}", _playerId, ex.Message));
            }
        }

        private void SendButtonEvents(bool left, bool right, bool middle)
        {
            // Left button (EN/FR: Bouton gauche)
            if (left != _leftButtonPressed)
            {
                SendButtonEvent(left ? InterceptionMouseState.LeftButtonDown : InterceptionMouseState.LeftButtonUp);
                _leftButtonPressed = left;
                
                // Notify controller for rumble (EN/FR: Notifier controller pour vibration)
                OnLeftMouseButtonChanged?.Invoke(left);
            }

            // Right button (EN/FR: Bouton droit)
            if (right != _rightButtonPressed)
            {
                SendButtonEvent(right ? InterceptionMouseState.RightButtonDown : InterceptionMouseState.RightButtonUp);
                _rightButtonPressed = right;
            }

            // Middle button (EN/FR: Bouton du milieu)
            if (middle != _middleButtonPressed)
            {
                SendButtonEvent(middle ? InterceptionMouseState.MiddleButtonDown : InterceptionMouseState.MiddleButtonUp);
                _middleButtonPressed = middle;
            }
        }

        private void SendButtonEvent(InterceptionMouseState state)
        {
            InterceptionMouseStroke stroke = new InterceptionMouseStroke
            {
                state = state,
                flags = 0,
                rolling = 0,
                x = 0,
                y = 0,
                information = 0
            };

            InterceptionDriver.interception_send(_context, _mouseDevice, ref stroke, 1);
        }

        private void SendMouseMove(int x, int y)
        {
            // Clamp coordinates (EN/FR: Limiter les coordonnées)
            x = Math.Max(0, Math.Min(SCREEN_WIDTH, x));
            y = Math.Max(0, Math.Min(SCREEN_HEIGHT, y));

            InterceptionMouseStroke stroke = new InterceptionMouseStroke
            {
                state = InterceptionMouseState.Move,
                flags = InterceptionMouseFlag.MoveAbsolute,
                rolling = 0,
                x = x,
                y = y,
                information = 0
            };

            InterceptionDriver.interception_send(_context, _mouseDevice, ref stroke, 1);
        }

        public void Dispose()
        {
            lock (_lock)
            {
                SimpleLogger.Instance.Info(string.Format("Disconnecting Interception mouse for player {0}.", _playerId));
                
                _instanceCount--;
                
                // Destroy context when last instance is disposed (EN/FR: Détruire le contexte quand la dernière instance est libérée)
                if (_instanceCount == 0 && _context != IntPtr.Zero)
                {
                    InterceptionDriver.interception_destroy_context(_context);
                    _context = IntPtr.Zero;
                    SimpleLogger.Instance.Info("Interception context destroyed");
                }
            }
        }
    }
}
