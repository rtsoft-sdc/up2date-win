using System;
using System.IO;
using System.Linq;
using System.Management.Automation;
using Up2dateShared;

namespace Up2dateService.SetupManager
{
    public static class ChocoHelper
    {
        private const string NugetExtension = ".nupkg";
        public static bool IsPackageInstalled(Package package)
        {
            if (package.ProductCode == string.Empty ||
                package.DisplayVersion == string.Empty ||
                !IsChocoInstalled())
                return false;

            using (var ps = PowerShell.Create())
            {
                const string psCommand = @"choco list -li";
                ps.AddScript(psCommand);
                var result = ps.Invoke<string>();
                return result.Any(item => item.Equals($"{package.ProductCode} {package.DisplayVersion}"));
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

        public static void InstallChocoPackage(Package package, string logDirectory, string downloadLocation,
            string externalInstallLog)
        {
            // Second powershell starting is using as intermediate process to start choco process as detached 
            // Second source is used to prevent problems in downloading dependencies
            var chocoInstallCommand =
                @"Start-Process -FilePath powershell -ArgumentList(" +
                @"'Start-Process -FilePath choco -ArgumentList(''" +
                $@"install {package.ProductCode} --version {package.DisplayVersion} " +
                $@"-s ""{downloadLocation};https://community.chocolatey.org/api/v2/""" +
                @"-y --force --no-progress'')" +
                $@"-RedirectStandardOutput ""{logDirectory}\{externalInstallLog}""')" +
                $@"-RedirectStandardOutput ""{logDirectory}\{externalInstallLog}""";

            using (var ps = PowerShell.Create())
            {
                ps.AddScript(chocoInstallCommand);
                ps.Invoke<string>();
            }
        }

        public static void GetPackageInfo(ref Package package)
        {
            if (IsChocoInstalled() &&
                string.Equals(Path.GetExtension(package.Filepath),
                    NugetExtension,
                    StringComparison.InvariantCultureIgnoreCase))
            {
                ChocoNugetInfo nugetInfo = ChocoNugetInfo.GetInfo(package.Filepath);
                package.DisplayName = nugetInfo.Title;
                package.ProductCode = nugetInfo.Id;
                package.DisplayVersion = nugetInfo.Version;
                package.Publisher = nugetInfo.Publisher;
            }
        }
    }
}