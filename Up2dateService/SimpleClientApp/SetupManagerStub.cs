using System.Collections.Generic;
using Up2dateShared;

namespace SimpleClientApp
{
    public class SetupManagerStub : ISetupManager
    {
        public IEnumerable<string> SupportedExtensions => new[] { ".msi", ".nuget" };

        public List<Package> GetAvaliablePackages()
        {
            return new List<Package>();
        }

        public InstallPackageResult InstallPackage(string packageFile)
        {
            return InstallPackageResult.Success;
        }

        public void InstallPackages(IEnumerable<Package> packages)
        {
            throw new System.NotImplementedException();
        }

        public bool IsFileSupported(string artifactFileName)
        {
            throw new System.NotImplementedException();
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