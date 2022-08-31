using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace Up2dateService
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }

        private void serviceInstaller_AfterInstall(object sender, InstallEventArgs e)
        {
            ServiceInstaller serviceInstaller = (ServiceInstaller)sender;

            using (ServiceController sc = new ServiceController(serviceInstaller.ServiceName))
            {
                if (!sc.Status.Equals(ServiceControllerStatus.Running) && !sc.Status.Equals(ServiceControllerStatus.StartPending))
                {
                    sc.Start();
                }
            }
        }

        private void serviceInstaller_BeforeUninstall(object sender, InstallEventArgs e)
        {
            ServiceInstaller serviceInstaller = (ServiceInstaller)sender;

            using (ServiceController sc = new ServiceController(serviceInstaller.ServiceName))
            {
                if (!sc.Status.Equals(ServiceControllerStatus.Stopped) && !sc.Status.Equals(ServiceControllerStatus.StopPending))
                {
                    sc.Stop();
                }
            }
        }
    }
}
