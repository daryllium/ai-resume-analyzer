using AiResumeAnalyzer.Api.Contracts;

namespace AiResumeAnalyzer.Api.Services;

public interface IUploadFileExtractor
{
    Task<ExtractResponse> ExtractFileAsync(IEnumerable<IFormFile> files);
}
