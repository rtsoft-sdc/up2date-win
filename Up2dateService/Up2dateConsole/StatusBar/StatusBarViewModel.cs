using System;
using System.Diagnostics;
using System.Windows.Input;
using Up2dateConsole.Helpers;
using Up2dateConsole.Session;
using Up2dateConsole.StateIndicator;

namespace Up2dateConsole.StatusBar
{
    public class StatusBarViewModel : NotifyPropertyChanged
    {
        private readonly ISession session;
        private readonly IProcessHelper processHelper;
        private string deviceId;
        private string tenant;
        private string hawkbitEndpoint;
        private ServiceState serviceState;

        public StatusBarViewModel(ISession session, ICommand enterAdminModeCommand, IProcessHelper processHelper)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            EnterAdminModeCommand = enterAdminModeCommand ?? throw new ArgumentNullException(nameof(enterAdminModeCommand));
            this.processHelper = processHelper ?? throw new ArgumentNullException(nameof(processHelper));

            OpenHawkbitUrlCommand = new RelayCommand(OpenHawkbitUrl);
            StateIndicator = new StateIndicatorViewModel();
        }

        public ICommand OpenHawkbitUrlCommand { get; }

        public StateIndicatorViewModel StateIndicator { get; }

        public bool IsAdminMode => session.IsAdminMode;

        public bool IsUserMode => !session.IsAdminMode;

        public ICommand EnterAdminModeCommand { get; }

        public void SetBusy(bool busy)
        {
            StateIndicator.IsBusy = busy;
        }

        public void SetState(ServiceState state)
        {
            serviceState = state;
            StateIndicator.SetState(state);
            OnPropertyChanged(nameof(IsDeviceIdAvailable));
            OnPropertyChanged(nameof(IsTenantAvailable));
            OnPropertyChanged(nameof(IsHawkbitEndpointAvailable));
            OnPropertyChanged(nameof(IsUnprotectedMode));
        }

        public void SetInfo(string info)
        {
            StateIndicator.SetInfo(info);
        }

        public void SetConnectionInfo(string deviceId, string tenant, string hawkbitEndpoint)
        {
            DeviceId = deviceId;
            Tenant = tenant;
            HawkbitEndpoint = Uri.TryCreate(hawkbitEndpoint, UriKind.Absolute, out Uri uri)
                ? hawkbitEndpoint.Replace(uri.PathAndQuery, String.Empty)
                : String.Empty;
        }

        public string DeviceId
        {
            get => deviceId;
            private set
            {
                if (deviceId == value) return;
                deviceId = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsDeviceIdAvailable));
            }
        }

        public string Tenant
        {
            get => tenant;
            private set
            {
                if (tenant == value) return;
                tenant = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsTenantAvailable));
                OnPropertyChanged(nameof(IsUnprotectedMode));
            }
        }

        public string HawkbitEndpoint
        {
            get => hawkbitEndpoint;
            private set
            {
                if (hawkbitEndpoint == value) return;
                hawkbitEndpoint = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsHawkbitEndpointAvailable));
            }
        }

        public bool IsHawkbitEndpointAvailable => !string.IsNullOrEmpty(HawkbitEndpoint) && serviceState == ServiceState.Active;

        public bool IsTenantAvailable => !string.IsNullOrEmpty(Tenant) && serviceState == ServiceState.Active;

        public bool IsDeviceIdAvailable => !string.IsNullOrEmpty(DeviceId) && serviceState == ServiceState.Active;

        public bool IsUnprotectedMode => string.IsNullOrEmpty(Tenant) && serviceState == ServiceState.Active;

        private void OpenHawkbitUrl(object _)
        {
            if (!IsHawkbitEndpointAvailable) return;

            var sInfo = new ProcessStartInfo(HawkbitEndpoint)
            {
                UseShellExecute = true
            };
            processHelper.StartProcess(sInfo);
        }
    }
}
