namespace PDXLite.DTOs.Pdf
{
    public class PdfMetadata
    {
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public int PageCount { get; set; }
        public DateTime ExtractedAt { get; set; }
    }
}
