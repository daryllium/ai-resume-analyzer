namespace AiResumeAnalyzer.Api.Contracts;

public sealed record AnalyzeResponse(List<AnalyzeResultItem> Results, AnalyzeMeta Meta);

public sealed record AnalyzeResultItem(
    CandidateProfile Candidate,
    int MatchScore,
    string MatchLevel,
    List<string> MissingSkills,
    bool IsRecommended,
    string AnalysisSummary
);

public sealed record AnalyzeMeta(int ProcessedResumes, int FailedResumes);
