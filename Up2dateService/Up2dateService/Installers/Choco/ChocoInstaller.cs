using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;
using System.Security.Cryptography.X509Certificates;
using Up2dateService.Interfaces;
using Up2dateShared;

namespace Up2dateService.Installers.Choco
{
    public class ChocoInstaller : IPackageInstaller
    {
        private readonly List<string> productCodes = new List<string>();
        private readonly Func<string> getDefaultSources;
        private readonly Func<X509Certificate2, bool> verifyCertificate;

        public ChocoInstaller(Func<string> getDefaultSources, Func<X509Certificate2, bool> verifyCertificate)
        {
            this.getDefaultSources = getDefaultSources ?? throw new ArgumentNullException(nameof(getDefaultSources));
            this.verifyCertificate = verifyCertificate ?? throw new ArgumentNullException(nameof(verifyCertificate));
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
            p.StartInfo.Arguments = $"install {package.ProductName} --version {package.DisplayVersion} " +
                                    $"-s \"{location};{getDefaultSources()}\" " +
                                    "-y --force --no-progress";
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.UseShellExecute = false;
            p.Start();

            return p;
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
            ChocoNugetInfo info = ChocoNugetInfo.GetInfo(package.Filepath);
            if (info == null || string.IsNullOrWhiteSpace(info.Id) || string.IsNullOrWhiteSpace(info.Version)) return;

            package.DisplayName = info.Title;
            package.Publisher = info.Publisher;
        }

        public bool VerifySignature(Package package)
        {
            // TODO extract certificate from the choco package and check it using verifyCertificate delegate
            return true;
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
