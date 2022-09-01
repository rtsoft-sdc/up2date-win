using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Up2dateShared;

namespace Up2dateService.SetupManager
{
    public class SetupManager : ISetupManager
    {
        private const string MsiExtension = ".msi";
        private const string NugetExtension = ".nupkg";
        private const string ExternalInstallLog = "install.out";
        private const string Up2DateChocoId = "up2date";

        private const int MillisecondsToWait = 1000;

        private const int MsiExecResult_Success = 0;
        private const int MsiExecResult_RestartNeeded = 3010;

        private readonly Action<Package, int> onSetupFinished;
        private static readonly List<string> AllowedExtensions = new List<string>
        {
            MsiExtension, NugetExtension
        };

        private readonly Func<string> downloadLocationProvider;
        private readonly EventLog eventLog;
        private readonly List<Package> packages = new List<Package>();
        private readonly object packagesLock = new object();
        private readonly ISettingsManager settingsManager;

        public SetupManager(EventLog eventLog, Action<Package, int> onSetupFinished, Func<string> downloadLocationProvider, ISettingsManager settingsManager)
        {
            this.eventLog = eventLog ?? throw new ArgumentNullException(nameof(eventLog));
            this.onSetupFinished = onSetupFinished;
            this.downloadLocationProvider = downloadLocationProvider ?? throw new ArgumentNullException(nameof(downloadLocationProvider));
            this.settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));

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
            Package package = FindPackage(packageFile);
            return package.Status == PackageStatus.Installed || package.Status == PackageStatus.RestartNeeded;
        }

        public InstallPackageStatus InstallPackage(string packageFile)
        {
            try
            {
                Package package = FindPackage(packageFile);
                if (package.Status == PackageStatus.Unavailable) return InstallPackageStatus.PackageUnavailable;

                if (string.Equals(Path.GetExtension(packageFile),
                        NugetExtension,
                        StringComparison.InvariantCultureIgnoreCase))
                    try
                    {
                        return !ChocoHelper.IsChocoInstalled()
                            ? InstallPackageStatus.ChocoNotInstalled
                            : InstallChocoNupkg(package);
                    }
                    catch (Exception exception)
                    {
                        WriteLogEntry(exception);
                        return InstallPackageStatus.GeneralChocoError;
                    }

                int exitCode = InstallPackageAsync(package, CancellationToken.None).Result;
                return exitCode == 0 ? InstallPackageStatus.Ok : InstallPackageStatus.MsiInstallationError;
            }
            finally
            {
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

        private InstallPackageStatus InstallChocoNupkg(Package package)
        {
            if (!ChocoHelper.IsChocoInstalled())
            {
                return InstallPackageStatus.ChocoNotInstalled;
            }

            string logDirectory = downloadLocationProvider() + @"\install\";
            try
            {
                if (Directory.Exists(logDirectory)) Directory.Delete(logDirectory, true);

                Directory.CreateDirectory(logDirectory);
            }
            catch (Exception exception)
            {
                WriteLogEntry(exception);
                return InstallPackageStatus.TempDirectoryFail;
            }

            try
            {
                ChocoNugetInfo nugetInfo = ChocoNugetInfo.GetInfo(package.Filepath);
                package.ProductCode = nugetInfo.Id;
                package.DisplayVersion = nugetInfo.Version;
            }
            catch (Exception exception)
            {
                WriteLogEntry(exception);
                return InstallPackageStatus.InvalidChocoPackage;
            }

            try
            {
                ChocoHelper.InstallChocoPackage(package, logDirectory, downloadLocationProvider(), ExternalInstallLog);
            }
            catch (Exception exception)
            {
                WriteLogEntry(exception);
                return InstallPackageStatus.PsScriptInvokeError;
            }

            while (Process.GetProcessesByName("choco.exe").Length > 0)
                Thread.Sleep(MillisecondsToWait);
            
            return ChocoHelper.IsPackageInstalled(package) ? InstallPackageStatus.Ok : InstallPackageStatus.FailedToInstallChocoPackage;
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

        private async Task InstallPackages(IEnumerable<Package> packagesToInstall)
        {
            await Task.Run(() =>
            {
                foreach (string inPackage in packagesToInstall.Select(inPackage => inPackage.Filepath))
                {
                    if (!AllowedExtensions.Contains(Path.GetExtension(inPackage),
                            StringComparer.InvariantCultureIgnoreCase)) continue;

                    var lockedPackages = SafeGetPackages();

                    Package package = lockedPackages.FirstOrDefault(p =>
                        p.Filepath.Equals(inPackage, StringComparison.InvariantCultureIgnoreCase));
                    if (package.Status == PackageStatus.Unavailable) continue;

                    if (package.ProductName.Contains(Up2DateChocoId)) continue;
                    package.ErrorCode = 0;
                    package.Status = PackageStatus.Installing;

                    SafeUpdatePackage(package);
                    int result = string.Equals(Path.GetExtension(package.Filepath),
                        NugetExtension,
                        StringComparison.InvariantCultureIgnoreCase)
                        ? (int)InstallChocoNupkg(package)
                        : InstallPackageAsync(package, CancellationToken.None).Result;

                    UpdatePackageStatus(ref package, result);
                    eventLog.WriteEntry(
                        $"{Path.GetFileName(package.Filepath)} installation finished with result: {package.Status}");
                    onSetupFinished?.Invoke(package, result);

                    SafeUpdatePackage(package);
                }
            });
        }

        private void UpdatePackageStatus(ref Package package, int result)
        {
            switch (result)
            {
                case MsiExecResult_Success:
                    package.Status = PackageStatus.Installed;
                    break;
                case MsiExecResult_RestartNeeded:
                    package.Status = PackageStatus.RestartNeeded;
                    break;
                default:
                    package.Status = PackageStatus.Failed;
                    break;
            }
            package.ErrorCode = result;
        }

        private Task<int> InstallPackageAsync(Package package, CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                const int cancellationCheckPeriodMs = 1000;

                using (Process p = new Process())
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
                    }
                    while (!p.WaitForExit(cancellationCheckPeriodMs));

                    return p.ExitCode;
                }
            }, cancellationToken);
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
                    MsiInfo info = MsiHelper.GetInfo(file);
                    package.ProductCode = info?.ProductCode;
                    package.ProductName = info?.ProductName;
                    lockedPackages.Add(package);
                    package.Status = PackageStatus.Downloaded;
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
                    updatedPackage.DisplayVersion = null;
                    updatedPackage.Version = null;
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
    }
}
