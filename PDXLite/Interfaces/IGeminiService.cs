namespace PDXLite.Interfaces
{
    public interface IGeminiService
    {
        Task<string> AnalyzeTextAndGenerateJsonAsync(string extractedText);
    }
}
