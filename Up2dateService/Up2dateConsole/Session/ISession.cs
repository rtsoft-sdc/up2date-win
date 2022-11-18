using System;

namespace Up2dateConsole.Session
{
    public interface ISession
    {
        event EventHandler<EventArgs> ShuttingDown;
        void ToAdminMode();
        void ToUserMode();
        void Shutdown();
        bool IsAdminMode { get; }
        bool IsShuttingDown { get; }
    }
}
