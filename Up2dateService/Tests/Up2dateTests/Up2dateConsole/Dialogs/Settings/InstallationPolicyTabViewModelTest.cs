using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Tests_Shared;
using Up2dateConsole.Dialogs.Settings;
using Up2dateConsole.ServiceReference;

namespace Up2dateTests.Up2dateConsole.Dialogs.Settings
{
    [TestClass]
    public class InstallationPolicyTabViewModelTest
    {
        const string Header = "Installation Policy";

        private ViewServiceMock viewServiceMock;
        private WcfClientFactoryMock wcfClientFactoryMock;

        [TestMethod]
        public void WhenInitialized_ViewModelIsCorrectlyInitialized()
        {
            // arrange
            var vm = CreateViewModel();
            var wcfService = wcfClientFactoryMock.WcfServiceMock.Object;

            // act
            vm.Initialize(wcfService);

            // assert
            Assert.AreEqual(Header, vm.Header);
            Assert.IsTrue(vm.IsValid);
            Assert.AreEqual(wcfService.GetConfirmBeforeInstallation(), vm.ConfirmBeforeInstallation);
            Assert.AreEqual(wcfService.GetCheckSignature(), vm.CheckSignatureStatus);
            Assert.AreEqual(wcfService.GetSignatureVerificationLevel(), vm.SignatureVerificationLevel);
            Assert.IsNotNull(vm.AddCertificateCommand);
            Assert.IsNotNull(vm.LaunchCertMgrShapinCommand);
            Assert.AreEqual(wcfService.GetSignatureVerificationLevel() == SignatureVerificationLevel.SignedByWhitelistedCertificate, vm.AddCertificateCommand.CanExecute(null));
        }

        [TestMethod]
        public void WhenSignatureVerificationLevelSetToWhiteList_ThenCommandsAreEnabled()
        {
            // arrange
            var vm = CreateViewModel();
            var wcfService = wcfClientFactoryMock.WcfServiceMock.Object;
            vm.Initialize(wcfService);

            // act
            vm.CheckSignatureStatus = true;
            vm.SignatureVerificationLevel = SignatureVerificationLevel.SignedByWhitelistedCertificate;

            // assert
            Assert.IsTrue(vm.IsValid);
            Assert.IsTrue(vm.AddCertificateCommand.CanExecute(null));
            Assert.IsTrue(vm.LaunchCertMgrShapinCommand.CanExecute(null));
        }

        [TestMethod]
        public void GivenSignatureVerificationLevelSetToWhiteList_WhenAddCertificateCommandInvoked_ThenOpenCertificateDialogIsShown()
        {
            // arrange
            var vm = CreateViewModel();
            var wcfService = wcfClientFactoryMock.WcfServiceMock.Object;
            vm.Initialize(wcfService);
            vm.CheckSignatureStatus = true;
            vm.SignatureVerificationLevel = SignatureVerificationLevel.SignedByWhitelistedCertificate;

            // act
            vm.AddCertificateCommand.Execute(null);

            // assert
            viewServiceMock.Verify(m => m.ShowOpenDialog(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [TestMethod]
        public void WhenUserSuppliedValidCertificateFile_ThenItIsAddedToWhiteList()
        {
            // arrange
            const string CertFileName = "certFileName";
            var vm = CreateViewModel();
            var wcfService = wcfClientFactoryMock.WcfServiceMock.Object;
            vm.Initialize(wcfService);
            vm.CheckSignatureStatus = true;
            vm.SignatureVerificationLevel = SignatureVerificationLevel.SignedByWhitelistedCertificate;
            viewServiceMock.Setup(m => m.ShowOpenDialog(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(CertFileName);
            wcfClientFactoryMock.WcfServiceMock.Setup(m => m.IsCertificateValidAndTrusted(It.IsAny<string>())).Returns(true);

            // act
            vm.AddCertificateCommand.Execute(null);

            // assert
            wcfClientFactoryMock.WcfServiceMock.Verify(m => m.AddCertificateToWhitelist(CertFileName), Times.Once);
        }

        [TestMethod]
        public void WhenApplyCalled_ThenNewValuesAreAppliedToService()
        {
            // arrange
            var vm = CreateViewModel();
            var serviceMock = wcfClientFactoryMock.WcfServiceMock;
            vm.Initialize(serviceMock.Object);
            vm.ConfirmBeforeInstallation = true;
            vm.CheckSignatureStatus = true;
            vm.SignatureVerificationLevel = SignatureVerificationLevel.SignedByTrustedCertificate;

            // act
            var success = vm.Apply(serviceMock.Object);

            // assert
            Assert.IsTrue(success);
            serviceMock.Verify(m => m.SetConfirmBeforeInstallation(vm.ConfirmBeforeInstallation), Times.Once);
            serviceMock.Verify(m => m.SetCheckSignature(vm.CheckSignatureStatus), Times.Once);
            serviceMock.Verify(m => m.SetSignatureVerificationLevel(vm.SignatureVerificationLevel), Times.Once);
        }

        private InstallationPolicyTabViewModel CreateViewModel(WcfClientFactoryMock wcfClientFactoryMock = null, ViewServiceMock viewServiceMock = null)
        {
            this.viewServiceMock = viewServiceMock ?? new ViewServiceMock();
            this.wcfClientFactoryMock = wcfClientFactoryMock ?? new WcfClientFactoryMock();
            InstallationPolicyTabViewModel vm = new InstallationPolicyTabViewModel(Header,
                this.viewServiceMock.Object,
                this.wcfClientFactoryMock.Object);
            return vm;
        }
    }
}
