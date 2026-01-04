using System.Text;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;

namespace AiResumeAnalyzer.Api.Services;

public sealed class DocxExtractor : IDocxExtractor
{
    public bool CanHandleFile(string fileName, string contentType)
    {
        return fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase)
            || contentType.Equals(
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                StringComparison.OrdinalIgnoreCase
            );
    }

    public async Task<string?> ExtractTextFromDocxAsync(Stream docxStream)
    {
        try
        {
            using var document = WordprocessingDocument.Open(docxStream, false);
            var text = new StringBuilder();
            var body = document.MainDocumentPart?.Document.Body;

            if (body is not null)
                foreach (var paragraph in body.Elements<Paragraph>())
                    text.Append(paragraph.InnerText);

            return text.ToString();
        }
        catch
        {
            return null;
        }
    }
}
