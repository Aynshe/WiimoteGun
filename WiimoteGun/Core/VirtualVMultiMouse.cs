using System;
using WiimoteGun.VMulti;

namespace WiimoteGun
{
    /// <summary>
    /// Virtual mouse using VMulti driver - Direct HID communication without Interception
    /// (FR: Souris virtuelle utilisant le pilote VMulti - Communication HID directe sans Interception)
    /// </summary>
    class VirtualVMultiMouse : IVirtualMouse
    {
        private VMultiClient _client;
        private int _playerIndex;
        private string _deviceName;
        private bool _disposed = false;

        // Screen resolution for absolute positioning (EN/FR: Résolution d'écran pour positionnement absolu)
        // VMulti uses 0-32767 range (EN/FR: VMulti utilise la plage 0-32767)
        private const int SCREEN_MAX = 32767;
        
        // Conversion from 65535 range to 32767 (EN/FR: Conversion de la plage 65535 à 32767)
        private const double SCALE_FACTOR = (double)SCREEN_MAX / 65535.0;

        // Current mouse state (EN/FR: État actuel de la souris)
        private bool _leftButtonPressed = false;
        private bool _rightButtonPressed = false;
        private bool _middleButtonPressed = false;

        // Delegate for weapon rumble trigger (EN/FR: Délégué pour déclenchement vibration arme)
        public Action<bool> OnLeftMouseButtonChanged;

        /// <summary>
        /// Player index (1-4) (EN/FR: Index du joueur)
        /// </summary>
        public int PlayerIndex => _playerIndex;

        /// <summary>
        /// Connection status (EN/FR: Statut de connexion)
        /// </summary>
        public bool IsConnected => _client != null && _client.IsConnected;

        public VirtualVMultiMouse(int playerIndex, string uniqueId)
        {
            _playerIndex = playerIndex;
            _deviceName = string.Format("WiimoteGun_{0}_P{1}", uniqueId, playerIndex);

            SimpleLogger.Instance.Info($"[VMultiMouse] Creating VMulti mouse for Player {playerIndex}");

            try
            {
                _client = VMultiClient.GetSharedClient(playerIndex);
                
                if (_client.Connect())
                {
                    SimpleLogger.Instance.Info($"[VMultiMouse] Connected successfully for P{playerIndex}");
                }
                else
                {
                    SimpleLogger.Instance.Warning($"[VMultiMouse] Could not connect initially for P{playerIndex}. Will retry on first use.");
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"[VMultiMouse] Failed to create VMulti client for P{playerIndex}: {ex.Message}");
            }
        }

