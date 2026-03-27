using System.Globalization;

using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace JiraMetrics.Presentation.Pdf;

internal sealed class PdfPathGroupsSection : IPdfReportSection
{
    public void Compose(ColumnDescriptor column, JiraPdfReportData reportData)
    {
        ComposePathSummarySection(column, reportData.PathSummary);
        ComposePathGroupsSectionCore(
            column,
            reportData.PathGroups,
            reportData.Settings.BaseUrl,
            reportData.Settings.ShowTimeCalculationsInHoursOnly);
    }

    private static void ComposePathSummarySection(ColumnDescriptor column, PathGroupsSummary summary)
    {
        _ = column.Item().Text("Path groups summary").Bold().FontSize(12);
        _ = column.Item().Text("Filter: only tasks with code artefacts (pull request activity).").FontColor(Colors.Grey.Darken1);

        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2.4f);
                columns.RelativeColumn(1.1f);
            });

            table.Header(header =>
            {
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Metric");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Value");
            });

            _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text("Successful");
            _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(summary.SuccessfulCount.Value.ToString(CultureInfo.InvariantCulture));
            _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text("Matched stage");
            _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(summary.MatchedStageCount.Value.ToString(CultureInfo.InvariantCulture));
            _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text("Failed");
            _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(summary.FailedCount.Value.ToString(CultureInfo.InvariantCulture));
            _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text("Path groups");
            _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(summary.PathGroupCount.Value.ToString(CultureInfo.InvariantCulture));
        });
    }

    private static void ComposePathGroupsSectionCore(
        ColumnDescriptor column,
        IReadOnlyList<PathGroup> pathGroups,
        JiraBaseUrl baseUrl,
        bool showTimeCalculationsInHoursOnly)
    {
        _ = column.Item().Text("Path groups").Bold().FontSize(12);

        if (pathGroups.Count == 0)
        {
            _ = column.Item().Text("No path groups.").FontColor(Colors.Grey.Darken1);
            return;
        }

        for (var i = 0; i < pathGroups.Count; i++)
        {
            var group = pathGroups[i];
            _ = column
                .Item()
                .Text($"Group {i + 1} - {group.Issues.Count} issue(s)")
                .Bold();
            _ = column.Item().Text("Path: " + group.PathLabel.Value);
            column.Item().Text(text =>
            {
                _ = text.Span("Issues: ");
                for (var issueIndex = 0; issueIndex < group.Issues.Count; issueIndex++)
                {
                    if (issueIndex > 0)
                    {
                        _ = text.Span(", ");
                    }

                    var issue = group.Issues[issueIndex];
                    var issueUrl = PdfPresentationHelpers.BuildIssueBrowseUrl(baseUrl, issue.Key);
                    _ = text.Hyperlink(issue.Key.Value, issueUrl).FontColor(Colors.Blue.Darken2).Underline();
                }
            });
            _ = column
                .Item()
                .Text("TTM 75P: " + PdfPresentationHelpers.ToDurationLabel(group.TotalP75, showTimeCalculationsInHoursOnly));

            if (group.P75Transitions.Count == 0)
            {
                _ = column.Item().Text("No transitions in this path.").FontColor(Colors.Grey.Darken1);
                continue;
            }

            ComposeTimelineDiagramSection(column, group.P75Transitions);

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2.2f);
                });

                table.Header(header =>
                {
                    _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("From");
                    _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("To");
                    _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("P75 Time");
                });

                foreach (var transition in group.P75Transitions)
                {
                    _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(transition.From.Value);
                    _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(transition.To.Value);
                    _ = table.Cell()
                        .Element(PdfPresentationHelpers.StyleBodyCell)
                        .Text(PdfPresentationHelpers.ToDurationLabel(transition.P75Duration, showTimeCalculationsInHoursOnly));
                }
            });
        }
    }

    private static void ComposeTimelineDiagramSection(
        ColumnDescriptor column,
        IReadOnlyList<PercentileTransition> transitions)
    {
        ArgumentNullException.ThrowIfNull(column);
        ArgumentNullException.ThrowIfNull(transitions);

        if (transitions.Count == 0)
        {
            return;
        }

        _ = column.Item().Text("Timeline Diagram").Bold();

        var stageDurations = transitions
            .Select(static transition => (
                stage: transition.From.Value,
                duration: transition.P75Duration < TimeSpan.Zero ? TimeSpan.Zero : transition.P75Duration))
            .ToList();
        var stageColorItems = PdfPresentationFormatting.BuildStageColors(stageDurations);
        var stageColorByName = stageColorItems.ToDictionary(
            static item => item.stage,
            static item => item.colorHex,
            StringComparer.OrdinalIgnoreCase);
        var stageWeights = PdfPresentationFormatting.BuildStageWeights(stageDurations);

        column.Item().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Column(diagram =>
        {
            diagram.Spacing(4);

            diagram.Item().Height(16).Row(row =>
            {
                for (var index = 0; index < stageDurations.Count; index++)
                {
                    var stageName = stageDurations[index].stage;
                    var colorHex = stageColorByName.TryGetValue(stageName, out var resolvedColorHex)
                        ? resolvedColorHex
                        : "#9ca3af";

                    _ = row.RelativeItem(stageWeights[index]).Background(colorHex).Height(16);
                }
            });

            diagram.Item().Text(text =>
            {
                for (var index = 0; index < stageColorItems.Count; index++)
                {
                    if (index > 0)
                    {
                        _ = text.Span("  ");
                    }

                    _ = text.Span("[ ] ").FontColor(stageColorItems[index].colorHex);
                    _ = text.Span(stageColorItems[index].stage);
                }
            });
        });
    }
}
