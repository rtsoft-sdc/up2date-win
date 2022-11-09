namespace Up2dateConsole
{
    public interface ISettings
    {
        bool LeaveAdminModeOnClose { get; set; }
        bool LeaveAdminModeOnInactivity { get; set; }
        uint LeaveAdminModeOnInactivityTimeout { get; set; }
        void Save();
    }
}