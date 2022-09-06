using System.Collections.Generic;

namespace Up2dateShared
{
    public interface ISetupManager
    {
        List<Package> GetAvaliablePackages();
        InstallPackageResult InstallPackage(string packageFile);
        void InstallPackages(IEnumerable<Package> packages);
        bool IsPackageAvailable(string packageFile);
        bool IsPackageInstalled(string packageFile);
        void OnDownloadStarted(string artifactFileName);
        void OnDownloadFinished(string artifactFileName);
    }
}