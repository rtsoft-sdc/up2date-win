using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            RefreshPackageList();
        }

        public List<Package> GetAvaliablePackages()
        {
            RefreshPackageList();
            return SafeGetPackages();
        }

        public bool IsPackageAvailable(string packageFile)
        {
            RefreshPackageList();
            return FindPackage(packageFile).Status != PackageStatus.Unavailable;
        }

        public bool IsPackageInstalled(string packageFile)
        {
            RefreshPackageList();
            Package package = FindPackage(packageFile);
            return package.Status == PackageStatus.Installed || package.Status == PackageStatus.RestartNeeded;
        }

        public InstallPackageResult InstallPackage(string packageFile)
        {
            var package = FindPackage(packageFile);
            var result = InstallPackage(ref package);
            UpdatePackageStatus(ref package, result);
            SafeUpdatePackage(package);
            RefreshPackageList();

            return result;
        }

        public void InstallPackages(IEnumerable<Package> packagesToInstall)
        {
            foreach (string inPackage in packagesToInstall.Where(p => installerFactory.IsInstallerAvailable(p)).Select(inPackage => inPackage.Filepath))
            {
                var lockedPackages = SafeGetPackages();

                Package package = lockedPackages.FirstOrDefault(p => p.Filepath.Equals(inPackage, StringComparison.InvariantCultureIgnoreCase));
                if (package.Status == PackageStatus.Unavailable) continue;

                package.ErrorCode = InstallPackageResult.Success;
                package.Status = PackageStatus.Installing;

                SafeUpdatePackage(package);

                var result = InstallPackage(ref package);
                UpdatePackageStatus(ref package, result);
                SafeUpdatePackage(package);
                RefreshPackageList();
            }
        }

        public void OnDownloadStarted(string artifactFileName)
        {
            // add temporary "downloading" package item
            var package = new Package
            {
                Status = PackageStatus.Downloading,
                Filepath = Path.Combine(downloadLocationProvider(), artifactFileName)
            };
            SafeAddOrUpdatePackage(package);
        }

        public void OnDownloadFinished(string artifactFileName)
        {
            // remove temporary "downloading" package item, so refresh would be able to add "downloaded" package item instead
            SafeRemovePackage(Path.Combine(downloadLocationProvider(), artifactFileName), PackageStatus.Downloading);
            RefreshPackageList();
        }

        public bool IsFileSupported(string artifactFileName)
        {
            return installerFactory.IsInstallerAvailable(artifactFileName);
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

            SetPackageInProgressFlag(package);
            try
            {
                using (Process p = installer.StartInstallationProcess(package))
                {
                    const int checkPeriodMs = 1000;
                    const int ExitCodeSuccess = 0;
                    const int MsiExitCodeRestartNeeded = 3010;

                    while (!p.WaitForExit(checkPeriodMs)) ;

                    if (p.ExitCode == ExitCodeSuccess)
                    {
                        installer.UpdatePackageInfo(ref package);
                        return InstallPackageResult.Success;
                    }

                    if (p.ExitCode == MsiExitCodeRestartNeeded) return InstallPackageResult.RestartNeeded;

                    logger.WriteEntry(installer.GetType().Name, $"Installation of the package '{package.ProductName}' failed with the exit code: {p.ExitCode}");
                    return InstallPackageResult.GeneralInstallationError;
                }
            }
            catch (Exception exception)
            {
                logger.WriteEntry(installer.GetType().Name, $"Unhandled exception during package '{package.ProductName}' installation", exception);
                return InstallPackageResult.CannotStartInstaller;
            }
            finally
            {
                ClearPackageInProgressFlag();
            }
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

        private void SafeUpdatePackages(IEnumerable<Package> newPackageList)
        {
            lock (packagesLock)
            {
                packages.Clear();
                packages.AddRange(newPackageList);
            };
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

        private void RefreshPackageList()
        {
            var lockedPackages = SafeGetPackages();

            string downloadFolder = downloadLocationProvider();
            List<string> files = Directory.GetFiles(downloadFolder).ToList();

            List<Package> packagesToRemove = lockedPackages.Where(p => p.Status != PackageStatus.Downloading && !files.Any(f => p.Filepath.Equals(f, StringComparison.InvariantCultureIgnoreCase))).ToList();
            foreach (Package package in packagesToRemove)
            {
                _ = lockedPackages.Remove(package);
            }

            foreach (string file in files)
            {
                Package package = lockedPackages.FirstOrDefault(p => p.Filepath.Equals(file, StringComparison.InvariantCultureIgnoreCase));
                if (!lockedPackages.Contains(package))
                {
                    package.Filepath = file;

                    if (!installerFactory.IsInstallerAvailable(package)) continue;

                    IPackageInstaller installer = installerFactory.GetInstaller(package);
                    if (!installer.Initialize(ref package)) continue;

                    package.Status = PackageStatus.Downloaded;
                    lockedPackages.Add(package);
                }
            }

            RefreshinstallersProductList(lockedPackages);

            for (int i = 0; i < lockedPackages.Count; i++)
            {
                Package updatedPackage = lockedPackages[i];

                if (!installerFactory.IsInstallerAvailable(updatedPackage)) continue;

                var installer = installerFactory.GetInstaller(updatedPackage);
                if (installer.IsPackageInstalled(updatedPackage))
                {
                    if (updatedPackage.Status != PackageStatus.Installed)
                    {
                        installer.UpdatePackageInfo(ref updatedPackage);
                        updatedPackage.Status = PackageStatus.Installed;
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
                        && updatedPackage.Status != PackageStatus.Failed)
                    {
                        updatedPackage.Status = PackageStatus.Downloaded;
                    }
                }

                lockedPackages[i] = updatedPackage;
            }

            SafeUpdatePackages(lockedPackages);
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
