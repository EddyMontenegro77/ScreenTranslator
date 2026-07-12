using ScreenTranslator.Services;
using System.Windows;
using H.NotifyIcon;

namespace ScreenTranslator
{
    public partial class App : Application
    {
        public static OllamaProcessManager OllamaManager { get; } = new();
        public static GlobalHotkeyService? _globalHotkeyService { get; private set; }
        private TaskbarIcon? _trayIcon;

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
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"No se pudo iniciar el motor de traducción:\n{ex.Message}",
                    "Error al iniciar",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
            }
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