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
        SystemInfo GetSystemInfo();

        [OperationContract]
        string GetMsiFolder();

        [OperationContract]
        ClientState GetClientState();

        [OperationContract]
        string GetDeviceId();

        [OperationContract]
        bool IsCertificateAvailable();

        [OperationContract]
        Result<string> RequestCertificate(string oneTimeKey);

        [OperationContract]
        string GetRequestCertificateUrl();

        [OperationContract]
        void SetRequestCertificateUrl(string url);

        [OperationContract]
        string GetProvisioningUrl();

        [OperationContract]
        void SetProvisioningUrl(string url);

        [OperationContract]
        bool GetCheckSignature();

        [OperationContract]
        void SetCheckSignature(bool newState);

        [OperationContract]
        bool GetInstallAppFromSelectedIssuer();

        [OperationContract]
        void SetInstallAppFromSelectedIssuer(bool newState);

        [OperationContract]
        string GetSelectedIssuers();

        [OperationContract]
        void SetSelectedIssuers(string selectedIssuers);
    }
}
