using System.Text.Json.Serialization;

namespace PDXLite.DTOs.Error
{
    public class ErrorResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; } = false;

        [JsonPropertyName("error")]
        public ErrorDetail Error { get; set; } = new();

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [JsonPropertyName("path")]
        public string? Path { get; set; }
    }
}
