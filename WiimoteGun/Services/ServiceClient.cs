using System;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Threading.Tasks;

namespace WiimoteGun
{
    public static class ServiceClient
    {
        private const string PIPE_NAME = "WiimoteGunService";

        public static void SendCommand(string command)
        {
            Task.Run(() => 
            {
                try
                {
                    using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", PIPE_NAME, PipeDirection.InOut))
                    {
                        // Increased timeout to 3000ms to avoid flaky connection failures
                        pipeClient.Connect(3000); 
                        using (StreamWriter sw = new StreamWriter(pipeClient))
                        {
                            sw.AutoFlush = true;
                            sw.WriteLine(command);
                        }
                    }
                    SimpleLogger.Instance.Info($"Service Command Sent: {command}");
                }
                catch (Exception ex)
                {
                    // Service likely not running or not installed
                    SimpleLogger.Instance.Debug($"Service IPC failed ({command}): {ex.Message}");
                }
            });
        }

        public static void EnablePlayer(int index) => SendCommand($"ENABLE_P{index}");
        public static void DisablePlayer(int index) => SendCommand($"DISABLE_P{index}");
    }
}
