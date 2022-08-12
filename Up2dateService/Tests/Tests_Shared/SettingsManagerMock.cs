using System.Collections.Generic;
using Up2dateShared;

namespace Tests_Shared
{
    public class SettingsManagerMock : ISettingsManager
    {

        public string ProvisioningUrl { get; set; }

        public string XApigToken { get; set; }

        public string RequestCertificateUrl { get; set; }

        public string CertificateSerialNumber { get; set; }

        public List<string> PackageExtensionFilterList { get; set; }

        public bool CheckSignature { get; set; }

        public bool InstallAppFromSelectedIssuer { get; set; }

        public List<string> SelectedIssuers { get; set; }
    }
}
