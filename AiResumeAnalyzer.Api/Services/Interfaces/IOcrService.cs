using AiResumeAnalyzer.Api.Contracts;

namespace AiResumeAnalyzer.Api.Services.Interfaces;

public interface IOcrService
{
    Task<string> ExtractTextFromImageAsync(
        Stream imageStream,
        CancellationToken cancellationToken = default
    );
    Task<string> ExtractTextFromPdfPagesAsync(
        Stream pdfStream,
        CancellationToken cancellationToken = default
    );
}
