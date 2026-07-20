using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace ScreenTranslator.Services
{
    public class SystemHealthCheckService
    {
        public class HealthResult
        {
            public bool OllamaInstalled { get; set; }
            public bool AnyModelAvailable { get; set; }
            public List<string> InstalledModels { get; } = new();
            public bool JapaneseOcrAvailable { get; set; }
            public List<string> Issues { get; } = new();
        }

        public async Task<HealthResult> RunAsync(string defaultModelToInstall)
        {
            var result = new HealthResult();

            // 1. Ollama installed
            result.OllamaInstalled = File.Exists(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs", "Ollama", "ollama.exe"));

            if (!result.OllamaInstalled)
            {
                result.Issues.Add("Ollama is not installed.");
                return result;
            }

            // 2. Model check — no longer reports "server not responding" as a startup issue,
            // since the server may just still be finishing its own startup at this point.
            // If it's genuinely down, the capture flow will catch and report that in context.
            var installedModels = await TryGetInstalledModelsAsync();

            if (installedModels != null)
            {
                result.InstalledModels.AddRange(installedModels);
                result.AnyModelAvailable = installedModels.Count > 0;

                if (!result.AnyModelAvailable)
                {
                    result.Issues.Add(
                        $"No translation model is installed. '{defaultModelToInstall}' will be downloaded.");
                }
            }
            // else: server not responding yet — silently skip this check at startup,
            // it will be resolved/reported when the user actually captures.

            // 3. Japanese OCR available
            result.JapaneseOcrAvailable = Windows.Media.Ocr.OcrEngine
                .AvailableRecognizerLanguages
                .Any(l => l.LanguageTag.StartsWith("ja"));

            if (!result.JapaneseOcrAvailable)
                result.Issues.Add("The Japanese OCR language pack is missing in Windows.");

            return result;
        }

        private async Task<List<string>?> TryGetInstalledModelsAsync()
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                var response = await client.GetAsync("http://127.0.0.1:11434/api/tags");

                if (!response.IsSuccessStatusCode)
                    return null;

                var json = await response.Content.ReadAsStringAsync();
                using var document = JsonDocument.Parse(json);

                var models = new List<string>();
                if (document.RootElement.TryGetProperty("models", out var modelsArray))
                {
                    foreach (var model in modelsArray.EnumerateArray())
                    {
                        if (model.TryGetProperty("name", out var nameProperty))
                            models.Add(nameProperty.GetString() ?? string.Empty);
                    }
                }
                return models;
            }
            catch
            {
                return null;
            }
        }
    }
}