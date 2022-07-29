using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Up2dateShared
{
    public class CertificateProvider : ICertificateProvider
    {
        private struct OneTimeTokenDto
        {
            public string oneTimeToken { get; set; }
        }

        private struct CrtDto
        {
            public string crt { get; set; }
        }

        private readonly HttpClient client;
        private readonly ISettingsManager settingsManager;

        public CertificateProvider(ISettingsManager settingsManager)
        {
            this.settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));

            client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<string> RequestCertificateAsync(string ott)
        {
            string oneTimeTokenJson = JsonConvert.SerializeObject(new OneTimeTokenDto { oneTimeToken = ott });
            using (StringContent content = new StringContent(oneTimeTokenJson, Encoding.UTF8, "application/json"))
            {
                using (HttpResponseMessage response = await client.PostAsync(settingsManager.RequestCertificateUrl, content))
                {
                    string crtJson = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<CrtDto>(crtJson).crt;
                }
            }
        }
    }
}
