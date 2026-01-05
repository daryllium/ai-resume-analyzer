using System.Net.Http.Headers;

namespace AiResumeAnalyzer.Tests.IntegrationTests;

public class AnalyzeEndpointTests(WebApplicationFactory factory)
    : IClassFixture<WebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly WebApplicationFactory _factory = factory;

    [Fact]
    public async Task AnalyzeWithTextInput_ReturnsSuccess()
    {
        // Arrange
        using var form = new MultipartFormDataContent();

        // Add job description
        form.Add(new StringContent("Senior Software Developer Position"), "JobDescription");

        // Add text resume
        form.Add(
            new StringContent(
                "John Doe\nSoftware Engineer\nExperience: 5 years\nSkills: C#, .NET, SQL"
            ),
            "UploadText"
        );

        // Act
        var response = await _client.PostAsync("/api/analyze", form);

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task AnalyzeWithMixedInput_ReturnsSuccess()
    {
        // Arrange
        using var form = new MultipartFormDataContent();

        // Add job description
        form.Add(new StringContent("Data Scientist Role"), "JobDescription");

        // Add file
        using var fileStream = File.OpenRead("TestData/sample.pdf");
        using var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(fileContent, "UploadFiles", "sample.pdf");

        // Add text
        form.Add(
            new StringContent(
                "Jane Smith\nData Analyst\nExperience: 3 years\nSkills: Python, R, SQL"
            ),
            "UploadText"
        );

        // Act
        var response = await _client.PostAsync("/api/analyze", form);

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task AnalyzeWithFiles_ReturnsSuccess()
    {
        // Arrange
        using var form = new MultipartFormDataContent();

        // Add job description
        form.Add(new StringContent("Backend Developer Position"), "JobDescription");

        // Add file
        using var fileStream = File.OpenRead("TestData/sample.docx");
        using var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        );
        form.Add(fileContent, "UploadFiles", "sample.docx");

        // Act
        var response = await _client.PostAsync("/api/analyze", form);

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task AnalyzeWithEmptyRequest_ReturnsSuccess()
    {
        // Arrange
        using var form = new MultipartFormDataContent();

        // Add only job description, no files or text
        form.Add(new StringContent("Software Developer Position"), "JobDescription");

        // Act
        var response = await _client.PostAsync("/api/analyze", form);

        // Assert
        response.EnsureSuccessStatusCode();
    }
}
