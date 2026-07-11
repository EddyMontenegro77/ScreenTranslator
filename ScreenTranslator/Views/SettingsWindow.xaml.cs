using ScreenTranslator.Models;
using ScreenTranslator.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace ScreenTranslator.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly UserConfigService _configService = new();
        public UserConfig Config { get; set; }
        public ObservableCollection<string> AvailableModels { get; set; } = new();


        public SettingsWindow(UserConfig config)
        {
            InitializeComponent();
            Config = config;
            DataContext = this;
            Loaded += SettingsWindow_Loaded;
        }

        private async void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var models = await App.OllamaManager.GetAvailableModelsAsync();

            AvailableModels.Clear();
            foreach (var model in models)
                AvailableModels.Add(model);

            if (!AvailableModels.Contains(Config.OllamaModel) && AvailableModels.Count > 0)
                Config.OllamaModel = AvailableModels[0];
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            _configService.SaveUserConfig(Config);
            DialogResult = true;
            Close();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            Config = new UserConfig();
            DataContext = Config;
        }

        private void CmbContrastPresets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbContrastPresets.SelectedItem is not ComboBoxItem item) return;

            var tag = item.Tag as string;
            if (string.IsNullOrEmpty(tag)) return;

            var parts = tag.Split('|');
            Config.TranslationFontColor = parts[0];
            Config.TranslationBackgroundColor = parts[1];

            RefreshColorPreviews();
        }

        private void CmbFontColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbFontColor.SelectedItem is ComboBoxItem item && item.Tag is string hex)
            {
                Config.TranslationFontColor = hex;
                RefreshColorPreviews();
            }
        }

        private void CmbBackgroundColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbBackgroundColor.SelectedItem is ComboBoxItem item && item.Tag is string hex)
            {
                Config.TranslationBackgroundColor = hex;
                RefreshColorPreviews();
            }
        }

        private void RefreshColorPreviews()
        {
            DataContext = null;
            DataContext = this;
        }
    }
}