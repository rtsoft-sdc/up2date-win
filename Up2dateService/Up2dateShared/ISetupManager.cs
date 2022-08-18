using System.Collections.Generic;
using System.Threading.Tasks;
using Up2dateClient;

namespace Up2dateShared
{
    public interface ISetupManager
    {
        List<Package> GetAvaliablePackages();
        bool InstallPackage(string packageFile, SupportedTypes packageType = SupportedTypes.Unsupported);
        Task InstallPackagesAsync(IEnumerable<Package> packages);
        bool IsPackageAvailable(string packageFile);
        bool IsPackageInstalled(string packageFile);
        void OnDownloadStarted(string artifactFileName);
        void OnDownloadFinished(string artifactFileName);
    }
}