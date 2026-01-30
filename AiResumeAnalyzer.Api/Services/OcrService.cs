using System.Text;
using AiResumeAnalyzer.Api.Options;
using AiResumeAnalyzer.Api.Services.Interfaces;
using Microsoft.Extensions.Options;
using PDFtoImage;
using Tesseract;

namespace AiResumeAnalyzer.Api.Services;

public sealed class OcrService(IOptions<OcrOptions> ocrOptions, ILogger<OcrService> logger)
    : IOcrService, IDisposable
{
    private readonly OcrOptions _ocrOptions = ocrOptions.Value;
    private readonly ILogger<OcrService> _logger = logger;
    private readonly string _tessDataPath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "tessdata"
    );

    public async Task<string> ExtractTextFromImageAsync(
        Stream imageStream,
        CancellationToken cancellationToken = default
    )
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedCts.CancelAfter(TimeSpan.FromSeconds(_ocrOptions.TimeoutSeconds));

        try
        {
            if (imageStream.CanSeek)
                imageStream.Position = 0;

            using var engine = new TesseractEngine(
                _tessDataPath,
                _ocrOptions.Language,
                EngineMode.Default
            );

            // Tesseract requires a Pix object or a file path.
            // We can load from stream by converting to byte array.
            using var ms = new MemoryStream();
            await imageStream.CopyToAsync(ms, linkedCts.Token);
            var imageBytes = ms.ToArray();

            using var img = Pix.LoadFromMemory(imageBytes);
            using var page = engine.Process(img);

            linkedCts.Token.ThrowIfCancellationRequested();

            return page.GetText();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "OCR Image extraction timed out after {Timeout}s.",
                _ocrOptions.TimeoutSeconds
            );
            return "[OCR Timeout]";
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("OCR Image extraction cancelled.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OCR failed for image.");
            return string.Empty;
        }
    }

    public async Task<string> ExtractTextFromPdfPagesAsync(
        Stream pdfStream,
        CancellationToken cancellationToken = default
    )
    {
        var sb = new StringBuilder();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedCts.CancelAfter(TimeSpan.FromSeconds(_ocrOptions.TimeoutSeconds));

        try
        {
            if (pdfStream.CanSeek)
                pdfStream.Position = 0;

            // Render PDF pages to images using PDFtoImage
            var pageCount = 0;

            // We need to use a library that can iterate through PDF pages and render them.
            // PDFtoImage provides Conversion.ToImages which returns an IEnumerable<SKBitmap>
            // or we can use the stream-based approach.

            foreach (var pageImage in Conversion.ToImages(pdfStream))
            {
                linkedCts.Token.ThrowIfCancellationRequested();

                pageCount++;
                _logger.LogDebug("OCR Processing page {Page}", pageCount);

                using var ms = new MemoryStream();
                pageImage.Encode(ms, SkiaSharp.SKEncodedImageFormat.Png, 100);
                ms.Position = 0;

                var text = await ExtractTextFromImageAsync(ms, linkedCts.Token);
                sb.AppendLine(text);

                pageImage.Dispose();
            }

            return sb.ToString();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "OCR PDF page extraction timed out after {Timeout}s.",
                _ocrOptions.TimeoutSeconds
            );
            sb.AppendLine("[OCR Timeout]");
            return sb.ToString();
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("OCR PDF page extraction cancelled.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OCR failed for PDF pages.");
            return sb.ToString();
        }
    }

    public void Dispose()
    {
        // Engine is created per request to avoid multi-threading issues in Tesseract
    }
}
