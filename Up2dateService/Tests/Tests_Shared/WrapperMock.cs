using Moq;
using System;
using Up2dateClient;

namespace Tests_Shared
{
    public class WrapperMock : Mock<IWrapper>
    {
        private IntPtr dispatcher;

        public IntPtr Dispatcher
        {
            get => dispatcher;
            set
            {
                dispatcher = value;
                Setup(m => m.CreateDispatcher(It.IsAny<ConfigRequestFunc>(), It.IsAny<DeploymentActionFunc>(), It.IsAny<CancelActionFunc>())).Returns(dispatcher);
            }
        }

        public WrapperMock()
        {
            Dispatcher = new IntPtr(-1);
            Setup(m => m.RunClient(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IntPtr>(), It.IsAny<AuthErrorActionFunc>()));
        }
    }
}
