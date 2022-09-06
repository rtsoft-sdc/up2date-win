using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using Up2dateShared;

namespace Up2dateService.SetupManager
{
    public class ChocoInstaller : IPackageInstaller
    {
        private readonly List<string> productCodes = new List<string>();

        public bool Initialize(ref Package package)
        {
            ChocoNugetInfo info = ChocoNugetInfo.GetInfo(package.Filepath);
            if (info == null || string.IsNullOrWhiteSpace(info.Id) || string.IsNullOrWhiteSpace(info.Version)) return false;

            package.ProductName = info.Id;
            package.DisplayVersion = info.Version;
            package.ProductCode = $"{info.Id} {info.Version}";
            package.DisplayName = info.Title;
            package.Publisher = info.Publisher;

            return true;
        }

        public InstallPackageResult InstallPackage(Package package)
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

        public bool IsPackageInstalled(Package package)
        {
            if (package.ProductCode == string.Empty || !IsChocoInstalled()) return false;

            return productCodes.Contains(package.ProductCode);
        }

        public void Refresh()
        {
            using (var ps = PowerShell.Create())
            {
                const string psCommand = @"choco list -li";
                ps.AddScript(psCommand);
                productCodes.Clear();
                productCodes.AddRange(ps.Invoke<string>());
            }
        }

        public void UpdatePackageInfo(ref Package package)
        {
            // nothing to update while we don't know how to access Choco store
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
