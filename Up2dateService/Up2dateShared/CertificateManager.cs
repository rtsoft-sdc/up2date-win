using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace Up2dateShared
{
    public class CertificateManager : ICertificateManager
    {
        private const StoreName storeName = StoreName.TrustedPublisher;

        private readonly EventLog eventLog;
        private readonly ISettingsManager settingsManager;
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

        public CertificateManager(ISettingsManager settingsManager, EventLog eventLog)
        {
            this.settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            this.eventLog = eventLog;
        }

        public void ImportCertificate(byte[] certificateData)
        {
            try
            {
                X509Certificate2 cert = new X509Certificate2(certificateData);
                ImportCertificate(cert);
                Certificate = cert;
            }
            catch (Exception e)
            {
                eventLog.WriteEntry($"CertificateManager: Exception importing certificate. {e}");
                throw;
            }
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
            using (X509Store store = new X509Store(storeName, StoreLocation.LocalMachine))
            {
                Certificate = GetCertificates(store)?.OfType<X509Certificate2>().FirstOrDefault();
            }

            eventLog?.WriteEntry(Certificate != null 
                ? $"Certificate found; '{Certificate.Issuer}:{Certificate.Subject}'"
                : $"Cannot find certificate in {storeName} certificate store!");
        }

        private X509Certificate2Collection GetCertificates(X509Store store)
        {
            string CertificateSerialNumber = settingsManager.CertificateSerialNumber;

            if (string.IsNullOrEmpty(CertificateSerialNumber)) return null;

            store.Open(OpenFlags.ReadOnly);
            X509Certificate2Collection certificates = store.Certificates
                    .Find(X509FindType.FindBySerialNumber, CertificateSerialNumber, false);
            store.Close();

            return certificates;
        }

        private void ImportCertificate(X509Certificate2 cert)
        {
            using (X509Store store = new X509Store(storeName, StoreLocation.LocalMachine))
            {
                // first remove old certificate(s)
                X509Certificate2Collection oldCertificates = GetCertificates(store);
                store.Open(OpenFlags.ReadWrite);
                if (oldCertificates != null && oldCertificates.Count > 0)
                {
                    store.RemoveRange(oldCertificates);
                }

                // now add new certificate
                store.Add(cert);
                store.Close();

                // remember new certificate
                settingsManager.CertificateSerialNumber = cert.SerialNumber;
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
