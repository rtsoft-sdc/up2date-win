
using Up2dateShared;

namespace Up2dateService.Interfaces
{
    public interface IPackageInstaller
    {
        /// <summary>
        /// Initializes the main fields of Package by getting information from the file specified in Package.Filepath
        /// </summary>
        /// <param name="package">Package. Must have Package.Filepath property set</param>
        /// <returns>False if installer failed to initialize (e.g. wrong package format or access problems)</returns>
        bool Initialize(ref Package package);

        /// <summary>
        /// Checks if the Package is installed (tries to get information from installation registry/database/store)
        /// </summary>
        /// <param name="package"></param>
        /// <returns>True if the package is installed</returns>
        bool IsPackageInstalled(Package package);

        /// <summary>
        /// Updates the fields of the Package assuming it is already installed (tries to get extended info from installation registry/database/store)
        /// </summary>
        /// <param name="package"></param>
        void UpdatePackageInfo(ref Package package);

        /// <summary>
        /// Refreshes the internal cache of installed packages
        /// Recommended to invoke before using IsPackageInstalled/UpdatePackageInfo if some changes in installation base is expected
        /// </summary>
        void Refresh();

        /// <summary>
        /// Installs Package synchronously; write log to specified file
        /// </summary>
        /// <param name="Package">Package</param>
        /// <param name="logFilePath">Log file path</param>
        /// <returns></returns>
        InstallPackageResult InstallPackage(Package package, string logFilePath);
    }
}
