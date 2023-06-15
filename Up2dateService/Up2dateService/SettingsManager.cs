using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Up2dateShared;

namespace Up2dateService
{
    public class SettingsManager : ISettingsManager
    {
        private readonly ILogger logger;

        public SettingsManager(ILogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (Properties.Settings.Default.UpgradeFlag)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpgradeFlag = false;
                Properties.Settings.Default.Save();
            }

            WriteSettingsToLog();
        }

        public string ProvisioningUrl
        {
            get => Properties.Settings.Default.ProvisioningUrl;
            set
            {
                Properties.Settings.Default.ProvisioningUrl = value;
                Properties.Settings.Default.Save();
                WriteSettingsToLog();
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
                WriteSettingsToLog();
            }
        }

        public string CertificateThumbprint
        {
            get => Properties.Settings.Default.CertificateThumbprint;
            set
            {
                Properties.Settings.Default.CertificateThumbprint = value;
                Properties.Settings.Default.Save();
                WriteSettingsToLog();
            }
        }

        public List<string> PackageExtensionFilterList
        {
            get => Properties.Settings.Default.PackageExtensionFilterList.Split(':').ToList();
            set
            {
                Properties.Settings.Default.PackageExtensionFilterList = string.Join(":", value);
                Properties.Settings.Default.Save();
                WriteSettingsToLog();
            }
        }

        public bool CheckSignature
        {
            get => Properties.Settings.Default.CheckSignature;
            set
            {
                Properties.Settings.Default.CheckSignature = value;
                Properties.Settings.Default.Save();
                WriteSettingsToLog();
            }
        }

        public SignatureVerificationLevel SignatureVerificationLevel
        {
            get => Properties.Settings.Default.SignatureVerificationLevel;
            set
            {
                Properties.Settings.Default.SignatureVerificationLevel = value;
                Properties.Settings.Default.Save();
                WriteSettingsToLog();
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
                WriteSettingsToLog();
            }
        }

        public bool RequiresConfirmationBeforeInstall
        {
            get => Properties.Settings.Default.RequiresConfirmationBeforeInstall;
            set
            {
                Properties.Settings.Default.RequiresConfirmationBeforeInstall = value;
                Properties.Settings.Default.Save();
                WriteSettingsToLog();
            }
        }

        public string HawkbitUrl
        {
            get => Properties.Settings.Default.HawkbitUrl;
            set
            {
                Properties.Settings.Default.HawkbitUrl = value;
                Properties.Settings.Default.Save();
                WriteSettingsToLog();
            }
        }

        public string DeviceId
        {
            get => Properties.Settings.Default.DeviceId;
            set
            {
                Properties.Settings.Default.DeviceId = value;
                Properties.Settings.Default.Save();
                WriteSettingsToLog();
            }
        }

        public string SecurityToken
        {
            get => Properties.Settings.Default.SecurityToken;
            set
            {
                Properties.Settings.Default.SecurityToken = value;
                Properties.Settings.Default.Save();
                WriteSettingsToLog();
            }
        }

        public bool SecureAuthorizationMode
        {
            get => Properties.Settings.Default.SecureAuthorizationMode;
            set
            {
                Properties.Settings.Default.SecureAuthorizationMode = value;
                Properties.Settings.Default.Save();
                WriteSettingsToLog();
            }
        }

        private void WriteSettingsToLog()
        {
            StringBuilder sb = new StringBuilder("\n");
            sb.AppendLine($"{nameof(ProvisioningUrl)} = {ProvisioningUrl}");
            sb.AppendLine($"{nameof(RequestCertificateUrl)} = {RequestCertificateUrl}");
            sb.AppendLine($"{nameof(CertificateThumbprint)} = {CertificateThumbprint}");
            sb.AppendLine($"{nameof(PackageExtensionFilterList)} = {string.Join("|", PackageExtensionFilterList)}");
            sb.AppendLine($"{nameof(CheckSignature)} = {CheckSignature}");
            sb.AppendLine($"{nameof(SignatureVerificationLevel)} = {SignatureVerificationLevel}");
            sb.AppendLine($"{nameof(DefaultChocoSources)} = {DefaultChocoSources}");
            sb.AppendLine($"{nameof(HawkbitUrl)} = {HawkbitUrl}");
            sb.AppendLine($"{nameof(DeviceId)} = {DeviceId}");
            sb.AppendLine($"{nameof(SecurityToken)} = {SecurityToken}");
            sb.AppendLine($"{nameof(SecureAuthorizationMode)} = {SecureAuthorizationMode}");
            logger.WriteEntry(sb.ToString());
        }
    }
}
