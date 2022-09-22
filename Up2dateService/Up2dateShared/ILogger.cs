using System;

namespace Up2dateShared
{
    public interface ILogger
    {
        ILogger SubScope(string subScope);
        void WriteEntry(string message, Exception exception = null);
        void WriteEntry(Exception exception);
    }
}
