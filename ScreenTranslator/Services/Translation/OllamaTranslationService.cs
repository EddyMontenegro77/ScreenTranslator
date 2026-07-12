using System.Net.Http;
using System.Net.Http.Json;
using ScreenTranslator.Models.Translation;

namespace ScreenTranslator.Services.Translation
{
    public class OllamaTranslationService : ITranslationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _model;
        private readonly OllamaProcessManager _processManager;
        private const string OllamaChatUrl = "http://localhost:11434/api/chat";

        public OllamaTranslationService(OllamaProcessManager processManager, string model = "qwen3.5:9b")
        {
            _processManager = processManager;
            _model = model;
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        }

        public async Task<string> TranslateAsync(TranslationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Text))
                return string.Empty;

            await _processManager.EnsureRunningAsync();

            var ollamaRequest = new OllamaChatRequest
            {
                Model = _model,
                Think = false,
                Stream = false,
                Options = new OllamaOptions { Temperature = 0.2, NumCtx=2048 },
                Messages = new List<OllamaChatMessage>
                {
                    new() { Role = "system", Content = BuildSystemPrompt(request.TargetLanguage) },
                    new() { Role = "user", Content = request.Text }
                }
            };

            try
            {
                var httpResponse = await _httpClient.PostAsJsonAsync(OllamaChatUrl, ollamaRequest);

                var body = await httpResponse.Content.ReadAsStringAsync();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(
                        $"Ollama respondió {httpResponse.StatusCode}: {body}");
                }

                var result = System.Text.Json.JsonSerializer.Deserialize<OllamaChatResponse>(body);

                return CleanResponse(result?.Message?.Content ?? string.Empty);
            }
            catch (TaskCanceledException ex)
            {
                throw new InvalidOperationException(
                    "La traducción tardó demasiado (timeout).", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Error al comunicarse con Ollama.", ex);
            }
        }

        private string BuildSystemPrompt(string targetLanguage)
        {
            return $"""
                You are a professional Japanese-to-{targetLanguage} translator specialized in manga and video games.

                CRITICAL OUTPUT RULES (do not break these under any circumstance):
                - Your ENTIRE response must be written ONLY in {targetLanguage}.
                - NEVER respond in Japanese, English, romaji, or any language other than {targetLanguage} (unless {targetLanguage} IS English).
                - NEVER include the original Japanese text in your response.
                - NEVER transliterate into romaji.
                - Output ONLY the translated sentence. No notes, no explanations, no quotation marks, no alternates.

                Translation style:
                - Translate naturally, the way a native {targetLanguage} speaker would actually say it.
                - Preserve the original tone (casual, rude, formal, urgent, etc.).
                - Pay close attention to Japanese courtesy expressions (お元気で, お疲れ様, etc.) — translate intended meaning, not literal words.
                - Do not translate proper nouns.
                - The input may contain OCR errors; infer the most likely intended meaning.
                """;
        }

        private string CleanResponse(string response)
        {
            return response.Trim().Trim('"', '「', '」', '『', '』');
        }
    }
}
