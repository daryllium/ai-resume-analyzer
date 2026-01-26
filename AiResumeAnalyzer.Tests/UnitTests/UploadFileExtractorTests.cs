using AiResumeAnalyzer.Api.Options;
using AiResumeAnalyzer.Api.Services;
using AiResumeAnalyzer.Api.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;

namespace AiResumeAnalyzer.Tests.UnitTests;

public class UploadFileExtractorTests
{
    private readonly Mock<IFileTextExtractor> _mockFileTextExtractor = new();
    private readonly UploadFileExtractor _uploadFileExtractor;

    public UploadFileExtractorTests()
    {
        _uploadFileExtractor = new UploadFileExtractor(
            _mockFileTextExtractor.Object,
            Options.Create(new FileLimitOptions()),
            Options.Create(new ZipOptions())
        );
    }

    [Fact]
    public async Task ExtractFileAsync_WithValidPdfFile_ReturnsSuccess()
    {
        // Arrange
        var files = CreateMockFormFileCollection("test.pdf", "application/pdf");
        var expectedResult = TestDataFactory.CreateSuccessfulTextExtractionResult();

        _mockFileTextExtractor
            .Setup(x => x.ExtractFileTextAsync(It.IsAny<Stream>(), "test.pdf", "application/pdf"))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _uploadFileExtractor.ExtractFileAsync(files);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.True(result.Items[0].Success);
        Assert.Equal("test.pdf", result.Items[0].SourceName);
        Assert.Equal(1, result.Meta.SuccessCount);
        Assert.Equal(0, result.Meta.FailedCount);

        _mockFileTextExtractor.Verify(
            x => x.ExtractFileTextAsync(It.IsAny<Stream>(), "test.pdf", "application/pdf"),
            Times.Once
        );
    }

    [Fact]
    public async Task ExtractFileAsync_WithValidDocxFile_ReturnsSuccess()
    {
        // Arrange
        var files = CreateMockFormFileCollection(
            "resume.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        );
        var expectedResult = TestDataFactory.CreateSuccessfulTextExtractionResult();

        _mockFileTextExtractor
            .Setup(x =>
                x.ExtractFileTextAsync(It.IsAny<Stream>(), "resume.docx", It.IsAny<string>())
            )
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _uploadFileExtractor.ExtractFileAsync(files);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.True(result.Items[0].Success);
        Assert.Equal("resume.docx", result.Items[0].SourceName);

        _mockFileTextExtractor.Verify(
            x => x.ExtractFileTextAsync(It.IsAny<Stream>(), "resume.docx", It.IsAny<string>()),
            Times.Once
        );
    }

    [Fact]
    public async Task ExtractFileAsync_WithValidTxtFile_ReturnsSuccess()
    {
        // Arrange
        var files = CreateMockFormFileCollection("resume.txt", "text/plain");
        var expectedResult = TestDataFactory.CreateSuccessfulTextExtractionResult();

        _mockFileTextExtractor
            .Setup(x => x.ExtractFileTextAsync(It.IsAny<Stream>(), "resume.txt", "text/plain"))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _uploadFileExtractor.ExtractFileAsync(files);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.True(result.Items[0].Success);
        Assert.Equal("resume.txt", result.Items[0].SourceName);

        _mockFileTextExtractor.Verify(
            x => x.ExtractFileTextAsync(It.IsAny<Stream>(), "resume.txt", "text/plain"),
            Times.Once
        );
    }

    [Fact]
    public async Task ExtractFileAsync_WithEmptyFileList_ReturnsEmptyResponse()
    {
        // Arrange
        var files = new FormFileCollection();

        // Act
        var result = await _uploadFileExtractor.ExtractFileAsync(files);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.Meta.TotalItems);
        Assert.Equal(0, result.Meta.SuccessCount);
        Assert.Equal(0, result.Meta.FailedCount);

        _mockFileTextExtractor.Verify(
            x => x.ExtractFileTextAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never
        );
    }

    [Fact]
    public async Task ExtractFileAsync_WithFailedFileExtraction_ReturnsError()
    {
        // Arrange
        var files = CreateMockFormFileCollection("corrupted.pdf", "application/pdf");
        var failedResult = TestDataFactory.CreateFailedTextExtractionResult();

        _mockFileTextExtractor
            .Setup(x =>
                x.ExtractFileTextAsync(It.IsAny<Stream>(), "corrupted.pdf", It.IsAny<string>())
            )
            .ReturnsAsync(failedResult);

        // Act
        var result = await _uploadFileExtractor.ExtractFileAsync(files);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.False(result.Items[0].Success);
        Assert.NotNull(result.Items[0].Error);
        Assert.Equal(0, result.Meta.SuccessCount);
        Assert.Equal(1, result.Meta.FailedCount);
    }

    [Theory]
    [InlineData("test.pdf", "application/pdf")]
    [InlineData(
        "resume.docx",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    )]
    [InlineData("document.txt", "text/plain")]
    public async Task ExtractFileAsync_WithVariousContentTypes_DetectsCorrectly(
        string fileName,
        string contentType
    )
    {
        // Arrange
        var files = CreateMockFormFileCollection(fileName, contentType);
        var expectedResult = TestDataFactory.CreateSuccessfulTextExtractionResult();

        _mockFileTextExtractor
            .Setup(x => x.ExtractFileTextAsync(It.IsAny<Stream>(), fileName, contentType))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _uploadFileExtractor.ExtractFileAsync(files);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.True(result.Items[0].Success);
        Assert.Equal(fileName, result.Items[0].SourceName);
    }

    private static FormFileCollection CreateMockFormFileCollection(
        string fileName,
        string contentType
    )
    {
        var mockFormFile = new Mock<IFormFile>();
        mockFormFile.Setup(f => f.FileName).Returns(fileName);
        mockFormFile.Setup(f => f.ContentType).Returns(contentType);
        mockFormFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());

        var collection = new FormFileCollection { mockFormFile.Object };

        return collection;
    }
}
