using System.Net.Http.Headers;
using System.Net.Http.Json;
using AiResumeAnalyzer.Api.Contracts;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AiResumeAnalyzer.Tests;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ExtractApi_WithTxtFile_ReturnsExtractedContent()
    {
        // Ensure test data exists
        var testDataDir = Path.Combine(AppContext.BaseDirectory, "TestData");
        Assert.True(
            Directory.Exists(testDataDir),
            $"TestData directory not found at {testDataDir}"
        );

        var txtPath = Path.Combine(testDataDir, "sample.txt");
        Assert.True(File.Exists(txtPath), $"Test file not found at {txtPath}");

        await using var txtStream = File.OpenRead(txtPath);

        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(txtStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "resumeFiles", "sample.txt");

        var response = await _client.PostAsync("/api/extract", content);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ExtractResponse>();

        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.True(result.Items[0].Success);
        Assert.NotNull(result.Items[0].ExtractedText);
        Assert.Contains("Jane Doe", result.Items[0].ExtractedText);
        Assert.Contains("C#", result.Items[0].ExtractedText);
    }

    [Fact]
    public async Task ExtractApi_WithZipFile_ReturnsMultipleExtractedContents()
    {
        var zipPath = Path.Combine(AppContext.BaseDirectory, "TestData", "sample.zip");
        Assert.True(File.Exists(zipPath), $"ZIP file not found at {zipPath}");

        await using var zipStream = File.OpenRead(zipPath);

        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(zipStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        content.Add(fileContent, "zipFile", "sample.zip");

        var response = await _client.PostAsync("/api/extract", content);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ExtractResponse>();

        Assert.NotNull(result);
        Assert.Equal(3, result.Items.Count);
        Assert.All(result.Items, item => Assert.True(item.Success));
        Assert.All(result.Items, item => Assert.NotNull(item.ExtractedText));

        var txtItem = result.Items.First(item => item.SourceName.Contains("sample.txt"));
        var pdfItem = result.Items.First(item => item.SourceName.Contains("sample.pdf"));
        var docxItem = result.Items.First(item => item.SourceName.Contains("sample.docx"));

        Assert.Contains("Jane Doe", txtItem.ExtractedText);
        Assert.NotEmpty(pdfItem.ExtractedText);
        Assert.NotEmpty(docxItem.ExtractedText);
    }

    [Fact]
    public async Task ExtractApi_WithNestedZipFile_ProcessesAllNestedFiles()
    {
        var zipPath = Path.Combine(AppContext.BaseDirectory, "TestData", "sample-nested.zip");
        Assert.True(File.Exists(zipPath), $"Nested ZIP file not found at {zipPath}");

        await using var zipStream = File.OpenRead(zipPath);

        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(zipStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        content.Add(fileContent, "zipFile", "sample-nested.zip");

        var response = await _client.PostAsync("/api/extract", content);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ExtractResponse>();

        Assert.NotNull(result);
        Assert.Equal(6, result.Items.Count);
        Assert.All(result.Items, item => Assert.True(item.Success));

        var fileNames = result.Items.Select(item => item.SourceName).ToList();
        Assert.Contains("sample-nested.zip:sample.docx", fileNames);
        Assert.Contains("sample-nested.zip:nested/sample.pdf", fileNames);
        Assert.Contains("sample-nested.zip:nested/docx/sample.docx", fileNames);
    }

    [Fact]
    public async Task ExtractApi_WithMixedFilesAndZip_ProcessesAll()
    {
        // Arrange
        var txtPath = Path.Combine(AppContext.BaseDirectory, "TestData", "sample.txt");
        var zipPath = Path.Combine(AppContext.BaseDirectory, "TestData", "sample.zip");

        Assert.True(File.Exists(txtPath), $"TXT file not found at {txtPath}");
        Assert.True(File.Exists(zipPath), $"ZIP file not found at {zipPath}");

        await using var txtStream = File.OpenRead(txtPath);
        await using var zipStream = File.OpenRead(zipPath);

        using var content = new MultipartFormDataContent();

        // Add individual file
        var txtContent = new StreamContent(txtStream);
        txtContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(txtContent, "resumeFiles", "sample.txt");

        // Add ZIP file
        var zipContent = new StreamContent(zipStream);
        zipContent.Headers.ContentType = new MediaTypeHeaderValue("application/zip");
        content.Add(zipContent, "zipFile", "sample.zip");

        // Act
        var response = await _client.PostAsync("/api/extract", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ExtractResponse>();

        Assert.NotNull(result);
        Assert.Equal(4, result.Items.Count); // 1 TXT + 3 from ZIP
        Assert.All(result.Items, item => Assert.True(item.Success));
    }

    [Fact]
    public async Task ExtractApi_WithUnsupportedFile_ReturnsError()
    {
        // Arrange
        using var content = new MultipartFormDataContent();

        // Create a mock unsupported file
        var unsupportedContent = new StringContent("fake executable content");
        unsupportedContent.Headers.ContentType = new MediaTypeHeaderValue(
            "application/octet-stream"
        );
        content.Add(unsupportedContent, "resumeFiles", "malware.exe");

        // Act
        var response = await _client.PostAsync("/api/extract", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ExtractResponse>();

        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.False(result.Items[0].Success);
        Assert.Contains("Unsupported file type", result.Items[0].Error);
    }
}
