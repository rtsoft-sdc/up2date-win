using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Up2dateShared
{
    public enum SignatureVerificationLevel
    {
        SignedByAnyCertificate,
        SignedByTrustedCertificate,
        SignedByWhitelistedCertificate
    }

    public interface ISignatureVerifier
    {
        bool VerifySignature(string file, SignatureVerificationLevel level);
        bool VerifySignature(X509Certificate2 certificate, SignatureVerificationLevel level);
        bool IsCertificateValidAndTrusted(string certificateFilePath);
    }
}
