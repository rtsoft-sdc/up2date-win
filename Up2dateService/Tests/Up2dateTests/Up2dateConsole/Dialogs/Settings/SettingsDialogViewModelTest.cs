using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests_Shared;
using Up2dateConsole.Dialogs.Settings;

namespace Up2dateTests.Up2dateConsole.Dialogs.Settings
{
    [TestClass]
    public class SettingsDialogViewModelTest
    {
        private ViewServiceMock viewServiceMock;
        private WcfClientFactoryMock wcfClientFactoryMock;
        private SettingsMock settingsMock;

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
        public void GivenServerUrlsAreNotEmpty_AndInactivityTimeoutIsNotZero_WhenCreated_ThenOkCommandIsEnabled()
        {
            // arrange
            var wcfClientFactoryMock = new WcfClientFactoryMock();
            wcfClientFactoryMock.WcfServiceMock.ProvisioningUrl = "some URL";
            wcfClientFactoryMock.WcfServiceMock.RequestCertificateUrl = "some other URL";
            var settingsMock = new SettingsMock();
            settingsMock.Object.LeaveAdminModeOnInactivityTimeout = 60;

            // act
            var vm = CreateViewModel(wcfClientFactoryMock: wcfClientFactoryMock, settingsMock: settingsMock);

            // assert
            Assert.IsTrue(vm.OkCommand.CanExecute(null));
        }

        [TestMethod]
        public void GivenServerUrlsAreEmpty_WhenCreated_ThenOkCommandIsDisabled()
        {
            // arrange
            var wcfClientFactoryMock = new WcfClientFactoryMock();
            wcfClientFactoryMock.WcfServiceMock.ProvisioningUrl = "";
            wcfClientFactoryMock.WcfServiceMock.RequestCertificateUrl = "";
            var settingsMock = new SettingsMock();
            settingsMock.Object.LeaveAdminModeOnInactivityTimeout = 60;

            // act
            var vm = CreateViewModel(wcfClientFactoryMock: wcfClientFactoryMock, settingsMock: settingsMock);

            // assert
            Assert.IsFalse(vm.OkCommand.CanExecute(null));
        }

        [TestMethod]
        public void GivenInactivityTimeoutIsZero_WhenCreated_ThenOkCommandIsDisabled()
        {
            // arrange
            var wcfClientFactoryMock = new WcfClientFactoryMock();
            wcfClientFactoryMock.WcfServiceMock.ProvisioningUrl = "some URL";
            wcfClientFactoryMock.WcfServiceMock.RequestCertificateUrl = "some other URL";
            var settingsMock = new SettingsMock();
            settingsMock.Object.LeaveAdminModeOnInactivityTimeout = 0;

            // act
            var vm = CreateViewModel(wcfClientFactoryMock: wcfClientFactoryMock, settingsMock: settingsMock);

            // assert
            Assert.IsFalse(vm.OkCommand.CanExecute(null));
        }

        [TestMethod]
        public void GivenServiceIsNotRunning_WhenPressedOk_ThenDialogClosedWithSuccess()
        {
            // arrange
            var settingsMock = new SettingsMock();
            settingsMock.Object.LeaveAdminModeOnInactivityTimeout = 60;
            var vm = CreateViewModel(isServiceAvailable: false, settingsMock: settingsMock);
            bool success = false;
            vm.CloseDialog += (sender, args) => success = args;

            // act
            vm.OkCommand.Execute(null);

            // assert
            Assert.IsTrue(success);
        }

        private SettingsDialogViewModel CreateViewModel(bool isServiceAvailable = true, 
            WcfClientFactoryMock wcfClientFactoryMock = null, ViewServiceMock viewServiceMock = null, SettingsMock settingsMock = null)
        {
            this.viewServiceMock = viewServiceMock ?? new ViewServiceMock();
            this.wcfClientFactoryMock = wcfClientFactoryMock ?? new WcfClientFactoryMock();
            this.settingsMock = settingsMock ?? new SettingsMock();
            SettingsDialogViewModel vm = new SettingsDialogViewModel(
                this.viewServiceMock.Object,
                this.wcfClientFactoryMock.Object,
                this.settingsMock.Object, isServiceAvailable);
            return vm;
        }
    }
}
