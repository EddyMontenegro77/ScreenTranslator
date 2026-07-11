using System.Drawing;
using System.Windows;

namespace ScreenTranslator.Helpers
{
    public static class BitmapCropHelper
    {
        public static Rectangle ExpandAndClamp(Rect boundingBox, int maxWidth, int maxHeight, double marginFactor = 0.06)
        {
            double expandX = boundingBox.Width * marginFactor;
            double expandY = boundingBox.Height * marginFactor;

            int x = (int)Math.Max(0, boundingBox.X - expandX);
            int y = (int)Math.Max(0, boundingBox.Y - expandY);
            int width = (int)Math.Min(maxWidth - x, boundingBox.Width + 2 * expandX);
            int height = (int)Math.Min(maxHeight - y, boundingBox.Height + 2 * expandY);

            return new Rectangle(x, y, Math.Max(1, width), Math.Max(1, height));
        }

        public static Bitmap Crop(Bitmap source, Rectangle rect)
        {
            var clamped = Rectangle.Intersect(new Rectangle(0, 0, source.Width, source.Height), rect);
            if (clamped.Width <= 0 || clamped.Height <= 0)
                return new Bitmap(1, 1);

            var result = new Bitmap(clamped.Width, clamped.Height);
            using var g = Graphics.FromImage(result);
            g.DrawImage(source, new Rectangle(0, 0, result.Width, result.Height), clamped, GraphicsUnit.Pixel);
            return result;
        }
    }
}