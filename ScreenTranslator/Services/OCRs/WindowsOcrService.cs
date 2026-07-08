// Requirements:
// - Windows SDK (Developer Pack) TargetPlatformVersion in .csproj
// - PMicrosoft.Windows.SDK.Contracts y Microsoft.Windows.CsWinRT
// - Ejecutar 'dotnet restore' y disponer de las proyecciones WinRT (CsWinRT) en el entorno.

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ScreenTranslator.Models;
using System.Windows;

using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Globalization;
using System.Runtime.InteropServices.WindowsRuntime;

namespace ScreenTranslator.Services.OCRs
{
    public class WindowsOcrService : IOcrService
    {
        private readonly string _language;

        public WindowsOcrService() : this(null) { }

        public WindowsOcrService(string? language)
        {
            _language = language ?? string.Empty;
        }

        public async Task<OcrTextResults> ExtractTextAsync(Bitmap bitmap)
        {
            if (bitmap == null) throw new ArgumentNullException(nameof(bitmap));

            var results = new OcrTextResults();

            try
            {
                using var ms = new MemoryStream();
                bitmap.Save(ms, ImageFormat.Png);
                ms.Position = 0;

                var randomAccess = ms.AsRandomAccessStream();

                var decoder = await BitmapDecoder.CreateAsync(randomAccess);
                using (var softwareBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied))
                {
                    OcrEngine? engine = null;

                    if (!string.IsNullOrEmpty(_language))
                    {
                        try
                        {
                            var lang = new Language(_language);
                            engine = OcrEngine.TryCreateFromLanguage(lang);
                        }
                        catch
                        {
                            engine = null;
                        }
                    }

                    engine ??= OcrEngine.TryCreateFromUserProfileLanguages();
                    if (engine == null) return results;

                    var ocrResult = await engine.RecognizeAsync(softwareBitmap);
                    if (ocrResult == null) return results;

                    results.FullText = ocrResult.Text ?? string.Empty;

                    foreach (var line in ocrResult.Lines)
                    {
                        // Compute bounding box from words (compatible con distintas versiones)
                        double minX = double.MaxValue, minY = double.MaxValue, maxX = double.MinValue, maxY = double.MinValue;
                        foreach (var w in line.Words)
                        {
                            var brw = w.BoundingRect;
                            minX = Math.Min(minX, brw.X);
                            minY = Math.Min(minY, brw.Y);
                            maxX = Math.Max(maxX, brw.X + brw.Width);
                            maxY = Math.Max(maxY, brw.Y + brw.Height);
                        }

                        Rect rect;
                        if (minX == double.MaxValue)
                            rect = new Rect();
                        else
                            rect = new Rect(minX, minY, Math.Max(0, maxX - minX), Math.Max(0, maxY - minY));

                        results.Lines.Add(new OcrLineResult { Text = line.Text ?? string.Empty, BoundingBox = rect });
                    }
                }
            }
            catch
            {
                // On failure return empty results (caller can handle)
            }

            return results;
        }
    }
}
