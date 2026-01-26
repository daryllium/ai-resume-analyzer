using System.Net;
using System.Net.Http.Json;
using AiResumeAnalyzer.Api.Exceptions;
using AiResumeAnalyzer.Api.Options;
using AiResumeAnalyzer.Api.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace AiResumeAnalyzer.Tests.UnitTests;

public class AiModelClientTests
{
    private readonly Mock<HttpMessageHandler> _mockHandler = new();
    private readonly HttpClient _httpClient;
    private readonly Mock<ILogger<AiModelClient>> _mockLogger = new();
    private readonly IOptions<AiModelOptions> _options;

    public AiModelClientTests()
    {
        _httpClient = new HttpClient(_mockHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:11434"),
        };

        _options = Options.Create(new AiModelOptions { ModelName = "test-model" });
    }

    [Fact]
    public async Task GenerateJsonResponseAsync_WhenFirstAttemptFails_RetriesAndSucceeds()
    {
        // Arrange
        var client = new AiModelClient(_httpClient, _options, _mockLogger.Object);
        var systemPrompt = "sys";
        var prompt = "hello";

        var invalidJsonResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = JsonContent.Create(new { response = "not valid json", done = true }),
        };

        var validJsonResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = JsonContent.Create(new { response = "{\"name\":\"test\"}", done = true }),
        };

        _mockHandler
            .Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(invalidJsonResponse)
            .ReturnsAsync(validJsonResponse);

        // Act
        var result = await client.GenerateJsonResponseAsync<TestResult>(prompt, systemPrompt);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test", result.Name);

        _mockHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Exactly(2),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            );
    }

    [Fact]
    public async Task GenerateJsonResponseAsync_WhenBothAttemptsFail_ThrowsAiModelException()
    {
        // Arrange
        var client = new AiModelClient(_httpClient, _options, _mockLogger.Object);

        var invalidJsonResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = JsonContent.Create(new { response = "invalid", done = true }),
        };

        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(invalidJsonResponse);

        // Act & Assert
        await Assert.ThrowsAsync<AiModelException>(() =>
            client.GenerateJsonResponseAsync<TestResult>("prompt", "sys")
        );
    }

    private class TestResult
    {
        public string Name { get; set; } = string.Empty;
    }
}
