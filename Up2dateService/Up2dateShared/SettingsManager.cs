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
                return ConfigurationManager.AppSettings["ProvisioningUrl"];
            }
        }

        public string XApigToken
        {
            get
            {
                ConfigurationManager.RefreshSection(AppSettingSectionName);
                return ConfigurationManager.AppSettings["XApigToken"];
            }
        }

        public string RequestCertificateUrl
        {
            get
            {
                ConfigurationManager.RefreshSection(AppSettingSectionName);
                return ConfigurationManager.AppSettings["RequestCertificateUrl"];
            }
        }
    }
}
