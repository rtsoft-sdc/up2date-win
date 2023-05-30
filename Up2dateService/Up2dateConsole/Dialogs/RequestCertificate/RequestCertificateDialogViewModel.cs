using System;
using System.Linq;
using System.ServiceProcess;
using System.Threading.Tasks;
using System.Windows.Input;
using Up2dateConsole.Dialogs.QrCode;
using Up2dateConsole.Helpers;
using Up2dateConsole.ServiceReference;
using Up2dateConsole.ViewService;

namespace Up2dateConsole.Dialogs.RequestCertificate
{
    public class RequestCertificateDialogViewModel : DialogViewModelBase
    {
        enum ConnectionMode
        {
            Secure,
            Test
        };

        private readonly IViewService viewService;
        private readonly IWcfClientFactory wcfClientFactory;
        private string oneTimeKey;
        private bool isInProgress;
        private ConnectionMode connectionMode;
        private string hawkbitUrl;
        private string controllerId;
        private string deviceToken;
        private bool isCertificateAvailable;

        public RequestCertificateDialogViewModel(IViewService viewService, IWcfClientFactory wcfClientFactory, bool showExplanation)
        {
            this.viewService = viewService ?? throw new ArgumentNullException(nameof(viewService));
            this.wcfClientFactory = wcfClientFactory ?? throw new ArgumentNullException(nameof(wcfClientFactory));
            ShowExplanation = showExplanation;

            RequestCommand = new RelayCommand(async (_) => await ExecuteRequestAsync(), CanRequest);
            LoadCommand = new RelayCommand(async (_) => await ExecuteLoadAsync());
            QrCodeCommand = new RelayCommand(async (_) => await ExecuteQrCodeAsync(), (_) => IsSecureConnection);

            Initialize();
        }

        private void Initialize()
        {
            IWcfService service = null;
            string error = string.Empty;
            try
            {
                service = wcfClientFactory.CreateClient();
                isCertificateAvailable = service.IsCertificateAvailable();
                connectionMode = service.IsUnsafeConnection() ? ConnectionMode.Test : ConnectionMode.Secure;
                hawkbitUrl = service.GetUnsafeConnectionUrl();
                controllerId = service.GetUnsafeConnectionDeviceId();
                MachineGuid = service.GetSystemInfo().MachineGuid;
                if (string.IsNullOrEmpty(controllerId))
                {
                    controllerId = MachineGuid;
                }
                deviceToken = service.GetUnsafeConnectionToken();
            }
            catch (Exception e)
            {
                error = e.Message;
            }
            finally
            {
                wcfClientFactory.CloseClient(service);
                IsInProgress = false;
            }
        }

        public ICommand RequestCommand { get; }

        public ICommand LoadCommand { get; }

        public ICommand QrCodeCommand { get; }

        public string MachineGuid { get; private set; }

        public string OneTimeKey
        {
            get => oneTimeKey;
            set
            {
                if (oneTimeKey == value) return;
                oneTimeKey = value;
                OnPropertyChanged();
            }
        }

