using Moq;
using Up2dateConsole;
using Up2dateConsole.ServiceReference;

namespace Tests_Shared
{
    public class WcfClientFactoryMock : Mock<IWcfClientFactory>
    {
        public WcfClientFactoryMock()
        {
            WcfServiceMock = new WcfServiceMock();
            Setup(m => m.CreateClient()).Returns(WcfServiceMock.Object);
            Setup(m => m.CloseClient(It.IsAny<IWcfService>()));
        }

        public WcfServiceMock WcfServiceMock { get; }

    }
}
