using System;
using System.Collections.Generic;
using Up2dateService.Interfaces;
using Up2dateShared;

namespace Up2dateService.SetupManager
{
    public class ProductInstallationChecker
    {
        private readonly IPackageInstallerFactory installerFactory;

        public ProductInstallationChecker(IPackageInstallerFactory installerFactory, IEnumerable<Package> packages)
        {
            this.installerFactory = installerFactory ?? throw new ArgumentNullException(nameof(installerFactory));
            if (packages is null) throw new ArgumentNullException(nameof(packages));

            RefreshProductList(packages);
        }

        public bool IsPackageInstalled(Package package)
        {
            if (!installerFactory.IsSupported(package)) return false;

            var installer = installerFactory.GetInstaller(package);
            return installer.IsPackageInstalled(package);
        }

        public void UpdateInfo(ref Package package)
        {
            if (!installerFactory.IsSupported(package)) return;

            var installer = installerFactory.GetInstaller(package);
            installer.UpdatePackageInfo(ref package);
        }

        private void RefreshProductList(IEnumerable<Package> packages)
        {
            var installers = new List<IPackageInstaller>();
            foreach(Package package in packages)
            {
                if (!installerFactory.IsSupported(package)) continue;

                var installer = installerFactory.GetInstaller(package);
                if (!installers.Contains(installer))
                {
                    installers.Add(installer);
                    installer.Refresh();
                }
            }
        }
    }
}
