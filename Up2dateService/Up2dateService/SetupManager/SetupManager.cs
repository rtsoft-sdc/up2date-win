using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
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

            SafeRefreshPackageList(initializing:true);
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

        public void AcceptPackage(Package package)
        {
            Package packageToAccept = SafeFindPackage(package.Filepath);
            if (packageToAccept.Status == PackageStatus.Unavailable) return;

            packageToAccept.Status = PackageStatus.AcceptPending;
            SafeUpdatePackage(packageToAccept);
        }

        public void RejectPackage(Package package)
        {
            Package packageToReject = SafeFindPackage(package.Filepath);
            if (packageToReject.Status == PackageStatus.Unavailable) return;

            packageToReject.Status = PackageStatus.RejectPending;
            SafeUpdatePackage(packageToReject);
        }

        public void MarkPackageRejected(string artifactFileName)
        {
            Package package = SafeFindPackage(artifactFileName);
            if (package.Status == PackageStatus.Unavailable) return;

            package.Status = PackageStatus.Rejected;
            SafeUpdatePackage(package);
        }

        public void MarkPackageWaitingForConfirmation(string artifactFileName, bool forced)
        {
            Package package = SafeFindPackage(artifactFileName);
            if (package.Status == PackageStatus.Unavailable) return;

            package.Status = forced ? PackageStatus.WaitingForConfirmationForced : PackageStatus.WaitingForConfirmation;
            SafeUpdatePackage(package);
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

            var checkResult = CheckMD5(package.Filepath, artifactFileHashMd5);

            return checkResult.Success && checkResult.Value == true;
        }

        public bool IsPackageInstalled(string artifactFileName)
        {
            SafeRefreshPackageList();
            return SafeFindPackage(artifactFileName).Status == PackageStatus.Installed;
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

        public void CreateOrUpdatePackage(string artifactFileName, int id)
        {
            Package package = SafeFindPackage(artifactFileName);
            if (package.Status == PackageStatus.Unavailable)
            {
                package.Status = PackageStatus.Available;
                package.ErrorCode = InstallPackageResult.Success;
                package.Filepath = Path.Combine(downloadLocationProvider(), artifactFileName);
            }
            if (package.DeploymentActionID != id)
            {
                package.DeploymentActionID = id;
                SafeAddOrUpdatePackage(package);
            }
        }

        public Result DownloadPackage(string artifactFileName, string artifactFileHashMd5, Action<string> downloadArtifact)
        {
            try
            {
                downloadArtifact(downloadLocationProvider());
                Result<bool> checkResult = CheckMD5(Path.Combine(downloadLocationProvider(), artifactFileName), artifactFileHashMd5);
                if (!checkResult.Success)
                {
                    return Result.Failed($"Cannot verify MD5 checksum. {checkResult.ErrorMessage}");
                }
                else if (checkResult.Value != true)
                {
                    return Result.Failed($"MD5 checksum doesn't match.");
                }
            }
            catch (Exception e)
            {
                return Result.Failed($"Exception during download. {e.Message}");
            }
            finally
            {
                SafeRefreshPackageList();
            }

            return Result.Successful();
        }

        public bool Cancel(int actionId)
        {
            Package package = SafeFindPackage(actionId);
            if (package.Status == PackageStatus.RejectPending
                || package.Status == PackageStatus.AcceptPending
                || package.Status == PackageStatus.WaitingForConfirmation
                || package.Status == PackageStatus.WaitingForConfirmationForced)
            {
                package.Status = PackageStatus.Downloaded;
                SafeUpdatePackage(package);

                return true;
            }
            return false;
        }

        public Result DeletePackage(Package package)
        {
            return SafeRemovePackage(package.Filepath, deleteFile: true);
        }

        private Result<bool> CheckMD5(string filename, string expectedMd5hex)
        {
            const int maxAttempts = 10; // max attemts to read the file in case it is blocked by another process
            const int retryIntervalMs = 2000; // interval between attempts (2 sec)

            using (var md5 = MD5.Create())
            {
                Exception lastException = null;
                for (int i = 0; i < maxAttempts; i++)
                {
                    try
                    {
                        using (var stream = File.OpenRead(filename))
                        {
                            string actualMd5hex = BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "");
                            return Result<bool>.Successful(string.Equals(actualMd5hex, expectedMd5hex, StringComparison.InvariantCultureIgnoreCase));
                        }
                    }
                    catch (Exception e)
                    {
                        logger.WriteEntry("Exception on attempt to calculate MD5. ", e);
                        if (!(e is IOException)) return Result<bool>.Failed(e.Message);
                        lastException = e;
                    }
                    Thread.Sleep(retryIntervalMs);
                }
                return Result<bool>.Failed(lastException?.Message);
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
                logger.WriteEntry($"PackageInProgress flag is detected for the package {package.ProductName}. Status = {package.Status}");
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

        private Package SafeFindPackage(int actionId)
        {
            return SafeGetPackages().FirstOrDefault(p => p.DeploymentActionID == actionId);
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

        private Result SafeRemovePackage(string filepath, bool deleteFile)
        {
            lock (packagesLock)
            {
                Package package = FindPackage(filepath);
                if (package.Status != PackageStatus.Unavailable)
                {
                    if (deleteFile)
                    {
                        try
                        {
                            File.Delete(package.Filepath);
                        }
                        catch (Exception ex)
                        {
                            return Result.Failed(ex.Message);
                        }
                    }
                    packages.Remove(package);
                }
                return Result.Successful();
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

        private void SafeRefreshPackageList(bool initializing = false)
        {
            var logBuilder = new StringBuilder("\nPackages:\n");
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

                    // Don't update package status while it is being installed right now - status may be invalid
                    // (e.g. Choco reports product as installed at the very beginning of the installation process)
                    // Except the case when service had been restared during or because of installation process.
                    if (!initializing && IsSetPackageInProgressFlag(updatedPackage)) continue;

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
                            && updatedPackage.Status != PackageStatus.WaitingForConfirmation
                            && updatedPackage.Status != PackageStatus.WaitingForConfirmationForced
                            && updatedPackage.Status != PackageStatus.RejectPending
                            && updatedPackage.Status != PackageStatus.AcceptPending
                            && updatedPackage.Status != PackageStatus.Rejected
                            && updatedPackage.Status != PackageStatus.Failed)
                        {
                            updatedPackage.Status = PackageStatus.Downloaded;
                        }
                    }

                    packages[i] = updatedPackage;
                    logBuilder.AppendLine($"{updatedPackage.Status} {updatedPackage.ProductName}");
                }
                if (initializing)
                {
                    logger.WriteEntry(logBuilder.ToString());
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
