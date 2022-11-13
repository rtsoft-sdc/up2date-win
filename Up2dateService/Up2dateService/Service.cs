using System;
using System.IO;
using System.ServiceModel;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using Up2dateClient;
using Up2dateDotNet;
using Up2dateService.Installers;
using Up2dateService.Interfaces;
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
            const int clientRestartOnErrorPeriodMs = 30000;

            // System.Diagnostics.Debugger.Launch(); //todo: remove!

            serviceHost?.Close();
            EventLog.WriteEntry($"Packages folder: '{GetCreatePackagesFolder()}'");

            ISettingsManager settingsManager = new SettingsManager(new Logger(EventLog, nameof(SettingsManager)));
            IWhiteListManager whiteListManager = new WhiteListManager();
            ICertificateProvider certificateProvider = new CertificateProvider(settingsManager);
            ICertificateManager certificateManager = new CertificateManager(settingsManager, new Logger(EventLog, nameof(CertificateManager)));
            ISignatureVerifier signatureVerifier = new SignatureVerifier();
            IPackageInstallerFactory installerFactory = new PackageInstallerFactory(settingsManager, signatureVerifier, whiteListManager, new Logger(EventLog));
            IPackageValidatorFactory validatorFactory = new PackageValidatorFactory(settingsManager, signatureVerifier, whiteListManager);
            ISetupManager setupManager = new SetupManager.SetupManager(new Logger(EventLog, nameof(SetupManager)), GetCreatePackagesFolder, settingsManager, installerFactory, validatorFactory);

            Client client = new Client(new Wrapper(), settingsManager, certificateManager.GetCertificateString, setupManager, SystemInfo.Retrieve, new Logger(EventLog, nameof(Client)));

            WcfService wcfService = new WcfService(setupManager, SystemInfo.Retrieve, GetCreatePackagesFolder, () => client.State, () => client.RequestStop(),
                certificateProvider, certificateManager, settingsManager, signatureVerifier, whiteListManager);
            serviceHost = new ServiceHost(wcfService);
            serviceHost.Open();

            Task.Run(() =>
            {
                while (true)
                {
                    string stopReason = client.Run();
                    Thread.Sleep(string.IsNullOrEmpty(stopReason) ? 0 : clientRestartOnErrorPeriodMs);
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
