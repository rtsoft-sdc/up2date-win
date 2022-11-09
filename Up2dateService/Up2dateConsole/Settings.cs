namespace Up2dateConsole
{
    public class Settings : ISettings
    {
        public Settings()
        {
            if (Properties.Settings.Default.UpgradeFlag)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpgradeFlag = false;
                Properties.Settings.Default.Save();
            }
        }

        public bool LeaveAdminModeOnClose
        {
            get => Properties.Settings.Default.LeaveAdminModeOnClose;
            set => Properties.Settings.Default.LeaveAdminModeOnClose = value;
        }

        public bool LeaveAdminModeOnInactivity
        {
            get => Properties.Settings.Default.LeaveAdminModeOnInactivity;
            set => Properties.Settings.Default.LeaveAdminModeOnInactivity = value;
        }

        public uint LeaveAdminModeOnInactivityTimeout
        {
            get => Properties.Settings.Default.LeaveAdminModeOnInactivityTimeout;
            set => Properties.Settings.Default.LeaveAdminModeOnInactivityTimeout = value;
        }

        public void Save()
        {
            Properties.Settings.Default.Save();
        }
    }
}
