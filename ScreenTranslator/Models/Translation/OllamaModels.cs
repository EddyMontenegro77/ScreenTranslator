using System.Text.Json.Serialization;

namespace ScreenTranslator.Models.Translation
{
    public class OllamaChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<OllamaChatMessage> Messages { get; set; } = new();

        [JsonPropertyName("think")]
        public bool Think { get; set; } = false;

        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false;

        [JsonPropertyName("options")]
        public OllamaOptions? Options { get; set; }
    }

    public class OllamaChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    public class OllamaChatResponse
    {
        [JsonPropertyName("message")]
        public OllamaChatMessage? Message { get; set; }

        [JsonPropertyName("done")]
        public bool Done { get; set; }
    }

    public class OllamaOptions
    {
        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.2;
    }

    public class OllamaModelInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("size")]
        public long Size { get; set; }
    }

    public class OllamaTagsResponse
    {
        [JsonPropertyName("models")]
        public List<OllamaModelInfo> Models { get; set; } = new();
    }
}
