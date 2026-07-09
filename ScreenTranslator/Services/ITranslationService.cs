using ScreenTranslator.Models.Translation;

namespace ScreenTranslator.Services
{
    public interface ITranslationService
    {
        Task<string> TranslateAsync(TranslationRequest request);
    }
}
