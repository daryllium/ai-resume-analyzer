using AiResumeAnalyzer.Api.Contracts;
using AiResumeAnalyzer.Api.Services.Interfaces;

namespace AiResumeAnalyzer.Api.Services;

public sealed class ResumeParser(IAiModelClient aiModelClient) : IResumeParser
{
    private readonly IAiModelClient _aiModelClient = aiModelClient;

    private const string _systemPrompt = """
        You are an expert resume analyzer. Extract the candidate's details from the provided resume text.
        Return ONLY valid JSON that matches this structure. Do not include markdown code blocks or any other text.

        {
          "name": "string",
          "email": "string",
          "skills": ["string"],
          "yearsExperience": 0,
          "education": ["string"],
          "certifications": ["string"],
          "summary": "string"
        }
        """;

    public async Task<CandidateProfile> ParseResumeAsync(
        string resumeText,
        CancellationToken cancellationToken = default
    )
    {
        return await _aiModelClient.GenerateJsonResponseAsync<CandidateProfile>(
            resumeText,
            _systemPrompt,
            cancellationToken: cancellationToken
        );
    }
}
