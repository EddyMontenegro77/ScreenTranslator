using System.Windows;

namespace ScreenTranslator.Views
{
    public partial class ModelSelectionWindow : Window
    {
        public string? SelectedModel { get; private set; }
        public bool UserWantsManualSetup { get; private set; }

        public ModelSelectionWindow()
        {
            InitializeComponent();
        }

        private void Download_Click(object sender, RoutedEventArgs e)
        {
            SelectedModel = LightModelRadio.IsChecked == true ? "qwen3.5:4b" : "qwen3.5:9b";
            DialogResult = true;
            Close();
        }

        private void ManualSetup_Click(object sender, RoutedEventArgs e)
        {
            UserWantsManualSetup = true;
            DialogResult = false;
            Close();
        }
    }
}