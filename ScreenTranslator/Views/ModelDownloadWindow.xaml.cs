using System.Windows;
using ScreenTranslator.Services;

namespace ScreenTranslator.Views
{
    public partial class ModelDownloadWindow : Window
    {
        private readonly ModelDownloadService _downloadService = new();
        private readonly string _modelName;

        public bool DownloadSucceeded { get; private set; }
        public Exception? DownloadError { get; private set; }

        public ModelDownloadWindow(string modelName)
        {
            InitializeComponent();
            _modelName = modelName;
            Loaded += async (_, _) => await StartDownloadAsync();
        }

        private async Task StartDownloadAsync()
        {
            var progressReporter = new Progress<ModelDownloadProgressService>(OnProgressUpdate);

            try
            {
                await _downloadService.DownloadModelAsync(_modelName, progressReporter);
                DownloadSucceeded = true;
            }
            catch (Exception ex)
            {
                DownloadError = ex;
                DownloadSucceeded = false;
            }
            finally
            {
                DialogResult = DownloadSucceeded;
                Close();
            }
        }

        private void OnProgressUpdate(ModelDownloadProgressService progress)
        {
            StatusText.Text = progress.Status;

            if (progress.TotalBytes is > 0)
            {
                DownloadProgressBar.Value = progress.PercentComplete;
                PercentText.Text = $"{progress.PercentComplete:F1}% ({FormatBytes(progress.CompletedBytes ?? 0)} / {FormatBytes(progress.TotalBytes.Value)})";
            }
        }

        private static string FormatBytes(long bytes)
        {
            double gb = bytes / 1024.0 / 1024.0 / 1024.0;
            return gb >= 1 ? $"{gb:F2} GB" : $"{bytes / 1024.0 / 1024.0:F0} MB";
        }
    }
}