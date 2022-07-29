using System;

namespace Up2dateService.SetupManager
{
    public class MsiInfo
    {
        public MsiInfo(string productCode, string productName)
        {
            if (string.IsNullOrWhiteSpace(productCode))
            {
                throw new ArgumentException($"'{nameof(productCode)}' cannot be null or whitespace.", nameof(productCode));
            }

            ProductCode = productCode;
            ProductName = productName;
        }

        public string ProductCode { get; }

        public string ProductName { get; }
    }
}
