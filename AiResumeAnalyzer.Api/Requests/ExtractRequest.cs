using Microsoft.AspNetCore.Http;

namespace AiResumeAnalyzer.Api.Requests;

public sealed class ExtractRequest
{
    public IFormFileCollection? ResumeFiles { get; init; }
    public IFormFile? ZipFile { get; init; }
    public List<string>? ResumeText { get; init; }
}
