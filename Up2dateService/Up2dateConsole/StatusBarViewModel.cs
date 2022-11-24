using System;
using System.Windows.Input;
using Up2dateConsole.Helpers;
using Up2dateConsole.Session;
using Up2dateConsole.StateIndicator;

namespace Up2dateConsole
{
    public class StatusBarViewModel : NotifyPropertyChanged
    {
        private readonly ISession session;

        private string deviceId;
        private ServiceState serviceState;

        public StatusBarViewModel(ISession session, ICommand enterAdminModeCommand)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            EnterAdminModeCommand = enterAdminModeCommand ?? throw new ArgumentNullException(nameof(enterAdminModeCommand));
            StateIndicator = new StateIndicatorViewModel();
        }

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
        }

        public void SetInfo(string info)
        {
            StateIndicator.SetInfo(info);
        }

        public string DeviceId
        {
            get => deviceId;
            set
            {
                if (deviceId == value) return;
                deviceId = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsDeviceIdAvailable));
            }
        }

        public bool IsDeviceIdAvailable => !string.IsNullOrEmpty(DeviceId) && serviceState == ServiceState.Active;
    }
}
