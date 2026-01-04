using System.Text;
using AiResumeAnalyzer.Api.Contracts;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;

namespace AiResumeAnalyzer.Api.Services;

public sealed class FileTextExtractor() : IFileTextExtractor
{
    public async Task<TextExtractionResult> ExtractFileTextAsync(
        Stream fileStream,
        string fileName,
        string contentType
    )
    {
        try
        {
            if (fileStream.CanSeek)
                fileStream.Position = 0;

            if (fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                var text = await ExtractTextFromPdfAsync(fileStream);
                return new TextExtractionResult(true, text, null);
            }
            else if (fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            {
                var text = await ExtractTextFromDocxAsync(fileStream);
                return new TextExtractionResult(true, text, null);
            }
            else if (IsTextFile(fileName, contentType))
            {
                var text = await ExtractTextFromTxtAsync(fileStream);
                return new TextExtractionResult(true, text, null);
            }

            return new TextExtractionResult(false, null, $"Unsupported file type: {fileName}");
        }
        catch (Exception ex)
        {
            return new TextExtractionResult(false, null, $"Error: {ex.Message}");
        }
    }

    private async Task<string> ExtractTextFromPdfAsync(Stream pdfStream)
    {
        using var document = PdfDocument.Open(pdfStream);
        var text = new StringBuilder();

        foreach (var page in document.GetPages())
        {
            text.AppendLine(page.Text);
        }

        return text.ToString();
    }

    private async Task<string> ExtractTextFromDocxAsync(Stream docxStream)
    {
        using var document = WordprocessingDocument.Open(docxStream, false);
        var text = new StringBuilder();
        var body = document.MainDocumentPart?.Document.Body;

        if (body is not null)
        {
            foreach (var paragraph in body.Elements<Paragraph>())
            {
                foreach (var run in paragraph.Elements<Run>())
                {
                    foreach (var textElement in run.Elements<Text>())
                    {
                        text.Append(textElement.Text);
                    }
                }

                text.AppendLine();
            }
        }

        return text.ToString();
    }

    private async Task<string> ExtractTextFromTxtAsync(Stream txtStream)
    {
        using var reader = new StreamReader(txtStream, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }

    private bool IsTextFile(string fileName, string contentType)
    {
        var txtExtensions = new[] { ".txt", ".text", ".md", ".markdown" };
        return txtExtensions.Any(ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
            || contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase);
    }
}
