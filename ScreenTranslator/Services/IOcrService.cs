using System.Drawing;
using ScreenTranslator.Models;

namespace ScreenTranslator.Services
{
    public interface IOcrService
    {
        Task<OcrTextResults> ExtractTextAsync(Bitmap bitmap);
    }
}