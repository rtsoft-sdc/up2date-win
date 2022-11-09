using Moq;
using Up2dateConsole;

namespace Tests_Shared
{
    public class SettingsMock : Mock<ISettings>
    {
        public SettingsMock()
        {
            SetupProperty(m => m.LeaveAdminModeOnInactivityTimeout);
            SetupProperty(m => m.LeaveAdminModeOnInactivity);
            SetupProperty(m => m.LeaveAdminModeOnClose);
        }
    }
}
