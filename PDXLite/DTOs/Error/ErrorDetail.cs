using System.Text.Json.Serialization;

namespace PDXLite.DTOs.Error
{
    public class ErrorDetail
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("details")]
        public Dictionary<string, string[]>? Details { get; set; }
    }
}
