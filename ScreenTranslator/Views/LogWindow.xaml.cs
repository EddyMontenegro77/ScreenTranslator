using System.Collections.ObjectModel;
using System.Windows;
using ScreenTranslator.Models;

namespace ScreenTranslator.Views
{
    public partial class LogWindow : Window
    {
        public LogWindow(ObservableCollection<CaptureLogEntry> log)
        {
            InitializeComponent();
            DataContext = log;
        }
    }
}