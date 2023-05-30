using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using Up2dateClient;
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
        private readonly IClient client;
        private readonly ICertificateProvider certificateProvider;
        private readonly ICertificateManager certificateManager;
        private readonly ISettingsManager settingsManager;
        private readonly ISignatureVerifier signatureVerifier;
        private readonly IWhiteListManager whiteListManager;

        public WcfService(ISetupManager setupManager, Func<SystemInfo> getSysInfo, Func<string> getDownloadLocation, IClient client,
            ICertificateProvider certificateProvider, ICertificateManager certificateManager,
            ISettingsManager settingsManager, ISignatureVerifier signatureVerifier, IWhiteListManager whiteListManager)
        {
            this.setupManager = setupManager ?? throw new ArgumentNullException(nameof(setupManager));
            this.getSysInfo = getSysInfo ?? throw new ArgumentNullException(nameof(getSysInfo));
            this.getDownloadLocation = getDownloadLocation ?? throw new ArgumentNullException(nameof(getDownloadLocation));
            this.client = client ?? throw new ArgumentNullException(nameof(client));
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

        public void AcceptInstallation(Package package)
        {
            setupManager.AcceptPackage(package);
            client.RequestToPoll();
        }

        public void RejectInstallation(Package package)
        {
            setupManager.RejectPackage(package);
            client.RequestToPoll();
        }

        public string GetMsiFolder()
        {
            return getDownloadLocation();
        }

        public ClientState GetClientState()
        {
            return client.State;
        }

        public string GetHawkbitEndpoint()
        {
            return client.HawkbitEndpoint;
        }

        public string GetTenant()
        {
            if (settingsManager.SecureAuthorizationMode)
            {
                return certificateManager.IsCertificateAvailable()
                    ? certificateManager.CertificateIssuerName
                    : string.Empty;
            }
            return string.Empty;
        }

        public string GetDeviceId()
        {
            if (settingsManager.SecureAuthorizationMode)
            {
                return certificateManager.IsCertificateAvailable()
                    ? certificateManager.CertificateSubjectName
                    : string.Empty;
            }
            return settingsManager.DeviceId;
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
        public Result<string> OpenRequestCertificateSession()
        {
            return certificateProvider.OpenRequestCertificateSessionAsync().Result;
        }

        [PrincipalPermission(SecurityAction.Demand, Role = AdministratorsGroupSID)]
        [OperationBehavior(Impersonation = ImpersonationOption.Required)]
        public Result<string> GetCertificateBySessionHandle(string handle)
        {
            return certificateProvider.GetCertificateBySessionHandleAsync(handle).Result;
        }

        [PrincipalPermission(SecurityAction.Demand, Role = AdministratorsGroupSID)]
        [OperationBehavior(Impersonation = ImpersonationOption.Required)]
        public void CloseRequestCertificateSession(string handle)
        {
            certificateProvider.CloseRequestCertificateSession(handle);
        }

        [PrincipalPermission(SecurityAction.Demand, Role = AdministratorsGroupSID)]
        [OperationBehavior(Impersonation = ImpersonationOption.Required)]
        public Result<string> ImportCertificateFile(string filePath)
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

        [PrincipalPermission(SecurityAction.Demand, Role = AdministratorsGroupSID)]
        [OperationBehavior(Impersonation = ImpersonationOption.Required)]
        public Result<string> ImportCertificate(string certString)
        {
            try
            {
                byte[] certData = Encoding.UTF8.GetBytes(certString);
                certificateManager.ImportCertificate(certData);
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

        public string GetRequestOneTimeTokenUrl()
        {
            return settingsManager.RequestOneTimeTokenUrl;
        }

        public void SetRequestOneTimeTokenUrl(string url)
        {
            settingsManager.RequestOneTimeTokenUrl = url;
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

        public bool IsUnsafeConnection()
        {
            return !settingsManager.SecureAuthorizationMode;
        }

        public Result SetupUnsafeConnection(string url, string deviceId, string token)
        {
            settingsManager.HawkbitUrl = url;
            settingsManager.DeviceId = deviceId;
            settingsManager.SecurityToken = token;
            settingsManager.SecureAuthorizationMode = false;
            return Result.Successful();
        }

        public string GetUnsafeConnectionUrl()
        {
            return settingsManager.HawkbitUrl;
        }

        public string GetUnsafeConnectionDeviceId()
        {
            return settingsManager.DeviceId;
        }

        public string GetUnsafeConnectionToken()
        {
            return settingsManager.SecurityToken;
        }

        public Result SetupSecureConnection()
        {
            settingsManager.SecureAuthorizationMode = true;
            return Result.Successful();
        }

        public Result DeletePackage(Package package)
        {
            return setupManager.DeletePackage(package);
        }
    }
}
