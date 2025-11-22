using System;
using System.Collections.Generic;
using System.Linq;
using WiimoteLib;
using WiimoteLib.Events;

namespace WiimoteGun
{
    class WiimoteControllerManager : IDisposable
    {
        private List<WiiMoteController> _controllers;
        private int MaxWiimotes => Options.Instance.Enable4Players ? 4 : 2;

        public WiimoteControllerManager()
        {
            _controllers = new List<WiiMoteController>();

            WiimoteManager.DolphinBarMode = Options.Instance.DetectDolphinbar;
            WiimoteManager.BluetoothMode = Options.Instance.DetectBlueTooth;
            WiimoteManager.AutoConnect = true;
            WiimoteManager.AutoDiscoveryCount = MaxWiimotes;

            WiimoteManager.Connected += OnWiimoteConnected;
            WiimoteManager.Disconnected += OnWiimoteDisconnected;
            WiimoteManager.WiimoteException += OnWiimoteException;

            WiimoteManager.StartDiscovery();
        }

        public void Dispose()
        {
            WiimoteManager.Connected -= OnWiimoteConnected;
            WiimoteManager.Disconnected -= OnWiimoteDisconnected;
            WiimoteManager.WiimoteException -= OnWiimoteException;

            foreach (var controller in _controllers)
            {
                controller.Dispose();
            }
            _controllers.Clear();
        }

        private void OnWiimoteException(object sender, WiimoteExceptionEventArgs e)
        {
            SimpleLogger.Instance.Error("Wiimote Exception from Manager: " + e.ToString());
        }

        private void OnWiimoteConnected(object sender, WiimoteEventArgs e)
        {
            if (_controllers.Count >= MaxWiimotes)
            {
                SimpleLogger.Instance.Warning("Max number of Wiimotes reached. Ignoring new connection.");
                // Maybe provide some feedback to the user, like a short rumble.
                try
                {
                    e.Wiimote.SetRumble(true);
                    System.Threading.Thread.Sleep(200);
                    e.Wiimote.SetRumble(false);
                }
                catch { }
                return;
            }

            try
            {
                string mac = e.Wiimote.Address.ToString();
                int playerIndex = -1;

                // 1. Check if this MAC is already assigned to a preferred slot
                if (Options.Instance.PreferredMacP1 == mac) playerIndex = 1;
                else if (Options.Instance.PreferredMacP2 == mac) playerIndex = 2;
                else if (Options.Instance.PreferredMacP3 == mac) playerIndex = 3;
                else if (Options.Instance.PreferredMacP4 == mac) playerIndex = 4;

                // 2. If found, check if available
                if (playerIndex != -1)
                {
                    if (_controllers.Any(c => c.PlayerIndex == playerIndex))
                    {
                        SimpleLogger.Instance.Warning($"Wiimote {mac} is preferred for P{playerIndex} but slot is busy. Finding next available.");
                        playerIndex = -1;
                    }
                }

                // 3. If not found or busy, find first available slot
                if (playerIndex == -1)
                {
                    for (int i = 1; i <= MaxWiimotes; i++)
                    {
                        if (!_controllers.Any(c => c.PlayerIndex == i))
                        {
                            playerIndex = i;
                            break;
                        }
                    }
                }

                if (playerIndex == -1)
                {
                    SimpleLogger.Instance.Error("No available player slots for Wiimote " + mac);
                    return;
                }

                // 4. Auto-save preference removed. 
                // "None (Auto)" should mean dynamic assignment, not "Auto-Learn and Fix".
                // If user wants to fix a Wiimote to a player, they must do it manually via the menu.
                SimpleLogger.Instance.Info($"Assigned Wiimote {mac} to Player {playerIndex} (Dynamic)");

                var controller = new WiiMoteController(e.Wiimote, playerIndex);
                _controllers.Add(controller);

                Program.SetConnectedState(true);
            }
            catch (BadImageFormatException)
            {
                System.Windows.Forms.MessageBox.Show("A fatal error occurred while connecting to the Interception driver.\n\n" +
                                                      "This is likely caused by a 32-bit/64-bit architecture mismatch.\n\n" +
                                                      "Please ensure that 'interception.dll' is the x86 (32-bit) version and the Interception driver is installed correctly.",
                                                      "Architecture Mismatch", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                // Optionally, shut down the application
                Program.PostToUIThread(() => System.Windows.Forms.Application.Exit());
            }
            catch (Exception ex)
            {
                SimpleLogger.Instance.Error("Failed to create WiiMoteController: " + ex.ToString());
            }
        }

        private void OnWiimoteDisconnected(object sender, WiimoteDisconnectedEventArgs e)
        {
            var controller = _controllers.FirstOrDefault(c => c.Wiimote == e.Wiimote);
            if (controller != null)
            {
                _controllers.Remove(controller);
                controller.Dispose();
                SimpleLogger.Instance.Info($"Wiimote P{controller.PlayerIndex} disconnected.");
            }

            if (_controllers.Count == 0)
            {
                Program.SetConnectedState(false);
            }
        }
        public IEnumerable<WiiMoteController> GetControllers()
        {
            return _controllers.ToList();
        }
    }
}
