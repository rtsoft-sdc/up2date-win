using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Input;
using Up2dateConsole.Helpers;
using Up2dateConsole.ServiceReference;
using Up2dateConsole.ViewService;

namespace Up2dateConsole.Dialogs.Settings
{
    public class InstallationPolicyTabViewModel : NotifyPropertyChanged
    {
        private bool checkSignatureStatus;
        private bool confirmBeforeInstallation;
        private SignatureVerificationLevel signatureVerificationLevel;
        private readonly IViewService viewService;
        private readonly IWcfClientFactory wcfClientFactory;

        public InstallationPolicyTabViewModel(string header, IViewService viewService, IWcfClientFactory wcfClientFactory)
        {
            Header = header;
            this.viewService = viewService ?? throw new ArgumentNullException(nameof(viewService));
            this.wcfClientFactory = wcfClientFactory ?? throw new ArgumentNullException(nameof(wcfClientFactory));

            AddCertificateCommand = new RelayCommand(ExecuteAddCertificate, CanAddCertificate);
            LaunchCertMgrShapinCommand = new RelayCommand(ExecuteLaunchCertMgrShapin);
        }

        public string Header { get; }

        public bool Initialize(IWcfService service)
        {
            checkSignatureStatus = service.GetCheckSignature();
            signatureVerificationLevel = service.GetSignatureVerificationLevel();
            confirmBeforeInstallation = service.GetConfirmBeforeInstallation();

            return true;
        }

        public bool IsValid => true;

        public bool Apply(IWcfService service)
        {
            if (SignatureVerificationLevel == SignatureVerificationLevel.SignedByWhitelistedCertificate
                && service.GetWhitelistedCertificates().Length == 0)
            {
                var r = viewService.ShowMessageBox(Texts.NoAnyWhitelistedCertificate, System.Windows.MessageBoxButton.OKCancel);
                if (r == System.Windows.MessageBoxResult.Cancel) return false;
            }

            service.SetCheckSignature(CheckSignatureStatus);
            service.SetSignatureVerificationLevel(SignatureVerificationLevel);
            service.SetConfirmBeforeInstallation(ConfirmBeforeInstallation);

            return true;
        }

        public ICommand AddCertificateCommand { get; }

        public ICommand LaunchCertMgrShapinCommand { get; }

        public bool CheckSignatureStatus
        {
            get => checkSignatureStatus;
            set
            {
                if (checkSignatureStatus == value) return;
                checkSignatureStatus = value;
                OnPropertyChanged();
            }
        }

        public bool ConfirmBeforeInstallation
        {
            get => confirmBeforeInstallation;
            set
            {
                if (confirmBeforeInstallation == value) return;
                confirmBeforeInstallation = value;
                OnPropertyChanged();
            }
        }

        public SignatureVerificationLevel SignatureVerificationLevel
        {
            get => signatureVerificationLevel;
            set
            {
                if (signatureVerificationLevel == value) return;
                signatureVerificationLevel = value;
                OnPropertyChanged();
            }
        }

        private void ExecuteLaunchCertMgrShapin(object obj)
        {
            using (var p = new Process())
            {
                p.StartInfo.FileName = "mmc.exe";
                p.StartInfo.Arguments = "WhiteList.msc";
                p.StartInfo.WorkingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                p.StartInfo.UseShellExecute = false;
                p.Start();
            }
        }

        private bool CanAddCertificate(object obj)
        {
            return CheckSignatureStatus && SignatureVerificationLevel == SignatureVerificationLevel.SignedByWhitelistedCertificate;
        }

        private void ExecuteAddCertificate(object obj)
        {
            var certFilePath = viewService.ShowOpenDialog(viewService.GetText(Texts.AddCertificateToWhiteList),
                "X.509 certificate files|*.cer|All files|*.*");
            if (string.IsNullOrWhiteSpace(certFilePath)) return;

            IWcfService service = null;
            try
            {
                service = wcfClientFactory.CreateClient();
                if (!service.IsCertificateValidAndTrusted(certFilePath))
                {
                    var r = viewService.ShowMessageBox(Texts.InvalidCertificateForWhiteList, System.Windows.MessageBoxButton.YesNo);
                    if (r == System.Windows.MessageBoxResult.No) return;
                }

                var result = service.AddCertificateToWhitelist(certFilePath);
                if (result.Success)
                {
                    viewService.ShowMessageBox(Texts.CertificateAddedToWhiteList);
                }
                else
                {
                    string message = string.Format(viewService.GetText(Texts.FailedToAddCertificateToWhiteList), result.ErrorMessage);
                    viewService.ShowMessageBox(message);
                }
            }
            catch (Exception e)
            {
                string message = string.Format(viewService.GetText(Texts.FailedToAddCertificateToWhiteList), e.Message);
                viewService.ShowMessageBox(message);
            }
            finally
            {
                wcfClientFactory.CloseClient(service);
            }
        }

    }
}
