using System.ComponentModel;
using System.Configuration.Install;

namespace WiimoteGun.Service
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
            this.serviceInstaller1.AfterInstall += new System.Configuration.Install.InstallEventHandler(this.serviceInstaller1_AfterInstall);
        }

        private void serviceInstaller1_AfterInstall(object sender, System.Configuration.Install.InstallEventArgs e)
        {
            using (System.ServiceProcess.ServiceController sc = new System.ServiceProcess.ServiceController(serviceInstaller1.ServiceName))
            {
                sc.Start();
            }
        }
    }
}
