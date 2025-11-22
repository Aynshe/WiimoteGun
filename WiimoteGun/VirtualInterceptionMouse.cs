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
        
        private int _mouseDevice;
        private int _playerId;
        private string _deviceName;

        // Screen resolution for absolute positioning (EN/FR: Résolution d'écran pour positionnement absolu)
        private const int SCREEN_WIDTH = 65535;
        private const int SCREEN_HEIGHT = 65535;

        // Current mouse state (EN/FR: État actuel de la souris)
        private bool _leftButtonPressed = false;
        private bool _rightButtonPressed = false;
        private bool _middleButtonPressed = false;

        public VirtualInterceptionMouse(int playerIndex, string uniqueId)
        {
            _playerId = playerIndex;
            _deviceName = string.Format("WiimoteGun_{0}_P{1}", uniqueId, playerIndex);
            
            lock (_lock)
            {
                // Initialize Interception context on first instance (EN/FR: Initialiser le contexte Interception à la première instance)
                if (_context == IntPtr.Zero)
                {
                    _context = InterceptionDriver.interception_create_context();
                    if (_context == IntPtr.Zero)
                    {
                        throw new Exception("Failed to create Interception context. Is the driver installed?");
                    }
                    SimpleLogger.Instance.Info("Interception context created successfully");
                }

                _instanceCount++;
                
                // Each player gets a unique mouse device ID (EN/FR: Chaque joueur obtient un ID de périphérique souris unique)
                // Mouse devices are 11-20 in Interception (EN/FR: Les périphériques souris sont 11-20 dans Interception)
                _mouseDevice = 10 + playerIndex;
                
                SimpleLogger.Instance.Info(string.Format("Initializing Interception mouse: {0} (Device ID: {1})", _deviceName, _mouseDevice));
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
