using Moq;
using Up2dateShared;

namespace Tests_Shared
{
    public class SettingsManagerMock : Mock<ISettingsManager>
    {
        public SettingsManagerMock()
        {
            SetupProperty(o => o.CheckSignature);
            SetupProperty(o => o.SignatureVerificationLevel);
        }
    }
}
