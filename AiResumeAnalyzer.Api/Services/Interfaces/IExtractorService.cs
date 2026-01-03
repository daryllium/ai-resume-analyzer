using AiResumeAnalyzer.Api.Contracts;
using AiResumeAnalyzer.Api.Requests;

namespace AiResumeAnalyzer.Api.Services;

public interface IExtractorService
{
    ExtractResponse Extract(ExtractRequest req);
}
