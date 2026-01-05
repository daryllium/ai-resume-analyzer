namespace AiResumeAnalyzer.Api.Contracts;

public sealed record ExtractResponse(List<ExtractItemResult> Items, ExtractMeta Meta);

public sealed record ExtractItemResult(
    string SourceType,
    string SourceName,
    bool Success,
    string? ExtractedText,
    string? Error,
    int? TextLength
);

public sealed record TextExtractionResult(
    bool Success,
    string? ExtractedText,
    string? ErrorMessage
);

public sealed record ExtractMeta(int TotalItems, int SuccessCount, int FailedCount);
