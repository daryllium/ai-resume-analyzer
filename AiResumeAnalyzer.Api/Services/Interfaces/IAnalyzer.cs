using AiResumeAnalyzer.Api.Contracts;
using AiResumeAnalyzer.Api.Requests;

namespace AiResumeAnalyzer.Api.Services;

public interface IAnalyzer
{
    Task<AnalyzeResponse> AnalyzeAsync(
        AnalyzeRequest request,
        CancellationToken cancellationToken = default
    );
}
