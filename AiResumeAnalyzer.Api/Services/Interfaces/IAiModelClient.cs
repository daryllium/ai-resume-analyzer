using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace AiResumeAnalyzer.Api.Services;

public interface IAiModelClient
{
    Task<T> GenerateJsonResponseAsync<T>(string prompt, string systemPrompt, string modelName)
        where T : class;
    Task<string> GenerateTextResponseAsync(string prompt, string systemPrompt, string modelName);
    bool IsValidJson(string json);
    T? TryDeserializeJson<T>(string json)
        where T : class;
}
