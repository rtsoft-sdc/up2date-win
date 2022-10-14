using Moq;
using System;
using System.Threading;
using Up2dateClient;

namespace Tests_Shared
{
    public class WrapperMock : Mock<IWrapper>
    {
        private ManualResetEvent runExitEvent = new ManualResetEvent(false);
        private IntPtr dispatcher;

        public AuthErrorActionFunc AuthErrorCallback { get; private set; }
        public ConfigRequestFunc ConfigRequestFunc { get; private set; }
        public DeploymentActionFunc DeploymentActionFunc { get; private set; }
        public CancelActionFunc CancelActionFunc { get; private set; }

        public IntPtr Dispatcher
        {
            get => dispatcher;
            set
            {
                dispatcher = value;
                Setup(m => m.CreateDispatcher(It.IsAny<ConfigRequestFunc>(), It.IsAny<DeploymentActionFunc>(), It.IsAny<CancelActionFunc>())).Returns(dispatcher)
                    .Callback<ConfigRequestFunc, DeploymentActionFunc, CancelActionFunc>((cr, da, ca) =>
                    {
                        ConfigRequestFunc = cr;
                        DeploymentActionFunc = da;
                        CancelActionFunc = ca;
                    });
            }
        }

        public WrapperMock()
        {
            Dispatcher = new IntPtr(-1);
            Setup(m => m.RunClient(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IntPtr>(), It.IsAny<AuthErrorActionFunc>()))
                .Callback<string, string, string, IntPtr, AuthErrorActionFunc>((p1, p2, p3, p4, ae) => 
                {
                    AuthErrorCallback = ae;
                    runExitEvent.WaitOne(); 
                });
        }

        public void ExitRun()
        {
            runExitEvent.Set();
        }
    }
}
