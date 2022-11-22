using System;
using System.IO;

namespace Up2dateConsole
{
    public class Logger
    {
        private const string LogFileName = "Up2dateConsole.log";
        private const string LogFileLocation = @"Up2dateConsole\logs";
        private readonly string filePath;

        public Logger()
        {
            try
            {
                var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), LogFileLocation);
                Directory.CreateDirectory(folder);
                filePath = Path.Combine(folder, LogFileName);
            }
            catch
            {
                // failure in logging should not crash the application
            }
        }

        public void Info(string message)
        {
            if (string.IsNullOrEmpty(filePath)) return;

            try
            {
                using (var fs = new StreamWriter(filePath, append: true))
                {
                    fs.WriteLine($"{DateTime.Now} | INFO  | {message}");
                    fs.Flush();
                }
            }
            catch
            {
                // failure in logging should not crash the application
            }
        }

        public void Error(Exception e, string message)
        {
            if (string.IsNullOrEmpty(filePath)) return;

            try
            {
                using (var fs = new StreamWriter(filePath, append: true))
                {
                    fs.WriteLine($"{DateTime.Now} | ERROR | {message}\n{e}");
                    fs.Flush();
                }
            }
            catch
            {
                // failure in logging should not crash the application
            }
        }
    }
}
