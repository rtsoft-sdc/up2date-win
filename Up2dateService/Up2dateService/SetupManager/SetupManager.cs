using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Up2dateClient;
using Up2dateShared;

namespace Up2dateService.SetupManager
{
    public class SetupManager : ISetupManager
    {
        private const string MsiExtension = ".msi";
        private const string ZipExtension = ".zip";

        private const int MsiExecResultSuccess = 0;
        private const int MsiExecResultRestartNeeded = 3010;
        private readonly Func<string> downloadLocationProvider;
        private readonly EventLog eventLog;

        private readonly Action<Package, int> onSetupFinished;
        private readonly List<Package> packages = new List<Package>();
        private readonly object packagesLock = new object();

        public SetupManager(EventLog eventLog, Action<Package, int> onSetupFinished,
            Func<string> downloadLocationProvider)
        {
            this.eventLog = eventLog ?? throw new ArgumentNullException(nameof(eventLog));
            this.onSetupFinished = onSetupFinished;
            this.downloadLocationProvider = downloadLocationProvider ??
                                            throw new ArgumentNullException(nameof(downloadLocationProvider));

            RefreshPackageList();
        }

        public List<Package> GetAvaliablePackages()
        {
            RefreshPackageList();
            return SafeGetPackages();
        }

        public async Task InstallPackagesAsync(IEnumerable<Package> packages)
        {
            await InstallPackages(packages);
        }

        public bool IsPackageAvailable(string packageFile)
        {
            RefreshPackageList();
            return FindPackage(packageFile).Status != PackageStatus.Unavailable;
        }

        public bool IsPackageInstalled(string packageFile)
        {
            RefreshPackageList();
            var package = FindPackage(packageFile);
            return package.Status == PackageStatus.Installed || package.Status == PackageStatus.RestartNeeded;
        }

