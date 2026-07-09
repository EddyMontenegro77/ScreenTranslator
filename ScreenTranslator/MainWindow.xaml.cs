using ScreenTranslator.Helpers;
using ScreenTranslator.Models;
using ScreenTranslator.Models.Translation;
using ScreenTranslator.Services;
using ScreenTranslator.Services.OCRs;
using ScreenTranslator.Services.Translation;
using System.Drawing;
using System.Windows;
using System.Windows.Input;

namespace ScreenTranslator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly GlobalHotkeyService _hotkeyService = new();
        private readonly IOcrService _ocrService = new WindowsOcrService("ja");
        private readonly ITranslationService _translationService = new OllamaTranslationService(App.OllamaManager, "qwen3.5:9b");
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Executes function when shortcut is pressed (CTRL+SHIFT+T)
            _hotkeyService.Register(
                this,
                GlobalHotkeyService.MOD_CONTROL | GlobalHotkeyService.MOD_SHIFT,
                (uint)KeyInterop.VirtualKeyFromKey(Key.T),
                onPressed: () =>
                {
                    BtnCapture_Click(this, null!);
                });
        }

        protected override void OnClosed(EventArgs e)
        {
            // Free resources and unregister hotkeys when the window is closed
            _hotkeyService.Dispose(); 
            base.OnClosed(e);
        }

        private async void BtnCapture_Click(object sender, RoutedEventArgs e)
        {
            var overlay = new Views.SelectionOverlayWindow();
            this.WindowState = WindowState.Minimized;

            bool? result = overlay.ShowDialog();

            if (result == true && !overlay.WasCancelled)
            {
                Rect selectedArea = overlay.SelectedArea;
                Rectangle physicalRect = selectedArea.ToPhysicalRectangle(this);

                Bitmap bitmap = ScreenCaptureService.CaptureScreen(physicalRect);
                bitmap = ImagePreprocessingService.PreprocessImage(bitmap, new List<PreprocessOption> { PreprocessOption.Upscale, PreprocessOption.Grayscale, PreprocessOption.Binarize });

                //CopyToClipboardService.CopyImageToClipboard(bitmap);

                var ocrResult = await _ocrService.ExtractTextAsync(bitmap);

                // Copy detected text to clipboard using existing service
                if (!string.IsNullOrEmpty(ocrResult.FullText))
                {
                    //CopyToClipboardService.CopyTextToClipboard(ocrResult.FullText);
                    var request = new TranslationRequest
                    {
                        Text = ocrResult.FullText,
                        TargetLanguage = "es",
                        SourceContext = "code"
                        //AdditionalInstructions = TxtExtraInstructions.Text
                    };

                    var traduccion = await _translationService.TranslateAsync(request);
                    MessageBox.Show($"Texto traducido:\n{traduccion}");
                }

            }
        }
    }
}
