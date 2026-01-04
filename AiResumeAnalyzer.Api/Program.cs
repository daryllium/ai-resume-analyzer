using AiResumeAnalyzer.Api.Contracts;
using AiResumeAnalyzer.Api.Options;
using AiResumeAnalyzer.Api.Requests;
using AiResumeAnalyzer.Api.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<ZipOptions>(builder.Configuration.GetSection("ZipOptions"));
builder.Services.AddSingleton<IAnalyzerService, AnalyzerService>();
builder.Services.AddSingleton<IZipExtractor, ZipExtractorService>();
builder.Services.AddSingleton<ITextExtractor, TextExtractor>();
builder.Services.AddSingleton<IPdfExtractor, PdfExtractor>();
builder.Services.AddSingleton<IDocxExtractor, DocxExtractor>();

var app = builder.Build();

app.MapPost(
        "/api/analyze",
        ([FromServices] IAnalyzerService svc, [FromForm] string jobDescription) =>
        {
            var resp = svc.Analyze(jobDescription);
            return Results.Ok(resp);
        }
    )
    .DisableAntiforgery()
    .Accepts<string>("multipart/form-data")
    .Produces<AnalyzeResponse>(StatusCodes.Status200OK);

app.MapPost(
        "/api/extract",
        ([FromServices] IZipExtractor svc, [FromForm] ExtractRequest req) =>
        {
            var resp = svc.Extract(req);
            return Results.Ok(resp);
        }
    )
    .DisableAntiforgery()
    .Accepts<ExtractRequest>("multipart/form-data")
    .Produces<ExtractResponse>(StatusCodes.Status200OK);

app.MapGet("/", () => "Hello World!");

app.Run();
