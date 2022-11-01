using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Up2dateConsole.Helpers;

namespace Up2dateConsole.Dialogs.Settings
{
    public class ConsoleSecurityTabViewModel : NotifyPropertyChanged
    {
        private bool leaveAdminModeOnClose;
        private bool leaveAdminModeOnInactivity;
        private uint leaveAdminModeOnInactivityTimeout;

        public ConsoleSecurityTabViewModel(string header)
        {
            Header = header;
        }

        public string Header { get; }

        public bool Initialize()
        {
            leaveAdminModeOnClose = Properties.Settings.Default.LeaveAdminModeOnClose;
            leaveAdminModeOnInactivity = Properties.Settings.Default.LeaveAdminModeOnInactivity;
            leaveAdminModeOnInactivityTimeout = Properties.Settings.Default.LeaveAdminModeOnInactivityTimeout;

            return true;
        }

        public bool IsValid => true;

        public bool Apply()
        {
            Properties.Settings.Default.LeaveAdminModeOnClose = LeaveAdminModeOnClose;
            Properties.Settings.Default.LeaveAdminModeOnInactivity = LeaveAdminModeOnInactivity;
            Properties.Settings.Default.LeaveAdminModeOnInactivityTimeout = LeaveAdminModeOnInactivityTimeout;
            Properties.Settings.Default.Save();

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
