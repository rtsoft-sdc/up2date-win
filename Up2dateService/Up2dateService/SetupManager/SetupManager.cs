using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Up2dateService.Interfaces;
using Up2dateShared;

namespace Up2dateService.SetupManager
{
    public class SetupManager : ISetupManager
    {
        private readonly Func<string> downloadLocationProvider;
        private readonly IPackageInstallerFactory installerFactory;
        private readonly IPackageValidatorFactory validatorFactory;
        private readonly ILogger logger;
        private readonly List<Package> packages = new List<Package>();
        private readonly object packagesLock = new object();
        private readonly ISettingsManager settingsManager;

        public SetupManager(ILogger logger, Func<string> downloadLocationProvider, ISettingsManager settingsManager,
            IPackageInstallerFactory installerFactory, IPackageValidatorFactory validatorFactory)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.downloadLocationProvider = downloadLocationProvider ?? throw new ArgumentNullException(nameof(downloadLocationProvider));
            this.settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            this.installerFactory = installerFactory ?? throw new ArgumentNullException(nameof(installerFactory));
            this.validatorFactory = validatorFactory ?? throw new ArgumentNullException(nameof(validatorFactory));

            SafeRefreshPackageList();
        }

        public List<Package> GetAvaliablePackages()
        {
            SafeRefreshPackageList();
            return SafeGetPackages();
        }

        public InstallPackageResult InstallPackage(string packageFile)
        {
            var package = FindPackage(packageFile);
            return InstallPackage(package);
        }

        public void InstallPackages(IEnumerable<Package> packagesToInstall)
        {
            foreach (string inPackage in packagesToInstall.Where(p => installerFactory.IsInstallerAvailable(p)).Select(inPackage => inPackage.Filepath))
            {
                Package package = SafeGetPackages().FirstOrDefault(p => p.Filepath.Equals(inPackage, StringComparison.InvariantCultureIgnoreCase));
                if (package.Status == PackageStatus.Unavailable) continue;

                InstallPackage(package);
            }
        }

        public void OnDownloadStarted(string artifactFileName)
        {
            // add temporary "downloading" package item
            var package = new Package
            {
                Status = PackageStatus.Downloading,
                ErrorCode = InstallPackageResult.Success,
            Filepath = Path.Combine(downloadLocationProvider(), artifactFileName)
            };
            SafeAddOrUpdatePackage(package);
        }

        public void OnDownloadFinished(string artifactFileName)
        {
            // remove temporary "downloading" package item, so refresh would be able to add "downloaded" package item instead
            SafeRemovePackage(Path.Combine(downloadLocationProvider(), artifactFileName), PackageStatus.Downloading);
            SafeRefreshPackageList();
        }

        public bool IsFileSupported(string artifactFileName)
        {
            return installerFactory.IsInstallerAvailable(artifactFileName);
        }

        public bool IsFileDownloaded(string artifactFileName, string artifactFileHashMd5)
        {
            // todo - check file hash
            SafeRefreshPackageList();
            return SafeGetPackages().Any(p => string.Equals(Path.GetFileName(p.Filepath), artifactFileName, StringComparison.InvariantCultureIgnoreCase)
                                     && p.Status != PackageStatus.Unavailable && p.Status != PackageStatus.Downloading);
        }

        public bool IsPackageInstalled(string artifactFileName)
        {
            SafeRefreshPackageList();
            return SafeGetPackages().Any(p => string.Equals(Path.GetFileName(p.Filepath), artifactFileName, StringComparison.InvariantCultureIgnoreCase)
                                     && p.Status == PackageStatus.Installed);
        }

        public void MarkPackageAsSuggested(string artifactFileName)
        {
            Package package = SafeGetPackages().FirstOrDefault(p => string.Equals(Path.GetFileName(p.Filepath), artifactFileName, StringComparison.InvariantCultureIgnoreCase)
                                     && p.Status == PackageStatus.Downloaded);
            if (package.Status != PackageStatus.Unavailable)
            {
                package.Status = PackageStatus.SuggestedToInstall;
                SafeUpdatePackage(package);
            }
        }

