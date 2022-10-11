using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
            var package = SafeFindPackage(packageFile);
            return InstallPackage(package);
        }

        public void InstallPackages(IEnumerable<Package> packagesToInstall)
        {
            foreach (string inPackage in packagesToInstall.Where(p => installerFactory.IsInstallerAvailable(p)).Select(inPackage => inPackage.Filepath))
            {
                Package package = SafeFindPackage(inPackage);
                if (package.Status == PackageStatus.Unavailable) continue;

                InstallPackage(package);
            }
        }

        public bool IsFileSupported(string artifactFileName)
        {
            return installerFactory.IsInstallerAvailable(artifactFileName);
        }

        public bool IsFileDownloaded(string artifactFileName, string artifactFileHashMd5)
        {
            SafeRefreshPackageList();
            Package package = SafeFindPackage(artifactFileName);
            if (package.Status == PackageStatus.Unavailable || package.Status == PackageStatus.Downloading) return false;

            bool isMd5OK = CheckMD5(package.Filepath, artifactFileHashMd5).Success;

            return isMd5OK;
        }

        public bool IsPackageInstalled(string artifactFileName)
        {
            SafeRefreshPackageList();
            return SafeFindPackage(artifactFileName).Status == PackageStatus.Installed;
        }

        public void MarkPackageAsSuggested(string artifactFileName)
        {
            Package package = SafeFindPackage(artifactFileName);
            if (package.Status == PackageStatus.Downloaded)
            {
                package.Status = PackageStatus.SuggestedToInstall;
                SafeUpdatePackage(package);
            }
        }

        public PackageStatus GetStatus(string artifactFileName)
        {
            SafeRefreshPackageList();
            return SafeFindPackage(artifactFileName).Status;
        }

        public InstallPackageResult GetInstallPackageResult(string artifactFileName)
        {
            SafeRefreshPackageList();
            return SafeFindPackage(artifactFileName).ErrorCode;
        }

        public Result DownloadPackage(string artifactFileName, string artifactFileHashMd5, Action<string> downloadArtifact)
        {
            // add temporary "downloading" package item
            var package = new Package
            {
                Status = PackageStatus.Downloading,
                ErrorCode = InstallPackageResult.Success,
                Filepath = Path.Combine(downloadLocationProvider(), artifactFileName)
            };
            SafeAddOrUpdatePackage(package);

            try
            {
                downloadArtifact(downloadLocationProvider());
                Result checkResult = CheckMD5(package.Filepath, artifactFileHashMd5);
                if (!checkResult.Success)
                {
                    return Result.Failed($"MD5 verification failed. {checkResult.ErrorMessage}");
                }
            }
            catch (Exception e)
            {
                return Result.Failed(e.Message);
            }
            finally
            {
                // remove temporary "downloading" package item, so refresh would be able to add "downloaded" package item instead
                SafeRemovePackage(Path.Combine(downloadLocationProvider(), artifactFileName), PackageStatus.Downloading);
                SafeRefreshPackageList();
            }

            return Result.Successful();
        }

        static private Result CheckMD5(string filename, string md5hex)
        {
            using (var md5 = MD5.Create())
            {
                try
                {
                    using (var stream = File.OpenRead(filename))
                    {
                        var hash = md5.ComputeHash(stream);
                        if (BitConverter.ToString(hash).Replace("-", "").Equals(md5hex, StringComparison.InvariantCultureIgnoreCase))
                        {
                            return Result.Successful();
                        }
                        return Result.Failed();
                    }
                }
                catch (Exception e)
                {
                    return Result.Failed(e.Message);
                }
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

        private Package SafeFindPackage(string packageFile)
        {
            return SafeGetPackages().FirstOrDefault(p => Path.GetFileName(p.Filepath).Equals(Path.GetFileName(packageFile), StringComparison.InvariantCultureIgnoreCase));
        }

        private Package FindPackage(string packageFile)
        {
            return packages.FirstOrDefault(p => Path.GetFileName(p.Filepath).Equals(Path.GetFileName(packageFile), StringComparison.InvariantCultureIgnoreCase));
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
                Package original = FindPackage(package.Filepath);
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
                Package package = FindPackage(filepath);
                if (package.Status == status)
                {
                    packages.Remove(package);
                }
            };
        }

        private void SafeAddOrUpdatePackage(Package package)
        {
            lock (packagesLock)
            {
                Package original = FindPackage(package.Filepath);
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
                    Package package = FindPackage(file);
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
