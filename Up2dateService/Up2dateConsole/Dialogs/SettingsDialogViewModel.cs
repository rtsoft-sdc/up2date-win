using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Input;
using Up2dateConsole.Helpers;
using Up2dateConsole.ServiceReference;
using Up2dateConsole.ViewService;

namespace Up2dateConsole.Dialogs
{
    public class SettingsDialogViewModel : DialogViewModelBase
    {
        private readonly IViewService viewService;
        private readonly IWcfClientFactory wcfClientFactory;
        private string tokenUrl;
        private string dpsUrl;
        private bool checkSignatureStatus;
        private bool confirmBeforeInstallation;
        private SignatureVerificationLevel signatureVerificationLevel;

        public SettingsDialogViewModel(IViewService viewService, IWcfClientFactory wcfClientFactory)
        {
            this.viewService = viewService ?? throw new ArgumentNullException(nameof(viewService));
            this.wcfClientFactory = wcfClientFactory ?? throw new ArgumentNullException(nameof(wcfClientFactory));

            IsInitialized = Initialize();

            OkCommand = new RelayCommand(ExecuteOk, CanOk);
            AddCertificateCommand = new RelayCommand(ExecuteAddCertificate, CanAddCertificate);
            LaunchCertMgrShapinCommand = new RelayCommand(ExecuteLaunchCertMgrShapin);
        }

        public bool IsInitialized { get; }

        public ICommand OkCommand { get; }

        public ICommand AddCertificateCommand { get; }

        public ICommand LaunchCertMgrShapinCommand { get; }

        public string TokenUrl
        {
            get => tokenUrl;
            set
            {
                if (tokenUrl == value) return;
                tokenUrl = value;
                OnPropertyChanged();
            }
        }

        public string DpsUrl
        {
            get => dpsUrl;
            set
            {
                if (dpsUrl == value) return;
                dpsUrl = value;
                OnPropertyChanged();
            }
        }

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

        private bool CanOk(object obj)
        {
            return !string.IsNullOrWhiteSpace(TokenUrl) && !string.IsNullOrWhiteSpace(DpsUrl);
        }

        private void ExecuteOk(object obj)
        {
            IWcfService service = null;
            string error = string.Empty;
            try
            {
                service = wcfClientFactory.CreateClient();
                if (SignatureVerificationLevel == SignatureVerificationLevel.SignedByWhitelistedCertificate
                    && service.GetWhitelistedCertificates().Length == 0)
                {
                    var r = viewService.ShowMessageBox(Texts.NoAnyWhitelistedCertificate, System.Windows.MessageBoxButton.OKCancel);
                    if (r == System.Windows.MessageBoxResult.Cancel) return;
                }

                service.SetRequestCertificateUrl(TokenUrl);
                service.SetProvisioningUrl(DpsUrl);
                service.SetCheckSignature(CheckSignatureStatus);
                service.SetSignatureVerificationLevel(SignatureVerificationLevel);
                service.SetConfirmBeforeInstallation(ConfirmBeforeInstallation);
            }
            catch (Exception e)
            {
                error = e.Message;
            }
            finally
            {
                wcfClientFactory.CloseClient(service);
            }

            if (!string.IsNullOrEmpty(error))
            {
                viewService.ShowMessageBox(Texts.ServiceAccessError);
                Close(false);
            }

            Close(true);
        }

        private bool Initialize()
        {
            IWcfService service = null;
            string error = string.Empty;
            try
            {
                service = wcfClientFactory.CreateClient();
                TokenUrl = service.GetRequestCertificateUrl();
                DpsUrl = service.GetProvisioningUrl();
                checkSignatureStatus = service.GetCheckSignature();
                signatureVerificationLevel = service.GetSignatureVerificationLevel();
                confirmBeforeInstallation = service.GetConfirmBeforeInstallation();
            }
            catch (Exception e)
            {
                error = e.Message;
            }
            finally
            {
                wcfClientFactory.CloseClient(service);
            }

            if (!string.IsNullOrEmpty(error))
            {
                viewService.ShowMessageBox(Texts.ServiceAccessError);
                return false;
            }

            return true;
        }
    }
}
