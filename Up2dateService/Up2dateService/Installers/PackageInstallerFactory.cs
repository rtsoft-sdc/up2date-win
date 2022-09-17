using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
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
        private readonly ISettingsManager settingsManager;
        private readonly ISignatureVerifier signatureVerifier;

        public PackageInstallerFactory(ISettingsManager settingsManager, ISignatureVerifier signatureVerifier, IWhiteListManager whiteListManager)
        {
            this.settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            this.signatureVerifier = signatureVerifier ?? throw new ArgumentNullException(nameof(signatureVerifier));

            installers.Add(MsiExtension, () =>
            {
                if (msiInstaller == null) msiInstaller = new MsiInstaller(VerifyCertificate);
                return msiInstaller;
            });
            installers.Add(NugetExtension, () =>
            {
                if (chocoInstaller == null) chocoInstaller = new ChocoInstaller(() => settingsManager.DefaultChocoSources, settingsManager, whiteListManager);
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
            return IsSupported(package.Filepath);
        }

        public bool IsSupported(string artifactFileName)
        {
            string key = Path.GetExtension(artifactFileName).ToLower(System.Globalization.CultureInfo.InvariantCulture);
            return installers.ContainsKey(key);
        }

        private bool VerifyCertificate(X509Certificate2 certificate)
        {
            return signatureVerifier.VerifySignature(certificate, settingsManager.SignatureVerificationLevel);
        }
    }
}
