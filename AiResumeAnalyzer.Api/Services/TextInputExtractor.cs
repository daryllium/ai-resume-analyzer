using AiResumeAnalyzer.Api.Contracts;

namespace AiResumeAnalyzer.Api.Services;

public sealed class TextInputExtractor : ITextInputExtractor
{
    public async Task<ExtractResponse> ExtractTextInputAsync(List<string> textInputs)
    {
        var items = new List<ExtractItemResult>();

        for (int i = 0; i < textInputs.Count; i++)
        {
            var text = textInputs[i] ?? string.Empty;
            items.Add(new ExtractItemResult("text", $"textInput[{i}]", true, text, null));
        }

        var success = items.Count(i => i.Success);
        var failed = items.Count(i => !i.Success);

        return new ExtractResponse(items, new ExtractMeta(items.Count, success, failed));
    }
}
