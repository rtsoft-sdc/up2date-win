using System;
using System.Collections.Generic;
using System.IO;
using Up2dateShared;

namespace Up2dateService.SetupManager
{
    public class PackageInstallerFactory : IPackageInstallerFactory
    {
        private const string MsiExtension = ".msi";
        private const string NugetExtension = ".nupkg";

        private MsiInstaller msiInstaller = null;
        private ChocoInstaller chocoInstaller = null;

        private readonly Dictionary<string, Func<IPackageInstaller>> installers = new Dictionary<string, Func<IPackageInstaller>>();

        public PackageInstallerFactory()
        {
            installers.Add(MsiExtension, () =>
            {
                if (msiInstaller == null) msiInstaller = new MsiInstaller();
                return msiInstaller;
            });
            installers.Add(NugetExtension, () =>
            {
                if (chocoInstaller == null) chocoInstaller = new ChocoInstaller();
                return chocoInstaller;
            });
        }

        public IPackageInstaller GetInstaller(Package package)
        {
            string key = Path.GetExtension(package.Filepath).ToLower(System.Globalization.CultureInfo.InvariantCulture);
            if (!installers.ContainsKey(key)) throw new Exception($"Package type {key} is not supported.");

            return installers[key]();
        }

        public bool IsSupported(Package package)
        {
            string key = Path.GetExtension(package.Filepath).ToLower(System.Globalization.CultureInfo.InvariantCulture);
            return installers.ContainsKey(key);
        }
    }
}
