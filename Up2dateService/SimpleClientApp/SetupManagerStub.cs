using System.Collections.Generic;
using System.Threading.Tasks;
using Up2dateClient;
using Up2dateShared;

namespace SimpleClientApp
{
    public class SetupManagerStub : ISetupManager
    {
        public List<Package> GetAvaliablePackages()
        {
            return new List<Package>();
        }

        public bool InstallPackage(string packageFile, SupportedTypes packageType = SupportedTypes.Unsupported)
        {
            return true;
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
