using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Up2dateConsole.Helpers;
using Up2dateConsole.ViewService;

namespace Up2dateConsole.Dialogs.QrCode
{
    public class QrCodeDialogViewModel : DialogViewModelBase
    {
        private readonly IViewService viewService;

        public QrCodeDialogViewModel(IViewService viewService, IQrCodeHelper qrCodeHelper, string clientID, string requestID, string requestOneTimeTokenUrl)
        {
            this.viewService = viewService ?? throw new ArgumentNullException(nameof(viewService));

            if (qrCodeHelper is null)
            {
                throw new ArgumentNullException(nameof(qrCodeHelper));
            }

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

            Bitmap = qrCodeHelper.CreateQrCode($"t.me/RTSOFTbot?start={clientID}_{requestID}");
            _ = LongPoll(requestOneTimeTokenUrl, clientID, requestID);
        }

        private async Task LongPoll(string requestOneTimeTokenUrl, string clientID, string requestID)
        {
            string uri = $"{requestOneTimeTokenUrl}/request-ott?client_id={clientID}&request_id={requestID}";
            try
            {
                using (HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(300) })
                {
                    using (var msg = await client.GetAsync(uri))
                    {
                        if (msg.IsSuccessStatusCode)
                        {
                            string response = await msg.Content.ReadAsStringAsync();
                            JObject json = JObject.Parse(response);
                            OTT = json["ott"].ToString();
                            Close(true);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                string message = string.Format(viewService.GetText(Texts.RequestOneTimeTokenUrlAccessError), requestOneTimeTokenUrl) + $"\n\n{e.Message}";
                viewService.ShowMessageBox(message);
            }
            Close(false);
        }

        public BitmapSource Bitmap { get; }

        public string OTT { get; private set; }
    }
}
