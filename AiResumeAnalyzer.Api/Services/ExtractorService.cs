using AiResumeAnalyzer.Api.Contracts;
using AiResumeAnalyzer.Api.Options;
using AiResumeAnalyzer.Api.Requests;
using Microsoft.Extensions.Options;

namespace AiResumeAnalyzer.Api.Services;

public sealed class ExtractorService : IExtractorService
{
    private readonly ZipOptions _zipOptions;
    private readonly ILogger _logger;

    public ExtractorService(IOptions<ZipOptions> zipOptions, ILogger<ExtractorService> logger)
    {
        _zipOptions = zipOptions.Value;
        _logger = logger;
    }

    public ExtractResponse Extract(ExtractRequest req)
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

                items.Add(
                    new ExtractItemResult(
                        "file",
                        f.FileName,
                        true,
                        $"[RECEIVED FILE] name={f.FileName} contentType={f.ContentType} length={f.Length}",
                        null
                    )
                );
            }
        }

        if (req.ZipFile is not null)
        {
            using var zs = req.ZipFile.OpenReadStream();
            var expanded = ZipExpander.ExpandZipRecursive(
                zs,
                req.ZipFile.FileName,
                _zipOptions,
                _logger
            );

            foreach (var zi in expanded.Items)
            {
                items.Add(
                    new ExtractItemResult(
                        "zip-entry",
                        zi.EntryPath,
                        true,
                        $"[RECEIVED ZIP ENTRY] name={zi.FileName} size={zi.Content.Length} bytes",
                        null
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
}
