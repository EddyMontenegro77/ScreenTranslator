using ScreenTranslator.Helpers;
using ScreenTranslator.Models;
using ScreenTranslator.Models.Translation;
using ScreenTranslator.Views;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Windows;
using DrawingSize = System.Drawing.Size;
using TextBlockModel = ScreenTranslator.Services.TextBlockResult;

namespace ScreenTranslator.Services
{
    public class CaptureWorkflowService
    {
        private readonly IOcrService _ocrService;
        private readonly ITranslationService _translationService;
        private readonly UserConfig _userConfig;

        public CaptureWorkflowService(IOcrService ocrService, ITranslationService translationService, UserConfig userConfig)
        {
            _ocrService = ocrService;
            _translationService = translationService;
            _userConfig = userConfig;
        }

        public async Task RunAsync(
            Rectangle physicalRect,
            Window ownerWindow,
            StatusIndicatorWindow status,
            ObservableCollection<CaptureLogEntry> captureLog)
        {
            status.Show();
            status.ProgressIndeterminate();
            status.Status = "Capturing screen...";
            status.ShowProgress();

            Bitmap capturedBitmap = ScreenCaptureService.CaptureScreen(physicalRect);
            Bitmap? processedBitmap = null;

            try
            {
                status.Status = "Preprocessing image...";
                processedBitmap = ImagePreprocessingService.PreprocessImage(
                    capturedBitmap, PreprocessOptionsHelper.FromConfig(_userConfig));

                status.Status = "Extracting text blocks...";
                var detectResult = await _ocrService.ExtractTextAsync(processedBitmap);
                var blocks = TextBlockDetectionService.GroupLinesIntoBlocks(detectResult.Lines);

                if (blocks.Count == 0)
                {
                    status.Status = "No text detected.";
                    await Task.Delay(1200);
                    return;
                }

                var processedSize = new DrawingSize(processedBitmap.Width, processedBitmap.Height);
                var captureSize = new DrawingSize(capturedBitmap.Width, capturedBitmap.Height);

                status.ProgressMaximum = blocks.Count;
                status.ProgressValue = 0;

                for (int i = 0; i < blocks.Count; i++)
                {
                    status.Status = $"Translating {i + 1}/{blocks.Count} text blocks...";

                    await ProcessBlockAsync(
                        blocks[i], processedBitmap, processedSize, captureSize,
                        physicalRect, ownerWindow, captureLog);

                    status.ProgressValue = i + 1;
                }
            }
            finally
            {
                status.HideProgress();
                status.Hide();
                processedBitmap?.Dispose();
                capturedBitmap.Dispose();
            }
        }

        private async Task ProcessBlockAsync(
            TextBlockModel block,
            Bitmap processedBitmap,
            DrawingSize processedSize,
            DrawingSize captureSize,
            Rectangle physicalRect,
            Window ownerWindow,
            ObservableCollection<CaptureLogEntry> captureLog)
        {
            var cropRect = BitmapCropHelper.ExpandAndClamp(
                block.BoundingBox, processedBitmap.Width, processedBitmap.Height);

            using Bitmap regionBitmap = BitmapCropHelper.Crop(processedBitmap, cropRect);

            var regionOcr = await _ocrService.ExtractTextAsync(regionBitmap);
            string detectedText = !string.IsNullOrEmpty(regionOcr.FullText) ? regionOcr.FullText : block.Text;

            if(_userConfig.CopyLastToClipboard)
                CopyToClipboardService.CopyTextToClipboard(detectedText);

            if (string.IsNullOrWhiteSpace(detectedText))
                return;

            var request = new TranslationRequest
            {
                Text = detectedText,
                TargetLanguage = _userConfig.TargetLanguage,
                SourceContext = _userConfig.SourceContent,
                AdditionalInstructions = _userConfig.AdditionalInstructions
            };

            var translation = await _translationService.TranslateAsync(request);

            var dipRect = CoordinateMapper.MapOcrRectToDip(
                block.BoundingBox, physicalRect, processedSize, captureSize, ownerWindow);
            
            bool isVertical = block.BoundingBox.Height > block.BoundingBox.Width * 2;

            captureLog.Add(new CaptureLogEntry
            {
                DetectedText = detectedText,
                Translation = translation,
                DipRect = dipRect
            });

            var backgroundColor = ColorHexHelper.FromHex(_userConfig.TranslationBackgroundColor);
            var fontColor = ColorHexHelper.FromHex(_userConfig.TranslationFontColor);

            var popup = new TranslationOverlayWindow(
                translation,
                dipRect,
                backgroundColor,
                fontColor,
                isVertical,
                _userConfig.TranslationFontSize)
            {
                Owner = ownerWindow,
                Topmost = true
            };

            popup.Show();
        }
    }
}