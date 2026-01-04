using System.Text;
using UglyToad.PdfPig;

namespace AiResumeAnalyzer.Api.Services;

public sealed class PdfExtractor : IPdfExtractor
{
    public bool CanHandleFile(string fileName, string contentType)
    {
        return fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<string?> ExtractTextFromPdfAsync(Stream pdfStream)
    {
        try
        {
            using var document = PdfDocument.Open(
                pdfStream,
                new ParsingOptions { ClipPaths = true }
            );
            var text = new StringBuilder();

            foreach (var page in document.GetPages())
                text.Append(page.Text);

            return text.ToString();
        }
        catch
        {
            return null;
        }
    }
}