        /// <summary>
        /// Update mouse position and button states
        /// (EN/FR: Mettre à jour la position et les boutons de la souris)
        /// </summary>
        /// <param name="x">X coordinate (0-65535)</param>
        /// <param name="y">Y coordinate (0-65535)</param>
        /// <param name="leftButton">Left button state</param>
        /// <param name="rightButton">Right button state</param>
        /// <param name="middleButton">Middle button state</param>
        /// <param name="moveCursor">Whether to move cursor (EN/FR: Si on doit déplacer le curseur)</param>
        public void UpdateMouse(int x, int y, bool leftButton, bool rightButton, bool middleButton, bool moveCursor = true, bool isAbsolute = true)
        {
            if (_client == null || _disposed)
                return;

            try
            {
                // Handle button state changes (EN/FR: Gérer les changements d'état des boutons)
                bool buttonChanged = (leftButton != _leftButtonPressed) || 
                                    (rightButton != _rightButtonPressed) || 
                                    (middleButton != _middleButtonPressed);

                // Notify controller for rumble on left button change (EN/FR: Notifier controller pour vibration)
                if (leftButton != _leftButtonPressed)
                {
                    OnLeftMouseButtonChanged?.Invoke(leftButton);
                }

                // Update stored states (EN/FR: Mettre à jour les états stockés)
                _leftButtonPressed = leftButton;
                _rightButtonPressed = rightButton;
                _middleButtonPressed = middleButton;

                // Build button flags (EN/FR: Construire les flags des boutons)
                VMultiMouseButton buttons = VMultiMouseButton.None;
                if (leftButton) buttons |= VMultiMouseButton.Left;
                if (rightButton) buttons |= VMultiMouseButton.Right;
                if (middleButton) buttons |= VMultiMouseButton.Middle;

                // Send if moving cursor or buttons changed (EN/FR: Envoyer si déplacement curseur ou boutons changés)
                if (moveCursor || buttonChanged)
                {
                    if (isAbsolute)
                    {
                        // Clamp and scale coordinates (EN/FR: Limiter et mettre à l'échelle les coordonnées)
                        int absX = Math.Max(0, Math.Min(65535, x));
                        int absY = Math.Max(0, Math.Min(65535, y));

                        // Convert to VMulti range (0-32767) (EN/FR: Convertir à la plage VMulti)
                        ushort vmultiX = (ushort)(absX * SCALE_FACTOR);
                        ushort vmultiY = (ushort)(absY * SCALE_FACTOR);

                        _client.UpdateMouse(vmultiX, vmultiY, buttons, 0);
                    }
                    else
                    {
                        // Relative Move (EN/FR: Mouvement relatif)
                        // VMulti uses sbyte (-127 to 127) for relative reports
                        sbyte dx = (sbyte)Math.Max(-127, Math.Min(127, x));
                        sbyte dy = (sbyte)Math.Max(-127, Math.Min(127, y));

                        _client.UpdateRelativeMouse(dx, dy, buttons, 0);
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"[VMultiMouse] Failed to update mouse P{_playerIndex}: {ex.Message}");
            }
        }

        /// <summary>
        /// Send button events only (no movement)
        /// (EN/FR: Envoyer uniquement les événements de boutons)
        /// </summary>
        public void SendButtonOnly(bool left, bool right, bool middle)
        {
            if (_client == null || _disposed)
                return;

            VMultiMouseButton buttons = VMultiMouseButton.None;
            if (left) buttons |= VMultiMouseButton.Left;
            if (right) buttons |= VMultiMouseButton.Right;
            if (middle) buttons |= VMultiMouseButton.Middle;

            // Send with position 0,0 - VMulti should not move cursor
            // (EN/FR: Envoyer avec position 0,0 - VMulti ne devrait pas déplacer le curseur)
            _client.UpdateMouse(0, 0, buttons, 0);
        }

        /// <summary>
        /// Refresh device connection (EN/FR: Rafraîchir la connexion au périphérique)
        /// </summary>
        public void RefreshDevice()
        {
            if (_client != null)
            {
                _client.Disconnect();
                _client.Connect();
            }
        }

        /// <summary>
        /// EN: Release all mouse buttons immediately.
        /// FR: Relâcher tous les boutons de la souris immédiatement.
        /// </summary>
        public void ResetAll()
        {
            if (_client != null && _client.IsConnected)
            {
                if (_leftButtonPressed || _rightButtonPressed || _middleButtonPressed)
                {
                    _leftButtonPressed = false;
                    _rightButtonPressed = false;
                    _middleButtonPressed = false;
                    _client.UpdateMouse(0, 0, VMultiMouseButton.None, 0);
                }
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            SimpleLogger.Instance.Info($"[VMultiMouse] Disconnecting VMulti mouse for player {_playerIndex}.");

            // Release all buttons before disconnecting (EN/FR: Relâcher tous les boutons avant déconnexion)
            ResetAll();

            if (_client != null)
            {
                _client.Dispose();
                _client = null;
            }
        }

        /// <summary>
        /// Static method to check if VMulti driver is available for all players
        /// (EN/FR: Méthode statique pour vérifier si le pilote VMulti est disponible pour tous les joueurs)
        /// </summary>
        public static bool IsVMultiAvailable()
        {
            return VMultiClient.IsDeviceAvailable(1);
        }

        /// <summary>
        /// Get list of available VMulti mouse devices
        /// (EN/FR: Obtenir la liste des souris VMulti disponibles)
        /// </summary>
        public static System.Collections.Generic.List<int> GetAvailableMice()
        {
            return VMultiClient.GetAvailablePlayers();
        }
    }
}
