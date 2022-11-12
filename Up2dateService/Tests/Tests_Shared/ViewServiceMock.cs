using Moq;
using Up2dateConsole;
using Up2dateConsole.ViewService;

namespace Tests_Shared
{
    public class ViewServiceMock : Mock<IViewService>
    {
        public ViewServiceMock()
        {
            Setup(m => m.GetText(It.IsAny<Texts>())).Returns<Texts>(t => t.ToString());
        }
    }
}
