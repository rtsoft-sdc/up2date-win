using Up2dateConsole.ServiceReference;

namespace Up2dateConsole
{
    public class WcfClientFactory : IWcfClientFactory
    {
        public IWcfService CreateClient()
        {
            var client = new WcfServiceClient();
            client.ClientCredentials.Windows.AllowedImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Impersonation;
            return client;
        }

        public void CloseClient(IWcfService client)
        {
            if (client == null) return;

            WcfServiceClient sc = (WcfServiceClient)client;
            if (sc.State == System.ServiceModel.CommunicationState.Opened)
            {
                try
                {
                    sc.Close();
                }
                catch
                {
                    // Possible when service is not responding after successfully opened -- nothing to do here, just ignore
                }
            }
        }
    }
}
