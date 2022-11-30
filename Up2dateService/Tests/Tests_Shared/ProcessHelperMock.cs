using Moq;
using System.Diagnostics;
using Up2dateConsole.Helpers;

namespace Tests_Shared
{
    public class ProcessHelperMock : Mock<IProcessHelper>
    {
        public ProcessHelperMock()
        {
            Setup(m => m.StartProcess(It.IsAny<ProcessStartInfo>())).Returns<ProcessStartInfo>(psi => new Process() { StartInfo = psi });
        }
    }
}
