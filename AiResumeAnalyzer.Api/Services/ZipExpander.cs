using System.IO.Compression;
using AiResumeAnalyzer.Api.Options;

namespace AiResumeAnalyzer.Api.Services;

public static class ZipExpander
{
    public sealed record ZipItem(string EntryPath, string FileName, byte[] Content);

    public sealed record ZipError(string EntryPath, string ErrorMessage);

    public sealed record ZipExpandResult(List<ZipItem> Items, List<ZipError> Errors);

    public static ZipExpandResult ExpandZipRecursive(
        Stream zipStream,
        string zipLabel,
        ZipOptions options,
        Microsoft.Extensions.Logging.ILogger? logger = null
    )
    {
        var items = new List<ZipItem>();
        var errors = new List<ZipError>();

        logger?.LogDebug(
            "Expanding zip {ZipLabel} (maxDepth={MaxDepth}, maxItems={MaxItems})",
            zipLabel,
            options.MaxDepth,
            options.MaxItems
        );
        ExpandZipInternal(zipStream, zipLabel, options, items, errors, depth: 0, logger);

        return new ZipExpandResult(items, errors);
    }

    private static void ExpandZipInternal(
        Stream zipStream,
        string zipLabel,
        ZipOptions options,
        List<ZipItem> items,
        List<ZipError> errors,
        int depth,
        Microsoft.Extensions.Logging.ILogger? logger = null
    )
    {
        if (depth > options.MaxDepth)
        {
            logger?.LogDebug("Reached max depth {Depth} for {ZipLabel}", depth, zipLabel);
            return;
        }

        if (zipStream.CanSeek)
            zipStream.Position = 0;

        ZipArchive archive;
        try
        {
            archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);
        }
        catch (Exception ex)
        {
            errors.Add(
                new ZipError(
                    zipLabel,
                    $"Failed to open zip archive: {ex.GetType().Name}: {ex.Message}"
                )
            );
            return;
        }

        using (archive)
        {
            foreach (var entry in archive.Entries)
            {
                if (items.Count >= options.MaxItems)
                {
                    logger?.LogDebug(
                        "Reached max items ({MaxItems}) while expanding {ZipLabel}",
                        options.MaxItems,
                        zipLabel
                    );
                    return;
                }

                if (string.IsNullOrWhiteSpace(entry.Name))
                {
                    logger?.LogDebug(
                        "Skipping directory entry {Entry} in {ZipLabel}",
                        entry.FullName,
                        zipLabel
                    );
                    continue;
                }

                var entryPath = entry.FullName.Replace('\\', '/');
                if (
                    entryPath.StartsWith("/", StringComparison.Ordinal)
                    || entryPath.Contains("../", StringComparison.Ordinal)
                    || entryPath.Contains("..\\", StringComparison.Ordinal)
                )
                    continue;

                if (entry.Length > options.MaxEntryBytes)
                    continue;

                byte[] content;
                try
                {
                    using var entryStream = entry.Open();
                    using var ms = new MemoryStream();
                    entryStream.CopyTo(ms);
                    content = ms.ToArray();
                }
                catch (Exception ex)
                {
                    errors.Add(
                        new ZipError(
                            $"{zipLabel}:{entryPath}",
                            $"Failed to read zip entry: {ex.GetType().Name}: {ex.Message}"
                        )
                    );
                    continue;
                }

                var labelPath = $"{zipLabel}:{entryPath}";

                if (entry.FullName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        using var nestedMs = new MemoryStream(content);
                        logger?.LogDebug(
                            "Expanding nested zip entry {Entry} (label={LabelPath})",
                            entry.FullName,
                            labelPath
                        );
                        ExpandZipInternal(
                            nestedMs,
                            labelPath,
                            options,
                            items,
                            errors,
                            depth + 1,
                            logger
                        );
                    }
                    catch (Exception ex)
                    {
                        errors.Add(
                            new ZipError(
                                labelPath,
                                $"Failed to expand nested zip entry: {ex.GetType().Name}: {ex.Message}"
                            )
                        );
                    }

                    continue;
                }

                items.Add(new ZipItem(labelPath, entry.Name, content));
            }
        }
    }
}
