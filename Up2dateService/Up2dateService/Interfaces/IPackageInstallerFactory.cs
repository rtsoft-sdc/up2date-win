using Up2dateShared;

namespace Up2dateService.Interfaces
{
    public interface IPackageInstallerFactory
    {
        /// <summary>
        /// Creates Installer sutable for the specififed Package
        /// </summary>
        /// <param name="package">Package</param>
        /// <returns>Installer</returns>
        IPackageInstaller GetInstaller(Package package);

        /// <summary>
        /// Checks if an Installer is available for the specififed Package
        /// </summary>
        /// <param name="package">Package</param>
        /// <returns></returns>
        bool IsSupported(Package package);

        /// <summary>
        /// Checks if an Installer is available for the specified file
        /// </summary>
        /// <param name="artifactFileName"></param>
        /// <returns></returns>
        bool IsSupported(string artifactFileName);
    }
}
