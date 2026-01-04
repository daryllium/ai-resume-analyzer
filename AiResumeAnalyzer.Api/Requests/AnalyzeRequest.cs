using Microsoft.AspNetCore.Http;

namespace AiResumeAnalyzer.Api.Requests;

public sealed class AnalyzeRequest
{
    public required string JobDescription { get; init; }
    public IFormFileCollection? UploadFiles { get; init; }
    public List<string>? UploadText { get; init; }
}
