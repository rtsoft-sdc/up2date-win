using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Up2dateService.SetupManager
{
    public class MsiInfo
    {
        [DllImport("msi.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
        private static extern uint MsiOpenPackageW(string szPackagePath, out IntPtr hProduct);

        [DllImport("msi.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
        private static extern uint MsiCloseHandle(IntPtr hAny);

        [DllImport("msi.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
        private static extern uint MsiGetPropertyW(IntPtr hAny, string name, StringBuilder buffer, ref int bufferLength);

        private MsiInfo(string productCode, string productName, string productVersion)
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

        public static MsiInfo GetInfo(string msiFileName)
        {
            const uint ErrorSuccess = 0;
            IntPtr MsiHandle = IntPtr.Zero;
            try
            {
                var errcode = MsiOpenPackageW(msiFileName, out MsiHandle);
                if (errcode != ErrorSuccess) return null;

                int length = 256;
                var buffer = new StringBuilder(length);
                errcode = MsiGetPropertyW(MsiHandle, "ProductCode", buffer, ref length);
                if (errcode != ErrorSuccess) return null;
                string productCode = buffer.ToString();

                string productName = null;
                errcode = MsiGetPropertyW(MsiHandle, "ProductName", buffer, ref length);
                if (errcode == ErrorSuccess)
                {
                    productName = buffer.ToString();
                }

                string productVersion = null;
                errcode = MsiGetPropertyW(MsiHandle, "ProductVersion", buffer, ref length);
                if (errcode == ErrorSuccess)
                {
                    productVersion = buffer.ToString();
                }

                return new MsiInfo(productCode, productName, productVersion);
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                if (MsiHandle != IntPtr.Zero)
                {
                    MsiCloseHandle(MsiHandle);
                }
            }
        }
    }
}
