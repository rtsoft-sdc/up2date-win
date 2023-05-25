using Newtonsoft.Json.Linq;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Windows.Media.Imaging;
using Up2dateConsole.Helpers;
using Up2dateConsole.ViewService;

namespace Up2dateConsole.Dialogs.QrCode
{
    public class QrCodeDialogViewModel : DialogViewModelBase
    {
        private readonly IViewService viewService;
        private readonly IQrCodeHelper qrCodeHelper;
        private readonly CancellationTokenSource cancellationTokenSource;
        private BitmapSource bitmap;

        public QrCodeDialogViewModel(IViewService viewService, IQrCodeHelper qrCodeHelper, string clientID, string requestID, string requestOneTimeTokenUrl)
        {
            this.viewService = viewService ?? throw new ArgumentNullException(nameof(viewService));
            this.qrCodeHelper = qrCodeHelper ?? throw new ArgumentNullException(nameof(qrCodeHelper));

            if (string.IsNullOrWhiteSpace(clientID))
            {
                throw new ArgumentException($"'{nameof(clientID)}' cannot be null or whitespace.", nameof(clientID));
            }

            if (string.IsNullOrWhiteSpace(requestID))
            {
                throw new ArgumentException($"'{nameof(requestID)}' cannot be null or whitespace.", nameof(requestID));
            }

            if (string.IsNullOrWhiteSpace(requestOneTimeTokenUrl))
            {
                throw new System.ArgumentException($"'{nameof(requestOneTimeTokenUrl)}' cannot be null or whitespace.", nameof(requestOneTimeTokenUrl));
            }

            cancellationTokenSource = new CancellationTokenSource();

            GetAttAsync(requestOneTimeTokenUrl, clientID, requestID);
        }

        public override bool OnClosing()
        {
            cancellationTokenSource.Cancel();
            return base.OnClosing();
        }

        private async void GetAttAsync(string requestOneTimeTokenUrl, string clientID, string requestID)
        {
            using (ClientWebSocket client = new ClientWebSocket())
            {
                try
                {
                    await client.ConnectAsync(
                        new Uri($"{requestOneTimeTokenUrl}/request-ott?client_id={clientID}&request_id={requestID}"), 
                        cancellationTokenSource.Token);

                    Bitmap = qrCodeHelper.CreateQrCode($"t.me/RTSOFTbot?start={clientID}_{requestID}");

                    ArraySegment<byte> response = new ArraySegment<byte>(new byte[2048]);
                    WebSocketReceiveResult r = await client.ReceiveAsync(response, cancellationTokenSource.Token);
                    string s = Encoding.UTF8.GetString(response.Array, 0, r.Count);
                    JObject json = JObject.Parse(s);
                    OTT = json["ott"].ToString();

                    Close(true);
                }
                catch (Exception e)
                {
                    if (!cancellationTokenSource.IsCancellationRequested)
                    {
                        string message = string.Format(viewService.GetText(Texts.RequestOneTimeTokenUrlAccessError), requestOneTimeTokenUrl) + $"\n\n{e.Message}";
                        viewService.ShowMessageBox(message);
                    }
                }
                finally
                {
                    if (client.State == WebSocketState.Open || client.State == WebSocketState.Aborted)
                    {
                        await client.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    }
                }
            }
            if (!cancellationTokenSource.IsCancellationRequested)
            {
                Close(false);
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

        public string OTT { get; private set; }
    }
}
