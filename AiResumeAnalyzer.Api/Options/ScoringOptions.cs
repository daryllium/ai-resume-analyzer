namespace AiResumeAnalyzer.Api.Options;

public sealed record ScoringOptions
{
    public const string SectionName = "Scoring";

    public int StrongYesThreshold { get; init; } = 85;
    public int YesThreshold { get; init; } = 70;
    public int MaybeThreshold { get; init; } = 55;
}
