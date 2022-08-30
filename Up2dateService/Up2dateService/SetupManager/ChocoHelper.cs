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

        public static void InstallChocoPackage(string packageId, string packageVersion, string logDirectory, string downloadLocation, string externalInstallLog)
        {
            // TODO: Possible will find better way to work with
            var chocoInstallCommand =
                $@"Start-Process -FilePath powershell -ArgumentList('Start-Process -FilePath choco -ArgumentList(''install {packageId} --version {packageVersion} -s {downloadLocation} -y --force --no-progress'') -RedirectStandardOutput ""{logDirectory}\{externalInstallLog}""') -RedirectStandardOutput ""{logDirectory}\{externalInstallLog}""";
            var ps = PowerShell.Create();
            ps.AddScript(chocoInstallCommand);
            ps.Invoke<string>();
        }
    }
}