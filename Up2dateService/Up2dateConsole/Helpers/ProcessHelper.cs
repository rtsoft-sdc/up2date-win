using System.Diagnostics;

namespace Up2dateConsole.Helpers
{
    public class ProcessHelper : IProcessHelper
    {
        public Process StartProcess(ProcessStartInfo startInfo)
        {
            return Process.Start(startInfo);
        }
    }
}
