using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DrawingColor = System.Drawing.Color;

namespace ScreenTranslator.Views
{
    public partial class TranslationOverlayWindow : Window
    {

        private readonly bool _isVertical;
        public TranslationOverlayWindow(
            string text,
            Rect placement,
            DrawingColor backgroundColor,
            DrawingColor fontColor,
            bool isVertical,
            double fontSize)
        {
            InitializeComponent();

            _isVertical = isVertical;

            TxtTranslation.Text = text ?? string.Empty;
            TxtTranslation.FontSize = fontSize;
            TxtTranslation.Foreground = new SolidColorBrush(
                Color.FromArgb(fontColor.A, fontColor.R, fontColor.G, fontColor.B));

            Left = placement.X;
            Top = placement.Y;

            if (_isVertical)
            {

                Width = Math.Max(300, placement.Width);
                Height = Math.Max(placement.Height, 80);

                TxtTranslation.MaxWidth = 300;
            }
            else
            {
                if (placement.Width > 0)
                {
                    Width = placement.Width;
                    TxtTranslation.MaxWidth = placement.Width;
                }

                if (placement.Height > 0)
                    Height = placement.Height;
            }

            RootBorder.Background = new SolidColorBrush(
                Color.FromArgb(backgroundColor.A, backgroundColor.R, backgroundColor.G, backgroundColor.B));
        }

        public void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}