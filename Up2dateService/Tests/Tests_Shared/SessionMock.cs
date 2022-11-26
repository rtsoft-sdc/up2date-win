using Moq;
using Up2dateConsole.Session;

namespace Tests_Shared
{
    public class SessionMock : Mock<ISession>
    {
        private bool isAdminMode;
        public bool IsAdminMode
        {
            get => isAdminMode;
            set
            {
                isAdminMode = value;
                SetupGet(m => m.IsAdminMode).Returns(value);
            }
        }
    }
}
