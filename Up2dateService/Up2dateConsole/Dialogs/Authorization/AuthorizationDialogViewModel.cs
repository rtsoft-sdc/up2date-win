using System;
using System.ServiceProcess;
using System.Threading.Tasks;
using Up2dateConsole.ServiceReference;
using Up2dateConsole.ViewService;

namespace Up2dateConsole.Dialogs.Authorization
{
    public class AuthorizationDialogViewModel : DialogViewModelBase
    {
        private enum Mode
        {
            QrCode,
            OneTimeToken,
            ImportCertificate,
            Reconnect,
            PlainToken
        };

        private Mode mode;
        private bool isInProgress;
        private readonly IViewService viewService;
        private readonly IWcfClientFactory wcfClientFactory;

        public AuthorizationDialogViewModel(IViewService viewService, IWcfClientFactory wcfClientFactory, bool showExplanation)
        {
            this.viewService = viewService ?? throw new ArgumentNullException(nameof(viewService));
            this.wcfClientFactory = wcfClientFactory ?? throw new ArgumentNullException(nameof(wcfClientFactory));
            ShowExplanation = showExplanation;

            QrCodePage = new QrCodePageViewModel(EstablishConnection, viewService, wcfClientFactory);
            OneTimeTokenPage = new OneTimeTokenPageViewModel(EstablishConnection);
            ImportCertificatePage = new ImportCertificatePageViewModel(EstablishConnection, viewService);
            PlainTokenPage = new PlainTokenPageViewModel(EstablishConnection);
            ReconnectPage = new ReconnectPageViewModel(EstablishConnection);

            Initialize();
        }

        private void Initialize()
        {
            IWcfService service = null;
            string error = string.Empty;
            try
            {
                service = wcfClientFactory.CreateClient();
                IsCertificateAvailable = service.IsCertificateAvailable();
                mode = service.IsUnsafeConnection() ? Mode.PlainToken : Mode.QrCode;
                MachineGuid = service.GetSystemInfo().MachineGuid;
                PlainTokenPage.Initialize(service);
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

        public bool IsCertificateAvailable { get; private set; }

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

        public string MachineGuid { get; private set; }

        public bool ShowExplanation { get; }

        public QrCodePageViewModel QrCodePage { get; }

        public OneTimeTokenPageViewModel OneTimeTokenPage { get; }

        public ImportCertificatePageViewModel ImportCertificatePage { get; }

        public ReconnectPageViewModel ReconnectPage { get; }

        public PlainTokenPageViewModel PlainTokenPage { get; }

        public bool IsQrCodeMode
        {
            get => mode == Mode.QrCode;
            set => SetMode(value, Mode.QrCode);
        }

        public bool IsOneTimeTokenMode
        {
            get => mode == Mode.OneTimeToken;
            set => SetMode(value, Mode.OneTimeToken);
        }

        public bool IsImportCertificateMode
        {
            get => mode == Mode.ImportCertificate;
            set => SetMode(value, Mode.ImportCertificate);
        }

        public bool IsReconnectMode
        {
            get => mode == Mode.Reconnect;
            set => SetMode(value, Mode.Reconnect);
        }

        public bool IsPlainTokenMode
        {
            get => mode == Mode.PlainToken;
            set => SetMode(value, Mode.PlainToken);
        }

        private void SetMode(bool set, Mode mode)
        {
            if (!set || this.mode == mode) return;

            this.mode = mode;
            OnPropertyChanged(nameof(IsQrCodeMode));
            OnPropertyChanged(nameof(IsOneTimeTokenMode));
            OnPropertyChanged(nameof(IsImportCertificateMode));
            OnPropertyChanged(nameof(IsPlainTokenMode));
            OnPropertyChanged(nameof(IsReconnectMode));
        }

        private async Task EstablishConnection(Func<IWcfService, Task<ResultOfstring>> setupCredentials, bool isSecureConnection)
        {
            IsInProgress = true;

            IWcfService service = null;
            string certificateError = string.Empty;
            try
            {
                service = wcfClientFactory.CreateClient();

                ResultOfstring result = new ResultOfstring() { Success = true };

                if (!(setupCredentials is null))
                {
                    result = await setupCredentials.Invoke(service);
                }

                if (isSecureConnection)
                {
                    await service.SetupSecureConnectionAsync();
                }

                if (!result.Success)
                {
                    certificateError = result.ErrorMessage;
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
