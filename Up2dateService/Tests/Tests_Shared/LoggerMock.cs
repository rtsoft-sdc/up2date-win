using Moq;
using System;
using Up2dateShared;

namespace Tests_Shared
{
    public class LoggerMock : Mock<ILogger>
    {
        public LoggerMock()
        {
            Setup(o => o.WriteEntry(It.IsAny<Exception>()));
            Setup(o => o.WriteEntry(It.IsAny<string>(), It.IsAny<Exception>()));
        }
    }
}
