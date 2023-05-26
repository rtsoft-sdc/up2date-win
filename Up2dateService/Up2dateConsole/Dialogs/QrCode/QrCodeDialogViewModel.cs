using Newtonsoft.Json.Linq;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Windows.Media.Imaging;
using Up2dateConsole.Helpers;
using Up2dateConsole.ViewService;
using System.Security.Cryptography;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net;
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
                if (!result.Success)
                {
                    string message = result.ErrorMessage; //string.Format(viewService.GetText(Texts.RequestOneTimeTokenUrlAccessError), requestCertUrl) + $"\n\n{result.ErrorMessage}";
                    viewService.ShowMessageBox(message);
                    Close(false);
                    return;
                }

                handle = result.Value;

                Bitmap = qrCodeHelper.CreateQrCode($"t.me/RTSOFTbot?start={clientID}_{handle}");

                const int period = 2; // sec
                const int timeout = 120; // sec
                string cert = string.Empty;
                for (int i = 0; i < timeout; i += period)
                {
                    result = await server.GetCertificateBySessionHandleAsync(handle);
                    if (!result.Success)
                    {
                        await server.CloseRequestCertificateSessionAsync(handle);
                        string message = result.ErrorMessage; //string.Format(viewService.GetText(Texts.RequestOneTimeTokenUrlAccessError), requestCertUrl) + $"\n\n{result.ErrorMessage}";
                        viewService.ShowMessageBox(message);
                        Close(false);
                        return;
                    }

                    Cert = result.Value;
                    if (!string.IsNullOrWhiteSpace(Cert) || cancellationTokenSource.IsCancellationRequested)
                        break;
                    await Task.Delay(period * 1000);
                }
                Close(!string.IsNullOrWhiteSpace(Cert));
            }
            catch (HttpRequestException e)
            {
                string message = e.Message; //string.Format(viewService.GetText(Texts.RequestOneTimeTokenUrlAccessError), requestCertUrl) + $"\n\n{e.Message}";
                viewService.ShowMessageBox(message);
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
