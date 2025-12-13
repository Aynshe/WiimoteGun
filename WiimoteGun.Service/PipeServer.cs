using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

namespace WiimoteGun.Service
{
    public class PipeServer
    {
        private Thread _serverThread;
        private bool _isRunning;
        private const string PIPE_NAME = "WiimoteGunService";

        public void Start()
        {
            _isRunning = true;
            _serverThread = new Thread(ServerLoop);
            _serverThread.IsBackground = true;
            _serverThread.Start();
        }

        public void Stop()
        {
            _isRunning = false;
            // Connect dummy client to unblock WaitConnection if needed, or just Abort if stuck (Service stop needs to be fast)
            try 
            {
                // Force abort for immediate stop during service shutdown
                if (_serverThread != null && _serverThread.IsAlive)
                    _serverThread.Abort(); 
            } 
            catch {}
        }

        private void ServerLoop()
        {
            while (_isRunning)
            {
                try
                {
                    // Create pipe with security allowing Authenticated Users to connect
                    PipeSecurity ps = new PipeSecurity();
                    SecurityIdentifier sid = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
                    ps.AddAccessRule(new PipeAccessRule(sid, PipeAccessRights.ReadWrite, AccessControlType.Allow));

                    using (NamedPipeServerStream pipeServer = new NamedPipeServerStream(PIPE_NAME, PipeDirection.InOut, 1, PipeTransmissionMode.Message, PipeOptions.None, 1024, 1024, ps))
                    {
                        pipeServer.WaitForConnection();

                        using (StreamReader sr = new StreamReader(pipeServer))
                        {
                            string command = sr.ReadLine();
                            if (!string.IsNullOrEmpty(command))
                            {
                                ProcessCommand(command.Trim());
                            }
                        }
                    }
                }
                catch (ThreadAbortException) { return; }
                catch (Exception ex)
                {
                    // Log error but continue loop (backoff to prevent tight loop spin on error)
                    DriverController.Log("Pipe Error: " + ex.Message);
                    Thread.Sleep(2000); 
                }
            }
        }

        private void ProcessCommand(string command)
        {
            try
            {
                DriverController.Log("Service received command: " + command);
                switch (command.ToUpper())
                {
                    case "ENABLE_P1": DriverController.EnablePlayer(1); break;
                    case "DISABLE_P1": DriverController.DisablePlayer(1); break;
                    case "ENABLE_P2": DriverController.EnablePlayer(2); break;
                    case "DISABLE_P2": DriverController.DisablePlayer(2); break;
                    case "ENABLE_P3": DriverController.EnablePlayer(3); break;
                    case "DISABLE_P3": DriverController.DisablePlayer(3); break;
                    case "ENABLE_P4": DriverController.EnablePlayer(4); break;
                    case "DISABLE_P4": DriverController.DisablePlayer(4); break;
                }
            }
            catch (Exception ex)
            {
                DriverController.Log("Error processing command: " + ex.Message);
            }
        }
    }
}
