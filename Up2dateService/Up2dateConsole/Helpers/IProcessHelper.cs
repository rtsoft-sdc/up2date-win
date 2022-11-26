using System.Diagnostics;

namespace Up2dateConsole.Helpers
{
    public interface IProcessHelper
    {
        Process StartProcess(ProcessStartInfo startInfo);
    }
}