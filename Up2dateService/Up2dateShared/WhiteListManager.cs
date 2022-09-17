using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Up2dateShared
{
    public class WhiteListManager : IWhiteListManager
    {
        private const string defaultWhiteListStoreName = "RITMS_UP2DATE_WhiteList";
        private readonly string whiteListStoreName;
        private readonly StoreLocation whiteListStoreLocation;

        public WhiteListManager(string whiteListStoreName = defaultWhiteListStoreName, StoreLocation whiteListStoreLocation = StoreLocation.LocalMachine)
        {
            this.whiteListStoreName = whiteListStoreName;
            this.whiteListStoreLocation = whiteListStoreLocation;
        }

        public IList<X509Certificate2> GetWhitelistedCertificates()
        {
            var certs = new List<X509Certificate2>();
            using (X509Store store = new X509Store(whiteListStoreName, whiteListStoreLocation))
            {
                store.Open(OpenFlags.ReadOnly);
                X509Certificate2Enumerator enumerator = store.Certificates.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    certs.Add(enumerator.Current);
                }
                store.Close();
            }
            return certs;
        }

        public IList<string> GetWhitelistedCertificatesSha256()
        {
            var certs = GetWhitelistedCertificates();
            return certs.Select(cert => GetSha256(cert)).ToList();
        }

        public void RemoveCertificateFromWhilelist(X509Certificate2 certificate)
        {
            using (X509Store store = new X509Store(whiteListStoreName, whiteListStoreLocation))
            {
                store.Open(OpenFlags.ReadWrite);
                store.Remove(certificate);
                store.Close();
            }
        }

        public Result AddCertificateToWhitelist(X509Certificate2 certificate)
        {
            try
            {
                using (X509Store store = new X509Store(whiteListStoreName, whiteListStoreLocation))
                {
                    store.Open(OpenFlags.ReadWrite);
                    store.Add(certificate);
                    store.Close();
                }
            }
            catch (Exception e)
            {
                return Result.Failed(e.Message);
            }

            return Result.Successful();
        }

        public Result AddCertificateToWhitelist(string certificateFilePath)
        {
            X509Certificate2 cert;
            try
            {
                cert = new X509Certificate2(certificateFilePath);
            }
            catch (Exception e)
            {
                return Result.Failed(e.Message);
            }

            return AddCertificateToWhitelist(cert);
        }

        public bool IsWhitelistedCertificate(X509Certificate2 certificate)
        {
            using (X509Store store = new X509Store(whiteListStoreName, whiteListStoreLocation))
            {
                store.Open(OpenFlags.ReadOnly);
                bool result = store.Certificates.Contains(certificate);
                store.Close();

                return result;
            }
        }

        private string GetSha256(X509Certificate2 cert)
        {
            byte[] hashBytes;
            using (var hasher = new SHA256Managed())
            {
                hashBytes = hasher.ComputeHash(cert.RawData);
            }
            string result = BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLower();
            //string result = hashBytes.Aggregate(String.Empty, (str, hashByte) => str + hashByte.ToString("x2"))
            return result;
        }
    }
}
