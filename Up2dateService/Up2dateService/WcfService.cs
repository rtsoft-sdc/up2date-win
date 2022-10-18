using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
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
        private readonly ISettingsManager settingsManager;
        private readonly ISignatureVerifier signatureVerifier;
        private readonly IWhiteListManager whiteListManager;

        public WcfService(ISetupManager setupManager, Func<SystemInfo> getSysInfo, Func<string> getDownloadLocation, Func<ClientState> getClientState,
            ICertificateProvider certificateProvider, ICertificateManager certificateManager,
            ISettingsManager settingsManager, ISignatureVerifier signatureVerifier, IWhiteListManager whiteListManager)
        {
            this.setupManager = setupManager ?? throw new ArgumentNullException(nameof(setupManager));
            this.getSysInfo = getSysInfo ?? throw new ArgumentNullException(nameof(getSysInfo));
            this.getDownloadLocation = getDownloadLocation ?? throw new ArgumentNullException(nameof(getDownloadLocation));
            this.getClientState = getClientState ?? throw new ArgumentNullException(nameof(getClientState));
            this.certificateProvider = certificateProvider ?? throw new ArgumentNullException(nameof(certificateProvider));
            this.certificateManager = certificateManager ?? throw new ArgumentNullException(nameof(certificateManager));
            this.settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            this.signatureVerifier = signatureVerifier ?? throw new ArgumentNullException(nameof(signatureVerifier));
            this.whiteListManager = whiteListManager ?? throw new ArgumentNullException(nameof(whiteListManager));
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
            Task.Run(() => setupManager.InstallPackages(packages));
        }
     
        public void RejectInstallation(IEnumerable<Package> packages)
        {
            setupManager.RejectPackages(packages);
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
                return Result<string>.Failed(e);
            }
            return Result<string>.Successful(GetDeviceId());
        }

        [PrincipalPermission(SecurityAction.Demand, Role = AdministratorsGroupSID)]
        [OperationBehavior(Impersonation = ImpersonationOption.Required)]
        public Result<string> ImportCertificate(string filePath)
        {
            try
            {
                certificateManager.ImportCertificate(filePath);
            }
            catch (Exception e)
            {
                return Result<string>.Failed(e);
            }
            return Result<string>.Successful(GetDeviceId());
        }

        public string GetRequestCertificateUrl()
        {
            return settingsManager.RequestCertificateUrl;
        }

        public void SetRequestCertificateUrl(string url)
        {
            settingsManager.RequestCertificateUrl = url;
        }

        public string GetProvisioningUrl()
        {
            return settingsManager.ProvisioningUrl;
        }

        public void SetProvisioningUrl(string url)
        {
            settingsManager.ProvisioningUrl = url;
        }

        public bool GetCheckSignature()
        {
            return settingsManager.CheckSignature;
        }

        public void SetCheckSignature(bool newState)
        {
            settingsManager.CheckSignature = newState;
        }

        public bool GetConfirmBeforeInstallation()
        {
            return settingsManager.RequiresConfirmationBeforeInstall;
        }

        public void SetConfirmBeforeInstallation(bool newState)
        {
            settingsManager.RequiresConfirmationBeforeInstall = newState;
        }

        public SignatureVerificationLevel GetSignatureVerificationLevel()
        {
            return settingsManager.SignatureVerificationLevel;
        }

        public void SetSignatureVerificationLevel(SignatureVerificationLevel level)
        {
            settingsManager.SignatureVerificationLevel = level;
        }

        public bool IsCertificateValidAndTrusted(string certificateFilePath)
        {
            return signatureVerifier.IsCertificateValidAndTrusted(certificateFilePath);
        }

        public IList<string> GetWhitelistedCertificates()
        {
            return whiteListManager.GetWhitelistedCertificates().Select(c => c.FriendlyName).ToList();
        }

        public Result AddCertificateToWhitelist(string certificateFilePath)
        {
            return whiteListManager.AddCertificateToWhitelist(certificateFilePath);
        }
    }
}
