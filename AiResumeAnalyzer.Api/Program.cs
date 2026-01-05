using AiResumeAnalyzer.Api.Contracts;
using AiResumeAnalyzer.Api.Options;
using AiResumeAnalyzer.Api.Requests;
using AiResumeAnalyzer.Api.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<ZipOptions>(builder.Configuration.GetSection("ZipOptions"));
builder.Services.AddSingleton<IAnalyzer, Analyzer>();
builder.Services.AddSingleton<ITextInputExtractor, TextInputExtractor>();
builder.Services.AddSingleton<IFileTextExtractor, FileTextExtractor>();
builder.Services.AddSingleton<IUploadFileExtractor, UploadFileExtractor>();
builder.Services.AddHttpClient<IAiModelClient, AiModelClient>();
builder.Services.AddSingleton<IAiModelClient, AiModelClient>();

var app = builder.Build();

app.MapPost(
        "/api/analyze",
        async ([FromServices] IAnalyzer analyzer, [FromForm] AnalyzeRequest request) =>
        {
            var response = await analyzer.AnalyzeAsync(request);
            return response;
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
