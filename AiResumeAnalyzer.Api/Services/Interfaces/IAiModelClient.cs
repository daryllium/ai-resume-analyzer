namespace AiResumeAnalyzer.Api.Services;

public interface IAiModelClient
{
    Task<T> GenerateJsonResponseAsync<T>(
        string prompt,
        string systemPrompt,
        string? modelName = null,
        CancellationToken cancellationToken = default
    )
        where T : class;
    Task<string> GenerateTextResponseAsync(
        string prompt,
        string systemPrompt,
        string? modelName = null,
        CancellationToken cancellationToken = default
    );
    bool IsValidJson(string json);
    T? TryDeserializeJson<T>(string json)
        where T : class;
}
