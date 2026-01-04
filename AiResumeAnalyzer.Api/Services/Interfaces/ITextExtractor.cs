namespace AiResumeAnalyzer.Api.Services;

public interface ITextExtractor
{
    Task<TextExtractionResult> ExtractTextAsync(
        Stream filestream,
        string fileName,
        string contentType
    );
}

public record TextExtractionResult(bool Success, string? ExtractedText, string? ErrorMessage);
