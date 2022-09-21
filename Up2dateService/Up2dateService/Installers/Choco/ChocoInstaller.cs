using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Up2dateService.Interfaces;

using Up2dateShared;

namespace Up2dateService.Installers.Choco
{
    public class ChocoInstaller : IPackageInstaller
    {
        private readonly ILogger logger;
        private readonly List<string> productCodes = new List<string>();
        private readonly Func<string> getDefaultSources;

        public ChocoInstaller(Func<string> getDefaultSources, ILogger logger)
        {
            this.getDefaultSources = getDefaultSources ?? throw new ArgumentNullException(nameof(getDefaultSources));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool Initialize(ref Package package)
        {
            ChocoNugetInfo info = ChocoNugetInfo.GetInfo(package.Filepath);
            if (info == null || string.IsNullOrWhiteSpace(info.Id) || string.IsNullOrWhiteSpace(info.Version)) return false;

            package.ProductName = info.Id;
            package.DisplayVersion = info.Version;
            package.ProductCode = $"{info.Id} {info.Version}";

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
                p.StartInfo.RedirectStandardError = true;
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

                if (p.ExitCode != ExitCodeSuccess)
                {
                    if (!string.IsNullOrWhiteSpace(logFilePath))
                    {
                        try
                        {
                            File.WriteAllText(logFilePath, p.StandardError.ReadToEnd());
                        }
                        catch (Exception exception) 
                        {
                            logger.WriteEntry($"Failed to write installation log '{logFilePath}'", exception);
                        }
                    }

                    logger.WriteEntry($"Installation of the package '{package.ProductName}' failed with the exit code: {p.ExitCode}");
                    return InstallPackageResult.GeneralInstallationError;
                }

                return InstallPackageResult.Success;
            }
        }

        public bool IsPackageInstalled(Package package)
        {
            if (package.ProductCode == string.Empty || !IsChocoInstalled()) return false;

            return productCodes.Contains(package.ProductCode);
        }

        public void Refresh()
        {
            Process p = new Process();
            p.StartInfo.FileName = "choco.exe";
            p.StartInfo.Arguments = "list -l";
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.UseShellExecute = false;
            p.Start();
            p.WaitForExit();
            productCodes.Clear();
            StreamReader standardOutput = p.StandardOutput;

            bool firstLineSkipped = false;
            while (!standardOutput.EndOfStream)
            {
                string line = standardOutput.ReadLine();
                if (!firstLineSkipped)
                {
                    firstLineSkipped = true;
                    continue;
                }
                if (line != string.Empty)
                {
                    productCodes.Add(line);
                }
            }

            if (productCodes.Count > 0)
            {
                productCodes.RemoveAt(productCodes.Count - 1);
            }
        }

        public void UpdatePackageInfo(ref Package package)
        {
            ChocoNugetInfo info = ChocoNugetInfo.GetInfo(package.Filepath);
            if (info == null || string.IsNullOrWhiteSpace(info.Id) || string.IsNullOrWhiteSpace(info.Version)) return;
            package.DisplayName = info.Title;
            package.Publisher = info.Publisher;
        }

        private string GetInstallationVerb(Package package) =>
            productCodes.Any(item => item.Split(' ').
                First() == package.ProductName) ? "upgrade" : "install";

        private static bool IsChocoInstalled()
        {
            try
            {
                Process p = new Process();
                p.StartInfo.FileName = "choco.exe";
                p.StartInfo.Arguments = "--version";
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.UseShellExecute = false;
                p.Start();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
