using AiResumeAnalyzer.Api.Contracts;

namespace AiResumeAnalyzer.Api.Services;

public interface IAnalyzerService
{
    AnalyzeResponse Analyze(string jobDescription);
}
