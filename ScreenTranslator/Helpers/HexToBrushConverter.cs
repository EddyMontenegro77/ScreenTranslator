using System.Globalization;
using System.Windows.Data;

namespace ScreenTranslator.Helpers
{
    public class HexToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string hex)
            {
                try
                {
                    var color = (System.Windows.Media.Color)
                        System.Windows.Media.ColorConverter.ConvertFromString(hex);
                    return new System.Windows.Media.SolidColorBrush(color);
                }
                catch { return System.Windows.Media.Brushes.Transparent; }
            }
            return System.Windows.Media.Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}