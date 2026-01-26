using AiResumeAnalyzer.Api.Contracts;
using AiResumeAnalyzer.Api.Services.Interfaces;

namespace AiResumeAnalyzer.Api.Services;

public sealed class JobParser(IAiModelClient aiModelClient) : IJobParser
{
    private readonly IAiModelClient _aiModelClient = aiModelClient;

    private const string _systemPrompt = """
        You are a technical recruiting assistant. Parse the provided job description text into a structured JSON format.
        Return ONLY valid JSON that matches this structure. Do not include markdown code blocks or any other text.

        {
          "title": "string",
          "requiredSkills": ["string"],
          "preferredSkills": ["string"],
          "minimumYearsExperience": 0,
          "summary": "string"
        }
        """;

    public async Task<JobProfile> ParseJobDescriptionAsync(
        string jobDescription,
        CancellationToken cancellationToken = default
    )
    {
        return await _aiModelClient.GenerateJsonResponseAsync<JobProfile>(
            jobDescription,
            _systemPrompt,
            cancellationToken: cancellationToken
        );
    }
}
