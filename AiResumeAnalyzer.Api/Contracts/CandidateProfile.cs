namespace AiResumeAnalyzer.Api.Contracts;

public sealed record CandidateProfile(
    string? Name,
    string? Email,
    List<string> Skills,
    int? YearsExperience,
    List<string> Education,
    List<string> Certifications,
    string? Summary
);
