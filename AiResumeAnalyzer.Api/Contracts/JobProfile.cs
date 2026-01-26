namespace AiResumeAnalyzer.Api.Contracts;

public sealed record JobProfile(
    string Title,
    List<string> RequiredSkills,
    List<string> PreferredSkills,
    int MinimumYearsExperience,
    string Summary
);
