using AiResumeAnalyzer.Api.Contracts;
using AiResumeAnalyzer.Api.Options;
using AiResumeAnalyzer.Api.Requests;
using AiResumeAnalyzer.Api.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace AiResumeAnalyzer.Api.Services;

public sealed class Analyzer(
    IUploadFileExtractor uploadFileExtractor,
    ITextInputExtractor textInputExtractor,
    IJobParser jobParser,
    IResumeParser resumeParser,
    IMatcher matcher,
    IOptions<AiModelOptions> aiOptions,
    ILogger<Analyzer> logger
) : IAnalyzer
{
    private readonly IUploadFileExtractor _uploadFileExtractor = uploadFileExtractor;
    private readonly ITextInputExtractor _textInputExtractor = textInputExtractor;
    private readonly IJobParser _jobParser = jobParser;
    private readonly IResumeParser _resumeParser = resumeParser;
    private readonly IMatcher _matcher = matcher;
    private readonly AiModelOptions _aiOptions = aiOptions.Value;
    private readonly ILogger<Analyzer> _logger = logger;

    public async Task<AnalyzeResponse> AnalyzeAsync(
        AnalyzeRequest request,
        CancellationToken cancellationToken = default
    )
    {
        var sources = await ExtractAllSourcesAsync(request);

        if (sources.Count == 0)
        {
            return new AnalyzeResponse(new List<AnalyzeResultItem>(), new AnalyzeMeta(0, 0));
        }

        JobProfile? jobProfile = null;
        try
        {
            _logger.LogInformation("Parsing job description...");
            jobProfile = await _jobParser.ParseJobDescriptionAsync(
                request.JobDescription,
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical failure: Could not parse job description.");
            return new AnalyzeResponse(
                new List<AnalyzeResultItem>(),
                new AnalyzeMeta(0, sources.Count)
            );
        }

        var semaphore = new SemaphoreSlim(_aiOptions.MaxConcurrency);
        var tasks = sources.Select(async source =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                _logger.LogInformation("Processing resume: {SourceName}", source.SourceName);
                var candidate = await _resumeParser.ParseResumeAsync(
                    source.Text,
                    cancellationToken
                );
                var match = await _matcher.MatchAsync(jobProfile!, candidate, cancellationToken);

                return match with
                {
                    SourceName = source.SourceName,
                    Success = true,
                };
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "Processing cancelled for resume: {SourceName}",
                    source.SourceName
                );
                return new AnalyzeResultItem(
                    SourceName: source.SourceName,
                    Success: false,
                    Error: "Operation was cancelled or timed out."
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process resume: {SourceName}", source.SourceName);
                return new AnalyzeResultItem(
                    SourceName: source.SourceName,
                    Success: false,
                    Error: ex.Message
                );
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);
        var finalResults = results.ToList();
        var failedCount = finalResults.Count(r => !r.Success);

        return new AnalyzeResponse(
            finalResults,
            new AnalyzeMeta(finalResults.Count(r => r.Success), failedCount)
        );
    }

    private async Task<List<ExtractedSource>> ExtractAllSourcesAsync(AnalyzeRequest request)
    {
        var sources = new List<ExtractedSource>();

        if (request.UploadFiles?.Count > 0)
        {
            var fileResponse = await _uploadFileExtractor.ExtractFileAsync(request.UploadFiles);
            sources.AddRange(
                fileResponse
                    .Items.Where(x => x.Success && !string.IsNullOrEmpty(x.ExtractedText))
                    .Select(x => new ExtractedSource(x.SourceName, x.ExtractedText!))
            );
        }

        if (request.UploadText?.Count > 0)
        {
            var textResponse = await _textInputExtractor.ExtractTextInputAsync(request.UploadText);
            sources.AddRange(
                textResponse
                    .Items.Where(x => x.Success && !string.IsNullOrEmpty(x.ExtractedText))
                    .Select(x => new ExtractedSource(x.SourceName, x.ExtractedText!))
            );
        }

        return sources;
    }

    private record ExtractedSource(string SourceName, string Text);
}
