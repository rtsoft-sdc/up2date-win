using System.Linq;
using System.Management.Automation;
using Up2dateShared;

namespace Up2dateService.SetupManager
{
    public static class ChocoHelper
    {
        public static bool IsPackageInstalled(string packageName)
        {
            using (var ps = PowerShell.Create())
            {
                const string psCommand = @"choco list -li";
                ps.AddScript(psCommand);
                var result = ps.Invoke<string>();
                return result.Any(item => item.Contains(packageName));
            }
        }

        public static bool IsChocoInstalled()
        {
            using (var ps = PowerShell.Create())
            {
                ps.AddScript("choco --version");
                var value = ps.Invoke<string>();
                return value.Count > 0;
            }
        }
    }
}