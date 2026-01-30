namespace AiResumeAnalyzer.Api.Options;

public sealed record OcrOptions
{
    public const string SectionName = "OcrOptions";

    public int TimeoutSeconds { get; init; } = 30;
    public string Language { get; init; } = "eng";
}
