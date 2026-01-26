using AiResumeAnalyzer.Api.Contracts;
using AiResumeAnalyzer.Api.Options;
using AiResumeAnalyzer.Api.Requests;
using AiResumeAnalyzer.Api.Services;
using AiResumeAnalyzer.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<AiModelOptions>(
    builder.Configuration.GetSection(AiModelOptions.SectionName)
);
builder.Services.Configure<ZipOptions>(builder.Configuration.GetSection(ZipOptions.SectionName));

builder.Services.Configure<ScoringOptions>(
    builder.Configuration.GetSection(ScoringOptions.SectionName)
);
builder.Services.Configure<FileLimitOptions>(
    builder.Configuration.GetSection(FileLimitOptions.SectionName)
);

builder.Services.AddSingleton<IAnalyzer, Analyzer>();
builder.Services.AddSingleton<IJobParser, JobParser>();
builder.Services.AddSingleton<IResumeParser, ResumeParser>();
builder.Services.AddSingleton<IMatcher, Matcher>();
builder.Services.AddSingleton<ITextInputExtractor, TextInputExtractor>();
builder.Services.AddSingleton<IOcrService, OcrService>();
builder.Services.AddSingleton<IFileTextExtractor, FileTextExtractor>();
builder.Services.AddSingleton<IUploadFileExtractor, UploadFileExtractor>();
builder.Services.AddHttpClient<IAiModelClient, AiModelClient>(client =>
{
    client.BaseAddress = new Uri("http://localhost:11434");
});

var app = builder.Build();

app.MapPost(
        "/api/analyze",
        async (
            [FromServices] IAnalyzer analyzer,
            [FromServices] IOptions<FileLimitOptions> fileOptions,
            [FromForm] AnalyzeRequest request,
            CancellationToken ct
        ) =>
        {
            var options = fileOptions.Value;

            // Early validation
            if (request.UploadFiles is not null)
            {
                if (request.UploadFiles.Count > options.MaxFileCount)
                {
                    return Results.BadRequest(
                        new
                        {
                            error = $"Too many files. Maximum allowed is {options.MaxFileCount}.",
                        }
                    );
                }

                var totalBytes = request.UploadFiles.Sum(f => f.Length);
                if (totalBytes > options.MaxTotalSizeBytes)
                {
                    return Results.BadRequest(
                        new
                        {
                            error = $"Total upload size exceeds limit of {options.MaxTotalSizeBytes / 1024 / 1024}MB.",
                        }
                    );
                }

                foreach (var file in request.UploadFiles)
                {
                    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!options.AllowedExtensions.Contains(extension))
                    {
                        return Results.BadRequest(
                            new
                            {
                                error = $"File type {extension} is not allowed. Supported types: {string.Join(", ", options.AllowedExtensions)}",
                            }
                        );
                    }
                }
            }

            var response = await analyzer.AnalyzeAsync(request, ct);
            return Results.Ok(response);
        }
    )
    .DisableAntiforgery()
    .Accepts<AnalyzeRequest>("multipart/form-data")
    .Produces<AnalyzeResponse>(StatusCodes.Status200OK);

app.MapPost(
        "/api/extract",
        async ([FromServices] IUploadFileExtractor extractor, HttpContext context) =>
        {
            var files = context.Request.Form.Files;
            var response = await extractor.ExtractFileAsync(files);
            return response;
        }
    )
    .DisableAntiforgery()
    .Accepts<object>("multipart/form-data")
    .Produces<ExtractResponse>(StatusCodes.Status200OK);

app.MapGet("/", () => "Hello World!");

app.Run();
