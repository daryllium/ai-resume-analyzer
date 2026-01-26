using System.Text;
using AiResumeAnalyzer.Api.Contracts;
using AiResumeAnalyzer.Api.Services.Interfaces;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;

namespace AiResumeAnalyzer.Api.Services;

public sealed class FileTextExtractor(IOcrService ocrService, ILogger<FileTextExtractor> logger)
    : IFileTextExtractor
{
    private readonly IOcrService _ocrService = ocrService;
    private readonly ILogger<FileTextExtractor> _logger = logger;

    public async Task<TextExtractionResult> ExtractFileTextAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            if (fileStream.CanSeek)
                fileStream.Position = 0;

            if (fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var text = await ExtractTextFromPdfAsync(fileStream);
                    return new TextExtractionResult(true, text, null);
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogInformation(
                        "Text-based PDF extraction failed for {FileName}. Falling back to OCR. Reason: {Message}",
                        fileName,
                        ex.Message
                    );
                    var ocrText = await _ocrService.ExtractTextFromPdfPagesAsync(
                        fileStream,
                        cancellationToken
                    );
                    return new TextExtractionResult(true, ocrText, null);
                }
            }
            else if (fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            {
                var text = await ExtractTextFromDocxAsync(fileStream);
                return new TextExtractionResult(true, text, null);
            }
            else if (IsTextFile(fileName, contentType))
            {
                var text = await ExtractTextFromTxtAsync(fileStream, cancellationToken);
                return new TextExtractionResult(true, text, null);
            }
            else if (IsImageFile(fileName, contentType))
            {
                var text = await _ocrService.ExtractTextFromImageAsync(
                    fileStream,
                    cancellationToken
                );
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

        var extractedText = text.ToString();

        // Detect if this is a scanned PDF (insufficient extractable text)
        // If the document has pages but almost no text (average < 100 chars per page), treat as scanned.
        if (document.NumberOfPages > 0)
        {
            var textDensity = extractedText.Length / document.NumberOfPages;
            if (textDensity < 100)
            {
                throw new InvalidOperationException("This appears to be a scanned PDF.");
            }
        }

        return extractedText;
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

    private async Task<string> ExtractTextFromTxtAsync(
        Stream txtStream,
        CancellationToken cancellationToken
    )
    {
        using var reader = new StreamReader(txtStream, Encoding.UTF8, leaveOpen: true);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private bool IsTextFile(string fileName, string contentType)
    {
        var txtExtensions = new[] { ".txt", ".text", ".md", ".markdown" };
        return txtExtensions.Any(ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
            || contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsImageFile(string fileName, string contentType)
    {
        var imgExtensions = new[] { ".png", ".jpg", ".jpeg", ".tiff", ".bmp" };
        return imgExtensions.Any(ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
            || contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }
}
