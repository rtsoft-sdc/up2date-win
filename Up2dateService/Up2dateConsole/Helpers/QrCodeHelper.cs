using QRCoder;
using System;
using System.Drawing;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Up2dateConsole.Helpers
{
    public class QrCodeHelper : IQrCodeHelper
    {
        public BitmapSource CreateQrCode(string uri)
        {
            var pl = new PayloadGenerator.Url(uri);
            using (var qrGenerator = new QRCodeGenerator())
            {
                using (QRCodeData qrCodeInfo = qrGenerator.CreateQrCode(pl, QRCodeGenerator.ECCLevel.Q))
                {
                    using (var qrCode = new QRCode(qrCodeInfo))
                    {
                        using (Bitmap qrBitmap = qrCode.GetGraphic(60))
                        {
                            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                            qrBitmap.GetHbitmap(),
                                            IntPtr.Zero,
                                            Int32Rect.Empty,
                                            BitmapSizeOptions.FromEmptyOptions());
                        }
                    }
                }
            }
        }
    }
}
