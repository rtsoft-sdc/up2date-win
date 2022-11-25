using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests_Shared;
using Up2dateConsole.StateIndicator;
using Up2dateConsole.StatusBar;

namespace Up2dateTests.Up2dateConsole.StatusBar
{
    [TestClass]
    public class StatusBarViewModelTest
    {
        private SessionMock sessionMock;
        private CommandMock commandMock;
        private ProcessHelperMock processHelperMock;

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void WhenInitialized_ViewModelIsCorrectlyInitialized(bool isAdminMode)
        {
            // arrange
            var sessionMock = new SessionMock();
            sessionMock.IsAdminMode = isAdminMode;

            // act
            var vm = CreateViewModel(sessionMock: sessionMock);

            // assert
            Assert.AreEqual(isAdminMode, vm.IsAdminMode);
            Assert.AreEqual(!isAdminMode, vm.IsUserMode);
            Assert.AreEqual(commandMock.Object, vm.EnterAdminModeCommand);
            Assert.IsNotNull(vm.OpenHawkbitUrlCommand);
            Assert.IsInstanceOfType(vm.StateIndicator, typeof(StateIndicatorViewModel));
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void WhenSetBusy_ThenStateIndicatorIndicatesBusy(bool busy)
        {
            // arrange
            var vm = CreateViewModel();

            // act
            vm.SetBusy(busy);

            // assert
            Assert.AreEqual(busy, vm.StateIndicator.IsBusy);
        }

        [DataTestMethod]
        [DataRow("")]
        [DataRow("any info")]
        public void WhenSetInfo_ThenStateIndicatorIndicatesInfo(string info)
        {
            // arrange
            var vm = CreateViewModel();

            // act
            vm.SetInfo(info);

            // assert
            Assert.AreEqual(info, vm.StateIndicator.Info);
        }

        [TestMethod]
        public void WhenSetConnectionInfo_ThenDeviceIdTenantAndHawkbitUrlAreSet()
        {
            // arrange
            const string DeviceID = "device-id";
            const string Tenant = "tenant";
            const string HawkbitEndpoint = "https://hawkbit.domain.org";
            const string DdiPath = "/ddi/sub.path?parameter=optional";
            var vm = CreateViewModel();

            // act
            vm.SetConnectionInfo(DeviceID, Tenant, HawkbitEndpoint + DdiPath);

            // assert
            Assert.AreEqual(DeviceID, vm.DeviceId);
            Assert.AreEqual(Tenant, vm.Tenant);
            Assert.AreEqual(HawkbitEndpoint, vm.HawkbitEndpoint);
        }

        [DataTestMethod]
        [DataRow(ServiceState.Active)]
        [DataRow(ServiceState.Unknown)]
        [DataRow(ServiceState.Error)]
        public void GivenConnectionInfoIsSet_WhenStateIsChanged_ThenAvailabilityOfConnectionInidicatorsIsChanged(ServiceState state)
        {
            // arrange
            const string DeviceID = "device-id";
            const string Tenant = "tenant";
            const string HawkbitEndpoint = "https://hawkbit.domain.org";
            const string DdiPath = "/ddi/sub.path?parameter=optional";
            var vm = CreateViewModel();
            vm.SetConnectionInfo(DeviceID, Tenant, HawkbitEndpoint + DdiPath);

            // act
            vm.SetState(state);

            // assert
            Assert.AreEqual(state == ServiceState.Active, vm.IsDeviceIdAvailable);
            Assert.AreEqual(state == ServiceState.Active, vm.IsTenantAvailable);
            Assert.AreEqual(state == ServiceState.Active, vm.IsHawkbitEndpointAvailable);
            if (state != ServiceState.Active)
            {
                Assert.IsFalse(vm.IsUnprotectedMode);
            }
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("tenant")]
        public void GivenStateIsActive_WhenTenantIsNotSet_ThenUnprotectedModeIsIndicated(string tenant)
        {
            // arrange
            const string DeviceID = "device-id";
            const string HawkbitEndpoint = "https://hawkbit.domain.org";
            const string DdiPath = "/ddi/sub.path?parameter=optional";
            var vm = CreateViewModel();
            vm.SetState(ServiceState.Active);

            // act
            vm.SetConnectionInfo(DeviceID, tenant, HawkbitEndpoint + DdiPath);

            // assert
            Assert.AreEqual(!string.IsNullOrEmpty(tenant), vm.IsTenantAvailable);
            Assert.AreEqual(string.IsNullOrEmpty(tenant), vm.IsUnprotectedMode);
        }

        private StatusBarViewModel CreateViewModel(SessionMock sessionMock = null, CommandMock commandMock = null, ProcessHelperMock processHelperMock = null)
        {
            this.sessionMock = sessionMock ?? new SessionMock();
            this.commandMock = commandMock ?? new CommandMock();
            this.processHelperMock = processHelperMock ?? new ProcessHelperMock();

            StatusBarViewModel vm = new StatusBarViewModel(this.sessionMock.Object, this.commandMock.Object, this.processHelperMock.Object);
            return vm;
        }
    }
}
