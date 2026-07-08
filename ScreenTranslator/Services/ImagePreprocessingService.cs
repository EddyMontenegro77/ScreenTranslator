using ScreenTranslator.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace ScreenTranslator.Services
{
    internal class ImagePreprocessingService
    {

        private static readonly Dictionary<PreprocessOption, Func<Bitmap, Bitmap>> _operations = new()
        {
            { PreprocessOption.Upscale, img => UpscaleImage(img, 2.5f) },
            { PreprocessOption.Grayscale, ConvertToGrayscaleWithContrast },
            { PreprocessOption.Binarize, img => Binarize(img, 128) }
        };

        public static Bitmap PreprocessImage(Bitmap original, IEnumerable<PreprocessOption> operations)
        {
            if (operations == null)
                return original;

            Bitmap current = original;
            foreach (var operation in operations)
            {
                if (_operations.TryGetValue(operation, out var process))
                {
                    Bitmap next = process(current);

                    if (!ReferenceEquals(current, original))
                    {
                        current.Dispose();
                    }

                    current = next;
                }
            }
            return current;
        }

        private static Bitmap UpscaleImage(Bitmap original, float factor = 2.5f)
        {
            int newWidth = (int)(original.Width * factor);
            int newHeight = (int)(original.Height * factor);

            var resized = new Bitmap(newWidth, newHeight);
            using (var graphics = Graphics.FromImage(resized))
            {
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                graphics.DrawImage(original, 0, 0, newWidth, newHeight);
            }
            return resized;
        }

        private static Bitmap ConvertToGrayscaleWithContrast(Bitmap original)
        {
            var result = new Bitmap(original.Width, original.Height);

            using (var graphics = Graphics.FromImage(result))
            {
                // Matrix that converts to grayscale and increases contrast
                float[][] matrix = {
            new float[] {0.3f, 0.3f, 0.3f, 0, 0},
            new float[] {0.59f, 0.59f, 0.59f, 0, 0},
            new float[] {0.11f, 0.11f, 0.11f, 0, 0},
            new float[] {0, 0, 0, 1, 0},
            new float[] {0, 0, 0, 0, 1}
            };

                var colorMatrix = new System.Drawing.Imaging.ColorMatrix(matrix);
                var attributes = new System.Drawing.Imaging.ImageAttributes();
                attributes.SetColorMatrix(colorMatrix);

                graphics.DrawImage(original, new Rectangle(0, 0, original.Width, original.Height),
                    0, 0, original.Width, original.Height, GraphicsUnit.Pixel, attributes);
            }

            return result;
        }


        private static Bitmap Binarize(Bitmap original, byte threshold = 128)
        {
            var result = new Bitmap(original.Width, original.Height);

            for (int y = 0; y < original.Height; y++)
            {
                for (int x = 0; x < original.Width; x++)
                {
                    var pixel = original.GetPixel(x, y);
                    int gray = (pixel.R + pixel.G + pixel.B) / 3;
                    var newColor = gray < threshold ? Color.Black : Color.White;
                    result.SetPixel(x, y, newColor);
                }
            }

            return result;
        }

    }
}
