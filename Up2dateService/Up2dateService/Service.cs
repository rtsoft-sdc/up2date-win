using System;
using System.IO;
using System.ServiceModel;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Up2dateClient;
using Up2dateShared;

namespace Up2dateService
{
    public partial class Service : ServiceBase
    {
        private static ServiceHost serviceHost = null;

        public Service()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            const int clientStartRertyPeriodMs = 30000;

            //System.Diagnostics.Debugger.Launch(); //todo: remove!

            serviceHost?.Close();
            EventLog.WriteEntry($"Packages folder: '{GetCreatePackagesFolder()}'");

            ISettingsManager settingsManager = new SettingsManager(); 
            ICertificateProvider certificateProvider = new CertificateProvider(settingsManager);
            ICertificateManager certificateManager = new CertificateManager(settingsManager, EventLog);
            SetupManager.IPackageInstallerFactory installerFactory = new SetupManager.PackageInstallerFactory();
            ISetupManager setupManager = new SetupManager.SetupManager(EventLog, GetCreatePackagesFolder, settingsManager, certificateManager, installerFactory);

            Client client = new Client(settingsManager, certificateManager.GetCertificateString, setupManager, SystemInfo.Retrieve, GetCreatePackagesFolder, EventLog);
            WcfService wcfService = new WcfService(setupManager, SystemInfo.Retrieve, GetCreatePackagesFolder, () => client.State, certificateProvider, certificateManager, settingsManager);
            serviceHost = new ServiceHost(wcfService);
            serviceHost.Open();

            Task.Run(() =>
            {
                while (true)
                {
                    client.Run();
                    Thread.Sleep(clientStartRertyPeriodMs);
                }
            });
        }

        protected override void OnStop()
        {
            if (serviceHost != null)
            {
                serviceHost.Close();
                serviceHost = null;
            }
        }

        private string GetCreatePackagesFolder()
        {
            return GetCreateServiceDataFolder(@"Packages\");
        }

        private string GetCreateServiceDataFolder(string subfolder = null)
        {
            subfolder = subfolder ?? string.Empty;
            string localDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            string path = Path.Combine(localDataFolder, @"Up2dateService\", subfolder);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            return path;
        }
    }
}
