namespace PDXLite.DTOs.Pdf
{
    public class PdfExtractionResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public object? Data { get; set; }
        public PdfMetadata? Metadata { get; set; }
    }
}
