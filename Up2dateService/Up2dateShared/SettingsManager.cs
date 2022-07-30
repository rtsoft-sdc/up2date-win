using System.Configuration;

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
