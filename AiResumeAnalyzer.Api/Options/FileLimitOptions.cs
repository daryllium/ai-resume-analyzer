namespace AiResumeAnalyzer.Api.Options;

public sealed record FileLimitOptions
{
    public const string SectionName = "FileLimitOptions";

    public int MaxFileCount { get; init; } = 50;
    public int MaxTotalCandidates { get; init; } = 50;
    public long MaxFileSizeBytes { get; init; } = 10 * 1024 * 1024; // 10MB
    public long MaxTotalSizeBytes { get; init; } = 100 * 1024 * 1024; // 100MB
    public int MaxResumeTextLength { get; init; } = 10000; // ~2-3 pages
    public int MaxJobDescriptionLength { get; init; } = 20000;
    public int GlobalTimeoutSeconds { get; init; } = 300; // 5 minutes
    public string[] AllowedExtensions { get; init; } = [".pdf", ".docx", ".txt", ".png", ".jpg", ".jpeg", ".zip"];
}
