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

        public string PackageInProgress { get; set; }

        public string DefaultChocoSources { get; set; }
        public SignatureVerificationLevel SignatureVerificationLevel { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public string CertificateThumbprint { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
    }
}
