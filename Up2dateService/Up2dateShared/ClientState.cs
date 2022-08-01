using System.Runtime.Serialization;

namespace Up2dateShared
{
    public enum ClientStatus
    {
        Stopped,
        CannotAccessServer,
        Running,
        NoCertificate
    }

    [DataContract(Namespace = "http://RTSoft.Ritms.Up2date.win")]
    public struct ClientState
    {
        public ClientState(ClientStatus status, string lastError)
        {
            Status = status;
            LastError = lastError;
        }

        [DataMember]
        public ClientStatus Status { get; set; }
        [DataMember]
        public string LastError { get; set; }
    }
}
