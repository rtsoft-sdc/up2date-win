using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Up2dateService.Interfaces;

using Up2dateShared;

namespace Up2dateService.Installers.Choco
{
    public class ChocoInstaller : IPackageInstaller
    {
        private const char productCodeSeparator = '|';

        private readonly ILogger logger;
        private readonly List<string> productCodes = new List<string>();
        private readonly Func<string> getDefaultSources;
        private bool isChocoInstalled;

        public ChocoInstaller(Func<string> getDefaultSources, ILogger logger)
        {
            this.getDefaultSources = getDefaultSources ?? throw new ArgumentNullException(nameof(getDefaultSources));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            Refresh();
        }

        public bool Initialize(ref Package package)
        {
            ChocoNugetInfo info = ChocoNugetInfo.GetInfo(package.Filepath);
            if (info == null || string.IsNullOrWhiteSpace(info.Id) || string.IsNullOrWhiteSpace(info.Version)) return false;

            package.ProductName = info.Id;
            package.DisplayVersion = info.Version;
            package.ProductCode = $"{info.Id}{productCodeSeparator}{info.Version}";

            return true;
        }

        public InstallPackageResult InstallPackage(Package package, string logFilePath)
        {
            const int checkPeriodMs = 1000;
            const int ExitCodeSuccess = 0;

            string location = Path.GetDirectoryName(package.Filepath);

            using (Process p = new Process())
            {
                p.StartInfo.FileName = "choco.exe";
                p.StartInfo.Arguments = $"{GetInstallationVerb(package)} {package.ProductName} " +
                                        $"--version {package.DisplayVersion} " +
                                        $"-s \"{location};{getDefaultSources()}\" " +
                                        "-y --no-progress";
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.UseShellExecute = false;

                try
                {
                    p.Start();
                }
                catch (Win32Exception exception)
                {
                    logger.WriteEntry("Failed to start choco.exe process, perhaps Chocolatey is not installed.", exception);
                    return InstallPackageResult.ChocoNotInstalled;
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

                if (p.ExitCode != ExitCodeSuccess)
                {
                    bool logFileCreated = false;
                    if (!string.IsNullOrWhiteSpace(logFilePath))
                    {
                        try
                        {
                            File.WriteAllText(logFilePath, p.StandardOutput.ReadToEnd());
                            logFileCreated = true;
                        }
                        catch (Exception exception) 
                        {
                            logger.WriteEntry($"Failed to write installation log '{logFilePath}'", exception);
                        }
                    }

                    var message = $"Installation of the package '{package.ProductName}' failed with the exit code: {p.ExitCode}.";
                    if (logFileCreated)
                    {
                        message += $"\nFor details see '{logFilePath}'";
                    }
                    logger.WriteEntry(message);
                    return InstallPackageResult.GeneralInstallationError;
                }

                Refresh();

                return InstallPackageResult.Success;
            }
        }

        public bool IsPackageInstalled(Package package)
        {
            if (package.ProductCode == string.Empty || !isChocoInstalled) return false;

            lock (productCodes)
            {
                return productCodes.Contains(package.ProductCode);
            }
        }

        public void Refresh()
        {
            Process p = new Process();
            p.StartInfo.FileName = "choco.exe";
            p.StartInfo.Arguments = "list --local-only --limit-output";
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.UseShellExecute = false;
            try
            {
                p.Start();
                isChocoInstalled = true;
                p.WaitForExit();
            }
            catch
            {
                isChocoInstalled = false;
            }

            lock (productCodes)
            {
                productCodes.Clear();
                if (isChocoInstalled)
                {
                    StreamReader standardOutput = p.StandardOutput;
                    while (!standardOutput.EndOfStream)
                    {
                        string line = standardOutput.ReadLine();
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        productCodes.Add(line.Trim());
                    }
                }
            }
        }

        public void UpdatePackageInfo(ref Package package)
        {
            ChocoNugetInfo info = ChocoNugetInfo.GetInfo(package.Filepath);
            if (info == null || string.IsNullOrWhiteSpace(info.Id) || string.IsNullOrWhiteSpace(info.Version)) return;
            package.DisplayName = info.Title;
            package.Publisher = info.Publisher;
        }

        private string GetInstallationVerb(Package package)
        {
            lock (productCodes)
            {
                return productCodes.Any(item => item.Split(productCodeSeparator).First() == package.ProductName) ? "upgrade" : "install";
            }
        }
    }
}
