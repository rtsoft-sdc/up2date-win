using Microsoft.Win32;
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

        private readonly List<string> productCodes = new List<string>();
        private readonly List<string> wow6432productCodes = new List<string>();

        public MsiInstaller()
        {
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

        public Process StartInstallationProcess(Package package)
        {
            Process p = new Process();
            p.StartInfo.FileName = "msiexec.exe";
            p.StartInfo.Arguments = $"/i \"{package.Filepath}\" ALLUSERS=1 /qn";
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.UseShellExecute = false;
            p.Start();

            return p;
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
