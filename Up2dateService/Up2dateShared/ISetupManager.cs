using System.Collections.Generic;

namespace Up2dateShared
{
    public interface ISetupManager
    {
        List<Package> GetAvaliablePackages();
        InstallPackageResult InstallPackage(string packageFile);
        void InstallPackages(IEnumerable<Package> packages);
        void OnDownloadStarted(string artifactFileName);
        void OnDownloadFinished(string artifactFileName);
        bool IsFileSupported(string artifactFileName);
        bool IsFileDownloaded(string artifactFileName, string artifactFileHashMd5);
        bool IsPackageInstalled(string artifactFileName);
        void MarkPackageAsSuggested(string artifactFileName);
        PackageStatus GetStatus(string artifactFileName);
        InstallPackageResult GetInstallPackageResult(string artifactFileName);
    }
}