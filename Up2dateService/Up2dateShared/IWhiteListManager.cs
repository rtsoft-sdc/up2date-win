using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace Up2dateShared
{
    public interface IWhiteListManager
    {
        IList<X509Certificate2> GetWhitelistedCertificates();
        IList<string> GetWhitelistedCertificatesSha256();
        void RemoveCertificateFromWhilelist(X509Certificate2 certificate);
        Result AddCertificateToWhitelist(X509Certificate2 certificate);
        Result AddCertificateToWhitelist(string certificateFilePath);
        bool IsWhitelistedCertificate(X509Certificate2 certificate);
    }
}
