using System;
using System.IO;
using Up2dateClient;
using Up2dateDotNet;
using Up2dateShared;

namespace SimpleClientApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new Client(
                new Wrapper(), 
                new SettingsManagerStub(), 
                () => File.OpenText(args[0]).ReadToEnd(), 
                new SetupManagerStub(), 
                SystemInfo.Retrieve, 
                new LoggerStub("Client"),
                new Version(2,0,0));

            client.Run();
        }
    }
}
