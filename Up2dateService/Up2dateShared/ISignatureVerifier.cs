namespace Up2dateShared
{
    public interface ISignatureVerifier
    {
        bool IsSignedbyAnyCertificate(string filePath);
        bool IsSignedbyValidAndTrustedCertificate(string filePath);
        bool IsSignedByWhitelistedCertificate(string filepath, IWhiteListManager whiteListManager);
        bool IsCertificateValidAndTrusted(string certificateFilePath);
    }
}
