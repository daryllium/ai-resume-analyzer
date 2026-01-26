using AiResumeAnalyzer.Api.Contracts;

namespace AiResumeAnalyzer.Api.Services.Interfaces;

public interface IJobParser
{
    Task<JobProfile> ParseJobDescriptionAsync(
        string jobDescription,
        CancellationToken cancellationToken = default
    );
}

public interface IResumeParser
{
    Task<CandidateProfile> ParseResumeAsync(
        string resumeText,
        CancellationToken cancellationToken = default
    );
}

public interface IMatcher
{
    Task<AnalyzeResultItem> MatchAsync(
        JobProfile job,
        CandidateProfile candidate,
        CancellationToken cancellationToken = default
    );
}