        private InstallPackageResult InstallPackage(Package package)
        {
            package.ErrorCode = InstallPackageResult.Success;
            package.Status = PackageStatus.Installing;
            SafeUpdatePackage(package);

            var result = InstallPackage(ref package);

            UpdatePackageStatus(ref package, result);
            SafeUpdatePackage(package);
            SafeRefreshPackageList();

            return result;
        }

        private InstallPackageResult InstallPackage(ref Package package)
        {
            if (IsSetPackageInProgressFlag(package))
            {
                ClearPackageInProgressFlag();
                if (package.Status == PackageStatus.Installed) return InstallPackageResult.Success;
                if (package.Status == PackageStatus.RestartNeeded) return InstallPackageResult.RestartNeeded;
                return InstallPackageResult.GeneralInstallationError;
            }

            if (package.Status == PackageStatus.Unavailable) return InstallPackageResult.PackageUnavailable;
            if (package.Status == PackageStatus.Installed) return InstallPackageResult.Success;
            if (package.Status == PackageStatus.RestartNeeded) return InstallPackageResult.RestartNeeded;

            if (!installerFactory.IsInstallerAvailable(package)) return InstallPackageResult.PackageNotSupported;

            if (validatorFactory.IsValidatorAvailable(package))
            {
                IPackageValidator validator = validatorFactory.GetValidator(package);
                if (settingsManager.CheckSignature && !validator.VerifySignature(package))
                {
                    return InstallPackageResult.SignatureVerificationFailed;
                }
            }

            IPackageInstaller installer = installerFactory.GetInstaller(package);

            var logsLocation = Path.Combine(downloadLocationProvider(), "Logs");
            var logFilePath = Path.Combine(logsLocation, Path.GetFileName(package.Filepath) + ".log");
            try
            {
                Directory.CreateDirectory(logsLocation);
                if (File.Exists(logFilePath))
                {
                    File.Delete(logFilePath);
                }
            }
            catch
            {
                logFilePath = null;
            }

            SetPackageInProgressFlag(package);
            InstallPackageResult result;
            try
            {
                result = installer.InstallPackage(package, logFilePath);
                if (result == InstallPackageResult.Success)
                {
                    installer.UpdatePackageInfo(ref package);
                }
            }
            finally
            {
                ClearPackageInProgressFlag();
            }

            return result;
        }

        private void SetPackageInProgressFlag(Package package)
        {
            settingsManager.PackageInProgress = package.ProductCode;
        }

        private void ClearPackageInProgressFlag()
        {
            settingsManager.PackageInProgress = string.Empty;
        }

        private bool IsSetPackageInProgressFlag(Package package)
        {
            return string.Equals(package.ProductCode, settingsManager.PackageInProgress);
        }

        private Package FindPackage(string packageFile)
        {
            return SafeGetPackages().FirstOrDefault(p => Path.GetFileName(p.Filepath).Equals(packageFile, StringComparison.InvariantCultureIgnoreCase));
        }

        private List<Package> SafeGetPackages()
        {
            List<Package> lockedPackages;
            lock (packagesLock)
            {
                lockedPackages = packages.ToList();
            };
            return lockedPackages;
        }

        private void SafeUpdatePackage(Package package)
        {
            lock (packagesLock)
            {
                Package original = packages.FirstOrDefault(p => p.Filepath.Equals(package.Filepath, StringComparison.InvariantCultureIgnoreCase));
                if (original.Status != PackageStatus.Unavailable)
                {
                    packages[packages.IndexOf(original)] = package;
                }
            };
        }

        private void SafeRemovePackage(string filepath, PackageStatus status)
        {
            lock (packagesLock)
            {
                Package package = packages.FirstOrDefault(p => p.Status == status && p.Filepath.Equals(filepath, StringComparison.InvariantCultureIgnoreCase));
                if (package.Status != PackageStatus.Unavailable)
                {
                    packages.Remove(package);
                }
            };
        }

