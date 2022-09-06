using System;

namespace Up2dateService.SetupManager
{
    public class MsiInfo
    {
        public MsiInfo(string productCode, string productName, string productVersion)
        {
            if (string.IsNullOrWhiteSpace(productCode))
            {
                throw new ArgumentException($"'{nameof(productCode)}' cannot be null or whitespace.", nameof(productCode));
            }

            ProductCode = productCode;
            ProductName = productName;
            ProductVersion = productVersion;
        }

        public string ProductCode { get; }

        public string ProductName { get; }

        public string ProductVersion { get; }
    }
}
