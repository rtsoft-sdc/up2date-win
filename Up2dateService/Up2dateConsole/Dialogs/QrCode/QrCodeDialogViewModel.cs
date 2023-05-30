using System;
using System.Threading;
using System.Windows.Media.Imaging;
using Up2dateConsole.Helpers;
using Up2dateConsole.ViewService;
using System.Net.Http;
using System.Threading.Tasks;

namespace Up2dateConsole.Dialogs.QrCode
{
    public class QrCodeDialogViewModel : DialogViewModelBase
    {
        private readonly IViewService viewService;
        private readonly IQrCodeHelper qrCodeHelper;
        private readonly IWcfClientFactory wcfClientFactory;
        private readonly CancellationTokenSource cancellationTokenSource;
        private BitmapSource bitmap;

        public QrCodeDialogViewModel(IViewService viewService, IQrCodeHelper qrCodeHelper, IWcfClientFactory wcfClientFactory, string clientID)
        {
            this.viewService = viewService ?? throw new ArgumentNullException(nameof(viewService));
            this.qrCodeHelper = qrCodeHelper ?? throw new ArgumentNullException(nameof(qrCodeHelper));
            this.wcfClientFactory = wcfClientFactory ?? throw new ArgumentNullException(nameof(wcfClientFactory));
            if (string.IsNullOrWhiteSpace(clientID))
            {
                throw new ArgumentException($"'{nameof(clientID)}' cannot be null or whitespace.", nameof(clientID));
            }

            cancellationTokenSource = new CancellationTokenSource();

            GetCertAsync(clientID);
        }

        public override bool OnClosing()
        {
            cancellationTokenSource.Cancel();
            return base.OnClosing();
        }

        private async void GetCertAsync(string clientID)
        {
            string handle = string.Empty;
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

                handle = result.Value;

                Bitmap = qrCodeHelper.CreateQrCode($"t.me/RTSOFTbot?start={clientID}_{handle}");

                const int period = 2; // sec
                const int timeout = 120; // sec
                int t;
                for (t = 0; t < timeout; t += period)
                {
                    result = await server.GetCertificateBySessionHandleAsync(handle);
                    if (cancellationTokenSource.IsCancellationRequested) return;

                    if (!result.Success)
                    {
                        await server.CloseRequestCertificateSessionAsync(handle);
                        if (cancellationTokenSource.IsCancellationRequested) return;

                        string message = viewService.GetText(Texts.GetCertificateError) + $"\n\n{result.ErrorMessage}";
                        viewService.ShowMessageBox(message);
                        Close(false);
                        return;
                    }

                    Cert = result.Value;
                    if (!string.IsNullOrWhiteSpace(Cert) || cancellationTokenSource.IsCancellationRequested)
                        break;
                    await Task.Delay(period * 1000, cancellationTokenSource.Token);
                }

                if (!cancellationTokenSource.IsCancellationRequested && string.IsNullOrWhiteSpace(Cert))
                {
                    viewService.ShowMessageBox(viewService.GetText(
                        t < timeout ? Texts.ServerRefusedProvidingCertificate : Texts.TimeoutGettingCertificate));
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
                if (!string.IsNullOrEmpty(handle))
                {
                    server?.CloseRequestCertificateSessionAsync(handle);
                }
            }
        }

        public BitmapSource Bitmap
        {
            get => bitmap;
            private set
            {
                bitmap = value;
                OnPropertyChanged();
            }
        }

        public string Cert { get; private set; }
    }
}
