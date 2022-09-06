using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Up2dateShared;

namespace Up2dateService.SetupManager
{
    public class SetupManager : ISetupManager
    {
        private enum PackageType
        {
            Msi,
            Choco,
            Unknown
        }

        private const string MsiExtension = ".msi";
        private const string NugetExtension = ".nupkg";

        private static readonly List<string> SupportedExtensions = new List<string>
        {
            MsiExtension, NugetExtension
        };

        private readonly Func<string> downloadLocationProvider;
        private readonly EventLog eventLog;
        private readonly List<Package> packages = new List<Package>();
        private readonly object packagesLock = new object();

        public SetupManager(EventLog eventLog, Func<string> downloadLocationProvider)
        {
            this.eventLog = eventLog ?? throw new ArgumentNullException(nameof(eventLog));
            this.downloadLocationProvider = downloadLocationProvider ?? throw new ArgumentNullException(nameof(downloadLocationProvider));

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
            return InstallPackage(FindPackage(packageFile));
        }

        public void InstallPackages(IEnumerable<Package> packagesToInstall)
        {
            foreach (string inPackage in packagesToInstall.Select(inPackage => inPackage.Filepath))
            {
                if (!SupportedExtensions.Contains(Path.GetExtension(inPackage), StringComparer.InvariantCultureIgnoreCase)) continue;

                var lockedPackages = SafeGetPackages();

                Package package = lockedPackages.FirstOrDefault(p => p.Filepath.Equals(inPackage, StringComparison.InvariantCultureIgnoreCase));
                if (package.Status == PackageStatus.Unavailable) continue;

                package.ErrorCode = 0;
                package.Status = PackageStatus.Installing;

                SafeUpdatePackage(package);

                var result = InstallPackage(package);

                UpdatePackageStatus(ref package, result);
                eventLog.WriteEntry($"{Path.GetFileName(package.Filepath)} installation finished with result: {package.Status}");

                SafeUpdatePackage(package);
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

        private InstallPackageResult InstallPackage(Package package)
        {
            try
            {
                if (package.Status == PackageStatus.Unavailable) return InstallPackageResult.PackageUnavailable;
                if (package.Status == PackageStatus.Installed) return InstallPackageResult.Success;
                if (package.Status == PackageStatus.RestartNeeded) return InstallPackageResult.RestartNeeded;

                return GetType(package) == PackageType.Choco
                    ? ChocoHelper.InstallPackage(package)
                    : MsiHelper.InstallPackage(package);
            }
            catch (Exception exception)
            {
                WriteLogEntry(exception);
                return InstallPackageResult.GeneralInstallationError;
            }
            finally
            {
                RefreshPackageList();
            }
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
            package.ErrorCode = (int)result;
        }

        private void RefreshPackageList()
        {
            var lockedPackages = SafeGetPackages();

            string msiFolder = downloadLocationProvider();
            List<string> files = Directory.GetFiles(msiFolder).ToList();

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

                    if (GetType(package) == PackageType.Choco)
                    {
                        ChocoNugetInfo info = ChocoNugetInfo.GetInfo(package.Filepath);
                        package.ProductName = info?.Id;
                        package.DisplayVersion = info?.Version;
                        package.ProductCode = $"{info?.Id} {info?.Version}";
                    }
                    else
                    {
                        MsiInfo info = MsiHelper.GetInfo(file);
                        package.ProductName = info?.ProductName;
                        package.DisplayVersion = info?.ProductVersion;
                        package.ProductCode = info?.ProductCode;
                    }

                    package.Status = PackageStatus.Downloaded;
                    lockedPackages.Add(package);
                }
            }

            ProductInstallationChecker installationChecker = new ProductInstallationChecker();

            for (int i = 0; i < lockedPackages.Count; i++)
            {
                Package updatedPackage = lockedPackages[i];
                if (installationChecker.IsPackageInstalled(updatedPackage))
                {
                    installationChecker.UpdateInfo(ref updatedPackage);
                    updatedPackage.Status = PackageStatus.Installed;
                }
                else
                {
                    updatedPackage.DisplayName = null;
                    updatedPackage.Publisher = null;
                    updatedPackage.InstallDate = null;
                    updatedPackage.EstimatedSize = null;
                    updatedPackage.UrlInfoAbout = null;
                    if (updatedPackage.Status != PackageStatus.Downloading && updatedPackage.Status != PackageStatus.Installing)
                    {
                        updatedPackage.Status = PackageStatus.Downloaded;
                    }
                }

                lockedPackages[i] = updatedPackage;
            }

            SafeUpdatePackages(lockedPackages);
        }

        private void WriteLogEntry(Exception error)
        {
            EventLog.WriteEntry("UP2DATEService", error.Message);
        }

        private PackageType GetType(Package package)
        {
            if (string.Equals(Path.GetExtension(package.Filepath), NugetExtension, StringComparison.InvariantCultureIgnoreCase)) return PackageType.Choco;
            if (string.Equals(Path.GetExtension(package.Filepath), MsiExtension, StringComparison.InvariantCultureIgnoreCase)) return PackageType.Msi;
            return PackageType.Unknown;
        }
    }
}
