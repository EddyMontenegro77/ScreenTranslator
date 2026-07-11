using ScreenTranslator.Models;

namespace ScreenTranslator.Helpers
{
    public static class PreprocessOptionsHelper
    {
        public static List<PreprocessOption> FromConfig(UserConfig config)
        {
            var options = new List<PreprocessOption>();

            if (config.PreProcessOptionUpscale) options.Add(PreprocessOption.Upscale);
            if (config.PreProcessOptionGrayScale) options.Add(PreprocessOption.Grayscale);
            if (config.PreProcessOptionBinarize) options.Add(PreprocessOption.Binarize);

            return options;
        }
    }
}