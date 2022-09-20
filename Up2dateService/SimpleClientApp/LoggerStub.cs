using System;
using Up2dateShared;

namespace SimpleClientApp
{
    public class LoggerStub : ILogger
    {
        public void WriteEntry(string source, string message, Exception exception = null)
        {
        }

        public void WriteEntry(string source, Exception exception)
        {
        }
    }
}
