namespace AiResumeAnalyzer.Api.Contracts;

public sealed record ExtractResponse(List<ExtractItemResult> Items, ExtractMeta Meta);

public sealed record ExtractItemResult(
    string SourceType,
    string SourceName,
    bool Success,
    string? ExtractedText,
    string? Error
);

public sealed record ExtractMeta(int TotalItems, int SuccessCount, int FailedCount);
