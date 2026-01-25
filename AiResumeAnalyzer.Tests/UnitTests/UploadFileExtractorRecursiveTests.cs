using System.IO.Compression;
using AiResumeAnalyzer.Api.Contracts;
using AiResumeAnalyzer.Api.Services;
using AiResumeAnalyzer.Tests.UnitTests;
using Microsoft.AspNetCore.Http;
using Moq;

namespace AiResumeAnalyzer.Tests.UnitTests;

public class UploadFileExtractorRecursiveTests
{
    private readonly Mock<IFileTextExtractor> _mockFileTextExtractor = new();
    private readonly UploadFileExtractor _uploadFileExtractor;

    public UploadFileExtractorRecursiveTests()
    {
        _uploadFileExtractor = new UploadFileExtractor(_mockFileTextExtractor.Object);
    }

    [Fact]
    public async Task ExtractFileAsync_WithNestedZipFile_ExtractsInnerFiles()
    {
        // Arrange
        // Structure:
        // outer.zip
        //   - inner.zip
        //     - nested_resume.txt
        
        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            var innerZipEntry = archive.CreateEntry("inner.zip");
            using var innerZipStream = innerZipEntry.Open();
            // Create inner zip
            using var innerArchive = new ZipArchive(innerZipStream, ZipArchiveMode.Create, true);
            var txtEntry = innerArchive.CreateEntry("nested_resume.txt");
            using var txtStream = txtEntry.Open();
            using var writer = new StreamWriter(txtStream);
            writer.Write("Resume Content");
        }
        memoryStream.Position = 0;

        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns("outer.zip");
        mockFile.Setup(f => f.OpenReadStream()).Returns(memoryStream);
        mockFile.Setup(f => f.Length).Returns(memoryStream.Length);
        
        var files = new FormFileCollection { mockFile.Object };

        var expectedResult = new TextExtractionResult(true, "Resume Content", null);
        _mockFileTextExtractor
            .Setup(x => x.ExtractFileTextAsync(It.IsAny<Stream>(), "nested_resume.txt", "text/plain"))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _uploadFileExtractor.ExtractFileAsync(files);

        // Assert
        Assert.NotNull(result);
        
        // We expect finding the nested text file
        Assert.Contains(result.Items, i => i.SourceName.EndsWith("nested_resume.txt") && i.Success);
        
        // Verify we called the extractor for the text file
        _mockFileTextExtractor.Verify(
            x => x.ExtractFileTextAsync(It.IsAny<Stream>(), "nested_resume.txt", "text/plain"),
            Times.Once
        );
    }
}
