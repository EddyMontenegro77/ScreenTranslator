using ScreenTranslator.Helpers;
using ScreenTranslator.Models;
using ScreenTranslator.Models.Translation;
using ScreenTranslator.Services;
using ScreenTranslator.Services.OCRs;
using ScreenTranslator.Services.Translation;
using System.Drawing;
using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;
using System.Linq;
using ScreenTranslator.Views;
using System.Threading.Tasks;

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
        private readonly List<CaptureLogEntry> _captureLog = new();
        private StatusIndicatorWindow status = new StatusIndicatorWindow();

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
            var overlay = new SelectionOverlayWindow();
            this.WindowState = WindowState.Minimized;

            bool? result = overlay.ShowDialog();

            if (result == true && !overlay.WasCancelled)
            {
                status.ProgressMaximum = 0;
                status.ProgressValue = 0;
                status.Show();
                status.ProgressIndeterminate();
                status.Status = "Capturing screen...";
                status.ShowProgress();

                Rect selectedArea = overlay.SelectedArea;
                Rectangle physicalRect = selectedArea.ToPhysicalRectangle(this);

                Bitmap capturedBitmap = ScreenCaptureService.CaptureScreen(physicalRect);

                status.Status = "Preprocessing Image...";
                Bitmap processedBitmap = ImagePreprocessingService.PreprocessImage(capturedBitmap, new List<PreprocessOption> { PreprocessOption.Upscale, PreprocessOption.Grayscale, PreprocessOption.Binarize });

                try
                {
                    status.Status = "Extracting text blocks...";
                    // Executes OCR to get the text from the processed image
                    var detectResult = await _ocrService.ExtractTextAsync(processedBitmap);
                    var blocks = TextBlockDetectionService.GroupLinesIntoBlocks(detectResult.Lines);

                    var processedSize = new System.Drawing.Size(processedBitmap.Width, processedBitmap.Height);
                    var captureSize = new System.Drawing.Size(capturedBitmap.Width, capturedBitmap.Height);

                    status.ProgressMaximum = blocks.Count;
                    status.ProgressValue = 0;
                    int blocks_processed = 0;

                    foreach (var block in blocks)
                    {
                        status.Status = $"Translating {blocks_processed}/{blocks.Count} text blocks...";
                        status.ProgressValue = blocks_processed;

                        double marginFactor = 0.06;
                        var br = block.BoundingBox;
                        double expandX = br.Width * marginFactor;
                        double expandY = br.Height * marginFactor;

                        int cropX = (int)System.Math.Max(0, br.X - expandX);
                        int cropY = (int)System.Math.Max(0, br.Y - expandY);
                        int cropW = (int)System.Math.Min(processedBitmap.Width - cropX, br.Width + 2 * expandX);
                        int cropH = (int)System.Math.Min(processedBitmap.Height - cropY, br.Height + 2 * expandY);

                        var cropRect = new System.Drawing.Rectangle(cropX, cropY, System.Math.Max(1, cropW), System.Math.Max(1, cropH));

                        using Bitmap regionBitmap = CropBitmap(processedBitmap, cropRect);

                        var regionOcr = await _ocrService.ExtractTextAsync(regionBitmap);
                        string detectedText = !string.IsNullOrEmpty(regionOcr.FullText) ? regionOcr.FullText : block.Text;
                        if (string.IsNullOrWhiteSpace(detectedText)) continue;

                        var request = new TranslationRequest
                        {
                            Text = detectedText,
                            TargetLanguage = "es",
                            SourceContext = "code"
                        };

                        var translation = await _translationService.TranslateAsync(request);

                        var dipRect = CoordinateMapper.MapOcrRectToDip(block.BoundingBox, physicalRect, processedSize, captureSize, this);

                        double scaleX = processedSize.Width / (double)captureSize.Width;
                        double scaleY = processedSize.Height / (double)captureSize.Height;

                        var physRectForLog = new System.Drawing.Rectangle(
                            (int)(physicalRect.X + (block.BoundingBox.X / scaleX)),
                            (int)(physicalRect.Y + (block.BoundingBox.Y / scaleY)),
                            System.Math.Max(1, (int)(block.BoundingBox.Width / scaleX)),
                            System.Math.Max(1, (int)(block.BoundingBox.Height / scaleY))
                        );

                        var entry = new CaptureLogEntry
                        {
                            DetectedText = detectedText,
                            Translation = translation,
                            PhysicalRect = physRectForLog,
                            DipRect = dipRect
                        };
                        _captureLog.Add(entry);

                        var popup = new TranslationOverlayWindow(entry.Translation, dipRect)
                        {
                            Owner = this,
                            Topmost = true
                        };

                        blocks_processed++;
                        popup.Show();
                    }
                }
                finally
                {
                    status.HideProgress();
                    status.Hide();
                    processedBitmap.Dispose();
                    capturedBitmap.Dispose();
                }
            }
        }

        private static Bitmap CropBitmap(Bitmap src, Rectangle rect)
        {
            var fixedRect = Rectangle.Intersect(new Rectangle(0, 0, src.Width, src.Height), rect);
            if (fixedRect.Width <= 0 || fixedRect.Height <= 0)
                return new Bitmap(1, 1);

            var crop = new Bitmap(fixedRect.Width, fixedRect.Height);
            using (var g = Graphics.FromImage(crop))
            {
                g.DrawImage(src, new Rectangle(0, 0, crop.Width, crop.Height), fixedRect, GraphicsUnit.Pixel);
            }
            return crop;
        }
    }
}
