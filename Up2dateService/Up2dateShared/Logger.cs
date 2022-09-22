using System;
using System.Diagnostics;

namespace Up2dateShared
{
    public class Logger : ILogger
    {
        private readonly EventLog eventLog;
        private readonly string scope;

        public Logger(EventLog eventLog, string scope = null)
        {
            this.eventLog = eventLog ?? throw new ArgumentNullException(nameof(eventLog));
            this.scope = scope;
        }

        public ILogger SubScope(string subScope)
        {
            var newScope = string.IsNullOrEmpty(scope) ? subScope : scope + "." + subScope;
            return new Logger(eventLog, newScope);
        }

        public void WriteEntry(string message, Exception exception = null)
        {
            string entry = $"{scope}: {message}";
            if (exception != null)
            {
                entry += $"\n{exception}";
            }
            eventLog.WriteEntry(entry);
        }

        public void WriteEntry(Exception exception)
        {
            eventLog.WriteEntry($"{scope}:\n{exception}");
        }
    }
}
