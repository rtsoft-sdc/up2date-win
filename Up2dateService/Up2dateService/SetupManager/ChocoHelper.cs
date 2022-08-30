using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;

namespace Up2dateService.SetupManager
{
    public static class ChocoHelper
    {
        public static bool IsPackageInstalled(string packageName)
        {
            if (packageName == null)
            {
                return false;
            }

            packageName = packageName.ToLowerInvariant();

            using (PowerShell ps = PowerShell.Create())
            {
                const string psCommand = @"choco list -li";
                ps.AddScript(psCommand);
                Collection<string> result = ps.Invoke<string>();
                return result.Any(item => item.ToLowerInvariant().Contains(packageName));
            }
        }

        public static bool IsChocoInstalled()
        {
            using (PowerShell ps = PowerShell.Create())
            {
                ps.AddScript("choco --version");
                Collection<string> value = ps.Invoke<string>();
                return value.Count > 0;
            }
        }

        public static void InstallChocoPackage(string packageId, string packageVersion, string logDirectory, string downloadLocation, string externalInstallLog)
        {
            // Second powershell starting is using as intermediate process to start choco process as detached 
            string chocoInstallCommand =
                @"Start-Process -FilePath powershell -ArgumentList(" +
                @"'Start-Process -FilePath choco -ArgumentList(''" +
                $@"install {packageId} --version {packageVersion} -s {downloadLocation}" +
                @"-y --force --no-progress'')" +
                $@"-RedirectStandardOutput ""{logDirectory}\{externalInstallLog}""')" +
                $@"-RedirectStandardOutput ""{logDirectory}\{externalInstallLog}""";

            using (PowerShell ps = PowerShell.Create())
            {
                ps.AddScript(chocoInstallCommand);
                ps.Invoke<string>();
            }
        }
    }
}