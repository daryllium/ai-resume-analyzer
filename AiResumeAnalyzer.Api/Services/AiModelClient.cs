using System.Net.Http.Json;
using System.Text.Json;
using AiResumeAnalyzer.Api.Exceptions;
using AiResumeAnalyzer.Api.Options;
using Microsoft.Extensions.Options;

namespace AiResumeAnalyzer.Api.Services;

public sealed class AiModelClient(
    HttpClient httpClient,
    IOptions<AiModelOptions> aiOptions,
    ILogger<AiModelClient> logger
) : IAiModelClient
{
    private readonly AiModelOptions _aiOptions = aiOptions.Value;

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
        string? modelName = null,
        CancellationToken cancellationToken = default
    )
        where T : class
    {
        modelName ??= _aiOptions.ModelName;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(_aiOptions.TimeoutSeconds));

        try
        {
            try
            {
                return await SendRequestAsync<T>(prompt, systemPrompt, modelName, cts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new AiModelException(
                    $"AI model request timed out after {_aiOptions.TimeoutSeconds} seconds."
                );
            }
            catch (Exception ex) when (ex is JsonException || ex is AiModelException)
            {
                _logger.LogWarning(
                    ex,
                    "First AI attempt failed or returned invalid JSON. Retrying with correction prompt..."
                );

                var correctionPrompt = $"""
                    Your previous response was invalid. 
                    Error: {ex.Message}

                    Please try again and provide ONLY the corrected, valid JSON.

                    Original Context:
                    {prompt}
                    """;

                try
                {
                    return await SendRequestAsync<T>(
                        correctionPrompt,
                        systemPrompt,
                        modelName,
                        cts.Token
                    );
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new AiModelException(
                        $"AI model request timed out during retry after {_aiOptions.TimeoutSeconds} seconds."
                    );
                }
                catch (Exception retryEx)
                {
                    _logger.LogError(retryEx, "AI retry attempt also failed.");
                    throw new AiModelException(
                        "AI model failed to provide valid JSON after a retry attempt.",
                        retryEx
                    );
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("AI request was cancelled by the user.");
            throw;
        }
    }

    private async Task<T> SendRequestAsync<T>(
        string prompt,
        string systemPrompt,
        string modelName,
        CancellationToken cancellationToken
    )
        where T : class
    {
        var request = new OllamaRequest(modelName, prompt, systemPrompt, false, "json");
        var response = await _httpClient.PostAsJsonAsync(
            "/api/generate",
            request,
            cancellationToken
        );

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new AiModelException(
                $"AI model request failed with status {response.StatusCode}: {errorContent}"
            );
        }

        var result = await response.Content.ReadFromJsonAsync<OllamaResponse>(
            _options,
            cancellationToken
        );
        if (result == null || string.IsNullOrEmpty(result.Response))
        {
            throw new AiModelException("AI model returned an empty or null response");
        }

        try
        {
            return JsonSerializer.Deserialize<T>(result.Response, _options)
                ?? throw new AiModelException(
                    "Failed to deserialize AI response to the expected JSON format"
                );
        }
        catch (JsonException ex)
        {
            _logger.LogDebug(ex, "Failed to parse AI response as JSON.");
            throw;
        }
    }

    public async Task<string> GenerateTextResponseAsync(
        string prompt,
        string systemPrompt,
        string? modelName = null,
        CancellationToken cancellationToken = default
    )
    {
        modelName ??= _aiOptions.ModelName;
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(_aiOptions.TimeoutSeconds));

        try
        {
            var request = new OllamaRequest(modelName, prompt, systemPrompt, false);
            var response = await _httpClient.PostAsJsonAsync("/api/generate", request, cts.Token);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OllamaResponse>(
                _options,
                cts.Token
            );
            return result?.Response ?? string.Empty;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AiModelException(
                $"AI model request timed out after {_aiOptions.TimeoutSeconds} seconds."
            );
        }
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

    private record OllamaRequest(
        string Model,
        string Prompt,
        string System,
        bool Stream,
        string? Format = null
    );

    private record OllamaResponse(string Response, bool Done);
}
