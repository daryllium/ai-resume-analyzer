using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using AiResumeAnalyzer.Api.Contracts;

namespace AiResumeAnalyzer.Tests.IntegrationTests;

public class ExtractEndpointTests(WebApplicationFactory factory)
    : IClassFixture<WebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly WebApplicationFactory _factory = factory;

    [Fact]
    public async Task HealthCheck_ReturnsHelloWorld()
    {
        // Arrange & Act
        var response = await _client.GetAsync("/");

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal("Hello World!", content);
    }

    [Fact]
    public async Task ExtractTxtFile_ReturnsSuccess()
    {
        // Arrange
        using var form = new MultipartFormDataContent();
        using var fileStream = File.OpenRead("TestData/sample.txt");
        using var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        form.Add(fileContent, "files", "sample.txt");

        // Act
        var response = await _client.PostAsync("/api/extract", form);

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task ExtractDocxFile_ReturnsSuccess()
    {
        // Arrange
        using var form = new MultipartFormDataContent();
        using var fileStream = File.OpenRead("TestData/sample.docx");
        using var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        );
        form.Add(fileContent, "files", "sample.docx");

        // Act
        var response = await _client.PostAsync("/api/extract", form);

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task ExtractPdfFile_ReturnsSuccess()
    {
        // Arrange
        using var form = new MultipartFormDataContent();
        using var fileStream = File.OpenRead("TestData/sample.pdf");
        using var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(fileContent, "files", "sample.pdf");

        // Act
        var response = await _client.PostAsync("/api/extract", form);

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task ExtractZipFile_ReturnsSuccess()
    {
        // Arrange
        using var form = new MultipartFormDataContent();
        using var fileStream = File.OpenRead("TestData/sample.zip");
        using var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        form.Add(fileContent, "files", "sample.zip");

        // Act
        var response = await _client.PostAsync("/api/extract", form);

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task ExtractNestedZipFile_ReturnsSuccess()
    {
        // Arrange
        using var form = new MultipartFormDataContent();
        using var fileStream = File.OpenRead("TestData/sample.zip");
        using var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        form.Add(fileContent, "files", "sample-nested.zip");

        // Act
        var response = await _client.PostAsync("/api/extract", form);

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task ExtractMultipleFiles_ReturnsSuccess()
    {
        // Arrange
        using var form = new MultipartFormDataContent();

        // Add PDF
        using var pdfStream = File.OpenRead("TestData/sample.pdf");
        using var pdfContent = new StreamContent(pdfStream);
        pdfContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(pdfContent, "files", "sample.pdf");

        // Add TXT
        using var txtStream = File.OpenRead("TestData/sample.txt");
        using var txtContent = new StreamContent(txtStream);
        txtContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        form.Add(txtContent, "files", "sample.txt");

        // Act
        var response = await _client.PostAsync("/api/extract", form);

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task ExtractUnsupportedFile_ReturnsSuccess()
    {
        // Arrange
        using var form = new MultipartFormDataContent();
        using var fileStream = File.OpenRead("TestData/sample.xyz");
        using var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        form.Add(fileContent, "files", "sample.xyz");

        // Act
        var response = await _client.PostAsync("/api/extract", form);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ExtractWithNoFiles_ReturnsSuccess()
    {
        // Arrange
        using var form = new MultipartFormDataContent();

        // Add a dummy field to make the form valid
        form.Add(new StringContent(""), "dummy");

        // Act
        var response = await _client.PostAsync("/api/extract", form);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var extractResponse = JsonSerializer.Deserialize<ExtractResponse>(
            content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        Assert.NotNull(extractResponse);
        Assert.NotNull(extractResponse.Items);
        Assert.Empty(extractResponse.Items);
        Assert.Equal(0, extractResponse.Meta.TotalItems);
        Assert.Equal(0, extractResponse.Meta.SuccessCount);
        Assert.Equal(0, extractResponse.Meta.FailedCount);
    }
}
