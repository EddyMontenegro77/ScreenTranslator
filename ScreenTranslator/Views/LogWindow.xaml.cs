using ScreenTranslator.Models;
using ScreenTranslator.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace ScreenTranslator.Views
{
    public partial class LogWindow : Window
    {
        public LogWindow(ObservableCollection<CaptureLogEntry> log)
        {
            InitializeComponent();
            DataContext = log;
        }

        private void CopyOriginal_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string text)
                CopyToClipboardService.CopyTextToClipboard(text);
        }

        private void CopyTranslation_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string text)
                CopyToClipboardService.CopyTextToClipboard(text);
        }
    }
}