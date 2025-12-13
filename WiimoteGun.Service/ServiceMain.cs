using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Threading;

namespace WiimoteGun.Service
{
    public partial class ServiceMain : ServiceBase
    {
        private PipeServer _pipeServer;
        
        public ServiceMain()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            try 
            {
                _pipeServer = new PipeServer();
                _pipeServer.Start();
                DriverController.Log("WiimoteGun Helper Service Started.");
            }
            catch(Exception ex)
            {
                DriverController.Log("WiimoteGun.Service Start Error: " + ex.Message);
                Stop();
            }
        }

        protected override void OnStop()
        {
             if (_pipeServer != null)
             {
                 _pipeServer.Stop();
                 _pipeServer = null;
             }
             DriverController.Log("WiimoteGun Helper Service Stopped.");
        }
    }
}
