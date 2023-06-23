using System.Windows.Media.Imaging;

namespace Up2dateConsole.Helpers
{
    public interface IQrCodeHelper
    {
        BitmapSource CreateQrCode(string uri);
    }
}
