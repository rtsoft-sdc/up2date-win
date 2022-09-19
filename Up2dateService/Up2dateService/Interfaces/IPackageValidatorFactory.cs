using Up2dateShared;

namespace Up2dateService.Interfaces
{
    public interface IPackageValidatorFactory
    {
        /// <summary>
        /// Creates Validator sutable for the specififed Package
        /// </summary>
        /// <param name="package">Package</param>
        /// <returns>Validator</returns>
        IPackageValidator GetValidator(Package package);

        /// <summary>
        /// Checks if an Validator is available for the specififed Package
        /// </summary>
        /// <param name="package">Package</param>
        /// <returns></returns>
        bool IsValidatorAvailable(Package package);

        /// <summary>
        /// Checks if an Validator is available for the specified file
        /// </summary>
        /// <param name="artifactFileName"></param>
        /// <returns></returns>
        bool IsValidatorAvailable(string artifactFileName);
    }
}
