using System.Threading.Tasks;

namespace Up2dateShared
{
    public interface ICertificateProvider
    {
        Task<string> RequestCertificateAsync(string oneTimeToken);
    }
}