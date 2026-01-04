using AiResumeAnalyzer.Api.Contracts;
using AiResumeAnalyzer.Api.Options;
using AiResumeAnalyzer.Api.Requests;
using Microsoft.Extensions.Options;

namespace AiResumeAnalyzer.Api.Services;

public sealed class ZipExtractorService(
    ITextExtractor _textExtractor,
    IOptions<ZipOptions> zipOptions,
    ILogger<ZipExtractorService> logger
) : IZipExtractor
{
    private readonly ZipOptions _zipOptions = zipOptions.Value;
    private readonly ILogger _logger = logger;
    private readonly ITextExtractor _textExtractor = _textExtractor;

    public async Task<ExtractResponse> Extract(ExtractRequest req)
    {
        var items = new List<ExtractItemResult>();

        if (req.ResumeText is not null)
        {
            for (var i = 0; i < req.ResumeText.Count; i++)
            {
                var t = req.ResumeText[i] ?? string.Empty;
                items.Add(new ExtractItemResult("text", $"resumeText[{i}]", true, t, null));
            }
        }

        if (req.ResumeFiles is not null)
        {
            foreach (var f in req.ResumeFiles)
            {
                if (f.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                    continue;

                using var stream = f.OpenReadStream();
                var result = await _textExtractor.ExtractTextAsync(
                    stream,
                    f.FileName,
                    f.ContentType
                );

                items.Add(
                    new ExtractItemResult(
                        "file",
                        f.FileName,
                        result.Success,
                        result.ExtractedText,
                        result.ErrorMessage
                    )
                );
            }
        }

        if (req.ZipFile is not null)
        {
            using var zs = req.ZipFile.OpenReadStream();
            var expanded = ZipExpanderService.ExpandZipRecursive(
                zs,
                req.ZipFile.FileName,
                _zipOptions,
                _logger
            );

            foreach (var zi in expanded.Items)
            {
                using var contentStream = new MemoryStream(zi.Content);
                var result = await _textExtractor.ExtractTextAsync(
                    contentStream,
                    zi.FileName,
                    GetContentTypeFromFileName(zi.FileName)
                );
                items.Add(
                    new ExtractItemResult(
                        "zip-entry",
                        zi.EntryPath,
                        result.Success,
                        result.ExtractedText,
                        result.ErrorMessage
                    )
                );
            }

            foreach (var ze in expanded.Errors)
            {
                items.Add(
                    new ExtractItemResult("zip-error", ze.EntryPath, false, null, ze.ErrorMessage)
                );
            }

            if (expanded.Items.Count == 0 && expanded.Errors.Count == 0)
            {
                items.Add(
                    new ExtractItemResult(
                        "zip",
                        req.ZipFile.FileName,
                        false,
                        null,
                        "No valid entries found in zip file."
                    )
                );
            }
        }

        var success = items.Count(x => x.Success);
        var failed = items.Count(x => !x.Success);

        return new ExtractResponse(items, new ExtractMeta(items.Count, success, failed));
    }

    private string GetContentTypeFromFileName(string fileName)
    {
        if (fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            return "application/pdf";
        if (fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        if (fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            return "text/plain";

        return "application/octet-stream";
    }
}
