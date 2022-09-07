using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;
using Up2dateShared;

namespace SimpleClientApp
{
    public class SettingsManagerStub : ISettingsManager
    {
        public string ProvisioningUrl { get => "https://dps.ritms.online/provisioning"; set => throw new System.NotImplementedException(); }

        public string XApigToken => "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx";

        public string RequestCertificateUrl { get => "http://enter.dev.ritms.online"; set => throw new System.NotImplementedException(); }
        public string CertificateSerialNumber
        {
            get
            {
                try
                {
                    // ReSharper disable PossibleNullReferenceException
                    var value = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default).OpenSubKey("SOFTWARE").OpenSubKey("RTSoft").OpenSubKey("RITMS").OpenSubKey("UP2DATE").GetValue("Certificate") as string;
                    return value;
                    // ReSharper restore PossibleNullReferenceException
                }
                catch
                {
                    return string.Empty;
                }
            }
            set => throw new System.NotImplementedException();
        }
        public List<string> PackageExtensionFilterList { get => ".msi:.cert:.exe".Split(':').ToList(); set => throw new System.NotImplementedException(); }
        public bool CheckSignature { get => true; set => throw new System.NotImplementedException(); }
        public bool InstallAppFromSelectedIssuer { get => false; set => throw new System.NotImplementedException(); }
        public List<string> SelectedIssuers { get => new List<string>(); set => throw new System.NotImplementedException(); }
        public string PackageInProgress { get; set; }
    }
}
