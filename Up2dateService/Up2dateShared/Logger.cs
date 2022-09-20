using System;
using System.Diagnostics;

namespace Up2dateShared
{
    public class Logger : ILogger
    {
        private readonly EventLog eventLog;

        public Logger(EventLog eventLog)
        {
            this.eventLog = eventLog ?? throw new ArgumentNullException(nameof(eventLog));
        }

        public void WriteEntry(string source, string message, Exception exception = null)
        {
            string entry = $"{source}: {message}";
            if (exception != null)
            {
                entry += $"\n{exception}";
            }
            eventLog.WriteEntry(entry);
        }

        public void WriteEntry(string source, Exception exception)
        {
            eventLog.WriteEntry($"{source}:\n{exception}");
        }
    }
}
