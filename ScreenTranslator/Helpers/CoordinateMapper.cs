using System.Drawing;
using System.Windows;
using System.Windows.Media;

namespace ScreenTranslator.Helpers
{
    public static class CoordinateMapper
    {
        // ocrRect in pixels of processedBitmap
        public static Rect MapOcrRectToDip(Rect ocrRect, Rectangle physicalCapture, System.Drawing.Size processedSize, System.Drawing.Size captureSize, Visual visual)
        {
            double scaleX = processedSize.Width / System.Math.Max(1.0, captureSize.Width);
            double scaleY = processedSize.Height / System.Math.Max(1.0, captureSize.Height);

            double physX = physicalCapture.X + (ocrRect.X / scaleX);
            double physY = physicalCapture.Y + (ocrRect.Y / scaleY);
            double physW = System.Math.Max(1.0, ocrRect.Width / scaleX);
            double physH = System.Math.Max(1.0, ocrRect.Height / scaleY);

            var physRect = new Rectangle((int)physX, (int)physY, (int)physW, (int)physH);
            return physRect.ToDeviceIndependentRect(visual);
        }
    }
}
