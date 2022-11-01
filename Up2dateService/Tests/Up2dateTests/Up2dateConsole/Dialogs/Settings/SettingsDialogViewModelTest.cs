using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests_Shared;
using Up2dateConsole.Dialogs.Settings;
using Up2dateConsole.ViewService;

namespace Up2dateTests.Up2dateConsole.Dialogs.Settings
{
    [TestClass]
    public class SettingsDialogViewModelTest
    {
        private ViewServiceMock viewServiceMock;
        private WcfClientFactoryMock wcfClientFactoryMock;

        [TestMethod]
        public void WhenCreated_ThenViewModelIsCorrectlyInitialized()
        {
            // arrange
            // act
            var vm = CreateViewModel();

            // assert
            Assert.IsNotNull(vm.OkCommand);
            Assert.IsNotNull(vm.Tabs);
            Assert.AreEqual(3, vm.Tabs.Count);
            Assert.IsTrue(vm.Tabs[0] is ServerConnectionTabViewModel);
            Assert.IsTrue(vm.Tabs[1] is InstallationPolicyTabViewModel);
            Assert.IsTrue(vm.Tabs[2] is ConsoleSecurityTabViewModel);
            Assert.IsTrue(vm.IsInitialized);
        }

        [TestMethod]
        public void GivenServiceIsNotRunning_WhenCreated_ThenViewModelIsCorrectlyInitializedWithOnlyOneTab()
        {
            // arrange
            // act
            var vm = CreateViewModel(isServiceAvailable: false);

            // assert
            Assert.IsNotNull(vm.OkCommand);
            Assert.IsNotNull(vm.Tabs);
            Assert.AreEqual(1, vm.Tabs.Count);
            Assert.IsTrue(vm.Tabs[0] is ConsoleSecurityTabViewModel);
            Assert.IsTrue(vm.IsInitialized);
        }

        [TestMethod]
        public void GivenServiceIsNotRunning_WhenCreated_ThenOkCommandIsDisabled()
        {
            // arrange
            // act
            var vm = CreateViewModel(isServiceAvailable: false);

            // assert
            Assert.IsTrue(vm.OkCommand.CanExecute(null));
        }

        [TestMethod]
        public void GivenServerUrlsAreEmpty_WhenCreated_ThenOkCommandIsDisabled()
        {
            // arrange
            // act
            var vm = CreateViewModel();

            // assert
            Assert.IsFalse(vm.OkCommand.CanExecute(null));
        }

        [TestMethod]
        public void GivenServerUrlsAreNotEmpty_WhenCreated_ThenOkCommandIsEnabled()
        {
            // arrange
            var wcfClientFactoryMock = new WcfClientFactoryMock();
            wcfClientFactoryMock.WcfServiceMock.ProvisioningUrl = "some URL";
            wcfClientFactoryMock.WcfServiceMock.RequestCertificateUrl = "some other URL";

            // act
            var vm = CreateViewModel(wcfClientFactoryMock: wcfClientFactoryMock);

            // assert
            Assert.IsTrue(vm.OkCommand.CanExecute(null));
        }

        [TestMethod]
        public void GivenServiceIsNotRunning_WhenPressedOk_ThenDialogClosedWithSuccess()
        {
            // arrange
            var vm = CreateViewModel(isServiceAvailable: false);
            bool success = false;
            vm.CloseDialog += (sender, args) => success = args;

            // act
            vm.OkCommand.Execute(null);

            // assert
            Assert.IsTrue(success);
        }

        private SettingsDialogViewModel CreateViewModel(bool isServiceAvailable = true, 
            WcfClientFactoryMock wcfClientFactoryMock = null, ViewServiceMock viewServiceMock=null)
        {
            this.viewServiceMock = viewServiceMock ?? new ViewServiceMock();
            this.wcfClientFactoryMock = wcfClientFactoryMock ?? new WcfClientFactoryMock();
            SettingsDialogViewModel vm = new SettingsDialogViewModel(this.viewServiceMock.Object, this.wcfClientFactoryMock.Object, isServiceAvailable);
            return vm;
        }
    }
}
