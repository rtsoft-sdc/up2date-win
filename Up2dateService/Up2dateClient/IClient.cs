using Up2dateShared;

namespace Up2dateClient
{
    public interface IClient
    {
        ClientState State { get; }
        string HawkbitEndpoint { get; }
        string Run();
        void RequestStop();
        void RequestToPoll();
    }
}
