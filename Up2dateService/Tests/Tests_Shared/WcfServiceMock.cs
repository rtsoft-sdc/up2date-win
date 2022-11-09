using Moq;
using System.Threading.Tasks;
using Up2dateConsole.ServiceReference;

namespace Tests_Shared
{
    public class WcfServiceMock : Mock<IWcfService>
    {
        private string provisioningUrl;

        public string ProvisioningUrl
        {
            get => provisioningUrl;
            set
            {
                provisioningUrl = value;
                Setup(m => m.GetProvisioningUrl()).Returns(provisioningUrl);
                Setup(m => m.GetProvisioningUrlAsync()).Returns(Task.FromResult(provisioningUrl));
            }
        }

        private string requestCertificateUrl;

        public string RequestCertificateUrl
        {
            get => requestCertificateUrl;
            set
            {
                requestCertificateUrl = value;
                Setup(m => m.GetRequestCertificateUrl()).Returns(requestCertificateUrl);
                Setup(m => m.GetRequestCertificateUrlAsync()).Returns(Task.FromResult(requestCertificateUrl));
            }
        }

    }
}
