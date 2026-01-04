using AiResumeAnalyzer.Api.Options;
using AiResumeAnalyzer.Api.Requests;
using AiResumeAnalyzer.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace AiResumeAnalyzer.Tests;

public class ExtractorTests
{
    [Fact]
    public async Task ExtractTextAsync_TxtFile_ReturnsTxtContent()
    {
        var pdfExtractorMock = new Mock<IPdfExtractor>();
        var docxExtractorMock = new Mock<IDocxExtractor>();
        var loggerMock = new Mock<ILogger<TextExtractor>>();

        pdfExtractorMock
            .Setup(x => x.CanHandleFile(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);
        docxExtractorMock
            .Setup(x => x.CanHandleFile(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        var extractor = new TextExtractor(
            pdfExtractorMock.Object,
            docxExtractorMock.Object,
            loggerMock.Object
        );

        var filePath = Path.Combine(AppContext.BaseDirectory, "TestData", "sample.txt");
        using var stream = File.OpenRead(filePath);
        var result = await extractor.ExtractTextAsync(stream, "sample.txt", "text/plain");

        Assert.True(result.Success);
        Assert.Contains("Jane Doe", result.ExtractedText);
    }

    [Fact]
    public async Task ExtractTextAsync_PdfFile_ReturnsPdfContent()
    {
        var pdfExtractorMock = new Mock<IPdfExtractor>();
        var docxExtractorMock = new Mock<IDocxExtractor>();
        var loggerMock = new Mock<ILogger<TextExtractor>>();

        pdfExtractorMock.Setup(x => x.CanHandleFile("test.pdf", "application/pdf")).Returns(true);
        pdfExtractorMock
            .Setup(x => x.ExtractTextFromPdfAsync(It.IsAny<Stream>()))
            .ReturnsAsync("PDF content");
        docxExtractorMock
            .Setup(x => x.CanHandleFile(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        var extractor = new TextExtractor(
            pdfExtractorMock.Object,
            docxExtractorMock.Object,
            loggerMock.Object
        );

        using var stream = new MemoryStream();
        var result = await extractor.ExtractTextAsync(stream, "test.pdf", "application/pdf");

        Assert.True(result.Success);
        Assert.Equal("PDF content", result.ExtractedText);
    }

    [Fact]
    public async Task ExtractTextAsync_DocxFile_ReturnsDocxContent()
    {
        var pdfExtractorMock = new Mock<IPdfExtractor>();
        var docxExtractorMock = new Mock<IDocxExtractor>();
        var loggerMock = new Mock<ILogger<TextExtractor>>();

        pdfExtractorMock
            .Setup(x => x.CanHandleFile(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);
        docxExtractorMock
            .Setup(x =>
                x.CanHandleFile(
                    "test.docx",
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                )
            )
            .Returns(true);
        docxExtractorMock
            .Setup(x => x.ExtractTextFromDocxAsync(It.IsAny<Stream>()))
            .ReturnsAsync("DOCX content");

        var extractor = new TextExtractor(
            pdfExtractorMock.Object,
            docxExtractorMock.Object,
            loggerMock.Object
        );

        using var stream = new MemoryStream();
        var result = await extractor.ExtractTextAsync(
            stream,
            "test.docx",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        );

        Assert.True(result.Success);
        Assert.Equal("DOCX content", result.ExtractedText);
    }

    [Fact]
    public async Task ExtractTextAsync_UnsupportedFileType_ReturnsError()
    {
        var pdfExtractorMock = new Mock<IPdfExtractor>();
        var docxExtractorMock = new Mock<IDocxExtractor>();
        var loggerMock = new Mock<ILogger<TextExtractor>>();

        pdfExtractorMock
            .Setup(x => x.CanHandleFile(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);
        docxExtractorMock
            .Setup(x => x.CanHandleFile(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(false);

        var extractor = new TextExtractor(
            pdfExtractorMock.Object,
            docxExtractorMock.Object,
            loggerMock.Object
        );

        using var stream = new MemoryStream();
        var result = await extractor.ExtractTextAsync(stream, "sample.xyz", "application/xyz");

        Assert.False(result.Success);
        Assert.Null(result.ExtractedText);
        Assert.Equal("Unsupported file type: sample.xyz", result.ErrorMessage);
    }

    [Fact]
    public async Task ZipExtractorService_Extract_ZipFile_ProcessesAllEntries()
    {
        var textExtractorMock = new Mock<ITextExtractor>();
        var zipOptions = Options.Create(
            new ZipOptions
            {
                MaxDepth = 5,
                MaxItems = 50,
                MaxEntryBytes = 10_000_000,
            }
        );
        var loggerMock = new Mock<ILogger<ZipExtractorService>>();

        textExtractorMock
            .Setup(x =>
                x.ExtractTextAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>())
            )
            .ReturnsAsync(new TextExtractionResult(true, "content", null));

        var zipExtractor = new ZipExtractorService(
            textExtractorMock.Object,
            zipOptions,
            loggerMock.Object
        );

        var zipPath = Path.Combine(AppContext.BaseDirectory, "TestData", "sample.zip");
        using var zipStream = File.OpenRead(zipPath);

        var zipFileMock = new Mock<IFormFile>();
        zipFileMock.Setup(f => f.FileName).Returns("sample.zip");
        zipFileMock.Setup(f => f.ContentType).Returns("application/zip");
        zipFileMock.Setup(f => f.OpenReadStream()).Returns(zipStream);

        var request = new ExtractRequest { ZipFile = zipFileMock.Object };
        var response = await zipExtractor.Extract(request);

        Assert.Equal(3, response.Items.Count);
        var fileNames = response.Items.Select(item => item.SourceName).ToList();
        Assert.Contains("sample.zip:sample.docx", fileNames);
        Assert.Contains("sample.zip:sample.txt", fileNames);
        Assert.Contains("sample.zip:sample.pdf", fileNames);
    }

    [Fact]
    public async Task ZipExtractorService_Extract_NestedZipFile_ProcessesAllEntries()
    {
        var textExtractorMock = new Mock<ITextExtractor>();
        var zipOptions = Options.Create(
            new ZipOptions
            {
                MaxDepth = 5,
                MaxItems = 50,
                MaxEntryBytes = 10_000_000,
            }
        );
        var loggerMock = new Mock<ILogger<ZipExtractorService>>();

        textExtractorMock
            .Setup(x =>
                x.ExtractTextAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>())
            )
            .ReturnsAsync(new TextExtractionResult(true, "Mock extracted content", null));

        var zipExtractor = new ZipExtractorService(
            textExtractorMock.Object,
            zipOptions,
            loggerMock.Object
        );

        var zipPath = Path.Combine(AppContext.BaseDirectory, "TestData", "sample-nested.zip");
        using var zipStream = File.OpenRead(zipPath);

        var zipFileMock = new Mock<IFormFile>();
        zipFileMock.Setup(f => f.FileName).Returns("sample-nested.zip");
        zipFileMock.Setup(f => f.ContentType).Returns("application/zip");
        zipFileMock.Setup(f => f.OpenReadStream()).Returns(zipStream);

        var request = new ExtractRequest { ZipFile = zipFileMock.Object };

        var response = await zipExtractor.Extract(request);

        Assert.Equal(6, response.Items.Count);
        Assert.All(response.Items, item => Assert.Equal("zip-entry", item.SourceType));
        Assert.All(response.Items, item => Assert.True(item.Success));
        Assert.All(
            response.Items,
            item => Assert.Equal("Mock extracted content", item.ExtractedText)
        );

        var fileNames = response.Items.Select(item => item.SourceName).ToList();
        Assert.Contains("sample-nested.zip:sample.docx", fileNames);
        Assert.Contains("sample-nested.zip:sample.txt", fileNames);
        Assert.Contains("sample-nested.zip:sample.pdf", fileNames);
        Assert.Contains("sample-nested.zip:nested/sample.pdf", fileNames);
        Assert.Contains("sample-nested.zip:nested/sample.txt", fileNames);
        Assert.Contains("sample-nested.zip:nested/docx/sample.docx", fileNames);

        textExtractorMock.Verify(
            x => x.ExtractTextAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Exactly(6)
        );
    }

    [Fact]
    public async Task ZipExtractorService_Extract_ExtractionFails_ReturnsError()
    {
        var textExtractorMock = new Mock<ITextExtractor>();
        var loggerMock = new Mock<ILogger<ZipExtractorService>>();

        var zipOptions = Options.Create(
            new ZipOptions
            {
                MaxDepth = 5,
                MaxItems = 50,
                MaxEntryBytes = 5 * 1024 * 1024,
            }
        );

        textExtractorMock
            .Setup(x =>
                x.ExtractTextAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>())
            )
            .ReturnsAsync(new TextExtractionResult(false, null, "Extraction error"));

        var zipExtractor = new ZipExtractorService(
            textExtractorMock.Object,
            zipOptions,
            loggerMock.Object
        );

        using var zipStream = File.OpenRead("TestData/sample.zip");
        var zipFileMock = new Mock<IFormFile>();
        zipFileMock.Setup(f => f.FileName).Returns("sample.zip");
        zipFileMock.Setup(f => f.ContentType).Returns("application/zip");
        zipFileMock.Setup(f => f.OpenReadStream()).Returns(zipStream);

        var request = new ExtractRequest { ZipFile = zipFileMock.Object };

        var response = await zipExtractor.Extract(request);

        Assert.Equal(3, response.Items.Count);
        Assert.All(response.Items, item => Assert.False(item.Success));
        Assert.All(response.Items, item => Assert.Equal("Extraction error", item.Error));
        Assert.All(response.Items, item => Assert.Null(item.ExtractedText));
    }
}
