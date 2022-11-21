using System;
using System.IO;

namespace Up2dateConsole
{
    public class Logger
    {
        private readonly string filePath;

        public Logger()
        {
            filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Up2dateConsole\logs\Up2dateConsole.log");
        }

        public void Info(string message)
        {
            using (var fs = new StreamWriter(filePath, append: true))
            {
                fs.WriteLine($"{DateTime.Now} | INFO  | {message}");
                fs.Flush();
            }
        }

        public void Error(Exception e, string message)
        {
            using (var fs = new StreamWriter(filePath, append: true))
            {
                fs.WriteLine($"{DateTime.Now} | ERROR | {message}\n{e}");
                fs.Flush();
            }
        }
    }
}
