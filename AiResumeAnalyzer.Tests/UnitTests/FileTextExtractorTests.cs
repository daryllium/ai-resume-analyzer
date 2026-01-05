using System.IO;
using System.Text;
using AiResumeAnalyzer.Api.Services;

namespace AiResumeAnalyzer.Tests.UnitTests;

/// <summary>
/// Unit tests for the FileTextExtractor service
/// </summary>
public class FileTextExtractorTests
{
    private readonly FileTextExtractor _fileTextExtractor;

    public FileTextExtractorTests()
    {
        _fileTextExtractor = new FileTextExtractor();
    }

    [Fact]
    public async Task ExtractFileTextAsync_WithValidPdfFile_ReturnsExtractedText()
    {
        // Arrange
        var pdfPath = Path.Combine(AppContext.BaseDirectory, "TestData", "sample.pdf");
        using var stream = File.OpenRead(pdfPath);

        // Act
        var result = await _fileTextExtractor.ExtractFileTextAsync(
            stream,
            "sample.pdf",
            "application/pdf"
        );

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success, $"Expected success, but got error: {result.ErrorMessage}");
        Assert.NotNull(result.ExtractedText);
        Assert.NotEmpty(result.ExtractedText!);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task ExtractFileTextAsync_WithValidDocxFile_ReturnsExtractedText()
    {
        // Arrange
        var docxPath = Path.Combine(AppContext.BaseDirectory, "TestData", "sample.docx");
        using var stream = File.OpenRead(docxPath);

        // Act
        var result = await _fileTextExtractor.ExtractFileTextAsync(
            stream,
            "sample.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        );

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success, $"Expected success, but got error: {result.ErrorMessage}");
        Assert.NotNull(result.ExtractedText);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task ExtractFileTextAsync_WithValidTxtStream_ReturnsExtractedText()
    {
        // Arrange
        var txtContent = Encoding.UTF8.GetBytes(
            "Sample resume text content\nJohn Doe\nSoftware Developer"
        );
        var stream = new MemoryStream(txtContent);

        // Act
        var result = await _fileTextExtractor.ExtractFileTextAsync(
            stream,
            "resume.txt",
            "text/plain"
        );

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.ExtractedText);
        Assert.NotNull(result.ExtractedText);
        Assert.NotEmpty(result.ExtractedText!);
        Assert.Contains("Sample resume text content", result.ExtractedText!);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task ExtractFileTextAsync_WithEmptyStream_ReturnsError()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act
        var result = await _fileTextExtractor.ExtractFileTextAsync(
            stream,
            "empty.pdf",
            "application/pdf"
        );

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Null(result.ExtractedText);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Error:", result.ErrorMessage);
    }

    [Fact]
    public async Task ExtractFileTextAsync_WithUnsupportedFileType_ReturnsError()
    {
        // Arrange
        var content = Encoding.UTF8.GetBytes("Random file content");
        var stream = new MemoryStream(content);

        // Act
        var result = await _fileTextExtractor.ExtractFileTextAsync(
            stream,
            "document.xyz",
            "application/octet-stream"
        );

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Null(result.ExtractedText);
        Assert.NotNull(result.ErrorMessage);
        Assert.Contains("Unsupported file type", result.ErrorMessage);
    }

    [Theory]
    [InlineData("test.txt", true, "text/plain")]
    [InlineData("test.TXT", true, "text/plain")]
    [InlineData("test.text", true, "text/plain")]
    [InlineData("test.md", true, "text/markdown")]
    [InlineData("test.markdown", true, "text/markdown")]
    [InlineData("test.unknown", true, "text/plain")] // Content type makes it text
    [InlineData("test.xyz", false, "application/octet-stream")]
    [InlineData("test.jpg", false, "image/jpeg")]
    [InlineData("test.pdf", false, "application/pdf")] // PDF extension with non-text content
    [InlineData(
        "test.docx",
        false,
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    )] // DOCX extension with non-text content
    public async Task ExtractFileTextAsync_WithVariousFileTypes_ReturnsCorrectResult(
        string fileName,
        bool expectedSuccess,
        string contentType
    )
    {
        // Arrange
        var content = Encoding.UTF8.GetBytes("Sample text content for testing");
        var stream = new MemoryStream(content);

        // Act
        var result = await _fileTextExtractor.ExtractFileTextAsync(stream, fileName, contentType);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedSuccess, result.Success);

        if (expectedSuccess)
        {
            Assert.NotNull(result.ExtractedText);
            Assert.Null(result.ErrorMessage);
        }
        else
        {
            Assert.Null(result.ExtractedText);
            Assert.NotNull(result.ErrorMessage);
            // PDF/DOCX files get parsing errors, others get unsupported type errors
            if (
                fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
                || fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase)
            )
            {
                Assert.Contains("Error:", result.ErrorMessage);
            }
            else
            {
                Assert.Contains("Unsupported file type", result.ErrorMessage);
            }
        }
    }

    [Theory]
    [InlineData("text/plain")]
    [InlineData("text/html")]
    [InlineData("text/csv")]
    [InlineData("text/markdown")]
    public async Task IsTextFile_WithTextContentTypes_ReturnsCorrectResult(string contentType)
    {
        // Arrange
        var content = Encoding.UTF8.GetBytes("Sample content");
        var stream = new MemoryStream(content);
        var fileName = "document.unknown";

        // Act
        var result = await _fileTextExtractor.ExtractFileTextAsync(stream, fileName, contentType);

        // Assert - Should be treated as text file based on content type
        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
    }
}
