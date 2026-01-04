namespace AiResumeAnalyzer.Api.Services;

public interface IPdfExtractor
{
    Task<string?> ExtractTextFromPdfAsync(Stream pdfStream);
    bool CanHandleFile(string fileName, string contentType);
}
