namespace Up2dateConsole.Helpers
{
    public interface IServiceHelper
    {
        bool IsServiceRunning { get; }

        string StartService();
        string StopService();
    }
}