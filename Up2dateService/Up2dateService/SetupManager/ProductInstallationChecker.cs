using Microsoft.Win32;
using System.Collections.Generic;
using System.Linq;
using Up2dateShared;

namespace Up2dateService.SetupManager
{
    public class ProductInstallationChecker
    {
        private const string UninstallKeyName = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
        private const string Wow6432UninstallKeyName = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";

        private readonly List<string> productCodes = new List<string>();
        private readonly List<string> wow6432productCodes = new List<string>();

        public ProductInstallationChecker()
        {
            UpdateProductList();
        }

        public void UpdateProductList()
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
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(Wow6432UninstallKeyName))
            {
                if (key != null)
                {
                    wow6432productCodes.AddRange(key.GetSubKeyNames());
                    key.Close();
                }
            }
        }

        public bool IsPackageInstalled(string productCode)
        {
            if (productCode == null) return false;
            bool packageInstalled = productCodes.Concat(wow6432productCodes).Contains(productCode);
            if (!packageInstalled)
            {
                packageInstalled = ChocoHelper.IsPackageInstalled(productCode);
            }

            return packageInstalled;
        }

        public void UpdateInfo(ref Package package)
        {
            string uninstallKeyName = productCodes.Contains(package.ProductCode) 
                ? UninstallKeyName 
                : wow6432productCodes.Contains(package.ProductCode) ? Wow6432UninstallKeyName : null;
            if (uninstallKeyName == null) return;

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
