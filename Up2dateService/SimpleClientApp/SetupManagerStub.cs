using System;
using System.Collections.Generic;
using Up2dateShared;

namespace SimpleClientApp
{
    public class SetupManagerStub : ISetupManager
    {
        public void AcceptPackage(Package package)
        {
        }

        public bool Cancel(int actionId)
        {
            return true;
        }

        public void CreateOrUpdatePackage(string artifactFileName, int id)
        {
        }

        public Result DeletePackage(Package package)
        {
            return Result.Successful();
        }

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
        }

        public bool IsFileDownloaded(string artifactFileName, string artifactFileHashMd5)
        {
            return true;
        }

        public bool IsFileSupported(string artifactFileName)
        {
            return true;
        }

        public bool IsPackageInstalled(string packageFile)
        {
            return false;
        }

        public void MarkPackageRejected(string artifactFileName)
        {
        }

        public void MarkPackageWaitingForConfirmation(string artifactFileName, bool forced)
        {
        }

        public void RejectPackage(Package package)
        {
        }
    }
}