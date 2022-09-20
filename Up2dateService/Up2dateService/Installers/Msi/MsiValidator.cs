using System;
using System.Security.Cryptography.X509Certificates;
using Up2dateService.Interfaces;
using Up2dateShared;

namespace Up2dateService.Installers.Msi
{
    public class MsiValidator : IPackageValidator
    {
        private readonly ISettingsManager settingsManager;
        private readonly IWhiteListManager whiteListManager;
        private readonly ISignatureVerifier signatureVerifier;

        public MsiValidator(ISettingsManager settingsManager, IWhiteListManager whiteListManager, ISignatureVerifier signatureVerifier)
        {
            this.settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            this.whiteListManager = whiteListManager ?? throw new ArgumentNullException(nameof(whiteListManager));
            this.signatureVerifier = signatureVerifier ?? throw new ArgumentNullException(nameof(signatureVerifier));
        }

        public bool VerifySignature(Package package)
        {
            if (!settingsManager.CheckSignature) return true;

            switch (settingsManager.SignatureVerificationLevel)
            {
                case SignatureVerificationLevel.SignedByAnyCertificate:
                    return signatureVerifier.IsSignedbyAnyCertificate(package.Filepath);
                case SignatureVerificationLevel.SignedByTrustedCertificate:
                    return signatureVerifier.IsSignedbyValidAndTrustedCertificate(package.Filepath);
                case SignatureVerificationLevel.SignedByWhitelistedCertificate:
                    return signatureVerifier.IsSignedByWhitelistedCertificate(package.Filepath, whiteListManager);
                default:
                    throw new InvalidOperationException($"Unsupported signature verification level {settingsManager.SignatureVerificationLevel}");
            }
        }
    }
}
