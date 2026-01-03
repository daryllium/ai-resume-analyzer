using AiResumeAnalyzer.Api.Contracts;

namespace AiResumeAnalyzer.Api.Services;

public sealed class AnalyzerService : IAnalyzerService
{
    public AnalyzeResponse Analyze(string jobDescription)
    {
        var results = new List<AnalyzeResultItem>
        {
            new(
                Candidate: new CandidateProfile(
                    Name: "Jane Doe",
                    Email: "jane@email.com",
                    Skills: new List<string> { "C#", "ASP.NET", "SQL" },
                    MissingSkills: new List<string> { "Azure", "Docker" },
                    YearsExperience: 5,
                    QualificationSummary: "BSc in Computer Science"
                ),
                MatchScore: 92,
                MatchLevel: "strong",
                IsRecommended: true,
                AnalysisSummary: "Candidate has strong experience in required skills."
            ),
        };

        return new AnalyzeResponse(results, new AnalyzeMeta(results.Count, 0));
    }
}
