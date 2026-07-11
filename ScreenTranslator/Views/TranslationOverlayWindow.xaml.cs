using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DrawingColor = System.Drawing.Color;

namespace ScreenTranslator.Views
{
    public partial class TranslationOverlayWindow : Window
    {
        public TranslationOverlayWindow(
            string text,
            Rect placement,
            DrawingColor backgroundColor,
            DrawingColor fontColor,
            double fontSize)
        {
            InitializeComponent();

            TxtTranslation.Text = text ?? string.Empty;
            TxtTranslation.FontSize = fontSize;
            TxtTranslation.Foreground = new SolidColorBrush(
                Color.FromArgb(fontColor.A, fontColor.R, fontColor.G, fontColor.B));

            Left = placement.X;
            Top = placement.Y;

            if (placement.Width > 0)
            {
                Width = placement.Width;
                TxtTranslation.MaxWidth = placement.Width;
            }

            if (placement.Height > 0)
                Height = placement.Height;

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