using System;
using Up2dateShared;

namespace SimpleClientApp
{
    public class LoggerStub : ILogger
    {
        public ILogger SubScope(string subScope)
        {
            return this;
        }

        public void WriteEntry(string message, Exception exception = null)
        {
        }

        public void WriteEntry(Exception exception)
        {
        }
    }
}
