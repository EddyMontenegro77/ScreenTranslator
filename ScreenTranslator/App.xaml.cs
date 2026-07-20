using H.NotifyIcon;
using ScreenTranslator.Services;
using ScreenTranslator.Views;
using System.Windows;

namespace ScreenTranslator
{
    public partial class App : Application
    {
        public static OllamaProcessManager OllamaManager { get; } = new();
        public static SystemHealthCheckService _systemHealthCheckService { get; private set; } = new();
        public static GlobalHotkeyService? _globalHotkeyService { get; private set; }

        private TaskbarIcon? _trayIcon;
        private const string DefaultModelToInstall = "qwen3.5:4b";

        private void TrayIcon_LeftClick(object sender, RoutedEventArgs e)
        {

            var mainWindow = Current.Windows
                .OfType<MainWindow>()
                .FirstOrDefault();

            if (mainWindow != null)
            {
                mainWindow.RestoreFromTray();
            }
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _trayIcon = (TaskbarIcon)Resources["TrayIcon"];
            _trayIcon.ForceCreate();

            try
            {
                await OllamaManager.EnsureRunningAsync();

                var health = await _systemHealthCheckService.RunAsync(DefaultModelToInstall);
                if (health.Issues.Count > 0)
                {
                    var message = string.Join("\n", health.Issues);
                    MessageBox.Show(
                        $"Problems found in Health Check:\n\n{message}",
                        "System verification",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    if (!health.JapaneseOcrAvailable)
                    {
                        MessageBox.Show(
                            "The Japanese language pack with Optical Character Recognition (OCR) is not installed on Windows.\n\n" +
                            "Go to Settings → Time & Language → Language & Region → Add a language → Japanese," +
                            "and make sure to check the Optical Character Recognition option.\n\n" +
                            "The application will close. Reopen it after installing the pack.",
                            "Japanese language is missing",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

                        Shutdown();
                        return;
                    }

                    if (!health.AnyModelAvailable)
                    {
                        bool setupCompleted = await HandleMissingModelAsync();
                        if (!setupCompleted)
                        {
                            Shutdown();
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not initiate translation engine:\n{ex.Message}",
                    "Error on start",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
            }
        }

        /// <summary>
        /// Displays the model selection dialog and, if the user chooses to download,
        /// displays the progress window. Returns true if the app can continue
        /// (model successfully downloaded), false if it should close.
        /// </summary>
        private async Task<bool> HandleMissingModelAsync()
        {
            var selectionWindow = new ModelSelectionWindow();
            bool? selectionResult = selectionWindow.ShowDialog();

            if (selectionResult != true || selectionWindow.SelectedModel == null)
            {
                // User canceled the selection or didn't choose a model, so we can't continue.
                // so that you can resume it once you have manually downloaded the model.
                MessageBox.Show(
                    "You can download a model manually by running, for example:\n\n" +
                    "ollama pull qwen3.5:4b\n\n" +
                    "Reopen the application when the model is ready.",
                    "Manual download",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return false;
            }

            var downloadWindow = new ModelDownloadWindow(selectionWindow.SelectedModel);
            bool? downloadResult = downloadWindow.ShowDialog();

            if (downloadResult != true)
            {
                MessageBox.Show(
                    $"Model could not be downloaded:\n{downloadWindow.DownloadError?.Message}",
                    "Error downloading",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                MessageBox.Show(
                    "You can download a model manually by running, for example:\n\n" +
                    "ollama pull qwen3.5:4b\n\n" +
                    "Reopen the application when the model is ready.",
                    "Manual download",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return false;
            }

            return true;
        }

        private void TrayIcon_Exit(object sender, RoutedEventArgs e)
        {
            var mainWindow = Current.Windows.OfType<MainWindow>().FirstOrDefault();
            mainWindow?.ExitApplication();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            OllamaManager.Dispose();
            _globalHotkeyService?.Dispose();
            _trayIcon?.Dispose();
            base.OnExit(e);
        }
    }
}