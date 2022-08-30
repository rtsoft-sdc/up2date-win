using System.Collections.Generic;
using System.Threading.Tasks;
using Up2dateShared;

namespace SimpleClientApp
{
    public class SetupManagerStub : ISetupManager
    {
        public List<Package> GetAvaliablePackages()
        {
            return new List<Package>();
        }

        public InstallPackageStatus InstallPackage(string packageFile)
        {
            return InstallPackageStatus.Ok;
        }

        public Task InstallPackagesAsync(IEnumerable<Package> packages)
        {
            return Task.CompletedTask;
        }

        public bool IsPackageAvailable(string packageFile)
        {
            return false;
        }

        public bool IsPackageInstalled(string packageFile)
        {
            return false;
        }

        public void OnDownloadFinished(string artifactFileName)
        {
        }

        public void OnDownloadStarted(string artifactFileName)
        {
        }
    }
}