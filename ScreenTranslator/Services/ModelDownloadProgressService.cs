using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace ScreenTranslator.Services
{
    public class ModelDownloadProgressService
    {
        public string Status { get; set; } = string.Empty;
        public long? TotalBytes { get; set; }
        public long? CompletedBytes { get; set; }
        public double PercentComplete =>
            TotalBytes is > 0 ? (double)(CompletedBytes ?? 0) / TotalBytes.Value * 100.0 : 0;
    }

    public class ModelDownloadService
    {
        private const string OllamaPullEndpoint = "http://127.0.0.1:11434/api/pull";

        public async Task DownloadModelAsync(
            string modelName,
            IProgress<ModelDownloadProgressService> progress,
            CancellationToken cancellationToken = default)
        {
            using var client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };

            var requestBody = JsonSerializer.Serialize(new { name = modelName, stream = true });
            using var request = new HttpRequestMessage(HttpMethod.Post, OllamaPullEndpoint)
            {
                Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
            };

            using var response = await client.SendAsync(
                request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            string? line;
            while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    using var doc = JsonDocument.Parse(line);
                    var root = doc.RootElement;

                    var status = root.TryGetProperty("status", out var s) ? s.GetString() ?? "" : "";
                    long? total = root.TryGetProperty("total", out var t) ? t.GetInt64() : null;
                    long? completed = root.TryGetProperty("completed", out var c) ? c.GetInt64() : null;

                    if (root.TryGetProperty("error", out var errorProp))
                        throw new InvalidOperationException($"Ollama pull error: {errorProp.GetString()}");

                    progress.Report(new ModelDownloadProgressService
                    {
                        Status = status,
                        TotalBytes = total,
                        CompletedBytes = completed
                    });
                }
                catch (JsonException)
                {
                    // No parsable JSON, ignore this line and continue reading
                }
            }
        }
    }
}