using System.Text.Json;

namespace AiResumeAnalyzer.Api.Services;

public sealed class AiModelClient(HttpClient httpClient, ILogger<AiModelClient> logger)
    : IAiModelClient
{
    private const string _modelName = "llama3.2";

    private readonly HttpClient _httpClient = httpClient;
    private readonly ILogger<AiModelClient> _logger = logger;
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false,
    };

    public async Task<T> GenerateJsonResponseAsync<T>(
        string prompt,
        string systemPrompt,
        string modelName = _modelName
    )
        where T : class
    {
        try { throw new NotImplementedException(); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating JSON response from AI model");
            throw;
        }
    }

    public async Task<string> GenerateTextResponseAsync(
        string prompt,
        string systemPrompt,
        string modelName = _modelName
    )
    {
        try { throw new NotImplementedException(); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating text response from AI model");
            throw;
        }
    }

    public bool IsValidJson(string json)
    {
        try
        {
            JsonDocument.Parse(json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public T? TryDeserializeJson<T>(string json)
        where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, _options);
        }
        catch
        {
            return null;
        }
    }
}
