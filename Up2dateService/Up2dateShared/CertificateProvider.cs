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

        private struct RequestDto
        {
            public string machineGUID { get; set; }
            public string modulus { get; set; }
            public string exponent { get; set; }
        }

        private struct HandleDto
        {
            public string handle_id { get; set; }
        }

        private struct ErrDetailDto
        {
            public string detail { get; set; }
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

        public async Task<Result<string>> RequestCertificateAsync(string ott)
        {
            string oneTimeTokenJson = JsonConvert.SerializeObject(new OneTimeTokenDto { oneTimeToken = ott });
            using (StringContent content = new StringContent(oneTimeTokenJson, Encoding.UTF8, "application/json"))
            {
                using (HttpResponseMessage response = await client.PostAsync(settingsManager.RequestCertificateUrl, content))
                {
                    string resp_str = await response.Content.ReadAsStringAsync();
                    return response.IsSuccessStatusCode
                        ? Result<string>.Successful(JsonConvert.DeserializeObject<CrtDto>(resp_str).crt)
                        : await GetError(response);
                }
            }
        }

        public async Task<Result<string>> OpenRequestCertificateSessionAsync(string machineGuid)
        {
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(1024);
            RSAParameters key = rsa.ExportParameters(false);

            Result<string> result;

            try
            {
                string publicKeyJson = JsonConvert.SerializeObject(new RequestDto
                {
                    machineGUID = machineGuid,
                    modulus = Convert.ToBase64String(key.Modulus),
                    exponent = Convert.ToBase64String(key.Exponent)
                });
                using (StringContent content = new StringContent(publicKeyJson, Encoding.UTF8, "application/json"))
                {
                    using (HttpResponseMessage response = await client.PutAsync(
                        $"{settingsManager.RequestCertificateUrl}/certificate", content))
                    {
                        string resp_str = await response.Content.ReadAsStringAsync();
                        result = response.IsSuccessStatusCode
                            ? Result<string>.Successful(JsonConvert.DeserializeObject<HandleDto>(resp_str).handle_id)
                            : await GetError(response);
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

                    string resp_str = await response.Content.ReadAsStringAsync();
                    
                    if (!response.IsSuccessStatusCode)
                        return await GetError(response);

                    const int chunkSize = 128;
                    string crtJson = await response.Content.ReadAsStringAsync();
                    string encodedCertBase64 = JsonConvert.DeserializeObject<CrtDto>(crtJson).crt;
                    byte[] encodedCert = Convert.FromBase64String(encodedCertBase64);
                    List<byte> certBlob = new List<byte>();
                    for (int i = 0; i < encodedCert.Length; i += chunkSize)
                    {
                        byte[] chunk = new byte[chunkSize];
                        Array.Copy(encodedCert, i, chunk, 0, chunkSize);
                        byte[] decodedChunk = rsa.Decrypt(chunk, RSAEncryptionPadding.Pkcs1);
                        certBlob.AddRange(decodedChunk);
                    }
                    string cert = Encoding.UTF8.GetString(certBlob.ToArray());
                    return Result<string>.Successful(cert);
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

        private async Task<Result<string>> GetError(HttpResponseMessage response)
        {
            string resp_str = await response.Content.ReadAsStringAsync();
            try
            {
                ErrDetailDto err = JsonConvert.DeserializeObject<ErrDetailDto>(resp_str);
                return Result<string>.Failed(err.detail ?? response.ReasonPhrase);
            }
            catch
            {
                return Result<string>.Failed(response.ReasonPhrase);
            }
        }
    }
}
