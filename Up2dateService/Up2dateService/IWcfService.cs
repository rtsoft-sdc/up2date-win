using System.Collections.Generic;
using System.ServiceModel;
using Up2dateShared;

namespace Up2dateService
{
    [ServiceContract]
    public interface IWcfService
    {
        [OperationContract]
        List<Package> GetPackages();

        [OperationContract]
        void StartInstallation(IEnumerable<Package> packages);

        [OperationContract]
        void AcceptInstallation(Package package);

        [OperationContract]
        void RejectInstallation(Package package);

        [OperationContract]
        SystemInfo GetSystemInfo();

        [OperationContract]
        string GetMsiFolder();

        [OperationContract]
        ClientState GetClientState();

        [OperationContract]
        string GetHawkbitEndpoint();

        [OperationContract]
        string GetTenant();

        [OperationContract]
        string GetDeviceId();

        [OperationContract]
        bool IsCertificateAvailable();

        [OperationContract]
        Result<string> RequestCertificate(string oneTimeKey);

        [OperationContract]
        Result<string> OpenRequestCertificateSession();

        [OperationContract]
        Result<string> GetCertificateBySessionHandle(string handle);

        [OperationContract]
        void CloseRequestCertificateSession(string handle);

        [OperationContract]
        Result<string> ImportCertificate(string filePath);

        [OperationContract]
        string GetRequestCertificateUrl();

        [OperationContract]
        void SetRequestCertificateUrl(string url);

        [OperationContract]
        string GetProvisioningUrl();

        [OperationContract]
        void SetProvisioningUrl(string url);

        [OperationContract]
        string GetRequestOneTimeTokenUrl();

        [OperationContract]
        void SetRequestOneTimeTokenUrl(string url);

        [OperationContract]
        bool GetCheckSignature();

        [OperationContract]
        void SetCheckSignature(bool newState);

        [OperationContract]
        bool GetConfirmBeforeInstallation();

        [OperationContract]
        void SetConfirmBeforeInstallation(bool newState);

        [OperationContract]
        SignatureVerificationLevel GetSignatureVerificationLevel();

        [OperationContract]
        void SetSignatureVerificationLevel(SignatureVerificationLevel level);

        [OperationContract]
        bool IsCertificateValidAndTrusted(string certificateFilePath);

        [OperationContract]
        IList<string> GetWhitelistedCertificates();

        [OperationContract]
        Result AddCertificateToWhitelist(string certificateFilePath);

        [OperationContract]
        bool IsUnsafeConnection();

        [OperationContract]
        Result SetupUnsafeConnection(string url, string deviceId, string token);

        [OperationContract]
        string GetUnsafeConnectionUrl();

        [OperationContract]
        string GetUnsafeConnectionDeviceId();

        [OperationContract]
        string GetUnsafeConnectionToken();

        [OperationContract]
        Result SetupSecureConnection();

        [OperationContract]
        Result DeletePackage(Package package);
    }
}
