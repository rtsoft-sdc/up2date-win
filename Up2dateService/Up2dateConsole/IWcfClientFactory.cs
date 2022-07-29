using Up2dateConsole.ServiceReference;

namespace Up2dateConsole
{
    public interface IWcfClientFactory
    {
        IWcfService CreateClient();
        void CloseClient(IWcfService client);
    }
}