using System.Threading.Tasks;

namespace Up2dateShared
{
    public interface ICertificateProvider
    {
        Task<Result<string>> RequestCertificateAsync(string oneTimeToken);
        Task<Result<string>> OpenRequestCertificateSessionAsync();
        Task<Result<string>> GetCertificateBySessionHandleAsync(string handle);
        void CloseRequestCertificateSession(string handle);
    }
}
