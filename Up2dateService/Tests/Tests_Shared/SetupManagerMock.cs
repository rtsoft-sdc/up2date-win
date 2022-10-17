using Moq;
using System;
using Up2dateShared;

namespace Tests_Shared
{
    public class SetupManagerMock : Mock<ISetupManager>
    {
        private PackageStatus packageStatus;
        public PackageStatus PackageStatus
        {
            get => packageStatus;
            set
            {
                packageStatus = value;
                Setup(m => m.GetStatus(It.IsAny<string>())).Returns(value);
            }
        }

        private bool isPackageInstalled;
        public bool IsPackageInstalled
        {
            get => isPackageInstalled;
            set
            {
                isPackageInstalled = value;
                Setup(m => m.IsPackageInstalled(It.IsAny<string>())).Returns(value);
            }
        }

        private bool isFileDownloaded;
        public bool IsFileDownloaded
        {
            get => isFileDownloaded;
            set
            {
                isFileDownloaded = value;
                Setup(m => m.IsFileDownloaded(It.IsAny<string>(), It.IsAny<string>())).Returns(value);
            }
        }

        private bool isFileSupported;
        public bool IsFileSupported
        {
            get => isFileSupported;
            set
            {
                isFileSupported = value;
                Setup(m => m.IsFileSupported(It.IsAny<string>())).Returns(value);
            }
        }

        private InstallPackageResult installPackageResult;
        public InstallPackageResult InstallPackageResult
        {
            get => installPackageResult;
            set
            {
                installPackageResult = value;
                Setup(m => m.GetInstallPackageResult(It.IsAny<string>())).Returns(value);
            }
        }

        public SetupManagerMock()
        {
            IsFileSupported = true;
            IsFileDownloaded = false;
            IsPackageInstalled = false;
            PackageStatus = PackageStatus.Unavailable;
            InstallPackageResult = InstallPackageResult.Success;

            Setup(m => m.DownloadPackage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<string>>())).Returns(Result.Successful);
            Setup(m => m.InstallPackage(It.IsAny<string>()));
        }

        public void VerifyExecution(string fileName, bool download, bool install)
        {
            if (download)
            {
                Verify(m => m.DownloadPackage(fileName, It.IsAny<string>(), It.IsAny<Action<string>>()), Times.Once);
            }
            else
            {
                Verify(m => m.DownloadPackage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<string>>()), Times.Never);
            }
            if (install)
            {
                Verify(m => m.InstallPackage(fileName), Times.Once);
            }
            else
            {
                Verify(m => m.InstallPackage(It.IsAny<string>()), Times.Never);
            }
        }
    }
}
