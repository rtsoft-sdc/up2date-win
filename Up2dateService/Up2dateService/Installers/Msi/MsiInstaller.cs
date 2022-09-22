using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Up2dateService.Interfaces;
using Up2dateShared;

namespace Up2dateService.Installers.Msi
{
    public class MsiInstaller : IPackageInstaller
    {
        private const string UninstallKeyName = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
        private const string Wow6432UninstallKeyName = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";

        private readonly ILogger logger;
        private readonly List<string> productCodes = new List<string>();
        private readonly List<string> wow6432productCodes = new List<string>();

        public MsiInstaller(ILogger logger)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            Refresh();
        }

        public bool Initialize(ref Package package)
        {
            MsiInfo info = MsiInfo.GetInfo(package.Filepath);
            if (info == null || string.IsNullOrWhiteSpace(info.ProductCode)) return false;

            package.ProductName = info.ProductName;
            package.DisplayVersion = info.ProductVersion;
            package.ProductCode = info.ProductCode;

            return true;
        }

        public InstallPackageResult InstallPackage(Package package, string logFilePath)
        {
            const int checkPeriodMs = 1000;
            const int ExitCodeSuccess = 0;
            const int MsiExitCodeRestartNeeded = 3010;

            using (Process p = new Process())
            {
                p.StartInfo.FileName = "msiexec.exe";
                p.StartInfo.Arguments = $"/i \"{package.Filepath}\" ALLUSERS=1 /quiet /qn /norestart";
                if (!string.IsNullOrWhiteSpace(logFilePath))
                {
                    p.StartInfo.Arguments += $" /log \"{logFilePath}\"";
                }
                p.StartInfo.UseShellExecute = false;

                try
                {
                    p.Start();
                }
                catch (Exception exception)
                {
                    logger.WriteEntry($"Failed to start installation of the package '{package.ProductName}'", exception);
                    return InstallPackageResult.CannotStartInstaller;
                }

                try
                {
                    while (!p.WaitForExit(checkPeriodMs)) ;
                }
                catch (Exception exception)
                {
                    logger.WriteEntry($"Failure while waiting for installation completion of the package '{package.ProductName}'", exception);
                    return InstallPackageResult.CannotStartInstaller;
                }

                if (p.ExitCode == MsiExitCodeRestartNeeded) return InstallPackageResult.RestartNeeded;

                if (p.ExitCode != ExitCodeSuccess)
                {
                    logger.WriteEntry($"Installation of the package '{package.ProductName}' failed with the exit code: {p.ExitCode}." +
                        $"\nFor details see '{logFilePath}'");
                    return InstallPackageResult.GeneralInstallationError;
                }

                Refresh();

                return InstallPackageResult.Success;
            }
        }

        public bool IsPackageInstalled(Package package)
        {
            if (package.ProductCode == null) return false;

            return productCodes.Concat(wow6432productCodes).Contains(package.ProductCode);
        }

        public void Refresh()
        {
            productCodes.Clear();
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(UninstallKeyName))
            {
                if (key != null)
                {
                    productCodes.AddRange(key.GetSubKeyNames());
                    key.Close();
                }
            }

            wow6432productCodes.Clear();
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(Wow6432UninstallKeyName))
            {
                if (key != null)
                {
                    wow6432productCodes.AddRange(key.GetSubKeyNames());
                    key.Close();
                }
            }
        }

        public void UpdatePackageInfo(ref Package package)
        {
            string uninstallKeyName = productCodes.Contains(package.ProductCode)
                ? UninstallKeyName
                : wow6432productCodes.Contains(package.ProductCode) ? Wow6432UninstallKeyName : null;

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(uninstallKeyName + @"\" + package.ProductCode))
            {
                if (key != null)
                {
                    package.DisplayName = key.GetValue("DisplayName") as string;
                    package.Publisher = key.GetValue("Publisher") as string;
                    package.DisplayVersion = key.GetValue("DisplayVersion") as string;
                    package.Version = key.GetValue("Version") as int?;
                    package.InstallDate = key.GetValue("InstallDate") as string;
                    package.EstimatedSize = key.GetValue("EstimatedSize") as int?;
                    package.UrlInfoAbout = key.GetValue("URLInfoAbout") as string;
                    key.Close();
                }
            }
        }
    }
}
