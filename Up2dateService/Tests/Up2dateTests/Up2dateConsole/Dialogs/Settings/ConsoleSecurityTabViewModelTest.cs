using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests_Shared;
using Up2dateConsole.Dialogs.Settings;

namespace Up2dateTests.Up2dateConsole.Dialogs.Settings
{
    [TestClass]
    public class ConsoleSecurityTabViewModelTest
    {
        const string Header = "Console Security";

        private SettingsMock settingsMock;

        [TestMethod]
        public void WhenInitialized_ViewModelIsCorrectlyInitialized()
        {
            // arrange
            var settingsMock = new SettingsMock();
            settingsMock.Object.LeaveAdminModeOnInactivityTimeout = 60;
            var vm = CreateViewModel(settingsMock: settingsMock);

            // act
            vm.Initialize();

            // assert
            Assert.AreEqual(Header, vm.Header);
            Assert.IsTrue(vm.IsValid);
            Assert.AreEqual(settingsMock.Object.LeaveAdminModeOnClose, vm.LeaveAdminModeOnClose);
            Assert.AreEqual(settingsMock.Object.LeaveAdminModeOnInactivity, vm.LeaveAdminModeOnInactivity);
            Assert.AreEqual(settingsMock.Object.LeaveAdminModeOnInactivityTimeout, vm.LeaveAdminModeOnInactivityTimeout);
        }

        [TestMethod]
        public void WhenApplyCalled_ThenNewValuesAreAppliedToSettings()
        {
            // arrange
            var settingsMock = new SettingsMock();
            settingsMock.Object.LeaveAdminModeOnInactivityTimeout = 60;
            var vm = CreateViewModel(settingsMock: settingsMock);
            vm.Initialize();
            vm.LeaveAdminModeOnClose = true;
            vm.LeaveAdminModeOnInactivity = true;
            vm.LeaveAdminModeOnInactivityTimeout = 10;

            // act
            var success = vm.Apply();

            // assert
            Assert.IsTrue(success);
            Assert.AreEqual(vm.LeaveAdminModeOnClose, settingsMock.Object.LeaveAdminModeOnClose);
            Assert.AreEqual(vm.LeaveAdminModeOnInactivity, settingsMock.Object.LeaveAdminModeOnInactivity);
            Assert.AreEqual(vm.LeaveAdminModeOnInactivityTimeout, settingsMock.Object.LeaveAdminModeOnInactivityTimeout);
        }

        private ConsoleSecurityTabViewModel CreateViewModel(SettingsMock settingsMock = null)
        {
            this.settingsMock = settingsMock ?? new SettingsMock();
            ConsoleSecurityTabViewModel vm = new ConsoleSecurityTabViewModel(Header, this.settingsMock.Object);
            return vm;
        }
    }
}
