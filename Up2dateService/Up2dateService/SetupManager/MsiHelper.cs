using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Up2dateService.SetupManager
{
    public class MsiHelper
    {
        [DllImport("msi.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
        private static extern uint MsiOpenPackageW(string szPackagePath, out IntPtr hProduct);

        [DllImport("msi.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
        private static extern uint MsiCloseHandle(IntPtr hAny);

        [DllImport("msi.dll", CharSet = CharSet.Unicode, PreserveSig = true, SetLastError = true, ExactSpelling = true)]
        private static extern uint MsiGetPropertyW(IntPtr hAny, string name, StringBuilder buffer, ref int bufferLength);

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
                return new MsiInfo(productCode, productName);
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
