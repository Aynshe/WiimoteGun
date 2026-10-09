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
                // [V57h] EN: Re-apply the persisted UMDF2 desired state in the background
                //     (survives a PC reboot with RawInput (UMDF2) selected - see
                //     HmHostSupervisor). No devices are created here: on demand per
                //     connected wiimote (V57e).
                //     FR: Réapplique l'état UMDF2 persisté en arrière-plan (survit à un
                //     reboot PC avec RawInput (UMDF2) sélectionné - voir
                //     HmHostSupervisor). Aucun device créé ici : à la demande par
                //     wiimote connectée (V57e).
                HmHostSupervisor.ReapplyPersistedState();
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
