namespace AiResumeAnalyzer.Api.Options;

public sealed record AiModelOptions
{
    public const string SectionName = "AiModel";
    public string ModelName { get; init; } = "llama3.2";
    public int MaxConcurrency { get; init; } = 3;
    public int TimeoutSeconds { get; init; } = 120;
}
