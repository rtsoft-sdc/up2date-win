namespace Up2dateShared
{
    public interface ISettingsManager
    {
        string ProvisioningUrl { get; }
        string XApigToken { get; }
        string RequestCertificateUrl { get; }
    }
}