using System;
using System.Collections.Generic;
using System.IO;
using Up2dateService.Installers.Choco;
using Up2dateService.Installers.Msi;
using Up2dateService.Interfaces;
using Up2dateShared;

namespace Up2dateService.Installers
{
    public class PackageInstallerFactory : IPackageInstallerFactory
    {
        private const string MsiExtension = ".msi";
        private const string NugetExtension = ".nupkg";

        private MsiInstaller msiInstaller = null;
        private ChocoInstaller chocoInstaller = null;

        private readonly Dictionary<string, Func<IPackageInstaller>> installers = new Dictionary<string, Func<IPackageInstaller>>();

        public PackageInstallerFactory(ISettingsManager settingsManager, ISignatureVerifier signatureVerifier, IWhiteListManager whiteListManager, ILogger logger)
        {
            if (settingsManager is null) throw new ArgumentNullException(nameof(settingsManager));
            if (signatureVerifier is null) throw new ArgumentNullException(nameof(signatureVerifier));
            if (whiteListManager is null) throw new ArgumentNullException(nameof(whiteListManager));

            installers.Add(MsiExtension, () =>
            {
                if (msiInstaller == null)
                {
                    msiInstaller = new MsiInstaller(logger.SubScope(nameof(MsiInstaller)));
                }
                return msiInstaller;
            });
            installers.Add(NugetExtension, () =>
            {
                if (chocoInstaller == null)
                {
                    chocoInstaller = new ChocoInstaller(() => settingsManager.DefaultChocoSources, logger.SubScope(nameof(ChocoInstaller)));
                }
                return chocoInstaller;
            });
        }

        public IPackageInstaller GetInstaller(Package package)
        {
            string key = Path.GetExtension(package.Filepath).ToLower(System.Globalization.CultureInfo.InvariantCulture);
            if (!installers.ContainsKey(key)) throw new Exception($"Package type {key} is not supported.");

            return installers[key]();
        }

        public bool IsInstallerAvailable(Package package)
        {
            return IsInstallerAvailable(package.Filepath);
        }

        public bool IsInstallerAvailable(string artifactFileName)
        {
            string key = Path.GetExtension(artifactFileName).ToLower(System.Globalization.CultureInfo.InvariantCulture);
            return installers.ContainsKey(key);
        }
    }
}
