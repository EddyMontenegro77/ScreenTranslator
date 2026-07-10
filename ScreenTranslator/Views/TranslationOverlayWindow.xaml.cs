using System.Windows;
using System.Windows.Input;

namespace ScreenTranslator.Views
{
    public partial class TranslationOverlayWindow : Window
    {
        public TranslationOverlayWindow(string text, Rect placement)
        {
            InitializeComponent();
            TxtTranslation.Text = text ?? string.Empty;

            Left = placement.X;
            Top = placement.Y;

            if (placement.Width > 0)
                TxtTranslation.MaxWidth = placement.Width;
        }

        public void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
