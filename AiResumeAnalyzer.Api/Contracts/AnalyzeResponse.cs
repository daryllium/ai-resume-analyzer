namespace AiResumeAnalyzer.Api.Contracts;

public sealed record AnalyzeResponse(List<AnalyzeResultItem> Results, AnalyzeMeta Meta);

public sealed record AnalyzeResultItem(
    string SourceName,
    bool Success,
    CandidateProfile? Candidate = null,
    int? MatchScore = null,
    string? MatchLevel = null,
    List<string>? MissingSkills = null,
    bool? IsRecommended = null,
    string? AnalysisSummary = null,
    string? Error = null
);

public sealed record AnalyzeMeta(int ProcessedResumes, int FailedResumes);
