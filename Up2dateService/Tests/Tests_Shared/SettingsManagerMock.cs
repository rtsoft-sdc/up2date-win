using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

using Up2dateShared;

namespace TestsShared
{
    public class SettingsManagerMock : ISettingsManager
    {
        private const string AppSettingSectionName = "appSettings";

        public string ProvisioningUrl
        {
            get
            {
                ConfigurationManager.RefreshSection(AppSettingSectionName);
                return ConfigurationManager.AppSettings[nameof(ProvisioningUrl)];
            }
        }

        public string XApigToken
        {
            get
            {
                ConfigurationManager.RefreshSection(AppSettingSectionName);
                return ConfigurationManager.AppSettings[nameof(XApigToken)];
            }
        }

        public string RequestCertificateUrl
        {
            get
            {
                ConfigurationManager.RefreshSection(AppSettingSectionName);
                return ConfigurationManager.AppSettings[nameof(RequestCertificateUrl)];
            }
        }

        public string CertificateSerialNumber
        {
            get
            {
                ConfigurationManager.RefreshSection(AppSettingSectionName);
                return ConfigurationManager.AppSettings[nameof(CertificateSerialNumber)];
            }
            set
            {
                AddUpdateAppSettings(nameof(CertificateSerialNumber), value);
            }
        }

        public List<string> PackageExtensionFilterList
        {
            get
            {
                ConfigurationManager.RefreshSection(AppSettingSectionName);
                return ConfigurationManager.AppSettings[nameof(PackageExtensionFilterList)].Split(':').ToList();
            }

            set => AddUpdateAppSettings(nameof(PackageExtensionFilterList), string.Join(":", value.ToArray()));
        }

        public bool CheckSignature
        {
            get
            {
                ConfigurationManager.RefreshSection(AppSettingSectionName);
                return Convert.ToBoolean(ConfigurationManager.AppSettings[nameof(CheckSignature)]);
            }

            set => AddUpdateAppSettings(nameof(CheckSignature), value.ToString());
        }

        public bool InstallAppFromSelectedIssuer
        {
            get
            {
                ConfigurationManager.RefreshSection(AppSettingSectionName);
                return Convert.ToBoolean(ConfigurationManager.AppSettings[nameof(InstallAppFromSelectedIssuer)]);
            }

            set => AddUpdateAppSettings(nameof(InstallAppFromSelectedIssuer), value.ToString());
        }

        public List<string> SelectedIssuers
        {
            get
            {
                ConfigurationManager.RefreshSection(AppSettingSectionName);
                return ConfigurationManager.AppSettings[nameof(SelectedIssuers)].Split(':').ToList(); ;
            }
            set => AddUpdateAppSettings(nameof(SelectedIssuers), string.Join(":", value.ToArray()));
        }

        private static void AddUpdateAppSettings(string key, string value)
        {
            var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var settings = configFile.AppSettings.Settings;
            if (settings[key] == null)
            {
                settings.Add(key, value);
            }
            else
            {
                settings[key].Value = value;
            }
            configFile.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
        }
    }
}
