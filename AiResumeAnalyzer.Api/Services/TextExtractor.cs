using System.Text;

namespace AiResumeAnalyzer.Api.Services;

public sealed class TextExtractor(
    IPdfExtractor pdfExtractor,
    IDocxExtractor docxExtractor,
    ILogger<TextExtractor> logger
) : ITextExtractor
{
    private readonly IPdfExtractor _pdfExtractor = pdfExtractor;
    private readonly IDocxExtractor _docxExtractor = docxExtractor;
    private readonly ILogger<TextExtractor> logger = logger;

    public async Task<TextExtractionResult> ExtractTextAsync(
        Stream fileStream,
        string fileName,
        string contentType
    )
    {
        try
        {
            if (fileStream.CanSeek)
                fileStream.Position = 0;

            if (_pdfExtractor.CanHandleFile(fileName, contentType))
            {
                var text = await _pdfExtractor.ExtractTextFromPdfAsync(fileStream);
                return new TextExtractionResult(true, text, null);
            }
            else if (_docxExtractor.CanHandleFile(fileName, contentType))
            {
                var text = await _docxExtractor.ExtractTextFromDocxAsync(fileStream);
                return new TextExtractionResult(true, text, null);
            }
            else if (IsTextFile(fileName, contentType))
            {
                var text = await ExtractTextFromTextFileAsync(fileStream);
                return new TextExtractionResult(true, text, null);
            }

            return new TextExtractionResult(false, null, $"Unsupported file type: {fileName}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error extracting text from file {FileName}", fileName);
            return new TextExtractionResult(false, null, $"Error extracting text: {ex.Message}");
        }
    }

    private async Task<string> ExtractTextFromTextFileAsync(Stream stream)
    {
        using var reader = new StreamReader(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true,
            leaveOpen: true
        );
        return await reader.ReadToEndAsync();
    }

    private bool IsTextFile(string fileName, string contentType)
    {
        var textExtensions = new[] { ".txt", ".text", ".md", ".markdown" };
        return textExtensions.Any(ext => fileName.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
            || contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase);
    }
}
