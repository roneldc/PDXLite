namespace PDXLite.Interfaces
{
    public interface IPdfService
    {
        Task<(string text, int pageCount)> ExtractTextFromPdfAsync(Stream pdfStream);
    }
}
