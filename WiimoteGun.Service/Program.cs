using System;
using System.Collections;
using System.Configuration.Install;
using System.Reflection;
using System.ServiceProcess;

namespace WiimoteGun.Service
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            if (Environment.UserInteractive)
            {
                string parameter = string.Concat(args);
                switch (parameter)
                {
                    case "--install":
                    case "-install":
                    case "/install":
                        try
                        {
                            ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
                            Console.WriteLine("Service installed successfully!");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error installing service: " + ex.Message);
                        }
                        break;
                    case "--uninstall":
                    case "-uninstall":
                    case "/uninstall":
                        try
                        {
                            ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
                            Console.WriteLine("Service uninstalled successfully!");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Error uninstalling service: " + ex.Message);
                        }
                        break;
                    default:
                        Console.WriteLine("WiimoteGun Helper Service");
                        Console.WriteLine("Usage: /install or /uninstall");
                        break;
                }
            }
            else
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                    new ServiceMain()
                };
                ServiceBase.Run(ServicesToRun);
            }
        }
    }
}
