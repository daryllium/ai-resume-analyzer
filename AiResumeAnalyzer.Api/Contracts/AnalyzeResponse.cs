namespace AiResumeAnalyzer.Api.Contracts;

public sealed record AnalyzeResponse(List<AnalyzeResultItem> Results, AnalyzeMeta Meta);

public sealed record AnalyzeResultItem(
    CandidateProfile Candidate,
    int MatchScore,
    string MatchLevel,
    bool IsRecommended,
    string AnalysisSummary
);

public sealed record CandidateProfile(
    string? Name,
    string? Email,
    List<string> Skills,
    List<string> MissingSkills,
    double? YearsExperience,
    string? QualificationSummary
);

public sealed record AnalyzeMeta(int ProcessedResumes, int FailedResumes);
