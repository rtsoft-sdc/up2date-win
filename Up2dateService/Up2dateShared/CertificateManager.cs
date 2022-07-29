using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Up2dateShared
{
    public class CertificateManager : ICertificateManager
    {
        private readonly EventLog eventLog;
        private X509Certificate2 certificate;

        public X509Certificate2 Certificate
        {
            get => certificate;
            private set
            {
                if (certificate != null)
                {
                    certificate.Dispose();
                }
                certificate = value;
            }
        }

        public string CertificateIssuerName => GetCN(certificate?.Issuer);

        public string CertificateSubjectName => GetCN(certificate?.Subject);

        public CertificateManager(EventLog eventLog)
        {
            this.eventLog = eventLog;
        }

        public void ImportCertificate(byte[] certificateData)
        {
            Certificate = new X509Certificate2(certificateData);
            ImportCertificate(Certificate);
        }

        public void ImportCertificate(string fileName)
        {
            Certificate = new X509Certificate2(fileName);
            ImportCertificate(Certificate);
        }

        public string GetCertificateString()
        {
            if (Certificate == null)
            {
                LoadCertificate();
            }

            if (Certificate == null) return null;

            byte[] arr = Certificate.GetRawCertData();
            return "-----BEGIN CERTIFICATE-----" + Convert.ToBase64String(arr) + "-----END CERTIFICATE-----";
        }

        public bool IsCertificateAvailable()
        {
            return Certificate != null;
        }

        private void LoadCertificate()
        {
            Certificate = TryGetCertificate(StoreName.TrustedPublisher);
            if (Certificate != null)
            {
                eventLog?.WriteEntry($"Certificate found; subject: '{Certificate.Subject}'");
            }
            eventLog?.WriteEntry($"Cannot find certificate in TrustedPublisher certificate store!");
        }

        private static X509Certificate2 TryGetCertificate(StoreName storeName)
        {
            const string certificateIssuer = "CN=rts";

            using (X509Store store = new X509Store(storeName, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadOnly);
                X509Certificate2 cert = store.Certificates
                        .Find(X509FindType.FindByIssuerDistinguishedName, certificateIssuer, false)
                        .OfType<X509Certificate2>()
                        .FirstOrDefault();
                return cert;
            }
        }

        private void ImportCertificate(X509Certificate2 cert)
        {
            using (X509Store store = new X509Store(StoreName.TrustedPublisher, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadWrite);
                store.Add(cert);
                store.Close();
            }
        }

        private string GetCN(string fullname)
        {
            const string cnPrefix = "CN=";

            string cnPart = fullname.Split(' ').FirstOrDefault(p => p.StartsWith(cnPrefix));
            if (string.IsNullOrEmpty(cnPart)) return string.Empty;

            return fullname.Substring(cnPrefix.Length);
        }

    }
}
