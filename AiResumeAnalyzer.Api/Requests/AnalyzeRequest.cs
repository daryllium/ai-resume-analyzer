using Microsoft.AspNetCore.Http;

namespace AiResumeAnalyzer.Api.Requests;

public sealed class AnalyzeRequest
{
    public required string JobDescription { get; init; }
    public IFormFileCollection? ResumeFiles { get; init; }
    public IFormFile? ZipFile { get; init; }
    public List<string>? ResumeText { get; init; }
}
