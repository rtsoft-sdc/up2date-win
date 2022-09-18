using Up2dateShared;

namespace Up2dateService.Interfaces
{
    public interface IPackageValidator
    {

        /// <summary>
        /// Checks if the package is appropriately signed
        /// </summary>
        /// <param name="package">Package</param>
        /// <returns>True is the package is appropriately signed or signing is not supported for this type of package</returns>
        bool VerifySignature(Package package);
    }
}
