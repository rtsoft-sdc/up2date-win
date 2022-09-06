using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using Up2dateShared;

namespace Up2dateService.SetupManager
{
    public static class ChocoHelper
    {
        public static bool IsPackageInstalled(Package package)
        {
            if (package.ProductCode == string.Empty || !IsChocoInstalled()) return false;

            using (var ps = PowerShell.Create())
            {
                const string psCommand = @"choco list -li";
                ps.AddScript(psCommand);
                var result = ps.Invoke<string>();
                return result.Any(item => item.Equals(package.ProductCode));
            }
        }

        public static void UpdatePackageInfo(ref Package package)
        {
            ChocoNugetInfo nugetInfo = ChocoNugetInfo.GetInfo(package.Filepath);
            package.DisplayName = nugetInfo.Title;
            package.Publisher = nugetInfo.Publisher;
        }

        public static InstallPackageResult InstallPackage(Package package)
        {
            if (!IsChocoInstalled()) return InstallPackageResult.ChocoNotInstalled;

            const int cancellationCheckPeriodMs = 1000;

            string location = Path.GetDirectoryName(package.Filepath);

            using (Process p = new Process())
            {
                p.StartInfo.FileName = "choco.exe";
                p.StartInfo.Arguments = $"install {package.ProductName} --version {package.DisplayVersion} " +
                                        $"-s \"{location};https://community.chocolatey.org/api/v2/\" " +
                                        "-y --force --no-progress";
                p.StartInfo.UseShellExecute = false;
                _ = p.Start();

                while (!p.WaitForExit(cancellationCheckPeriodMs)) ;

                if (p.ExitCode != 0) return InstallPackageResult.FailedToInstallChocoPackage;

                return IsPackageInstalled(package) ? InstallPackageResult.Success : InstallPackageResult.FailedToInstallChocoPackage;
            }
        }

        private static bool IsChocoInstalled()
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