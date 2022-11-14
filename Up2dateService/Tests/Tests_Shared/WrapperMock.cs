using Moq;
using System;
using System.Threading;
using Up2dateDotNet;

namespace Tests_Shared
{
    public class WrapperMock : Mock<IWrapper>
    {
        private ManualResetEvent runExitEvent = new ManualResetEvent(false);

        public AuthErrorActionFunc AuthErrorCallback { get; private set; }
        public ConfigRequestFunc ConfigRequestFunc { get; private set; }
        public DeploymentActionFunc DeploymentActionFunc { get; private set; }
        public CancelActionFunc CancelActionFunc { get; private set; }

        public WrapperMock()
        {
            Setup(m => m.RunClient(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AuthErrorActionFunc>(),
                It.IsAny<ConfigRequestFunc>(), It.IsAny<DeploymentActionFunc>(), It.IsAny<CancelActionFunc>()))
                .Callback<string, string, string, AuthErrorActionFunc, ConfigRequestFunc, DeploymentActionFunc, CancelActionFunc>((c, e, t, ae, cr, da, ca) =>
                {
                    AuthErrorCallback = ae;
                    ConfigRequestFunc = cr;
                    DeploymentActionFunc = da;
                    CancelActionFunc = ca;
                    runExitEvent.WaitOne();
                });
        }

        public void ExitRun()
        {
            runExitEvent.Set();
        }
    }
}
