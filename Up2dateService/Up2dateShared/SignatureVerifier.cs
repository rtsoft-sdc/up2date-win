using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Up2dateShared
{
    public class SignatureVerifier : ISignatureVerifier
    {
        private const string defaultWhiteListStoreName = "RITMS_UP2DATE_WhiteList";
        private readonly string whiteListStoreName;
        private readonly StoreLocation whiteListStoreLocation;

        public SignatureVerifier(string whiteListStoreName = defaultWhiteListStoreName, StoreLocation whiteListStoreLocation = StoreLocation.LocalMachine)
        {
            this.whiteListStoreName = whiteListStoreName;
            this.whiteListStoreLocation = whiteListStoreLocation;
        }

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

        public void RemoveCertificateFromWhilelist(X509Certificate2 certificate)
        {
            using (X509Store store = new X509Store(whiteListStoreName, whiteListStoreLocation))
            {
                store.Open(OpenFlags.ReadWrite);
                store.Remove(certificate);
                store.Close();
            }
        }

        public bool IsCertificateValidAndTrusted(string certificateFilePath)
        {
            X509Certificate2 cert;
            try
            {
                cert = new X509Certificate2(certificateFilePath);
            }
            catch
            {
                return false;
            }

            return IsTrustedCertificate(cert);
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
            using (X509Store store = new X509Store(whiteListStoreName, whiteListStoreLocation))
            {
                store.Open(OpenFlags.ReadOnly);
                bool result = store.Certificates.Contains(certificate);
                store.Close();

                return result;
            }
        }
    }
}
