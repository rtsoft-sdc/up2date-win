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
            set => RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default).CreateSubKey("SOFTWARE")?.CreateSubKey("RTSoft")?.CreateSubKey("RITMS")?.CreateSubKey("UP2DATE")?.SetValue("Certificate", value);
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

        public bool InstallAppFromSelectedIssuer
        {
            get => Properties.Settings.Default.InstallAppFromSelectedIssuer;
            set
            {
                Properties.Settings.Default.InstallAppFromSelectedIssuer = value;
                Properties.Settings.Default.Save();
            }
        }

        public List<string> SelectedIssuers
        {
            get => Properties.Settings.Default.SelectedIssuers.Split(':').ToList();
            set
            {
                Properties.Settings.Default.SelectedIssuers = string.Join(":", value);
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
    }
}