        public bool InstallPackage(string packageFile, SupportedTypes packageType = SupportedTypes.Unsupported)
        {
            switch (packageType)
            {
                case SupportedTypes.Msi:
                    return InstallPackageMsi(packageFile);
                case SupportedTypes.Zip:
                    return InstallPackageZip(packageFile);
                case SupportedTypes.Unsupported:
                default:
                    return false;
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

        public bool InstallPackageMsi(string packageFile)
        {
            var package = FindPackage(packageFile);
            if (package.Status == PackageStatus.Unavailable) return false;

            var exitCode = InstallPackageAsync(package,MsiExtension, CancellationToken.None).Result;

            RefreshPackageList();
            return exitCode == 0;
        }

        public bool InstallPackageBat(string packageFile)
        {
            var exitCode = InstallPackageBatAsync(packageFile, CancellationToken.None).Result;
            return exitCode == 0;
        }

        public bool InstallPackageSetupExe(string packageFile)
        {
            throw new NotImplementedException();
        }

        public bool InstallPackageZip(string packageFile)
        {
            if (packageFile.EndsWith($"installed{ZipExtension}"))
            {
                return true;
            }

            var package = FindPackage(packageFile);
            var directoryName = Path.GetDirectoryName(package.Filepath);
            if (directoryName != null)
            {
                var package1 = package;
                var files = Directory.EnumerateFiles(directoryName).Where(file=>file!= package1.Filepath);
                if (files.Any(file =>
                        Path.GetFileNameWithoutExtension(package.Filepath) == Path.GetFileNameWithoutExtension(file)))
                {
                    File.Delete(package.Filepath);
                    return true;
                }
            }

            var extractDirectory = Path.ChangeExtension(package.Filepath, null);
            try
            {
                if (Directory.Exists(extractDirectory)) Directory.Delete(extractDirectory, true);

                Directory.CreateDirectory(extractDirectory ?? throw new InvalidOperationException());
            }
            catch (Exception)
            {
                var exception = new Exception($"Failed to create directory {extractDirectory}");
                throw exception;
            }

            using (var zipFile = ZipFile.OpenRead(package.Filepath))
            {
                try
                {
                    zipFile.ExtractToDirectory(extractDirectory);
                }
                catch (Exception)
                {
                    var exception = new Exception("Failed to extract directory");
                    throw exception;
                }
            }

            var batList = Directory.GetFiles(extractDirectory, "*.bat", SearchOption.AllDirectories).ToList();
            if (batList.Count > 0)
            {
                var anyFailed = batList.Aggregate(false, (current, bat) => current || !InstallPackageBat(bat));
                if (!anyFailed)
                {
                    UpdatePackageStatus(ref package, MsiExecResultSuccess);
                    SafeUpdatePackage(package);
                    File.Move(package.Filepath,
                        Path.ChangeExtension(package.Filepath, $"installed{ZipExtension}") ??
                        throw new InvalidOperationException());
                    RefreshPackageList();
                }

                return !anyFailed;
            }

            var setupExe = Directory.GetFiles(extractDirectory, "*setup.exe", SearchOption.AllDirectories).ToList();
            if (setupExe.Count > 0)
            {
                var anyFailed = setupExe.Aggregate(false, (current, setup) => current || InstallPackageSetupExe(setup));
                if (!anyFailed)
                {
                    UpdatePackageStatus(ref package, MsiExecResultSuccess);
                    SafeUpdatePackage(package);
                    File.Move(package.Filepath,
                        Path.ChangeExtension(package.Filepath, $"installed{ZipExtension}") ??
                        throw new InvalidOperationException());
                    RefreshPackageList();
                }

                return !anyFailed;
            }

            var msiList = Directory.GetFiles(extractDirectory, "*.msi", SearchOption.AllDirectories).ToList();
            if (msiList.Count > 0)
            {
                var anyFailed = msiList.Aggregate(false, (current, msi) => current || InstallPackageMsi(msi));
                if (!anyFailed)
                {
                    UpdatePackageStatus(ref package, MsiExecResultSuccess);
                    SafeUpdatePackage(package);
                    File.Move(package.Filepath,
                        Path.ChangeExtension(package.Filepath, $"installed{ZipExtension}") ??
                        throw new InvalidOperationException());
                    RefreshPackageList();
                }

                return !anyFailed;
            }

            return false;
        }

        private Package FindPackage(string packageFile)
        {
            return SafeGetPackages().FirstOrDefault(p =>
                Path.GetFileName(p.Filepath).Equals(packageFile, StringComparison.InvariantCultureIgnoreCase));
        }

        private List<Package> SafeGetPackages()
        {
            List<Package> lockedPackages;
            lock (packagesLock)
            {
                lockedPackages = packages.ToList();
            }

            return lockedPackages;
        }

        private void SafeUpdatePackages(IEnumerable<Package> newPackageList)
        {
            lock (packagesLock)
            {
                packages.Clear();
                packages.AddRange(newPackageList);
            }
        }

        private void SafeUpdatePackage(Package package)
        {
            lock (packagesLock)
            {
                var original = packages.FirstOrDefault(p =>
                    p.Filepath.Equals(package.Filepath, StringComparison.InvariantCultureIgnoreCase));
                if (original.Status != PackageStatus.Unavailable) packages[packages.IndexOf(original)] = package;
            }
        }

        private void SafeRemovePackage(string filepath, PackageStatus status)
        {
            lock (packagesLock)
            {
                var package = packages.FirstOrDefault(p =>
                    p.Status == status && p.Filepath.Equals(filepath, StringComparison.InvariantCultureIgnoreCase));
                if (package.Status != PackageStatus.Unavailable) packages.Remove(package);
            }
        }

        private void SafeAddOrUpdatePackage(Package package)
        {
            lock (packagesLock)
            {
                var original = packages.FirstOrDefault(p =>
                    p.Filepath.Equals(package.Filepath, StringComparison.InvariantCultureIgnoreCase));
                if (original.Status == PackageStatus.Unavailable)
                    packages.Add(package);
                else
                    packages[packages.IndexOf(original)] = package;
            }
        }

        private async Task InstallPackages(IEnumerable<Package> packagesToInstall)
        {
            foreach (var inPackage in packagesToInstall)
            {
                var packageExtension = Path.GetExtension(inPackage.Filepath).ToLowerInvariant();
                if (!string.IsNullOrEmpty(packageExtension) && !packageExtension.Equals(MsiExtension, StringComparison.InvariantCultureIgnoreCase) && !packageExtension.Equals(ZipExtension, StringComparison.InvariantCultureIgnoreCase))
                    continue;

                var lockedPackages = SafeGetPackages();

                var package = lockedPackages.FirstOrDefault(p =>
                    p.Filepath.Equals(inPackage.Filepath, StringComparison.InvariantCultureIgnoreCase));
                if (package.Status == PackageStatus.Unavailable) continue;

                package.ErrorCode = 0;
                package.Status = PackageStatus.Installing;

                SafeUpdatePackage(package);

                var result = await InstallPackageAsync(package, packageExtension, CancellationToken.None);

                UpdatePackageStatus(ref package, result);
                eventLog.WriteEntry(
                    $"{Path.GetFileName(package.Filepath)} installation finished with result: {package.Status}");
                onSetupFinished?.Invoke(package, result);

                SafeUpdatePackage(package);
            }
        }

        private void UpdatePackageStatus(ref Package package, int result)
        {
            switch (result)
            {
                case MsiExecResultSuccess:
                    package.Status = PackageStatus.Installed;
                    break;
                case MsiExecResultRestartNeeded:
                    package.Status = PackageStatus.RestartNeeded;
                    break;
                default:
                    package.Status = PackageStatus.Failed;
                    break;
            }

            package.ErrorCode = result;
        }

        private Task<int> InstallPackageAsync(Package package, string packageExtension, CancellationToken cancellationToken)
        {
            switch (packageExtension)
            {
                case MsiExtension:
                    return InstallPackageMsiTaskAsync(package, cancellationToken);
                case ZipExtension:
                    return Task.Run(() => Convert.ToInt32(InstallPackageZip(Path.GetFileName(package.Filepath))), cancellationToken);
                default:
                    throw new Exception("Failed to install unknown package");
            }
        }

        private Task<int> InstallPackageMsiTaskAsync(Package package, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                const int cancellationCheckPeriodMs = 1000;

                using (var p = new Process())
                {
                    p.StartInfo.FileName = "msiexec.exe";
                    p.StartInfo.Arguments = $"/i \"{package.Filepath}\" ALLUSERS=1 /qn";
                    p.StartInfo.UseShellExecute = false;
                    //p.StartInfo.RedirectStandardOutput = true;
                    _ = p.Start();
                    //string output = p.StandardOutput.ReadToEnd();

                    do
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    } while (!p.WaitForExit(cancellationCheckPeriodMs));

                    return p.ExitCode;
                }
            }, cancellationToken);
        }

