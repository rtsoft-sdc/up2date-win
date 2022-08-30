using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Microsoft.Win32;

namespace Up2dateShared
{
    public class SettingsManager : ISettingsManager
    {
        private const string AppSettingSectionName = "appSettings";

        public string ProvisioningUrl
        {
            get
            {
                ConfigurationManager.RefreshSection(AppSettingSectionName);
                return ConfigurationManager.AppSettings[nameof(ProvisioningUrl)];
            }
            set
            {
                AddUpdateAppSettings(nameof(ProvisioningUrl), value);
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
            set
            {
                AddUpdateAppSettings(nameof(RequestCertificateUrl), value);
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
                return ConfigurationManager.AppSettings[nameof(SelectedIssuers)].Split(':').ToList();
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
