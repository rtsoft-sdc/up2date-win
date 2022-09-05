
using System.Security.Cryptography.X509Certificates;

namespace Up2dateShared
{
    public interface ICertificateManager
    {
        X509Certificate2 Certificate { get; }
        string CertificateIssuerName { get; }
        string CertificateSubjectName { get; }
        void ImportCertificate(byte[] certificateData);
        string GetCertificateString();
        bool IsCertificateAvailable();
        bool IsSigned(string file);
        bool IsSignedByIssuer(string file);
    }
}
