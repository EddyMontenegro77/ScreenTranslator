using System;
using System.Collections.Generic;
using System.Text;

namespace ScreenTranslator.Models.Translation
{
    public record TranslationRequest
    {
        public required string Text { get; init; }
        public string TargetLanguage { get; init; } = "es";
        public string? AdditionalInstructions { get; init; }
        public string? SourceContext { get; init; } = string.Empty;
    }
}
