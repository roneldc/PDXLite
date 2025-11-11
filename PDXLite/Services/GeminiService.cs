using PDXLite.Interfaces;
using System.Text.Json;
using System.Text;

namespace PDXLite.Services
{
    public class GeminiService : IGeminiService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public GeminiService(IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = new HttpClient();
        }

        public async Task<string> AnalyzeTextAndGenerateJsonAsync(string extractedText)
        {
            var apiKey = _configuration["Gemini:ApiKey"];
            var model = _configuration["Gemini:Model"] ?? "gemini-1.5-flash";

            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("Gemini API key is not configured");
            }

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

            var prompt = $@"Analyze the following text extracted from a PDF document and structure it into a meaningful JSON format. 
Identify key information such as:
- Document type (if identifiable)
- Main sections and their content
- Any structured data (tables, lists, key-value pairs)
- Important entities (dates, names, amounts, addresses, etc.)
- Metadata if present

Return ONLY valid JSON without any markdown formatting or explanation. Make the JSON structure logical and well-organized based on the content.

Text to analyze:
{extractedText}";

            var requestBody = new
            {
                contents = new[]
                {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
                generationConfig = new
                {
                    temperature = 0.4,
                    topK = 32,
                    topP = 1,
                    maxOutputTokens = 8192
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Gemini API error: {responseText}");
            }

            var responseJson = JsonDocument.Parse(responseText);
            var generatedText = responseJson.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "{}";

            // Clean up the response (remove markdown code blocks if present)
            generatedText = generatedText.Trim();
            if (generatedText.StartsWith("```json"))
            {
                generatedText = generatedText.Substring(7);
            }
            if (generatedText.StartsWith("```"))
            {
                generatedText = generatedText.Substring(3);
            }
            if (generatedText.EndsWith("```"))
            {
                generatedText = generatedText.Substring(0, generatedText.Length - 3);
            }

            // Validate it's proper JSON
            try
            {
                JsonDocument.Parse(generatedText);
            }
            catch
            {
                // If not valid JSON, wrap it
                generatedText = JsonSerializer.Serialize(new { extracted_content = generatedText });
            }

            return generatedText.Trim();
        }
    }
}
