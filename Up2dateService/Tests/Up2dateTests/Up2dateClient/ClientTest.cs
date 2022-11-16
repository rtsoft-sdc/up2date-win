using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tests_Shared;
using Up2dateClient;
using Up2dateDotNet;
using Up2dateShared;

namespace Up2dateTests.Up2dateClient
{
    [TestClass]
    public class ClientTest
    {
        private WrapperMock wrapperMock;
        private SettingsManagerMock settingsManagerMock;
        private SetupManagerMock setupManagerMock;
        private LoggerMock loggerMock;
        private string certificate;
        private SystemInfo sysInfo = SystemInfo.Retrieve();
        private readonly Version version = new Version(1, 2, 3);

        [TestCleanup]
        public void Cleanup()
        {
            wrapperMock.ExitRun();
        }


        //
        //  General client run tests
        //

        [TestMethod]
        public void WhenCreated_ClientStatusIsStopped()
        {
            // arrange
            // act
            Client client = CreateClient();

            // assert
            Assert.IsNotNull(client.State);
            Assert.AreEqual(ClientStatus.Stopped, client.State.Status);
            Assert.IsTrue(string.IsNullOrEmpty(client.State.LastError));
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        public void GivenNoCertificate_WhenRun_ThenWrapperRunIsNotExecuted_AndStatusIsNoCertificate(string certificate)
        {
            // arrange
            Client client = CreateClient();
            this.certificate = certificate;

            // act
            client.Run();

            // assert
            wrapperMock.VerifyNoOtherCalls();
            Assert.AreEqual(ClientStatus.NoCertificate, client.State.Status);
        }

        [TestMethod]
        public void WhenRun_WrapperClientRunIsCalledWithCorrectArguments()
        {
            // arrange
            Client client = CreateClient();

            // act
            StartClient(client);

            // assert
            wrapperMock.Verify(m => m.RunClient(certificate, settingsManagerMock.Object.ProvisioningUrl, settingsManagerMock.Object.XApigToken,
                It.IsNotNull<AuthErrorActionFunc>(), It.IsNotNull<ConfigRequestFunc>(), It.IsNotNull<DeploymentActionFunc>(), It.IsNotNull<CancelActionFunc>()));
        }

        [TestMethod]
        public void GivenClientRunning_WhenWrapperClientRunExited_ThenStatusIsReconnectingWithoutMessage()
        {
            // arrange
            Client client = CreateClient();
            StartClient(client);

            // act
            wrapperMock.ExitRun();
            Thread.Sleep(100);

            // assert
            Assert.AreEqual(ClientStatus.Reconnecting, client.State.Status);
            Assert.IsTrue(string.IsNullOrEmpty(client.State.LastError));
        }

        [TestMethod]
        public void GivenClientRunning_WhenWrapperClientThrewException_ThenStatusIsReconnectingWithMessage()
        {
            // arrange
            Client client = CreateClient();
            string message = "exception message";
            wrapperMock.Setup(m => m.RunClient(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsNotNull<AuthErrorActionFunc>(), It.IsNotNull<ConfigRequestFunc>(), It.IsNotNull<DeploymentActionFunc>(), It.IsNotNull<CancelActionFunc>()))
                .Throws(new Exception(message));

            // act
            client.Run();

            // assert
            Assert.AreEqual(ClientStatus.Reconnecting, client.State.Status);
            StringAssert.Contains(client.State.LastError, message);
        }

        [TestMethod]
        public void WhenRun_ThenStatusIsRunning()
        {
            // arrange
            Client client = CreateClient();

            // act
            StartClient(client);

            // assert
            Assert.AreEqual(ClientStatus.Running, client.State.Status);
            Assert.IsTrue(string.IsNullOrEmpty(client.State.LastError));
        }


        //
        //  Authorization error callback tests
        //

        [TestMethod]
        public void WhenAuthErrorActionBringsErrorMessage_ThenStatusIsAuthorizationError()
        {
            // arrange
            Client client = CreateClient();
            string message = "Authorization Error message";
            StartClient(client);

            // act
            wrapperMock.AuthErrorCallback(message);

            // assert
            Assert.AreEqual(ClientStatus.AuthorizationError, client.State.Status);
            Assert.AreEqual(message, client.State.LastError);
        }


        //
        //  Config request callback tests
        //

        [DataTestMethod]
        [DataRow(false, SignatureVerificationLevel.SignedByAnyCertificate)]
        [DataRow(true, SignatureVerificationLevel.SignedByAnyCertificate)]
        [DataRow(true, SignatureVerificationLevel.SignedByTrustedCertificate)]
        [DataRow(true, SignatureVerificationLevel.SignedByWhitelistedCertificate)]
        public void WhenConfigRequested_ThenAddConfigAttributeIsCalledSupplyingSysInfoValues(bool checkSignature, SignatureVerificationLevel signatureVerificationLevel)
        {
            // arrange
            Client client = CreateClient();
            IntPtr responseBuilder = new IntPtr(-1);
            var callSequence = new List<(IntPtr ptr, string key, string value)>();
            wrapperMock.Setup(m => m.AddConfigAttribute(It.IsAny<IntPtr>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<IntPtr, string, string>((ptr, key, value) => { callSequence.Add((ptr, key, value)); });
            settingsManagerMock.Object.CheckSignature = checkSignature;
            settingsManagerMock.Object.SignatureVerificationLevel = signatureVerificationLevel;
            StartClient(client);

            // act
            wrapperMock.ConfigRequestFunc(responseBuilder);

            // assert
            int expectedCount = 0;
            CollectionAssert.Contains(callSequence, (responseBuilder, "client", "RITMS UP2DATE for Windows"));
            expectedCount++;
            CollectionAssert.Contains(callSequence, (responseBuilder, "client version", $"{version.Major}.{version.Minor}.{version.Build}"));
            expectedCount++;
            CollectionAssert.Contains(callSequence, (responseBuilder, "computer", sysInfo.MachineName));
            expectedCount++;
            CollectionAssert.Contains(callSequence, (responseBuilder, "machine GUID", sysInfo.MachineGuid));
            expectedCount++;
            CollectionAssert.Contains(callSequence, (responseBuilder, "OS platform", sysInfo.PlatformID.ToString()));
            expectedCount++;
            CollectionAssert.Contains(callSequence, (responseBuilder, "OS type", sysInfo.Is64Bit ? "64-bit" : "32-bit"));
            expectedCount++;
            CollectionAssert.Contains(callSequence, (responseBuilder, "OS version", sysInfo.VersionString));
            expectedCount++;
            CollectionAssert.Contains(callSequence, (responseBuilder, "OS service pack", sysInfo.ServicePack));
            expectedCount++;
            CollectionAssert.Contains(callSequence, (responseBuilder, "settings.requires_confirmation_before_update",
                settingsManagerMock.Object.RequiresConfirmationBeforeInstall ? "yes" : "no"));
            expectedCount++;
            CollectionAssert.Contains(callSequence, (responseBuilder, "settings.signature_verification_level",
                settingsManagerMock.Object.CheckSignature ? settingsManagerMock.Object.SignatureVerificationLevel.ToString() : "off"));
            expectedCount++;

            Assert.AreEqual(expectedCount, callSequence.Count);
        }


        //
        //  Cancel request callback tests
        //

        [TestMethod]
        public void WhenCancelRequested_ThenRequestReturnsTrue()
        {
            // arrange
            const int stopID = 1;
            Client client = CreateClient();
            StartClient(client);

            // act
            var result = wrapperMock.CancelActionFunc(stopID);

            // assert
            Assert.IsTrue(result);
        }


        //
        //  Deployment request callback tests
        //

        [TestMethod]
        public void GivenDeniedPackageType_WhenDeploymentRequested_ThenFailure()
        {
            // arrange
            Client client = CreateClient();
            IntPtr artifact = new IntPtr(-1);
            StartClient(client);

            // act
            wrapperMock.DeploymentActionFunc(artifact, new DeploymentInfo { artifactFileName = "name.ext" }, out ClientResult result);

            // assert
            Assert.AreEqual(Execution.CLOSED, result.Execution);
            Assert.AreEqual(Finished.FAILURE, result.Finished);
            Assert.IsFalse(string.IsNullOrEmpty(result.Message));
        }

        [TestMethod]
        public void GivenUnsupportedPackageType_WhenDeploymentRequested_ThenFailure()
        {
            // arrange
            Client client = CreateClient();
            IntPtr artifact = new IntPtr(-1);
            StartClient(client);
            setupManagerMock.IsFileSupported = false;

            // act
            wrapperMock.DeploymentActionFunc(artifact, new DeploymentInfo { artifactFileName = "name.msi" }, out ClientResult result);

            // assert
            Assert.AreEqual(Execution.CLOSED, result.Execution);
            Assert.AreEqual(Finished.FAILURE, result.Finished);
            Assert.IsFalse(string.IsNullOrEmpty(result.Message));
        }

        [TestMethod]
        public void GivenCancelWasRequested_WhenDeploymentRequested_ThenResultIsCanceled()
        {
            // arrange
            Client client = CreateClient();
            IntPtr artifact = new IntPtr(-1);
            const int reqID = 1;
            StartClient(client);
            wrapperMock.CancelActionFunc(reqID);

            // act
            wrapperMock.DeploymentActionFunc(artifact, new DeploymentInfo { artifactFileName = "name.msi", id = reqID }, out ClientResult result);

            // assert
            Assert.AreEqual(Execution.CANCELED, result.Execution);
            Assert.AreEqual(Finished.NONE, result.Finished);
            Assert.IsFalse(string.IsNullOrEmpty(result.Message));
        }

        [TestMethod]
        public void WhenDeploymentRequested_ThenDownloadMethodIsCorrectlyCalled()
        {
            // arrange
            Client client = CreateClient();
            IntPtr artifact = new IntPtr(-1);
            const string fileName = "name.msi";
            StartClient(client);

            // act
            wrapperMock.DeploymentActionFunc(artifact, new DeploymentInfo { artifactFileName = fileName }, out ClientResult result);

            // assert
            setupManagerMock.Verify(m => m.DownloadPackage(fileName, It.IsAny<string>(), It.IsNotNull<Action<string>>()), Times.Once);
        }

        [TestMethod]
        public void GivenFileIsAlreadyInstalled_WhenDeploymentRequested_ThenResultIsSuccess()
        {
            // arrange
            Client client = CreateClient();
            IntPtr artifact = new IntPtr(-1);
            const string fileName = "name.msi";
            StartClient(client);
            setupManagerMock.IsPackageInstalled = true;

            // act
            wrapperMock.DeploymentActionFunc(artifact, new DeploymentInfo { artifactFileName = fileName }, out ClientResult result);

            // assert
            setupManagerMock.VerifyExecution(fileName, download: true, install: false);
            Assert.AreEqual(Execution.CLOSED, result.Execution);
            Assert.AreEqual(Finished.SUCCESS, result.Finished);
        }

        [TestMethod]
        public void WhenSkipUpdateInMaintenanceWindowRequested_ThenInstallationIsNotExecuted_AndResultIsSuccess()
        {
            // arrange
            Client client = CreateClient();
            IntPtr artifact = new IntPtr(-1);
            const string fileName = "name.msi";
            StartClient(client);

            // act
            wrapperMock.DeploymentActionFunc(artifact, new DeploymentInfo
            {
                artifactFileName = fileName,
                isInMaintenanceWindow = true,
                updateType = "skip"
            }, out ClientResult result);

            // assert
            setupManagerMock.VerifyExecution(fileName, download: true, install: false);
            Assert.AreEqual(Execution.CLOSED, result.Execution);
            Assert.AreEqual(Finished.SUCCESS, result.Finished);
        }

        [TestMethod]
        public void WhenSkipUpdateOutOfMaintenanceWindowRequested_ThenInstallationIsNotExecuted_AndResultIsDownloaded()
        {
            // arrange
            Client client = CreateClient();
            IntPtr artifact = new IntPtr(-1);
            const string fileName = "name.msi";
            StartClient(client);

            // act
            wrapperMock.DeploymentActionFunc(artifact, new DeploymentInfo
            {
                artifactFileName = fileName,
                isInMaintenanceWindow = false,
                updateType = "skip"
            }, out ClientResult result);

            // assert
            setupManagerMock.VerifyExecution(fileName, download: true, install: false);
            Assert.AreEqual(Execution.DOWNLOADED, result.Execution);
            Assert.AreEqual(Finished.NONE, result.Finished);
        }

        [DataTestMethod]
        [DataRow(PackageStatus.Failed)]
        [DataRow(PackageStatus.Rejected)]
        [DataRow(PackageStatus.Downloaded)]
        [DataRow(PackageStatus.WaitingForConfirmation)]
        [DataRow(PackageStatus.WaitingForConfirmationForced)]
        public void GivenNoPendingUserResponse_WhenAttemptUpdateRequested_ThenPackageIsMarkedForConfirmation_AndResultIsDownloaded(
            PackageStatus packageStatus)
        {
            // arrange
            Client client = CreateClient();
            IntPtr artifact = new IntPtr(-1);
            const string fileName = "name.msi";
            StartClient(client);
            setupManagerMock.PackageStatus = packageStatus;

            // act
            wrapperMock.DeploymentActionFunc(artifact, new DeploymentInfo
            {
                artifactFileName = fileName,
                isInMaintenanceWindow = true,
                updateType = "attempt"
            }, out ClientResult result);

            // assert
            setupManagerMock.VerifyExecution(fileName, download: true, install: false);
            setupManagerMock.Verify(m => m.MarkPackageWaitingForConfirmation(fileName, false), Times.AtLeastOnce);
            Assert.AreEqual(Execution.DOWNLOADED, result.Execution);
            Assert.AreEqual(Finished.NONE, result.Finished);
        }

        [TestMethod]
        public void GivenPackageHasRejectPendingStatus_WhenAttemptUpdateRequested_ThenInstallationIsNotExecuted_AndResultIsFailed()
        {
            // arrange
            Client client = CreateClient();
            IntPtr artifact = new IntPtr(-1);
            const string fileName = "name.msi";
            StartClient(client);
            setupManagerMock.PackageStatus = PackageStatus.RejectPending;

            // act
            wrapperMock.DeploymentActionFunc(artifact, new DeploymentInfo
            {
                artifactFileName = fileName,
                isInMaintenanceWindow = true,
                updateType = "attempt"
            }, out ClientResult result);

            // assert
            setupManagerMock.VerifyExecution(fileName, download: true, install: false);
            Assert.AreEqual(Execution.CLOSED, result.Execution);
            Assert.AreEqual(Finished.FAILURE, result.Finished);
        }

        [TestMethod]
        public void GivenPackageHasAcceptPendingStatus_WhenAttemptUpdateRequested_ThenInstallationIsExecuted_AndResultIsSuccess()
        {
            // arrange
            Client client = CreateClient();
            IntPtr artifact = new IntPtr(-1);
            const string fileName = "name.msi";
            StartClient(client);
            setupManagerMock.PackageStatus = PackageStatus.AcceptPending;

            // act
            wrapperMock.DeploymentActionFunc(artifact, new DeploymentInfo
            {
                artifactFileName = fileName,
                isInMaintenanceWindow = true,
                updateType = "attempt"
            }, out ClientResult result);

            // assert
            setupManagerMock.VerifyExecution(fileName, download: true, install: true);
            Assert.AreEqual(Execution.CLOSED, result.Execution);
            Assert.AreEqual(Finished.SUCCESS, result.Finished);
        }

        [TestMethod]
        public void GivenPackageIsInstalled_WhenForcedUpdateRequested_ThenInstallationIsExecuted_AndResultIsSuccess()
        {
            // arrange
            Client client = CreateClient();
            IntPtr artifact = new IntPtr(-1);
            const string fileName = "name.msi";
            StartClient(client);
            setupManagerMock.PackageStatus = PackageStatus.Installed;

            // act
            wrapperMock.DeploymentActionFunc(artifact, new DeploymentInfo
            {
                artifactFileName = fileName,
                isInMaintenanceWindow = true,
                updateType = "forced"
            }, out ClientResult result);

            // assert
            setupManagerMock.VerifyExecution(fileName, download: true, install: true);
            Assert.AreEqual(Execution.CLOSED, result.Execution);
            Assert.AreEqual(Finished.SUCCESS, result.Finished);
        }

        [DataTestMethod]
        [DataRow(PackageStatus.Failed)]
        [DataRow(PackageStatus.Rejected)]
        [DataRow(PackageStatus.Downloaded)]
        [DataRow(PackageStatus.WaitingForConfirmation)]
        [DataRow(PackageStatus.WaitingForConfirmationForced)]
        public void GivenRequiresConfirmationBeforeInstallIsSet_AndNoPendingUserResponse_WhenForcedUpdateRequested_ThenPackageIsMarkedForConfirmation_AndResultIsDownloaded(
            PackageStatus packageStatus)
        {
            // arrange
            Client client = CreateClient();
            IntPtr artifact = new IntPtr(-1);
            const string fileName = "name.msi";
            StartClient(client);
            settingsManagerMock.Object.RequiresConfirmationBeforeInstall = true;
            setupManagerMock.PackageStatus = packageStatus;

            // act
            wrapperMock.DeploymentActionFunc(artifact, new DeploymentInfo
            {
                artifactFileName = fileName,
                isInMaintenanceWindow = true,
                updateType = "forced"
            }, out ClientResult result);

            // assert
            setupManagerMock.VerifyExecution(fileName, download: true, install: false);
            setupManagerMock.Verify(m => m.MarkPackageWaitingForConfirmation(fileName, true), Times.AtLeastOnce);
            Assert.AreEqual(Execution.DOWNLOADED, result.Execution);
            Assert.AreEqual(Finished.NONE, result.Finished);
        }

        [TestMethod]
        public void GivenRequiresConfirmationBeforeInstallIsSet_AndPackageHasRejectPendingStatus_WhenForcedUpdateRequested_ThenInstallationIsNotExecuted_AndResultIsFailed()
        {
            // arrange
            Client client = CreateClient();
            IntPtr artifact = new IntPtr(-1);
            const string fileName = "name.msi";
            StartClient(client);
            settingsManagerMock.Object.RequiresConfirmationBeforeInstall = true;
            setupManagerMock.PackageStatus = PackageStatus.RejectPending;

            // act
            wrapperMock.DeploymentActionFunc(artifact, new DeploymentInfo
            {
                artifactFileName = fileName,
                isInMaintenanceWindow = true,
                updateType = "forced"
            }, out ClientResult result);

            // assert
            setupManagerMock.VerifyExecution(fileName, download: true, install: false);
            Assert.AreEqual(Execution.CLOSED, result.Execution);
            Assert.AreEqual(Finished.FAILURE, result.Finished);
        }

        [TestMethod]
        public void GivenRequiresConfirmationBeforeInstallIsSet_AndPackageHasAcceptPendingStatus_WhenForcedUpdateRequested_ThenInstallationIsExecuted_AndResultIsSuccess()
        {
            // arrange
            Client client = CreateClient();
            IntPtr artifact = new IntPtr(-1);
            const string fileName = "name.msi";
            StartClient(client);
            settingsManagerMock.Object.RequiresConfirmationBeforeInstall = true;
            setupManagerMock.PackageStatus = PackageStatus.AcceptPending;

            // act
            wrapperMock.DeploymentActionFunc(artifact, new DeploymentInfo
            {
                artifactFileName = fileName,
                isInMaintenanceWindow = true,
                updateType = "forced"
            }, out ClientResult result);

            // assert
            setupManagerMock.VerifyExecution(fileName, download: true, install: true);
            Assert.AreEqual(Execution.CLOSED, result.Execution);
            Assert.AreEqual(Finished.SUCCESS, result.Finished);
        }

        [DataTestMethod]
        [DataRow(PackageStatus.Failed)]
        [DataRow(PackageStatus.Rejected)]
        [DataRow(PackageStatus.AcceptPending)]
        [DataRow(PackageStatus.Downloaded)]
        [DataRow(PackageStatus.WaitingForConfirmation)]
        [DataRow(PackageStatus.WaitingForConfirmationForced)]
        public void WhenForcedUpdateRequested_ThenInstallationIsExecuted_AndResultIsSuccess(PackageStatus packageStatus)
        {
            // arrange
            Client client = CreateClient();
            IntPtr artifact = new IntPtr(-1);
            const string fileName = "name.msi";
            StartClient(client);
            setupManagerMock.PackageStatus = packageStatus;

            // act
            wrapperMock.DeploymentActionFunc(artifact, new DeploymentInfo
            {
                artifactFileName = fileName,
                isInMaintenanceWindow = true,
                updateType = "forced"
            }, out ClientResult result);

            // assert
            setupManagerMock.VerifyExecution(fileName, download: true, install: true);
            Assert.AreEqual(Execution.CLOSED, result.Execution);
            Assert.AreEqual(Finished.SUCCESS, result.Finished);
        }

        [TestMethod]
        public void WhenUnknownUpdateModeRequested_ResultIsFailed()
        {
            // arrange
            Client client = CreateClient();
            IntPtr artifact = new IntPtr(-1);
            const string fileName = "name.msi";
            StartClient(client);

            // act
            wrapperMock.DeploymentActionFunc(artifact, new DeploymentInfo
            {
                artifactFileName = fileName,
                updateType = "something_unknown"
            }, out ClientResult result);

            // assert
            Assert.AreEqual(Execution.CLOSED, result.Execution);
            Assert.AreEqual(Finished.FAILURE, result.Finished);
        }

        private void StartClient(Client client)
        {
            Task.Run(client.Run);
            Thread.Sleep(100);
        }

        private Client CreateClient()
        {
            wrapperMock = new WrapperMock();
            settingsManagerMock = new SettingsManagerMock();
            settingsManagerMock.Object.SecureAuthorizationMode = true;
            setupManagerMock = new SetupManagerMock();
            loggerMock = new LoggerMock();
            certificate = "certificate body";
            Client client = new Client(wrapperMock.Object, settingsManagerMock.Object, () => certificate, setupManagerMock.Object, () => sysInfo, loggerMock.Object, version);

            return client;
        }
    }
}
