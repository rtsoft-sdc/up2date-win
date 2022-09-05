using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation;
using Up2dateShared;

namespace Up2dateService.SetupManager
{
    public static class ChocoHelper
    {
        private const string NugetExtension = ".nupkg";
        public static ChocoPackageInstallationStatus IsPackageInstalled(Package package)
        {
            if (package.ProductCode == string.Empty ||
                package.DisplayVersion == string.Empty)
                return ChocoPackageInstallationStatus.ChocoPackageInvalid;

            if (!IsChocoInstalled())
            {
                return ChocoPackageInstallationStatus.ChocoNotInstalled;
            }

            using (PowerShell ps = PowerShell.Create())
            {
                const string psCommand = @"choco list -li";
                ps.AddScript(psCommand);
                Collection<string> result = ps.Invoke<string>();
                string[] resultingLine = result.First(item => item.StartsWith(package.ProductCode)).Split(' ');
                // Expecting following format:
                // ProductCode Version
                if (resultingLine.Length != 2) return ChocoPackageInstallationStatus.ChocoPackageNotInstalled;
                string installedVersion = resultingLine[1];
                return installedVersion != package.DisplayVersion ?
                    ChocoPackageInstallationStatus.ChocoPackageInstalledVersionDiffers :
                    ChocoPackageInstallationStatus.ChocoPackageInstalled;

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
            string commandType;
            ChocoPackageInstallationStatus status = IsPackageInstalled(package);
            switch (status)
            {
                case ChocoPackageInstallationStatus.ChocoPackageInstalledVersionDiffers:
                    commandType = "upgrade";
                    break;
                case ChocoPackageInstallationStatus.ChocoNotInstalled:
                    commandType = "install";
                    break;
                default:
                    return;
            }

            string chocoInstallCommand =
                @"Start-Process -FilePath powershell -ArgumentList(" +
                @"'Start-Process -FilePath choco -ArgumentList(''" +
                $@"{commandType} {package.ProductCode} --version {package.DisplayVersion} " +
                $@"-s ""{downloadLocation};https://community.chocolatey.org/api/v2/""" +
                @"-y --no-progress'')" +
                $@"-RedirectStandardOutput ""{logDirectory}\{externalInstallLog}""')" +
                $@"-RedirectStandardOutput ""{logDirectory}\{externalInstallLog}""";

            using (PowerShell ps = PowerShell.Create())
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