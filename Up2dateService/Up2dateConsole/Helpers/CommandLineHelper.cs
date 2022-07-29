using System;
using System.Linq;

namespace Up2dateConsole.Helpers
{
    public static class CommandLineHelper
    {
        internal const string StartUnelevatedCommand = "u";
        internal const string AllowSecondInstanceCommand = "s";
        internal const string VisibleMainWindowCommand = "v";

        internal static bool IsPresent(string command)
        {
            return Environment.GetCommandLineArgs().Skip(1).Any(a => a.ToLower() == command);
        }
    }
}
