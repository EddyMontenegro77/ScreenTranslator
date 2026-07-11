using System.Windows.Media;

namespace ScreenTranslator.Helpers
{
    public static class ColorHexHelper
    {
        public static string ToHex(System.Drawing.Color color)
        {
            return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        public static System.Drawing.Color FromHex(string hex)
        {
            try
            {
                var mediaColor = (Color)ColorConverter.ConvertFromString(hex);
                return System.Drawing.Color.FromArgb(mediaColor.A, mediaColor.R, mediaColor.G, mediaColor.B);
            }
            catch
            {
                return System.Drawing.Color.Black; // fallback if hex is corrupted or invalid
            }
        }

        public static Brush ToBrush(string hex)
        {
            try
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            }
            catch
            {
                return Brushes.Black;
            }
        }
    }
}