        private Task<int> InstallPackageBatAsync(string filename, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                const int cancellationCheckPeriodMs = 1000;

                using (var p = new Process())
                {
                    p.StartInfo.FileName = "cmd.exe";
                    p.StartInfo.Arguments = $"/c \"{filename}\"";
                    p.StartInfo.WorkingDirectory = Path.GetDirectoryName(filename) ?? string.Empty;
                    p.StartInfo.UseShellExecute = false;
                    _ = p.Start();

                    do
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    } while (!p.WaitForExit(cancellationCheckPeriodMs));

                    return p.ExitCode;
                }
            }, cancellationToken);
        }

        private void RefreshPackageList()
        {
            var lockedPackages = SafeGetPackages();

            var msiFolder = downloadLocationProvider();
            var files = Directory.GetFiles(msiFolder).ToList();

            var packagesToRemove = lockedPackages.Where(p =>
                p.Status != PackageStatus.Downloading &&
                !files.Any(f => p.Filepath.Equals(f, StringComparison.InvariantCultureIgnoreCase))).ToList();
            foreach (var package in packagesToRemove) _ = lockedPackages.Remove(package);

            foreach (var file in files)
            {
                var package = lockedPackages.FirstOrDefault(p =>
                    p.Filepath.Equals(file, StringComparison.InvariantCultureIgnoreCase));
                if (!lockedPackages.Contains(package))
                {
                    package.Filepath = file;
                    var info = MsiHelper.GetInfo(file);
                    package.ProductCode = info?.ProductCode;
                    package.ProductName = info?.ProductName;
                    lockedPackages.Add(package);
                    package.Status = PackageStatus.Downloaded;
                }
            }

            var installationChecker = new ProductInstallationChecker();


            for (var i = 0; i < lockedPackages.Count; i++)
            {
                var updatedPackage = lockedPackages[i];
                if (installationChecker.IsPackageInstalled(updatedPackage.ProductCode))
                {
                    installationChecker.UpdateInfo(ref updatedPackage);
                    updatedPackage.Status = PackageStatus.Installed;
                }
                else
                {
                    updatedPackage.DisplayName = null;
                    updatedPackage.Publisher = null;
                    updatedPackage.DisplayVersion = null;
                    updatedPackage.Version = null;
                    updatedPackage.InstallDate = null;
                    updatedPackage.EstimatedSize = null;
                    updatedPackage.UrlInfoAbout = null;
                    if (updatedPackage.Status != PackageStatus.Downloading &&
                        updatedPackage.Status != PackageStatus.Installing)
                    {
                        updatedPackage.Status = PackageStatus.Downloaded;
                        if (Path.GetExtension(updatedPackage.Filepath).ToLowerInvariant() == ZipExtension)
                        {
                            var filename = Path.GetFileName(updatedPackage.Filepath);
                            if (filename != null && filename.EndsWith($".installed{ZipExtension}"))
                                updatedPackage.Status = PackageStatus.Installed;
                        }
                    }
                }

                lockedPackages[i] = updatedPackage;
            }

            SafeUpdatePackages(lockedPackages);
        }
    }
}