using System;
using System.Collections.Generic;
using System.Security.Permissions;
using System.ServiceModel;
using System.Text;
using Up2dateShared;

namespace Up2dateService
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class WcfService : IWcfService
    {
        private const string AdministratorsGroupSID = "S-1-5-32-544";
        private readonly ISetupManager setupManager;
        private readonly Func<SystemInfo> getSysInfo;
        private readonly Func<string> getDownloadLocation;
        private readonly Func<ClientState> getClientState;
        private readonly ICertificateProvider certificateProvider;
        private readonly ICertificateManager certificateManager;

        public WcfService(ISetupManager setupManager, Func<SystemInfo> getSysInfo, Func<string> getDownloadLocation, Func<ClientState> getClientState, 
            ICertificateProvider certificateProvider, ICertificateManager certificateManager)
        {
            this.setupManager = setupManager ?? throw new ArgumentNullException(nameof(setupManager));
            this.getSysInfo = getSysInfo ?? throw new ArgumentNullException(nameof(getSysInfo));
            this.getDownloadLocation = getDownloadLocation ?? throw new ArgumentNullException(nameof(getDownloadLocation));
            this.getClientState = getClientState ?? throw new ArgumentNullException(nameof(getClientState));
            this.certificateProvider = certificateProvider ?? throw new ArgumentNullException(nameof(certificateProvider));
            this.certificateManager = certificateManager ?? throw new ArgumentNullException(nameof(certificateManager));
        }

        public List<Package> GetPackages()
        {
            return setupManager.GetAvaliablePackages();
        }

        public SystemInfo GetSystemInfo()
        {
            return getSysInfo();
        }

        [PrincipalPermission(SecurityAction.Demand, Role = AdministratorsGroupSID)]
        public void StartInstallation(IEnumerable<Package> packages)
        {
            _ = setupManager.InstallPackagesAsync(packages);
        }

        public string GetMsiFolder()
        {
            return getDownloadLocation();
        }

        public ClientState GetClientState()
        {
            return getClientState();
        }

        public string GetDeviceId()
        {
            return certificateManager.IsCertificateAvailable()
                ? $"{certificateManager.CertificateIssuerName}:{certificateManager.CertificateSubjectName}"
                : string.Empty;
        }

        public bool IsCertificateAvailable()
        {
            return certificateManager.IsCertificateAvailable();
        }

        [PrincipalPermission(SecurityAction.Demand, Role = AdministratorsGroupSID)]
        [OperationBehavior(Impersonation = ImpersonationOption.Required)]
        public Result<string> RequestCertificate(string oneTimeKey)
        {
            try
            {
                string certString = certificateProvider.RequestCertificateAsync(oneTimeKey).Result;
                byte[] certData = Encoding.UTF8.GetBytes(certString);
                certificateManager.ImportCertificate(certData);
            }
            catch (Exception e)
            {
                return Result<string>.Failed(e.Message);
            }
            return Result<string>.Successful(GetDeviceId());
        }
    }
}
