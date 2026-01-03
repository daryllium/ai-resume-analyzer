namespace AiResumeAnalyzer.Api.Options;

public sealed class ZipOptions
{
    public int MaxItems { get; init; } = 10;
    public long MaxEntryBytes { get; init; } = 10 * 1024 * 1024; // 10 MB
    public int MaxDepth { get; init; } = 5;
}
