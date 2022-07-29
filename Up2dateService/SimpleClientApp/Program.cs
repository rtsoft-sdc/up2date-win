using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Up2dateClient;
using Up2dateShared;

namespace SimpleClientApp
{
    class Program
    {
        private static string dataFolder = @"C:\Users\Basil\Downloads\";

        static void Main(string[] args)
        {
            var client = new Client(new SettingsManager(), GetCertificate, new SetupManagerStub(), SystemInfo.Retrieve, GetCreatePackagesFolder);
            client.Run();
        }

        static private string GetCertificate()
        {
            const string certificateFileName = "client.crt";

            string certificateFile = Path.Combine(GetCreateCetrificateFolder(), certificateFileName);
            if (File.Exists(certificateFile))
            {
                return File.ReadAllText(certificateFile);
            }
            else
            {
                using (X509Certificate2 cert = TryGetCertificate(StoreName.TrustedPublisher)
                    ?? TryGetCertificate(StoreName.My)
                    ?? TryGetCertificate(StoreName.AddressBook))
                {
                    if (cert != null)
                    {
                        byte[] arr = cert.GetRawCertData();
                        return "-----BEGIN CERTIFICATE-----" + Convert.ToBase64String(arr) + "-----END CERTIFICATE-----";
                    }
                }
            }
            throw new Exception("Certificate is not available!");
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

        static private string GetCreateCetrificateFolder()
        {
            string certFolder = Path.Combine(GetServiceDataFolder(), @"Cert\");
            if (!Directory.Exists(certFolder))
            {
                Directory.CreateDirectory(certFolder);
            }
            return certFolder;
        }

        static private string GetCreatePackagesFolder()
        {
            string packagesFolder = Path.Combine(GetServiceDataFolder(), @"Packages\");
            if (!Directory.Exists(packagesFolder))
            {
                Directory.CreateDirectory(packagesFolder);
            }
            return packagesFolder;
        }


        static private string GetServiceDataFolder()
        {
            return dataFolder;
        }

    }
}
