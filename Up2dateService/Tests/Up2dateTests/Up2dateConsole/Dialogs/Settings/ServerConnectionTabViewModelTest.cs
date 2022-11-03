using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Tests_Shared;
using Up2dateConsole.Dialogs.Settings;

namespace Up2dateTests.Up2dateConsole.Dialogs.Settings
{
    [TestClass]
    public class ServerConnectionTabViewModelTest
    {
        const string Header = "Server Connection";

        private WcfClientFactoryMock wcfClientFactoryMock;

        [TestMethod]
        public void WhenInitialized_ViewModelIsCorrectlyInitialized()
        {
            // arrange
            var vm = CreateViewModel();
            var serviceMock = wcfClientFactoryMock.WcfServiceMock;
            serviceMock.ProvisioningUrl = "some url";
            serviceMock.RequestCertificateUrl = "some other url";

            // act
            vm.Initialize(serviceMock.Object);

            // assert
            Assert.AreEqual(Header, vm.Header);
            Assert.IsTrue(vm.IsValid);
            Assert.AreEqual(serviceMock.ProvisioningUrl, vm.DpsUrl);
            Assert.AreEqual(serviceMock.RequestCertificateUrl, vm.TokenUrl);
        }

        [TestMethod]
        public void GivenEmptyUrls_WhenInitialized_ItIsNotValid()
        {
            // arrange
            var vm = CreateViewModel();
            var serviceMock = wcfClientFactoryMock.WcfServiceMock;
            serviceMock.ProvisioningUrl = string.Empty;
            serviceMock.RequestCertificateUrl = string.Empty;

            // act
            vm.Initialize(serviceMock.Object);

            // assert
            Assert.IsFalse(vm.IsValid);
        }

        [TestMethod]
        public void WhenApplyCalled_ThenNewValuesAreAppliedToService()
        {
            // arrange
            var vm = CreateViewModel();
            var serviceMock = wcfClientFactoryMock.WcfServiceMock;
            vm.Initialize(serviceMock.Object);
            vm.DpsUrl = "DpsUrl";
            vm.TokenUrl = "TokenUrl";

            // act
            var success = vm.Apply(serviceMock.Object);

            // assert
            Assert.IsTrue(success);
            serviceMock.Verify(m => m.SetProvisioningUrl(vm.DpsUrl), Times.Once);
            serviceMock.Verify(m => m.SetRequestCertificateUrl(vm.TokenUrl), Times.Once);
        }

        private ServerConnectionTabViewModel CreateViewModel(WcfClientFactoryMock wcfClientFactoryMock = null)
        {
            this.wcfClientFactoryMock = wcfClientFactoryMock ?? new WcfClientFactoryMock();
            ServerConnectionTabViewModel vm = new ServerConnectionTabViewModel(Header);
            return vm;
        }
    }
}
