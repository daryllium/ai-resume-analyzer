using AiResumeAnalyzer.Api.Contracts;
using AiResumeAnalyzer.Api.Options;
using AiResumeAnalyzer.Api.Requests;
using AiResumeAnalyzer.Api.Services;
using AiResumeAnalyzer.Api.Services.Interfaces;
using AiResumeAnalyzer.Tests.UnitTests;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace AiResumeAnalyzer.Tests.UnitTests;

public class AnalyzerTests
{
    private readonly Mock<IUploadFileExtractor> _mockUploadFileExtractor = new();
    private readonly Mock<ITextInputExtractor> _mockTextInputExtractor = new();
    private readonly Mock<IJobParser> _mockJobParser = new();
    private readonly Mock<IResumeParser> _mockResumeParser = new();
    private readonly Mock<IMatcher> _mockMatcher = new();
    private readonly Mock<ILogger<Analyzer>> _mockLogger = new();
    private readonly Analyzer _analyzer;

    public AnalyzerTests()
    {
        _analyzer = new Analyzer(
            _mockUploadFileExtractor.Object,
            _mockTextInputExtractor.Object,
            _mockJobParser.Object,
            _mockResumeParser.Object,
            _mockMatcher.Object,
            Options.Create(new AiModelOptions()),
            _mockLogger.Object
        );

        // Default setup for Matcher to return a dummy analysis
        _mockMatcher
            .Setup(x => x.MatchAsync(It.IsAny<JobProfile>(), It.IsAny<CandidateProfile>()))
            .ReturnsAsync(
                new AnalyzeResultItem(
                    SourceName: "Test Source",
                    Success: true,
                    Candidate: new CandidateProfile(
                        "Test Name",
                        "test@example.com",
                        new List<string>(),
                        0,
                        new List<string>(),
                        new List<string>(),
                        "Test summary"
                    ),
                    MatchScore: 100,
                    MatchLevel: "Expert",
                    MissingSkills: new List<string>(),
                    IsRecommended: true,
                    AnalysisSummary: "Great candidate"
                )
            );

        _mockJobParser
            .Setup(x => x.ParseJobDescriptionAsync(It.IsAny<string>()))
            .ReturnsAsync(new JobProfile("Title", [], [], 0, "Summary"));

        _mockResumeParser
            .Setup(x => x.ParseResumeAsync(It.IsAny<string>()))
            .ReturnsAsync(new CandidateProfile("Test", "test@test.com", [], 0, [], [], "Summary"));
    }

    [Fact]
    public async Task AnalyzeAsync_WithValidTextInputs_ReturnsValidResponse()
    {
        // Arrange
        var request = TestDataFactory.CreateAnalyzeRequestWithText();
        var expectedResponse = TestDataFactory.CreateSuccessfulTextExtraction();

        _mockTextInputExtractor
            .Setup(x => x.ExtractTextInputAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _analyzer.AnalyzeAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Meta);
        Assert.Equal(1, result.Meta.ProcessedResumes);

        _mockTextInputExtractor.Verify(
            x => x.ExtractTextInputAsync(It.IsAny<List<string>>()),
            Times.Once
        );
    }

    [Fact]
    public async Task AnalyzeAsync_WithValidUploadFiles_ReturnsSuccessfulResponse()
    {
        // Arrange
        var request = TestDataFactory.CreateAnalyzeRequestWithFiles();
        var expectedResponse = TestDataFactory.CreateSuccessfulFileExtraction();

        _mockUploadFileExtractor
            .Setup(x => x.ExtractFileAsync(It.IsAny<IEnumerable<IFormFile>>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _analyzer.AnalyzeAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Meta);
        Assert.Equal(1, result.Meta.ProcessedResumes);
        Assert.Equal(0, result.Meta.FailedResumes);

        _mockUploadFileExtractor.Verify(
            x => x.ExtractFileAsync(It.IsAny<IEnumerable<IFormFile>>()),
            Times.Once
        );
    }

    [Fact]
    public async Task AnalyzeAsync_WithEmptyInputs_ReturnsEmptyResponse()
    {
        // Arrange
        var request = TestDataFactory.CreateEmptyAnalyzeRequest();

        // Act
        var result = await _analyzer.AnalyzeAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Meta);
        Assert.Equal(0, result.Meta.ProcessedResumes);
        Assert.Empty(result.Results);
    }

    [Fact]
    public async Task AnalyzeAsync_WithFailedFileExtraction_ReturnsEmptyTextList()
    {
        // Arrange
        var request = TestDataFactory.CreateAnalyzeRequestWithFiles();
        var failedResponse = TestDataFactory.CreateFailedExtraction();

        _mockUploadFileExtractor
            .Setup(x => x.ExtractFileAsync(It.IsAny<IEnumerable<IFormFile>>()))
            .ReturnsAsync(failedResponse);

        // Act
        var result = await _analyzer.AnalyzeAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.Meta.ProcessedResumes);
        Assert.Empty(result.Results);

        _mockUploadFileExtractor.Verify(
            x => x.ExtractFileAsync(It.IsAny<IEnumerable<IFormFile>>()),
            Times.Once
        );
    }

    [Fact]
    public async Task AnalyzeAsync_WithFailedTextExtraction_ReturnsEmptyTextList()
    {
        // Arrange
        var request = TestDataFactory.CreateAnalyzeRequestWithText();
        var failedResponse = TestDataFactory.CreateFailedExtraction();

        _mockTextInputExtractor
            .Setup(x => x.ExtractTextInputAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(failedResponse);

        // Act
        var result = await _analyzer.AnalyzeAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.Meta.ProcessedResumes);
        Assert.Empty(result.Results);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task AnalyzeAsync_WithInvalidJobDescription_HandlesGracefully(
        string? jobDescription
    )
    {
        // Arrange
        var request = new AnalyzeRequest
        {
            JobDescription = jobDescription ?? "Default job description",
            UploadText = ["Sample text"],
        };

        var textExtraction = TestDataFactory.CreateSuccessfulTextExtraction();

        _mockTextInputExtractor
            .Setup(x => x.ExtractTextInputAsync(It.IsAny<List<string>>()))
            .ReturnsAsync(textExtraction);

        // Act
        var result = await _analyzer.AnalyzeAsync(request);

        // Assert
        Assert.NotNull(result);
        _mockTextInputExtractor.Verify(
            x => x.ExtractTextInputAsync(It.IsAny<List<string>>()),
            Times.Once
        );
    }
}