        public bool IsInProgress
        {
            get => isInProgress;
            set
            {
                if (isInProgress == value) return;
                isInProgress = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsEnabled));
            }
        }

        public bool IsEnabled => !IsInProgress;

        public string DeviceId { get; private set; }

        public bool ShowExplanation { get; }

        public bool IsSecureConnection
        {
            get => connectionMode == ConnectionMode.Secure;
            set => SetSecureConnectionMode(value);
        }

        public bool IsTestConnection
        {
            get => connectionMode == ConnectionMode.Test;
            set => SetSecureConnectionMode(!value);
        }

        public string HawkbitUrl
        {
            get => hawkbitUrl;
            set
            {
                if (hawkbitUrl == value) return;
                hawkbitUrl = value;
                OnPropertyChanged();
            }
        }

        public string ControllerId
        {
            get => controllerId;
            set
            {
                if (controllerId == value) return;
                controllerId = value;
                OnPropertyChanged();
            }
        }

        public string DeviceToken
        {
            get => deviceToken;
            set
            {
                if (deviceToken == value) return;
                deviceToken = value;
                OnPropertyChanged();
            }
        }

        private void SetSecureConnectionMode(bool value)
        {
            var newConnectionMode = value ? ConnectionMode.Secure : ConnectionMode.Test;
            if (connectionMode == newConnectionMode) return;
            connectionMode = newConnectionMode;

            OnPropertyChanged(nameof(IsTestConnection));
            OnPropertyChanged(nameof(IsSecureConnection));
        }

        private bool CanRequest(object _)
        {
            if (IsSecureConnection) return isCertificateAvailable || !string.IsNullOrWhiteSpace(OneTimeKey);
            if (IsTestConnection) return !string.IsNullOrWhiteSpace(HawkbitUrl) && !string.IsNullOrWhiteSpace(ControllerId) && !string.IsNullOrEmpty(DeviceToken);
            return false;
        }

        private async Task ExecuteRequestAsync()
        {
            await ImportAndApplyCertificateAsync();
        }

        private async Task ExecuteQrCodeAsync()
        {
            var vm = new QrCodeDialogViewModel(viewService, new QrCodeHelper(), wcfClientFactory, MachineGuid);
            bool ok = viewService.ShowDialog(vm);
            if (ok && !string.IsNullOrWhiteSpace(vm.Cert))
            {
                await ImportAndApplyCertificateAsync(cert: vm.Cert);
            }
        }

        private async Task ExecuteLoadAsync()
        {
            var certFilePath = viewService.ShowOpenDialog(viewService.GetText(Texts.LoadCertificate),
                "X.509 certificate files|*.cer|All files|*.*");
            if (string.IsNullOrWhiteSpace(certFilePath)) return;

            await ImportAndApplyCertificateAsync(certFilePath);
        }

        private async Task ImportAndApplyCertificateAsync(string certFilePath = null, string cert = null)
        {
            IsInProgress = true;

            IWcfService service = null;
            string certificateError = string.Empty;
            try
            {
                service = wcfClientFactory.CreateClient();
                if (IsTestConnection)
                {
                    await service.SetupUnsafeConnectionAsync(HawkbitUrl, ControllerId, DeviceToken);
                }
                else
                {
                    ResultOfstring result = new ResultOfstring { Success = true };
                    if (!string.IsNullOrWhiteSpace(cert))
                    {
                        result = await service.ImportCertificateAsync(cert);
                    }
                    if (!string.IsNullOrWhiteSpace(certFilePath))
                    {
                        result = await service.ImportCertificateFileAsync(certFilePath);
                    }
                    else if (!string.IsNullOrWhiteSpace(OneTimeKey))
                    {
                        result = await service.RequestCertificateAsync(RemoveWhiteSpaces(OneTimeKey));
                    }

                    await service.SetupSecureConnectionAsync();

                    if (!result.Success)
                    {
                        certificateError = result.ErrorMessage;
                    }
                    else
                    {
                        DeviceId = result.Value;
                    }
                }
            }
            catch (Exception e)
            {
                string message = viewService.GetText(Texts.ServiceAccessError) + $"\n\n{e.Message}";
                viewService.ShowMessageBox(message);
                return;
            }
            finally
            {
                wcfClientFactory.CloseClient(service);
                IsInProgress = false;
            }

            if (string.IsNullOrEmpty(certificateError))
            {
                IsInProgress = true;
                await Task.Run(() => RestartService(20000));
                await Task.Delay(5000);
                Close(true);
            }
            else
            {
                string message = viewService.GetText(Texts.FailedToAcquireCertificate) + $"\n\n{certificateError}";
                viewService.ShowMessageBox(message);
            }
        }

        private static string RemoveWhiteSpaces(string str)
        {
            return new string(str.Where(c => !char.IsWhiteSpace(c)).ToArray());
        }

        public static void RestartService(int timeout)
        {
            ServiceController service = new ServiceController("Up2dateService");
            try
            {
                int started = Environment.TickCount;
                if (service.Status != ServiceControllerStatus.Stopped)
                {
                    service.Stop();
                }
                service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromMilliseconds(timeout));

                int elapsed = Environment.TickCount - started;
                service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMilliseconds(timeout - elapsed));
            }
            catch
            {
            }
        }
    }
}
