using AiResumeAnalyzer.Api.Contracts;
using AiResumeAnalyzer.Api.Requests;

namespace AiResumeAnalyzer.Api.Services;

public interface IZipExtractor
{
    Task<ExtractResponse> Extract(ExtractRequest req);
}
