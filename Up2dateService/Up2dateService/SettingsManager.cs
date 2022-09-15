using System.Collections.Generic;
using System.Linq;
using Microsoft.Win32;
using Up2dateShared;

namespace Up2dateService
{
    public class SettingsManager : ISettingsManager
    {
        public SettingsManager()
        {
            if (Properties.Settings.Default.UpgradeFlag)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpgradeFlag = false;
                Properties.Settings.Default.Save();
            }
        }

        public string ProvisioningUrl
        {
            get => Properties.Settings.Default.ProvisioningUrl;
            set
            {
                Properties.Settings.Default.ProvisioningUrl = value;
                Properties.Settings.Default.Save();
            }
        }

        public string XApigToken
        {
            get => Properties.Settings.Default.XApigToken;
        }

        public string RequestCertificateUrl
        {
            get => Properties.Settings.Default.RequestCertificateUrl;
            set
            {
                Properties.Settings.Default.RequestCertificateUrl = value;
                Properties.Settings.Default.Save();
            }
        }

        public string CertificateThumbprint
        {
            get => Properties.Settings.Default.CertificateThumbprint;
            set
            {
                Properties.Settings.Default.CertificateThumbprint = value;
                Properties.Settings.Default.Save();
            }
        }

        public List<string> PackageExtensionFilterList
        {
            get => Properties.Settings.Default.PackageExtensionFilterList.Split(':').ToList();
            set
            {
                Properties.Settings.Default.PackageExtensionFilterList = string.Join(":", value);
                Properties.Settings.Default.Save();
            }
        }

        public bool CheckSignature
        {
            get => Properties.Settings.Default.CheckSignature;
            set
            {
                Properties.Settings.Default.CheckSignature = value;
                Properties.Settings.Default.Save();
            }
        }

        public SignatureVerificationLevel SignatureVerificationLevel
        {
            get => Properties.Settings.Default.SignatureVerificationLevel;
            set
            {
                Properties.Settings.Default.SignatureVerificationLevel = value;
                Properties.Settings.Default.Save();
            }
        }

        public string PackageInProgress 
        {
            get => Properties.Settings.Default.PackageInProgress;
            set
            {
                Properties.Settings.Default.PackageInProgress = value;
                Properties.Settings.Default.Save();
            }
        }

        public string DefaultChocoSources
        {
            get => Properties.Settings.Default.DefaultChocoSources;
            set
            {
                Properties.Settings.Default.DefaultChocoSources = value;
                Properties.Settings.Default.Save();
            }
        }
    }
}
