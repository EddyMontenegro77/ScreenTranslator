using ScreenTranslator.Helpers;
using ScreenTranslator.Models;
using ScreenTranslator.Services;
using ScreenTranslator.Services.OCRs;
using ScreenTranslator.Services.Translation;
using ScreenTranslator.Views;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Windows;
using System.Windows.Input;

namespace ScreenTranslator
{
    public partial class MainWindow : Window
    {
        private readonly GlobalHotkeyService _hotkeyService = new();
        private IOcrService _ocrService = null!;
        private readonly UserConfigService _userConfigService = new();
        private readonly ObservableCollection<CaptureLogEntry> _captureLog = new();
        private readonly StatusIndicatorWindow _status = new();
        private LogWindow? _logWindow;

        private bool _isExiting = false;
        private bool _suppressTrayOnMinimize = false;

        private UserConfig _userConfig;
        private ITranslationService _translationService = null!;
        private CaptureWorkflowService _workflowService = null!;

        public MainWindow()
        {
            InitializeComponent();
            _userConfig = _userConfigService.LoadUserConfig();
            RebuildServices();
            Loaded += MainWindow_Loaded;
        }

        // Rebuils in case of model change or config change
        private void RebuildServices()
        {
            _ocrService = new WindowsOcrService(_userConfig.SourceLanguage);
            _translationService = new OllamaTranslationService(App.OllamaManager, _userConfig.OllamaModel);
            _workflowService = new CaptureWorkflowService(_ocrService, _translationService, _userConfig);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _hotkeyService.Register(
                this,
                GlobalHotkeyService.MOD_CONTROL | GlobalHotkeyService.MOD_SHIFT,
                (uint)KeyInterop.VirtualKeyFromKey(Key.T),
                onPressed: () => BtnCapture_Click(this, null!));
        }
        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);

            if (WindowState == WindowState.Minimized && IsLoaded && !_suppressTrayOnMinimize)
            {
                Dispatcher.BeginInvoke(() => Hide());
            }
        }

        public void RestoreFromTray()
        {
            Dispatcher.Invoke(() =>
            {
                Show();
                Visibility = Visibility.Visible;

                WindowState = WindowState.Normal;

                Activate();
                Topmost = true;
                Topmost = false;
            });
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!_isExiting)
            {
                e.Cancel = true;
                WindowState = WindowState.Minimized;
            }
            else
            {
                base.OnClosing(e);
            }
        }

        public void ExitApplication()
        {
            _isExiting = true;
            System.Windows.Application.Current.Shutdown();
        }

        protected override void OnClosed(EventArgs e)
        {
            _hotkeyService.Dispose();
            base.OnClosed(e);
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow(_userConfig);

            if (settingsWindow.ShowDialog() == true)
            {
                _userConfig = settingsWindow.Config;
                RebuildServices();
            }
        }

        private async void BtnCapture_Click(object sender, RoutedEventArgs e)
        {
            var overlay = new SelectionOverlayWindow();
            _suppressTrayOnMinimize = true;
            WindowState = WindowState.Minimized;

            bool? result = overlay.ShowDialog();
            _suppressTrayOnMinimize = false;

            if (result != true || overlay.WasCancelled)
                return;

            Rect selectedArea = overlay.SelectedArea;
            Rectangle physicalRect = selectedArea.ToPhysicalRectangle(this);

            await _workflowService.RunAsync(physicalRect, this, _status, _captureLog);
        }

        private void Log_Click(object sender, RoutedEventArgs e)
        {
            if (_logWindow == null || !_logWindow.IsLoaded)
            {
                _logWindow = new LogWindow(_captureLog) { Owner = this };
                _logWindow.Show();
            }
            else
            {
                _logWindow.Activate();
            }
        }
    }
}