using System.Collections.Generic;
using System.Threading.Tasks;

namespace Up2dateShared
{
    public interface ISetupManager
    {
        List<Package> GetAvaliablePackages();
        InstallPackageStatus InstallPackage(string packageFile);
        Task InstallPackagesAsync(IEnumerable<Package> packagesList);
        bool IsPackageAvailable(string packageFile);
        bool IsPackageInstalled(string packageFile);
        void OnDownloadStarted(string artifactFileName);
        void OnDownloadFinished(string artifactFileName);
    }
}