        private void SafeAddOrUpdatePackage(Package package)
        {
            lock (packagesLock)
            {
                Package original = packages.FirstOrDefault(p => p.Filepath.Equals(package.Filepath, StringComparison.InvariantCultureIgnoreCase));
                if (original.Status == PackageStatus.Unavailable)
                {
                    packages.Add(package);
                }
                else
                {
                    packages[packages.IndexOf(original)] = package;
                }
            };
        }

        private void UpdatePackageStatus(ref Package package, InstallPackageResult result)
        {
            switch (result)
            {
                case InstallPackageResult.Success:
                    package.Status = PackageStatus.Installed;
                    break;
                case InstallPackageResult.RestartNeeded:
                    package.Status = PackageStatus.RestartNeeded;
                    break;
                default:
                    package.Status = PackageStatus.Failed;
                    break;
            }
            package.ErrorCode = result;
        }

        private void SafeRefreshPackageList()
        {
            lock (packagesLock)
            {
                string downloadFolder = downloadLocationProvider();
                List<string> files = Directory.GetFiles(downloadFolder).ToList();

                List<Package> packagesToRemove = packages.Where(p => p.Status != PackageStatus.Downloading && !files.Any(f => p.Filepath.Equals(f, StringComparison.InvariantCultureIgnoreCase))).ToList();
                foreach (Package package in packagesToRemove)
                {
                    _ = packages.Remove(package);
                }

                foreach (string file in files)
                {
                    Package package = packages.FirstOrDefault(p => p.Filepath.Equals(file, StringComparison.InvariantCultureIgnoreCase));
                    if (!packages.Contains(package))
                    {
                        package.Filepath = file;

                        if (!installerFactory.IsInstallerAvailable(package)) continue;

                        IPackageInstaller installer = installerFactory.GetInstaller(package);
                        if (!installer.Initialize(ref package)) continue;

                        package.Status = PackageStatus.Downloaded;
                        package.ErrorCode = InstallPackageResult.Success;
                        packages.Add(package);
                    }
                }

                RefreshinstallersProductList(packages);

                for (int i = 0; i < packages.Count; i++)
                {
                    Package updatedPackage = packages[i];

                    // Don't update package status while it is being installed - status may be invalid
                    // (e.g. Choco reports product as installed at the very beginning of the installation process)
                    if (IsSetPackageInProgressFlag(updatedPackage)) continue;

                    if (!installerFactory.IsInstallerAvailable(updatedPackage)) continue;

                    var installer = installerFactory.GetInstaller(updatedPackage);
                    if (installer.IsPackageInstalled(updatedPackage))
                    {
                        if (updatedPackage.Status != PackageStatus.Installed)
                        {
                            installer.UpdatePackageInfo(ref updatedPackage);
                            updatedPackage.Status = PackageStatus.Installed;
                            updatedPackage.ErrorCode = InstallPackageResult.Success;
                        }
                    }
                    else
                    {
                        updatedPackage.DisplayName = null;
                        updatedPackage.Publisher = null;
                        updatedPackage.InstallDate = null;
                        updatedPackage.EstimatedSize = null;
                        updatedPackage.UrlInfoAbout = null;
                        if (updatedPackage.Status != PackageStatus.Downloading 
                            && updatedPackage.Status != PackageStatus.Installing
                            && updatedPackage.Status != PackageStatus.SuggestedToInstall
                            && updatedPackage.Status != PackageStatus.Failed)
                        {
                            updatedPackage.Status = PackageStatus.Downloaded;
                        }
                    }

                    packages[i] = updatedPackage;
                }
            };
        }

        private void RefreshinstallersProductList(IEnumerable<Package> packages)
        {
            var installers = new List<IPackageInstaller>();
            foreach (IPackageInstaller installer in packages
                .Where(p => installerFactory.IsInstallerAvailable(p))
                .Select(p => installerFactory.GetInstaller(p))
                .Distinct())
            {
                installer.Refresh();
            }
        }
    }
}
