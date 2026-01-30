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
builder.Services.Configure<OcrOptions>(builder.Configuration.GetSection(OcrOptions.SectionName));

builder.Services.AddSingleton<IAnalyzer, Analyzer>();
builder.Services.AddSingleton<IJobParser, JobParser>();
builder.Services.AddSingleton<IResumeParser, ResumeParser>();
builder.Services.AddSingleton<IMatcher, Matcher>();
builder.Services.AddSingleton<ITextInputExtractor, TextInputExtractor>();
builder.Services.AddSingleton<IOcrService, OcrService>();
builder.Services.AddSingleton<IFileTextExtractor, FileTextExtractor>();
builder.Services.AddSingleton<IUploadFileExtractor, UploadFileExtractor>();
builder.Services.AddSingleton<IPdfExportService, PdfExportService>();
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

            // 1. Job Description Validation
            if (string.IsNullOrWhiteSpace(request.JobDescription))
            {
                return Results.BadRequest(new { error = "Job description is required." });
            }

            if (request.JobDescription.Length > options.MaxJobDescriptionLength)
            {
                return Results.BadRequest(
                    new
                    {
                        error = $"Job description is too long. Maximum allowed is {options.MaxJobDescriptionLength} characters.",
                    }
                );
            }

            // 2. Candidate Count Validation (Combined)
            var fileCount = request.UploadFiles?.Count ?? 0;
            var textCount = request.UploadText?.Count ?? 0;
            var totalInitialCandidates = fileCount + textCount;

            if (totalInitialCandidates > options.MaxTotalCandidates)
            {
                return Results.BadRequest(
                    new
                    {
                        error = $"Too many candidates. Maximum combined limit is {options.MaxTotalCandidates} (files + text entries).",
                    }
                );
            }

            // 3. Text Input Validation
            if (request.UploadText is not null)
            {
                for (int i = 0; i < request.UploadText.Count; i++)
                {
                    if (request.UploadText[i]?.Length > options.MaxResumeTextLength)
                    {
                        return Results.BadRequest(
                            new
                            {
                                error = $"Text entry {i + 1} is too long. Maximum allowed is {options.MaxResumeTextLength} characters (~2 pages).",
                            }
                        );
                    }
                }
            }

            // 4. File Validation
            if (request.UploadFiles is not null)
            {
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
                    if (file.Length > options.MaxFileSizeBytes)
                    {
                        return Results.BadRequest(
                            new
                            {
                                error = $"File {file.FileName} exceeds individual limit of {options.MaxFileSizeBytes / 1024 / 1024}MB.",
                            }
                        );
                    }

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

            using var globalCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            globalCts.CancelAfter(TimeSpan.FromSeconds(options.GlobalTimeoutSeconds));

            try
            {
                var response = await analyzer.AnalyzeAsync(request, globalCts.Token);
                return Results.Ok(response);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                return Results.Json(
                    new { error = "The request timed out while processing resumes." },
                    statusCode: StatusCodes.Status408RequestTimeout
                );
            }
        }
    )
    .DisableAntiforgery()
    .Accepts<AnalyzeRequest>("multipart/form-data")
    .Produces<AnalyzeResponse>(StatusCodes.Status200OK);

app.MapPost(
        "/api/extract",
        async (
            [FromServices] IUploadFileExtractor extractor,
            [FromServices] IOptions<FileLimitOptions> fileOptions,
            HttpContext context
        ) =>
        {
            var options = fileOptions.Value;
            var files = context.Request.Form.Files;

            if (files.Count > options.MaxFileCount)
            {
                return Results.BadRequest(new { error = $"Too many files. Max is {options.MaxFileCount}." });
            }

            var totalBytes = files.Sum(f => f.Length);
            if (totalBytes > options.MaxTotalSizeBytes)
            {
                return Results.BadRequest(new { error = $"Total size exceeds {options.MaxTotalSizeBytes / 1024 / 1024}MB." });
            }

            foreach (var file in files)
            {
                if (file.Length > options.MaxFileSizeBytes)
                {
                    return Results.BadRequest(new { error = $"File {file.FileName} is too large." });
                }

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!options.AllowedExtensions.Contains(extension))
                {
                    return Results.BadRequest(new { error = $"Type {extension} not allowed." });
                }
            }

            var response = await extractor.ExtractFileAsync(files);
            return Results.Ok(response);
        }
    )
    .DisableAntiforgery()
    .Accepts<object>("multipart/form-data")
    .Produces<ExtractResponse>(StatusCodes.Status200OK);

app.MapPost(
        "/api/export/pdf",
        async (
            [FromServices] IPdfExportService exportService,
            [FromBody] AnalyzeResponse results,
            CancellationToken ct
        ) =>
        {
            var pdfBytes = await exportService.ExportToPdfAsync(results, ct);
            return Results.File(pdfBytes, "application/pdf", "Resume-Analysis.pdf");
        }
    )
    .Produces<byte[]>(StatusCodes.Status200OK, "application/pdf");

app.MapGet("/", () => "Hello World!");

app.Run();
