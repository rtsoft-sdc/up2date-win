using System;

namespace Up2dateShared
{
    public interface ILogger
    {
        void WriteEntry(string source, string message, Exception exception = null);
        void WriteEntry(string source, Exception exception);
    }
}
