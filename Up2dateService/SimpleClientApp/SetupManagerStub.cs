using System;
using System.Collections.Generic;
using Up2dateShared;

namespace SimpleClientApp
{
    public class SetupManagerStub : ISetupManager
    {
        public IEnumerable<string> SupportedExtensions => new[] { ".msi", ".nuget" };

        public Result DownloadPackage(string artifactFileName, string artifactFileHashMd5, Action<string> downloadArtifact)
        {
            return Result.Successful();
        }

        public List<Package> GetAvaliablePackages()
        {
            return new List<Package>();
        }

        public InstallPackageResult GetInstallPackageResult(string artifactFileName)
        {
            return InstallPackageResult.Success;
        }

        public PackageStatus GetStatus(string artifactFileName)
        {
            return PackageStatus.Unavailable;
        }

        public InstallPackageResult InstallPackage(string packageFile)
        {
            return InstallPackageResult.Success;
        }

        public void InstallPackages(IEnumerable<Package> packages)
        {
            throw new System.NotImplementedException();
        }

        public bool IsFileDownloaded(string artifactFileName, string artifactFileHashMd5)
        {
            return true;
        }

        public bool IsFileSupported(string artifactFileName)
        {
            return true;
        }

        public bool IsPackageAvailable(string packageFile)
        {
            return false;
        }

        public bool IsPackageInstalled(string packageFile)
        {
            return false;
        }

        public void MarkPackageAsSuggested(string artifactFileName)
        {
        }
    }
}