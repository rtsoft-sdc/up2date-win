using System;
using System.Security.Cryptography.X509Certificates;

namespace Up2dateShared
{
    public class SignatureVerifier : ISignatureVerifier
    {
        private readonly IWhiteListManager whiteListManager;

        public SignatureVerifier(IWhiteListManager whiteListManager)
        {
            this.whiteListManager = whiteListManager ?? throw new ArgumentNullException(nameof(whiteListManager));
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
                    return certificate != null && whiteListManager.IsWhitelistedCertificate(certificate);
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
