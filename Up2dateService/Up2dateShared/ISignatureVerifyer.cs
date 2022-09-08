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

    public interface ISignatureVerifyer
    {
        bool VerifySignature(string file, SignatureVerificationLevel level);
        bool VerifySignature(X509Certificate2 certificate, SignatureVerificationLevel level);
        IList<X509Certificate2> GetWhitelistedCertificates();
        void RemoveCertificateFromWhilelist(X509Certificate2 certificate);
        void AddCertificateToWhilelist(X509Certificate2 certificate);
    }
}
