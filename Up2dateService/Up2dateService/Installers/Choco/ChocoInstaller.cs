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
        private readonly List<string> productCodes = new List<string>();
        private readonly Func<string> getDefaultSources;

        public ChocoInstaller(Func<string> getDefaultSources = null)
        {
            this.getDefaultSources = getDefaultSources ?? (() => "https://community.chocolatey.org/api/v2/");
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

        public Process StartInstallationProcess(Package package)
        {
            string location = Path.GetDirectoryName(package.Filepath);

            Process p = new Process();
            p.StartInfo.FileName = "choco.exe";
            p.StartInfo.Arguments = $"{GetInstallationType(package)} {package.ProductName} " +
                                    $"--version {package.DisplayVersion} " +
                                    $"-s \"{location};{getDefaultSources()}\" " +
                                    "-y --no-progress";
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.UseShellExecute = false;
            p.Start();

            return p;
        }

        private string GetInstallationType(Package package)
        {
            string packageInstallationType = "install";
            if (productCodes.Any(item => item.StartsWith(package.ProductCode.Split(' ').First())))
            {
                packageInstallationType = "upgrade";
            }

            return packageInstallationType;
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
            productCodes.AddRange(p.StandardOutput.ReadToEnd()
                .Split(new[] { Environment.NewLine },
                    StringSplitOptions.RemoveEmptyEntries));
        }

        public void UpdatePackageInfo(ref Package package)
        {
            ChocoNugetInfo info = ChocoNugetInfo.GetInfo(package.Filepath);
            if (info == null || string.IsNullOrWhiteSpace(info.Id) || string.IsNullOrWhiteSpace(info.Version)) return;
            package.DisplayName = info.Title;
            package.Publisher = info.Publisher;
        }

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
                p.WaitForExit();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
