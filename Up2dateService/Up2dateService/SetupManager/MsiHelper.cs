using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Up2dateShared;

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

        public static InstallPackageResult InstallPackage(Package package)
        {
            const int MsiExecResult_Success = 0;
            const int MsiExecResult_RestartNeeded = 3010;

            const int cancellationCheckPeriodMs = 1000;

            using (Process p = new Process())
            {
                p.StartInfo.FileName = "msiexec.exe";
                p.StartInfo.Arguments = $"/i \"{package.Filepath}\" ALLUSERS=1 /qn";
                p.StartInfo.UseShellExecute = false;
                _ = p.Start();

                while (!p.WaitForExit(cancellationCheckPeriodMs)) ;

                if (p.ExitCode == MsiExecResult_Success) return InstallPackageResult.Success;
                if (p.ExitCode == MsiExecResult_RestartNeeded) return InstallPackageResult.RestartNeeded;
                return InstallPackageResult.GeneralInstallationError;
            }
        }


    }
}
