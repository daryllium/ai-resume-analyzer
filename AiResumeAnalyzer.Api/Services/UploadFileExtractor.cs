using System.IO.Compression;
using AiResumeAnalyzer.Api.Contracts;

namespace AiResumeAnalyzer.Api.Services;

public sealed class UploadFileExtractor(IFileTextExtractor fileTextExtractor) : IUploadFileExtractor
{
    private readonly IFileTextExtractor _fileTextExtractor = fileTextExtractor;

    public async Task<ExtractResponse> ExtractFileAsync(IEnumerable<IFormFile> files)
    {
        var items = new List<ExtractItemResult>();

        foreach (var file in files)
        {
            if (file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                var zipResults = await ExtractZipFileAsync(file);
                items.AddRange(zipResults);
            }
            else
            {
                using var stream = file.OpenReadStream();
                var result = await _fileTextExtractor.ExtractFileTextAsync(
                    stream,
                    file.FileName,
                    file.ContentType
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
            }
        }

        var success = items.Count(i => i.Success);
        var failed = items.Count(i => !i.Success);

        return new ExtractResponse(items, new ExtractMeta(items.Count, success, failed));
    }

    private async Task<List<ExtractItemResult>> ExtractZipFileAsync(IFormFile zipFile)
    {
        var results = new List<ExtractItemResult>();

        using var zipStream = zipFile.OpenReadStream();
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        foreach (var entry in archive.Entries)
        {
            if (entry.Length == 0)
                continue;

            using var entryStream = entry.Open();
            using var memoryStream = new MemoryStream();
            await entryStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            var extractResult = await _fileTextExtractor.ExtractFileTextAsync(
                memoryStream,
                entry.FullName,
                GetContentTypeFromFileName(entry.Name)
            );

            results.Add(
                new ExtractItemResult(
                    "zip-entry",
                    $"{zipFile.FileName}:{entry.FullName}",
                    extractResult.Success,
                    extractResult.ExtractedText,
                    extractResult.ErrorMessage,
                    extractResult.ExtractedText?.Length ?? 0
                )
            );
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
