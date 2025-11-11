using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf;
using PDXLite.Interfaces;

namespace PDXLite.Services
{
    public class PdfService : IPdfService
    {
        public async Task<(string text, int pageCount)> ExtractTextFromPdfAsync(Stream pdfStream)
        {
            return await Task.Run(() =>
            {
                using var pdfReader = new PdfReader(pdfStream);
                using var pdfDocument = new PdfDocument(pdfReader);

                var pageCount = pdfDocument.GetNumberOfPages();
                var extractedText = new System.Text.StringBuilder();

                for (int i = 1; i <= pageCount; i++)
                {
                    var page = pdfDocument.GetPage(i);
                    var strategy = new SimpleTextExtractionStrategy();
                    var pageText = PdfTextExtractor.GetTextFromPage(page, strategy);

                    extractedText.AppendLine($"--- Page {i} ---");
                    extractedText.AppendLine(pageText);
                    extractedText.AppendLine();
                }

                return (extractedText.ToString(), pageCount);
            });
        }
    }
}
