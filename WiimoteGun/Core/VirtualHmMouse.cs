using System;

namespace WiimoteGun
{
    /// <summary>
    /// [V57d] EN: Virtual mouse backed by the UMDF2/HIDMaestro host (HmHost). Same
    ///     semantics as VirtualVMultiMouse (absolute 16-bit coordinates scaled from the
    ///     0-65535 range, button edge events, relative reports) but frames are sent to
    ///     HmHost through the service-supervised pipe instead of the vmulti driver.
    ///     FR: Souris virtuelle adossée à l'hôte UMDF2/HIDMaestro (HmHost). Sémantique
    ///     identique à VirtualVMultiMouse (coordonnées absolues 16 bits mises à
    ///     l'échelle depuis la plage 0-65535, événements de boutons en front, rapports
    ///     relatifs) mais les frames partent vers HmHost via le pipe supervisé par le
    ///     service, au lieu du pilote vmulti.
    /// </summary>
    class VirtualHmMouse : IVirtualMouse
    {
        private readonly int _playerIndex;
        private bool _disposed = false;

        // Screen resolution for absolute positioning (EN/FR: Résolution d'écran pour positionnement absolu)
        // HmHost/vmulti report range is 0-32767 (EN/FR: La plage de rapport HmHost/vmulti est 0-32767)
        private const int SCREEN_MAX = 32767;
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
        /// EN: Connection status - the host owns the devices, we are always "connected".
        /// FR: Statut de connexion - l'hôte possède les périphériques, toujours "connecté".
        /// </summary>
        public bool IsConnected => !_disposed;

        public VirtualHmMouse(int playerIndex)
        {
            _playerIndex = playerIndex;
            SimpleLogger.Instance.Info($"[HmMouse] Created UMDF2 (HIDMaestro) mouse for Player {playerIndex} - frames via HmHost.");
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
            if (_disposed)
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

                // Button bits: 1=Left 2=Right 4=Middle (matches the vmulti descriptor)
                // (EN/FR: Bits boutons : 1=Gauche 2=Droit 4=Milieu - identiques au descripteur vmulti)
                int buttons = (leftButton ? 1 : 0) | (rightButton ? 2 : 0) | (middleButton ? 4 : 0);

                // Send if moving cursor or buttons changed (EN/FR: Envoyer si déplacement curseur ou boutons changés)
                if (moveCursor || buttonChanged)
                {
                    if (isAbsolute)
                    {
                        // Clamp and scale coordinates (EN/FR: Limiter et mettre à l'échelle les coordonnées)
                        int absX = Math.Max(0, Math.Min(65535, x));
                        int absY = Math.Max(0, Math.Min(65535, y));

                        // Convert to HmHost/vmulti range (0-32767) (EN/FR: Convertir à la plage HmHost/vmulti)
                        int hmX = (int)(absX * SCALE_FACTOR);
                        int hmY = (int)(absY * SCALE_FACTOR);

                        HmFrameClient.SendMouse(_playerIndex, hmX, hmY, buttons, 0);
                    }
                    else
                    {
                        // Relative move (EN/FR: Mouvement relatif)
                        // HmHost uses sbyte (-127 to 127) for relative reports
                        sbyte dx = (sbyte)Math.Max(-127, Math.Min(127, x));
                        sbyte dy = (sbyte)Math.Max(-127, Math.Min(127, y));

                        HmFrameClient.SendMouseRelative(_playerIndex, dx, dy, buttons, 0);
                    }
                }
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error($"[HmMouse] Failed to update mouse P{_playerIndex}: {ex.Message}");
            }
        }

        /// <summary>
        /// Send button events only (no movement)
        /// (EN/FR: Envoyer uniquement les événements de boutons)
        /// </summary>
        public void SendButtonOnly(bool left, bool right, bool middle)
        {
            if (_disposed)
                return;

            int buttons = (left ? 1 : 0) | (right ? 2 : 0) | (middle ? 4 : 0);
            HmFrameClient.SendMouse(_playerIndex, 0, 0, buttons, 0);
        }

        /// <summary>
        /// Refresh device connection (EN/FR: Rafraîchir la connexion au périphérique)
        /// </summary>
        public void RefreshDevice()
        {
            // EN: No-op - HmHost owns the device lifecycle (unlike the vmulti HID reconnects)
            // FR: Sans effet - HmHost possède le cycle de vie (contrairement aux reconnexions HID vmulti)
        }

        /// <summary>
        /// EN: Release all mouse buttons immediately.
        /// FR: Relâcher tous les boutons de la souris immédiatement.
        /// </summary>
        public void ResetAll()
        {
            if (_disposed)
                return;

            if (_leftButtonPressed || _rightButtonPressed || _middleButtonPressed)
            {
                _leftButtonPressed = false;
                _rightButtonPressed = false;
                _middleButtonPressed = false;
                HmFrameClient.SendMouse(_playerIndex, 0, 0, 0, 0);
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            SimpleLogger.Instance.Info($"[HmMouse] Disposing UMDF2 mouse for player {_playerIndex}.");

            // Release all buttons before disposing (EN/FR: Relâcher tous les boutons avant destruction)
            ResetAll();
        }
    }
}
