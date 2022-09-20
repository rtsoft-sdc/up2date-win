using System;
using System.Security.Cryptography.X509Certificates;

namespace Up2dateShared
{
    public class SignatureVerifier : ISignatureVerifier
    {
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

        public bool IsSignedbyAnyCertificate(string filePath)
        {
            X509Certificate2 certificate = GetCertificateFromSignedFile(filePath);

            return certificate != null;
        }

        public bool IsSignedbyValidAndTrustedCertificate(string filePath)
        {
            X509Certificate2 certificate = GetCertificateFromSignedFile(filePath);
            if (certificate == null) return false;

            return IsTrustedCertificate(certificate);
        }

        public bool IsSignedByWhitelistedCertificate(string filepath, IWhiteListManager whiteListManager)
        {
            if (whiteListManager is null) throw new ArgumentNullException(nameof(whiteListManager));

            X509Certificate2 certificate = GetCertificateFromSignedFile(filepath);
            if (certificate == null) return false;

            return whiteListManager.IsWhitelistedCertificate(certificate);
        }

        private X509Certificate2 GetCertificateFromSignedFile(string signedFilePath)
        {
            X509Certificate2 certificate;
            try
            {
                X509Certificate theSigner = X509Certificate.CreateFromSignedFile(signedFilePath);
                certificate = new X509Certificate2(theSigner);
            }
            catch (Exception)
            {
                return null;
            }
            return certificate;
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
    }
}
