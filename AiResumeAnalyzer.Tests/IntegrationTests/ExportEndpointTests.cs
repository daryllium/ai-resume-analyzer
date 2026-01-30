using System.Net;
using System.Net.Http.Json;
using AiResumeAnalyzer.Api.Contracts;

namespace AiResumeAnalyzer.Tests.IntegrationTests;

public class ExportEndpointTests(WebApplicationFactory factory)
    : IClassFixture<WebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task ExportPdf_WithValidData_ReturnsPdfFile()
    {
        // Arrange
        var results = new AnalyzeResponse(
            Results: new List<AnalyzeResultItem>
            {
                new AnalyzeResultItem(
                    SourceName: "test-resume.pdf",
                    Success: true,
                    Candidate: new CandidateProfile(
                        Name: "Jane Doe",
                        Email: "jane@doe.com",
                        Skills: new List<string> { "C#", ".NET", "Azure" },
                        YearsExperience: 5,
                        Education: new List<string> { "BS Computer Science" },
                        Certifications: new List<string> { "AZ-204" },
                        Summary: "Experienced developer"
                    ),
                    MatchScore: 85,
                    MatchLevel: "Strong Match",
                    MissingSkills: new List<string> { "Kubernetes" },
                    IsRecommended: true,
                    AnalysisSummary: "Great fit for the role."
                )
            },
            Meta: new AnalyzeMeta(1, 0)
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/export/pdf", results);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
        
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.NotEmpty(bytes);
        
        // PDF header check (%PDF-)
        Assert.Equal(0x25, bytes[0]);
        Assert.Equal(0x50, bytes[1]);
        Assert.Equal(0x44, bytes[2]);
        Assert.Equal(0x46, bytes[3]);
    }

    [Fact]
    public async Task ExportPdf_WithEmptyResults_ReturnsPdfFile()
    {
        // Arrange
        var results = new AnalyzeResponse(
            Results: new List<AnalyzeResultItem>(),
            Meta: new AnalyzeMeta(0, 0)
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/export/pdf", results);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
    }
}
