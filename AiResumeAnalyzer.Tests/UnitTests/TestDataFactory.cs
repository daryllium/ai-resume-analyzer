using AiResumeAnalyzer.Api.Contracts;
using AiResumeAnalyzer.Api.Requests;
using Microsoft.AspNetCore.Http;
using Moq;

namespace AiResumeAnalyzer.Tests.UnitTests;

public static class TestDataFactory
{
    public static AnalyzeRequest CreateAnalyzeRequestWithText()
    {
        return new AnalyzeRequest
        {
            JobDescription = "Senior Software Developer Position",
            UploadText = ["John Doe - Software Engineer with 5 years of experience."],
        };
    }

    public static AnalyzeRequest CreateAnalyzeRequestWithFiles()
    {
        return new AnalyzeRequest
        {
            JobDescription = "Data Scientist Role",
            UploadFiles = CreateMockFormFileCollection(),
        };
    }

    public static AnalyzeRequest CreateEmptyAnalyzeRequest()
    {
        return new AnalyzeRequest { JobDescription = "Software Developer Position" };
    }

    public static ExtractResponse CreateSuccessfulTextExtraction()
    {
        return new ExtractResponse(
            [
                new ExtractItemResult(
                    "text",
                    "textInput[0]",
                    true,
                    "Jane Smith\nSoftware Engineer\nExperience: 3 years\nSkills: JavaScript, React, Node.js",
                    null,
                    85
                ),
            ],
            new ExtractMeta(1, 1, 0)
        );
    }

    public static ExtractResponse CreateSuccessfulFileExtraction()
    {
        return new ExtractResponse(
            [
                new(
                    "file",
                    "resume.pdf",
                    true,
                    "John Doe\nSoftware Engineer\nExperience: 5 years\nSkills: C#, .NET, SQL",
                    null,
                    76
                ),
            ],
            new ExtractMeta(1, 1, 0)
        );
    }

    public static ExtractResponse CreateFailedExtraction()
    {
        return new ExtractResponse([], new ExtractMeta(0, 0, 0));
    }

    public static TextExtractionResult CreateSuccessfulTextExtractionResult()
    {
        return new TextExtractionResult(true, "Extracted resume text content", null);
    }

    public static TextExtractionResult CreateFailedTextExtractionResult()
    {
        return new TextExtractionResult(false, null, "Unsupported file type");
    }

    private static FormFileCollection CreateMockFormFileCollection()
    {
        var mockFormFile = new Mock<IFormFile>();
        mockFormFile.Setup(f => f.FileName).Returns("resume.pdf");
        mockFormFile.Setup(f => f.ContentType).Returns("application/pdf");

        var collection = new FormFileCollection { mockFormFile.Object };

        return collection;
    }
}
