using System;
using System.Collections.Generic;

namespace Up2dateShared
{
    public interface ISetupManager
    {
        List<Package> GetAvaliablePackages();
        InstallPackageResult InstallPackage(string packageFile);
        void InstallPackages(IEnumerable<Package> packages);
        void AcceptPackage(Package package);
        void RejectPackage(Package package);
        bool IsFileSupported(string artifactFileName);
        bool IsFileDownloaded(string artifactFileName, string artifactFileHashMd5);
        bool IsPackageInstalled(string artifactFileName);
        void MarkPackageWaitingForConfirmation(string artifactFileName, bool forced);
        void MarkPackageRejected(string artifactFileName);
        PackageStatus GetStatus(string artifactFileName);
        InstallPackageResult GetInstallPackageResult(string artifactFileName);
        Result DownloadPackage(string artifactFileName, string artifactFileHashMd5, Action<string> downloadArtifact);
        bool Cancel(int actionId);
        void CreateOrUpdatePackage(string artifactFileName, int id);
        Result DeletePackage(Package package);
    }
}