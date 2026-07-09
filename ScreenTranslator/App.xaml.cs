using ScreenTranslator.Services;
using System.Windows;

namespace ScreenTranslator
{
    public partial class App : Application
    {
        public static OllamaProcessManager OllamaManager { get; } = new();

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

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

        protected override void OnExit(ExitEventArgs e)
        {
            OllamaManager.Dispose();
            base.OnExit(e);
        }
    }
}