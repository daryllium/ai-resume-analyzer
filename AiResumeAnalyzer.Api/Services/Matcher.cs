using System.Text.Json;
using AiResumeAnalyzer.Api.Contracts;
using AiResumeAnalyzer.Api.Options;
using AiResumeAnalyzer.Api.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace AiResumeAnalyzer.Api.Services;

public sealed class Matcher(IAiModelClient aiModelClient, IOptions<ScoringOptions> scoringOptions)
    : IMatcher
{
    private readonly IAiModelClient _aiModelClient = aiModelClient;
    private readonly ScoringOptions _scoringOptions = scoringOptions.Value;

    private const string _systemPrompt = """
        You are a senior technical recruiter. Compare the candidate's profile against the job requirements.
        Evaluate the fit based on skills, experience, and background.

        Return ONLY valid JSON that matches this structure. Do not include markdown code blocks or any other text.

        {
          "matchScore": 0,
          "missingSkills": ["string"],
          "analysisSummary": "string"
        }

        The matchScore should be a careful, objective integer between 0 and 100.
        Provide a concise analysisSummary explaining the score.
        """;

    public async Task<AnalyzeResultItem> MatchAsync(
        JobProfile job,
        CandidateProfile candidate,
        CancellationToken cancellationToken = default
    )
    {
        var comparisonInput = new { JobRequirement = job, Candidate = candidate };
        var prompt = JsonSerializer.Serialize(comparisonInput);

        var matchResult = await _aiModelClient.GenerateJsonResponseAsync<MatchResultStub>(
            prompt,
            _systemPrompt,
            cancellationToken: cancellationToken
        );

        var (level, recommended) = CalculateRecommendation(matchResult.MatchScore);

        return new AnalyzeResultItem(
            SourceName: string.Empty,
            Success: true,
            Candidate: candidate,
            MatchScore: matchResult.MatchScore,
            MatchLevel: level,
            MissingSkills: matchResult.MissingSkills,
            IsRecommended: recommended,
            AnalysisSummary: matchResult.AnalysisSummary
        );
    }

    private (string Level, bool Recommended) CalculateRecommendation(int score)
    {
        if (score >= _scoringOptions.StrongYesThreshold)
            return ("strong_yes", true);
        if (score >= _scoringOptions.YesThreshold)
            return ("yes", true);
        if (score >= _scoringOptions.MaybeThreshold)
            return ("maybe", false);
        return ("no", false);
    }

    private record MatchResultStub(
        int MatchScore,
        List<string> MissingSkills,
        string AnalysisSummary
    );
}
