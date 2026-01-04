using AiResumeAnalyzer.Api.Contracts;

namespace AiResumeAnalyzer.Api.Services;

public interface ITextInputExtractor
{
    Task<ExtractResponse> ExtractTextInputAsync(List<string> textInputs);
}
