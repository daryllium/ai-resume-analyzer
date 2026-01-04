using AiResumeAnalyzer.Api.Contracts;
using AiResumeAnalyzer.Api.Requests;

namespace AiResumeAnalyzer.Api.Services;

public sealed class Analyzer(
    IUploadFileExtractor uploadFileExtractor,
    ITextInputExtractor textInputExtractor
) : IAnalyzer
{
    private readonly IUploadFileExtractor _uploadFileExtractor = uploadFileExtractor;
    private readonly ITextInputExtractor _textInputExtractor = textInputExtractor;

    public async Task<AnalyzeResponse> AnalyzeAsync(AnalyzeRequest request)
    {
        var allExtractedText = new List<string>();
        var results = new List<AnalyzeResultItem>();

        if (request.UploadFiles?.Count > 0)
        {
            var fileResponse = await _uploadFileExtractor.ExtractFileAsync(request.UploadFiles);
            foreach (
                var item in fileResponse.Items.Where(x =>
                    x.Success && !string.IsNullOrEmpty(x.ExtractedText)
                )
            )
                allExtractedText.Add(item.ExtractedText!);
        }

        if (request.UploadText?.Count > 0)
        {
            var textResponse = await _textInputExtractor.ExtractTextInputAsync(request.UploadText);
            foreach (
                var item in textResponse.Items.Where(x =>
                    x.Success && !string.IsNullOrEmpty(x.ExtractedText)
                )
            )
                allExtractedText.Add(item.ExtractedText!);
        }

        return new AnalyzeResponse(results, new AnalyzeMeta(allExtractedText.Count, 0));
    }
}
