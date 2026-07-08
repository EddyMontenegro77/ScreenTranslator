using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace ScreenTranslator.Services
{
    public class CopyToClipboardService
    {
        public static void CopyTextToClipboard(string text)
        {
            Clipboard.SetText(text);
        }

        public static void CopyImageToClipboard(Bitmap bitmap)
        {
            BitmapSource bitmapSource = ConvertToBitmapSource(bitmap);
            Clipboard.SetImage(bitmapSource);
        }

        private static BitmapSource ConvertToBitmapSource(Bitmap bitmap)
        {
            IntPtr hBitmap = bitmap.GetHbitmap();

            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            finally
            {
                // Release the native GDI handle to avoid memory leaks
                DeleteObject(hBitmap);
            }
        }

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);
    }
}