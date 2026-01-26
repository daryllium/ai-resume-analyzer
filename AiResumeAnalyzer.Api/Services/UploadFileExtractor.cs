using System.IO.Compression;
using AiResumeAnalyzer.Api.Contracts;
using AiResumeAnalyzer.Api.Options;
using Microsoft.Extensions.Options;

namespace AiResumeAnalyzer.Api.Services;

public sealed class UploadFileExtractor(
    IFileTextExtractor fileTextExtractor,
    IOptions<FileLimitOptions> fileOptions,
    IOptions<ZipOptions> zipOptions
) : IUploadFileExtractor
{
    private readonly IFileTextExtractor _fileTextExtractor = fileTextExtractor;
    private readonly FileLimitOptions _fileOptions = fileOptions.Value;
    private readonly ZipOptions _zipOptions = zipOptions.Value;

    public async Task<ExtractResponse> ExtractFileAsync(
        IEnumerable<IFormFile> files,
        CancellationToken cancellationToken = default
    )
    {
        var items = new List<ExtractItemResult>();
        int totalFilesProcessed = 0;
        long cumulativeBytes = 0;

        foreach (var file in files)
        {
            if (totalFilesProcessed >= _fileOptions.MaxFileCount)
            {
                items.Add(
                    new ExtractItemResult(
                        "file",
                        file.FileName,
                        false,
                        null,
                        "Maximum file count exceeded",
                        0
                    )
                );
                continue;
            }

            if (file.Length > _fileOptions.MaxFileSizeBytes)
            {
                items.Add(
                    new ExtractItemResult(
                        "file",
                        file.FileName,
                        false,
                        null,
                        $"File size exceeds limit of {_fileOptions.MaxFileSizeBytes / 1024 / 1024}MB",
                        0
                    )
                );
                continue;
            }

            cumulativeBytes += file.Length;
            if (cumulativeBytes > _fileOptions.MaxTotalSizeBytes)
            {
                items.Add(
                    new ExtractItemResult(
                        "file",
                        file.FileName,
                        false,
                        null,
                        "Total upload size limit exceeded.",
                        0
                    )
                );
                break; // Stop processing further files
            }

            if (file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                var zipResults = await ExtractZipFileAsync(
                    file,
                    totalFilesProcessed,
                    cumulativeBytes,
                    cancellationToken
                );
                items.AddRange(zipResults);
                totalFilesProcessed += zipResults.Count(r => r.Success);
                // Update cumulative bytes based on extracted contents if needed,
                // but usually the zip itself is the primary limit for the request body.
                // However, for decompression risk, we track extracted size too.
            }
            else
            {
                using var stream = file.OpenReadStream();
                var result = await _fileTextExtractor.ExtractFileTextAsync(
                    stream,
                    file.FileName,
                    file.ContentType,
                    cancellationToken
                );

                items.Add(
                    new ExtractItemResult(
                        "file",
                        file.FileName,
                        result.Success,
                        result.ExtractedText,
                        result.ErrorMessage,
                        result.ExtractedText?.Length ?? 0
                    )
                );

                if (result.Success)
                    totalFilesProcessed++;
            }
        }

        var successCount = items.Count(i => i.Success);
        var failedCount = items.Count(i => !i.Success);

        return new ExtractResponse(items, new ExtractMeta(items.Count, successCount, failedCount));
    }

    private async Task<List<ExtractItemResult>> ExtractZipFileAsync(
        IFormFile zipFile,
        int currentFileCount,
        long cumulativeBytes,
        CancellationToken cancellationToken
    )
    {
        using var zipStream = zipFile.OpenReadStream();
        return await ExtractZipStreamAsync(
            zipStream,
            zipFile.FileName,
            0,
            currentFileCount,
            cumulativeBytes,
            cancellationToken
        );
    }

    private async Task<List<ExtractItemResult>> ExtractZipStreamAsync(
        Stream zipStream,
        string parentSource,
        int depth,
        int currentFileCount,
        long cumulativeBytes,
        CancellationToken cancellationToken
    )
    {
        var results = new List<ExtractItemResult>();

        if (depth > _zipOptions.MaxDepth)
        {
            return results; // Safety: stop recursion
        }

        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: true);

        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entry.Length > _zipOptions.MaxEntryBytes)
            {
                results.Add(
                    new ExtractItemResult(
                        "zip-entry",
                        $"{parentSource}:{entry.FullName}",
                        false,
                        null,
                        $"ZIP entry too large ({entry.Length / 1024 / 1024}MB)",
                        0
                    )
                );
                continue;
            }

            if (entry.Length == 0)
                continue;

            cumulativeBytes += entry.Length;
            if (cumulativeBytes > _fileOptions.MaxTotalSizeBytes)
            {
                results.Add(
                    new ExtractItemResult(
                        "zip-entry",
                        $"{parentSource}:{entry.FullName}",
                        false,
                        null,
                        "Total decompressed size limit exceeded.",
                        0
                    )
                );
                break;
            }

            if (currentFileCount + results.Count(r => r.Success) >= _fileOptions.MaxFileCount)
            {
                break; // Stop adding new files if limit reached
            }

            using var entryStream = entry.Open();
            using var memoryStream = new MemoryStream();
            await entryStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Position = 0;

            string currentSourceName = $"{parentSource}:{entry.FullName}";

            if (entry.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                var innerResults = await ExtractZipStreamAsync(
                    memoryStream,
                    currentSourceName,
                    depth + 1,
                    currentFileCount + results.Count(r => r.Success),
                    cumulativeBytes,
                    cancellationToken
                );
                results.AddRange(innerResults);
            }
            else
            {
                var extractResult = await _fileTextExtractor.ExtractFileTextAsync(
                    memoryStream,
                    entry.FullName,
                    GetContentTypeFromFileName(entry.Name),
                    cancellationToken
                );

                results.Add(
                    new ExtractItemResult(
                        "zip-entry",
                        currentSourceName,
                        extractResult.Success,
                        extractResult.ExtractedText,
                        extractResult.ErrorMessage,
                        extractResult.ExtractedText?.Length ?? 0
                    )
                );
            }
        }

        return results;
    }

    private string GetContentTypeFromFileName(string fileName)
    {
        if (fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            return "application/pdf";
        else if (fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        else if (fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            return "text/plain";

        return "application/octet-stream";
    }
}
