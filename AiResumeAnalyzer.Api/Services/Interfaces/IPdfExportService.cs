using AiResumeAnalyzer.Api.Contracts;

namespace AiResumeAnalyzer.Api.Services.Interfaces;

public interface IPdfExportService
{
    Task<byte[]> ExportToPdfAsync(AnalyzeResponse results, CancellationToken cancellationToken = default);
}
