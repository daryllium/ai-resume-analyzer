namespace AiResumeAnalyzer.Api.Options;

public sealed record FileLimitOptions
{
    public const string SectionName = "FileLimitOptions";

    public int MaxFileCount { get; init; } = 10;
    public long MaxFileSizeBytes { get; init; } = 10 * 1024 * 1024; // 10MB
    public long MaxTotalSizeBytes { get; init; } = 50 * 1024 * 1024; // 50MB
    public string[] AllowedExtensions { get; init; } = [".pdf", ".docx", ".txt"];
}
