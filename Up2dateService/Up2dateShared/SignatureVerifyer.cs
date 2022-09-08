using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Up2dateShared
{
    public class SignatureVerifyer : ISignatureVerifyer
    {
        private const string whiteListStore = "RITMS_UP2DATE_WhiteList";

        public bool VerifySignature(X509Certificate2 certificate, SignatureVerificationLevel level)
        {
            switch (level)
            {
                case SignatureVerificationLevel.SignedByAnyCertificate:
                    return certificate != null;
                case SignatureVerificationLevel.SignedByTrustedCertificate:
                    return certificate != null && IsTrustedCertificate(certificate);
                case SignatureVerificationLevel.SignedByWhitelistedCertificate:
                    return certificate != null && IsWhitelistedCertificate(certificate);
                default:
                    throw new InvalidOperationException($"Unsupported signature verification level {level}");
            }
        }

        public bool VerifySignature(string file, SignatureVerificationLevel level)
        {
            X509Certificate2 certificate;
            try
            {
                X509Certificate theSigner = X509Certificate.CreateFromSignedFile(file);
                certificate = new X509Certificate2(theSigner);
            }
            catch (Exception)
            {
                certificate = null;
            }

            return VerifySignature(certificate, level);
        }

        public IList<X509Certificate2> GetWhitelistedCertificates()
        {
            var certs = new List<X509Certificate2>();
            using (X509Store store = new X509Store(whiteListStore, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadOnly);
                var enumerator = store.Certificates.GetEnumerator();
                do certs.Add(enumerator.Current); while (enumerator.MoveNext());
                store.Close();
            }
            return certs;
        }

        public void RemoveCertificateFromWhilelist(X509Certificate2 certificate)
        {
            using (X509Store store = new X509Store(whiteListStore, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadWrite);
                store.Remove(certificate);
                store.Close();
            }
        }

        public void AddCertificateToWhilelist(X509Certificate2 certificate)
        {
            using (X509Store store = new X509Store(whiteListStore, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadWrite);
                store.Add(certificate);
                store.Close();
            }
        }

        private bool IsTrustedCertificate(X509Certificate2 certificate)
        {
            var theCertificateChain = new X509Chain();
            theCertificateChain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
            theCertificateChain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
            theCertificateChain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
            theCertificateChain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;

            return theCertificateChain.Build(certificate);
        }

        private bool IsWhitelistedCertificate(X509Certificate2 certificate)
        {
            using (X509Store store = new X509Store(whiteListStore, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadOnly);
                bool result = store.Certificates.Contains(certificate);
                store.Close();

                return result;
            }
        }
    }
}
