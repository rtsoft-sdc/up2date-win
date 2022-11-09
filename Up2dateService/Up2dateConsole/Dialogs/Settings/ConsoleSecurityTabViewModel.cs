using Up2dateConsole.Helpers;

namespace Up2dateConsole.Dialogs.Settings
{
    public class ConsoleSecurityTabViewModel : NotifyPropertyChanged
    {
        private bool leaveAdminModeOnClose;
        private bool leaveAdminModeOnInactivity;
        private uint leaveAdminModeOnInactivityTimeout;
        private readonly ISettings settings;

        public ConsoleSecurityTabViewModel(string header, ISettings settings)
        {
            Header = header;
            this.settings = settings ?? throw new System.ArgumentNullException(nameof(settings));
        }

        public string Header { get; }

        public bool Initialize()
        {
            leaveAdminModeOnClose = settings.LeaveAdminModeOnClose;
            leaveAdminModeOnInactivity = settings.LeaveAdminModeOnInactivity;
            leaveAdminModeOnInactivityTimeout = settings.LeaveAdminModeOnInactivityTimeout;

            return true;
        }

        public bool IsValid => LeaveAdminModeOnInactivityTimeout > 0;

        public bool Apply()
        {
            if (!IsValid) return false;

            settings.LeaveAdminModeOnClose = LeaveAdminModeOnClose;
            settings.LeaveAdminModeOnInactivity = LeaveAdminModeOnInactivity;
            settings.LeaveAdminModeOnInactivityTimeout = LeaveAdminModeOnInactivityTimeout;
            settings.Save();

            return true;
        }

        public bool LeaveAdminModeOnClose
        {
            get => leaveAdminModeOnClose;
            set
            {
                if (leaveAdminModeOnClose == value) return;
                leaveAdminModeOnClose = value;
                OnPropertyChanged();
            }
        }

        public bool LeaveAdminModeOnInactivity
        {
            get => leaveAdminModeOnInactivity;
            set
            {
                if (leaveAdminModeOnInactivity == value) return;
                leaveAdminModeOnInactivity = value;
                OnPropertyChanged();
            }
        }

        public uint LeaveAdminModeOnInactivityTimeout
        {
            get => leaveAdminModeOnInactivityTimeout;
            set
            {
                if (leaveAdminModeOnInactivityTimeout == value) return;
                leaveAdminModeOnInactivityTimeout = value;
                OnPropertyChanged();
            }
        }
    }
}
