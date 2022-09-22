using System;
using Up2dateShared;

namespace SimpleClientApp
{
    public class LoggerStub : ILogger
    {
        private string scope;

        public LoggerStub(string scope)
        {
            if (string.IsNullOrWhiteSpace(scope))
            {
                throw new ArgumentException($"'{nameof(scope)}' cannot be null or whitespace.", nameof(scope));
            }

            this.scope = scope;
        }

        public ILogger SubScope(string subScope)
        {
            return new LoggerStub(scope + "." + subScope);
        }

        public void WriteEntry(string message, Exception exception = null)
        {
            Console.WriteLine($"{scope}: {message}\n{exception}");
        }

        public void WriteEntry(Exception exception)
        {
            Console.WriteLine($"{scope}:\n{exception}");
        }
    }
}
