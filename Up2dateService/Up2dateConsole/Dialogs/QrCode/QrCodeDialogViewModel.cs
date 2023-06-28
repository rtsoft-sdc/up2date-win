using System;
using System.Threading;
using System.Windows.Media.Imaging;
using Up2dateConsole.Helpers;
using Up2dateConsole.ViewService;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Diagnostics;

namespace Up2dateConsole.Dialogs.QrCode
{
    public class QrCodeDialogViewModel : DialogViewModelBase
    {
        private readonly IViewService viewService;
        private readonly IQrCodeHelper qrCodeHelper;
        private readonly IWcfClientFactory wcfClientFactory;
        private readonly IProcessHelper processHelper;
        private readonly CancellationTokenSource cancellationTokenSource;
        private BitmapSource bitmap;
        private TimeSpan timeLeft;
        private string handle;
        private string approveUrl;

        public QrCodeDialogViewModel(IViewService viewService, IQrCodeHelper qrCodeHelper, IWcfClientFactory wcfClientFactory, IProcessHelper processHelper)
        {
            this.viewService = viewService ?? throw new ArgumentNullException(nameof(viewService));
            this.qrCodeHelper = qrCodeHelper ?? throw new ArgumentNullException(nameof(qrCodeHelper));
            this.wcfClientFactory = wcfClientFactory ?? throw new ArgumentNullException(nameof(wcfClientFactory));
            this.processHelper = processHelper ?? throw new ArgumentNullException(nameof(processHelper));
            cancellationTokenSource = new CancellationTokenSource();

            ApproveUrlCommand = new RelayCommand(ExecuteApproveUrlCommand);

            GetCertAsync();
        }

        public override bool OnClosing()
        {
            cancellationTokenSource.Cancel();
            return base.OnClosing();
        }

        private async void GetCertAsync()
        {
            ServiceReference.IWcfService server = null;
            try
            {
                server = wcfClientFactory.CreateClient();

                var result = await server.OpenRequestCertificateSessionAsync();
                if (cancellationTokenSource.IsCancellationRequested) return;

                if (!result.Success)
                {
                    string message = viewService.GetText(Texts.RequestCertificateError) + $"\n\n{result.ErrorMessage}";
                    viewService.ShowMessageBox(message);
                    Close(false);
                    return;
                }

                Handle = result.Value;

                ApproveUrl = $"http://t.me/RTSOFTbot?start=approve_{Handle}";

                Bitmap = qrCodeHelper.CreateQrCode(ApproveUrl);

                const int period = 3; // sec
                const int timeout = 180; // sec
                int t;
                for (t = timeout; t > 0; t--)
                {
                    TimeLeft = TimeSpan.FromSeconds(t);

                    if ((t % period) == 0)
                    {
                        result = await server.GetCertificateBySessionHandleAsync(Handle);
                        if (cancellationTokenSource.IsCancellationRequested) return;

                        if (!result.Success)
                        {
                            await server.CloseRequestCertificateSessionAsync(Handle);
                            if (cancellationTokenSource.IsCancellationRequested) return;

                            string message = viewService.GetText(Texts.GetCertificateError) + $"\n\n{result.ErrorMessage}";
                            viewService.ShowMessageBox(message);
                            Close(false);
                            return;
                        }

                        Cert = result.Value;
                    }

                    if (!string.IsNullOrWhiteSpace(Cert) || cancellationTokenSource.IsCancellationRequested)
                        break;
                    await Task.Delay(1000, cancellationTokenSource.Token);
                }

                TimeLeft = TimeSpan.FromSeconds(t);

                if (!cancellationTokenSource.IsCancellationRequested && string.IsNullOrWhiteSpace(Cert))
                {
                    viewService.ShowMessageBox(viewService.GetText(
                        t > 0 ? Texts.ServerRefusedProvidingCertificate : Texts.TimeoutGettingCertificate));
                }

                Close(!string.IsNullOrWhiteSpace(Cert));
            }
            catch (TaskCanceledException)
            {
            }
            catch (HttpRequestException e)
            {
                viewService.ShowMessageBox(e.Message);
                Close(false);
                return;
            }
            finally
            {
                if (!string.IsNullOrEmpty(Handle))
                {
                    server?.CloseRequestCertificateSessionAsync(Handle);
                }
            }
        }


        public ICommand ApproveUrlCommand { get; }

        public bool IsConnecting => Bitmap == null;

        public string ApproveUrl
        {
            get => approveUrl;
            set
            {
                approveUrl = value;
                OnPropertyChanged();
            }
        }

        public string Handle
        {
            get => handle;
            private set
            {
                handle = value;
                OnPropertyChanged();
            }
        }

        public BitmapSource Bitmap
        {
            get => bitmap;
            private set
            {
                bitmap = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsConnecting));
            }
        }

        public TimeSpan TimeLeft
        {
            get => timeLeft;
            private set
            {
                timeLeft = value;
                OnPropertyChanged();
            }
        }

        public string Cert { get; private set; }

        private void ExecuteApproveUrlCommand(object _)
        {
            var sInfo = new ProcessStartInfo(ApproveUrl)
            {
                UseShellExecute = true
            };
            processHelper.StartProcess(sInfo);
        }
    }
}
