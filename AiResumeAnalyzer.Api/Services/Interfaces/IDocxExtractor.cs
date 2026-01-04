namespace AiResumeAnalyzer.Api.Services;

public interface IDocxExtractor
{
    Task<string?> ExtractTextFromDocxAsync(Stream docxStream);
    bool CanHandleFile(string fileName, string contentType);
}
