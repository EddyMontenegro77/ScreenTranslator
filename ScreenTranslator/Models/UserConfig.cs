using System;
using System.Collections.Generic;
using System.Text;

namespace ScreenTranslator.Models
{
    public class UserConfig
    {
        public string SourceLanguage { get; set; } = "ja";
        public string TargetLanguage { get; set; } = "Spanish";
        public string OllamaModel { get; set; } = "qwen3.5:9b";
        public string SourceContent { get; set; } = "Manga";
        public string AdditionalInstructions { get; set; } = string.Empty;
        public bool CopyLastToClipboard { get; set; } = true;

        public bool PreProcessOptionUpscale { get; set; } = true;
        public bool PreProcessOptionGrayScale { get; set; } = true;
        public bool PreProcessOptionBinarize { get; set; } = true;
        public int TranslationFontSize { get; set; } = 24;
        public string TranslationFontColor { get; set; } = "#FFFFFFFF";
        public string TranslationBackgroundColor { get; set; } = "#FF000000";
    }


}
