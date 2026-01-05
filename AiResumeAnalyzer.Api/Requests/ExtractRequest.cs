namespace AiResumeAnalyzer.Api.Requests;

public sealed class ExtractRequest
{
    public IFormFileCollection? Files { get; init; }
    public List<string>? ResumeText { get; init; }
}
