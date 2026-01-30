using AiResumeAnalyzer.Api.Contracts;
using AiResumeAnalyzer.Api.Services.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Previewer;

namespace AiResumeAnalyzer.Api.Services;

public sealed class PdfExportService : IPdfExportService
{
    static PdfExportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public Task<byte[]> ExportToPdfAsync(AnalyzeResponse results, CancellationToken cancellationToken = default)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Verdana));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(column =>
                    {
                        column.Item().Text("AI Resume Analysis Report").FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);
                        column.Item().Text($"Generated on {DateTime.Now:f}").FontSize(10).FontColor(Colors.Grey.Medium);
                    });
                });

                page.Content().PaddingVertical(1, Unit.Centimetre).Column(x =>
                {
                    x.Spacing(20);

                    // Summary Section
                    x.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Cell().Row(1).Column(1).Text("Total Resumes Processed").SemiBold();
                        table.Cell().Row(1).Column(2).Text(results.Meta.ProcessedResumes.ToString());

                        table.Cell().Row(2).Column(1).Text("Failed Processes").SemiBold();
                        table.Cell().Row(2).Column(2).Text(results.Meta.FailedResumes.ToString()).FontColor(results.Meta.FailedResumes > 0 ? Colors.Red.Medium : Colors.Green.Medium);
                    });

                    // Candidate Details
                    foreach (var result in results.Results.OrderByDescending(r => r.MatchScore ?? 0))
                    {
                        x.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(10).Column(col =>
                        {
                            col.Spacing(10);

                            col.Item().Row(row =>
                            {
                                row.RelativeItem().Text(result.Candidate?.Name ?? result.SourceName).FontSize(14).SemiBold();
                                if (result.MatchScore.HasValue)
                                {
                                    row.AutoItem().Text($"{result.MatchScore}% Match").FontSize(14).SemiBold().FontColor(GetScoreColor(result.MatchScore.Value));
                                }
                            });

                            if (!result.Success)
                            {
                                col.Item().Text($"Error: {result.Error}").FontColor(Colors.Red.Medium).Italic();
                                return;
                            }

                            if (result.Candidate != null)
                            {
                                col.Item().Row(row =>
                                {
                                    row.RelativeItem().Column(c =>
                                    {
                                        c.Item().Text("Candidate Profile").SemiBold();
                                        c.Item().Text($"Email: {result.Candidate.Email ?? "N/A"}");
                                        c.Item().Text($"Experience: {result.Candidate.YearsExperience?.ToString() ?? "N/A"} years");
                                    });

                                    row.RelativeItem().Column(c =>
                                    {
                                        c.Item().Text("Recommendation").SemiBold();
                                        c.Item().Text(result.MatchLevel ?? "N/A").FontColor(GetScoreColor(result.MatchScore ?? 0));
                                        c.Item().Text(result.IsRecommended == true ? "Recommended" : "Not Recommended").SemiBold();
                                    });
                                });

                                if (result.Candidate.Skills.Any())
                                {
                                    col.Item().Text(t =>
                                    {
                                        t.Span("Skills: ").SemiBold();
                                        t.Span(string.Join(", ", result.Candidate.Skills));
                                    });
                                }
                            }

                            if (!string.IsNullOrEmpty(result.AnalysisSummary))
                            {
                                col.Item().Column(c =>
                                {
                                    c.Item().Text("Analysis").SemiBold();
                                    c.Item().Text(result.AnalysisSummary).Justify();
                                });
                            }

                            if (result.MissingSkills != null && result.MissingSkills.Any())
                            {
                                col.Item().Column(c =>
                                {
                                    c.Item().Text("Missing Skills / Gaps").SemiBold().FontColor(Colors.Red.Medium);
                                    c.Item().Text(string.Join(", ", result.MissingSkills));
                                });
                            }
                        });
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                });
            });
        });

        return Task.FromResult(document.GeneratePdf());
    }

    private string GetScoreColor(int score)
    {
        return score switch
        {
            >= 85 => Colors.Green.Medium,
            >= 70 => Colors.LightGreen.Medium,
            >= 55 => Colors.Orange.Medium,
            _ => Colors.Red.Medium
        };
    }
}
