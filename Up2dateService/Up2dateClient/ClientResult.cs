namespace Up2dateClient
{
    public enum Finished
    {
        SUCCESS,
        FAILURE,
        NONE
    };

    public enum Execution
    {
        CLOSED,
        PROCEEDING,
        CANCELED,
        SCHEDULED,
        REJECTED,
        RESUMED,
        DOWNLOAD,
        DOWNLOADED
    };

    public struct ClientResult
    {
        public Finished Finished { get; set; }
        public Execution Execution { get; set; }
        public string Message { get; set; }
    }
}