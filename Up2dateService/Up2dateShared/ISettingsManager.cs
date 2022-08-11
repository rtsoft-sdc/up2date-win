using System.Collections.Generic;

namespace Up2dateShared
{
    public interface ISettingsManager
    {
        string ProvisioningUrl { get; }
        string XApigToken { get; }
        string RequestCertificateUrl { get; }
        string CertificateSerialNumber { get; set; }

        List<string> PackageExtensionFilterList { get; set; }

        bool CheckSignature { get; set; }
        bool InstallAppFromSelectedIssuer { get; set; }
        List<string> SelectedIssuers { get; set; }
    }
}