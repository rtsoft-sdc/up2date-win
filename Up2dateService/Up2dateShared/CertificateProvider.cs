using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
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

        private struct PubliKeyDto
        {
            public string modulus { get; set; }
            public string exponent { get; set; }
        }

        private struct HandleDto
        {
            public string handle_id { get; set; }
        }

        private readonly HttpClient client;
        private readonly ISettingsManager settingsManager;
        private readonly Dictionary<string, RSACryptoServiceProvider> sessions = new Dictionary<string, RSACryptoServiceProvider>();

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

        public async Task<Result<string>> OpenRequestCertificateSessionAsync()
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(1024);
            RSAParameters key = rsa.ExportParameters(false);

            Result<string> result;

            try
            {
                string publicKeyJson = JsonConvert.SerializeObject(new PubliKeyDto
                {
                    modulus = Convert.ToBase64String(key.Modulus),
                    exponent = Convert.ToBase64String(key.Exponent)
                });
                using (StringContent content = new StringContent(publicKeyJson, Encoding.UTF8, "application/json"))
                {
                    using (HttpResponseMessage response = await client.PutAsync(
                        $"{settingsManager.RequestCertificateUrl}/certificate", content))
                    {
                        if (response.StatusCode == System.Net.HttpStatusCode.Created || response.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            string handleJson = await response.Content.ReadAsStringAsync();
                            string handle = JsonConvert.DeserializeObject<HandleDto>(handleJson).handle_id;
                            if (string.IsNullOrWhiteSpace(handle))
                            {
                                result = Result<string>.Failed("Server responded wirh empty handle.");
                            }
                            else
                            {
                                result = Result<string>.Successful(handle);
                            }
                        }
                        else
                        {
                            result = Result<string>.Failed(response.ReasonPhrase);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                result = Result<string>.Failed(e);
            }

            if (result.Success)
            {
                sessions.Add(result.Value, rsa);
            }
            else
            {
                rsa.Dispose();
            }

            return result;
        }

        public async Task<Result<string>> GetCertificateBySessionHandleAsync(string handle)
        {
            if (!sessions.ContainsKey(handle))
            {
                return Result<string>.Failed($"Invalid handle {handle}");
            }

            RSACryptoServiceProvider rsa = sessions[handle];

            try
            {
                using (HttpResponseMessage response = await client.GetAsync(
                    $"{settingsManager.RequestCertificateUrl}/certificate?handle_id={handle}"))
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                    {
                        return Result<string>.Successful(string.Empty);
                    }
                    if (response.StatusCode == System.Net.HttpStatusCode.Created || response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        string crtJson = await response.Content.ReadAsStringAsync();
                        string encodedCertBase64 = JsonConvert.DeserializeObject<CrtDto>(crtJson).crt;
                        byte[] encodedCert = Convert.FromBase64String(encodedCertBase64);
                        byte[] certBlob = rsa.Decrypt(encodedCert, RSAEncryptionPadding.Pkcs1);
                        string cert = Encoding.UTF8.GetString(certBlob);
                        return Result<string>.Successful(cert);
                    }
                    return Result<string>.Failed(response.ReasonPhrase);
                }
            }
            catch (Exception e)
            {
                return Result<string>.Failed(e);
            }
        }

        public void CloseRequestCertificateSession(string handle)
        {
            if (sessions.ContainsKey(handle))
            {
                sessions[handle].Dispose();
                sessions.Remove(handle);
            }
        }
    }
}
