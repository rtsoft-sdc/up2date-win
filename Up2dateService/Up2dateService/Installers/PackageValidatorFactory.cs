using System;
using System.Collections.Generic;
using System.IO;
using Up2dateService.Installers.Choco;
using Up2dateService.Installers.Msi;
using Up2dateService.Interfaces;
using Up2dateShared;

namespace Up2dateService.Installers
{
    public class PackageValidatorFactory : IPackageValidatorFactory
    {
        private const string MsiExtension = ".msi";
        private const string NugetExtension = ".nupkg";

        private MsiValidator msiValidator = null;
        private ChocoValidator chocoValidator = null;

        private readonly Dictionary<string, Func<IPackageValidator>> validators = new Dictionary<string, Func<IPackageValidator>>();

        public PackageValidatorFactory(ISettingsManager settingsManager, ISignatureVerifier signatureVerifier, IWhiteListManager whiteListManager, ILogger logger)
        {
            if (settingsManager is null) throw new ArgumentNullException(nameof(settingsManager));
            if (signatureVerifier is null) throw new ArgumentNullException(nameof(signatureVerifier));
            if (whiteListManager is null) throw new ArgumentNullException(nameof(whiteListManager));
            if (logger is null) throw new ArgumentNullException(nameof(logger));

            validators.Add(MsiExtension, () =>
            {
                if (msiValidator == null) msiValidator = new MsiValidator(settingsManager, whiteListManager, signatureVerifier);
                return msiValidator;
            });
            validators.Add(NugetExtension, () =>
            {
                if (chocoValidator == null) chocoValidator = new ChocoValidator(settingsManager, whiteListManager, logger);
                return chocoValidator;
            });
        }

        public IPackageValidator GetValidator(Package package)
        {
            string key = Path.GetExtension(package.Filepath).ToLower(System.Globalization.CultureInfo.InvariantCulture);
            if (!validators.ContainsKey(key)) throw new Exception($"Package type {key} is not supported.");

            return validators[key]();
        }

        public bool IsValidatorAvailable(Package package)
        {
            return IsValidatorAvailable(package.Filepath);
        }

        public bool IsValidatorAvailable(string artifactFileName)
        {
            string key = Path.GetExtension(artifactFileName).ToLower(System.Globalization.CultureInfo.InvariantCulture);
            return validators.ContainsKey(key);
        }
    }
}
