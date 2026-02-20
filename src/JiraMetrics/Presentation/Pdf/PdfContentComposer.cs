using System.Globalization;

using JiraMetrics.Abstractions;
using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace JiraMetrics.Presentation.Pdf;

/// <summary>
/// Default PDF content composer for Jira analytics report.
/// </summary>
public sealed class PdfContentComposer : IPdfContentComposer
{
    private const string OPEN_ISSUE_COLOR_HEX = "#dc2626";
    private const string DONE_ISSUE_COLOR_HEX = "#16a34a";
    private const string REJECTED_ISSUE_COLOR_HEX = "#f97316";
    private static readonly string[] _timelinePaletteHex =
    [
        "#0ea5e9",
        "#3b82f6",
        "#22d3ee",
        "#22c55e",
        "#eab308",
        "#f97316",
        "#f59e0b",
        "#9ca3af"
    ];

    /// <inheritdoc />
    public void ComposeContent(ColumnDescriptor column, JiraPdfReportData reportData)
    {
        ArgumentNullException.ThrowIfNull(column);
        ArgumentNullException.ThrowIfNull(reportData);

        column.Spacing(10);

        ComposeReleaseSection(column, reportData);
        ComposeBugRatioSection(column, reportData);
        ComposeTransitionSection(column, reportData);
        ComposePathSummarySection(column, reportData.PathSummary);
        ComposePathGroupsSection(column, reportData.PathGroups, reportData.Settings.BaseUrl);
        ComposeFailuresSection(column, reportData.Failures, reportData.Settings.BaseUrl);
    }

    private static void ComposeReleaseSection(ColumnDescriptor column, JiraPdfReportData reportData)
    {
        if (reportData.Settings.ReleaseReport is not { } releaseReport)
        {
            return;
        }

        _ = column.Item().Text("Release report").Bold().FontSize(12);
        _ = column
            .Item()
            .Text($"All releases by label \"{releaseReport.ProjectLabel}\"")
            .Bold()
            .FontColor(Colors.Red.Darken2);

        if (reportData.ReleaseIssues.Count == 0)
        {
            _ = column.Item().Text("No releases found for selected month.").FontColor(Colors.Grey.Darken1);
            return;
        }

        var includeComponents = !string.IsNullOrWhiteSpace(releaseReport.ComponentsFieldName);
        var jiraBaseUrl = reportData.Settings.BaseUrl;
        var orderedReleases = reportData.ReleaseIssues
            .OrderBy(static release => release.ReleaseDate)
            .ThenBy(static release => release.Key.Value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(26);
                columns.ConstantColumn(82);
                columns.ConstantColumn(76);
                columns.ConstantColumn(56);
                if (includeComponents)
                {
                    columns.ConstantColumn(76);
                }

                columns.RelativeColumn(4);
            });

            table.Header(header =>
            {
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("#");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Release Date");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Jira ID");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Tasks");
                if (includeComponents)
                {
                    _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Components");
                }

                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Title");
            });

            for (var i = 0; i < orderedReleases.Length; i++)
            {
                var release = orderedReleases[i];
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text((i + 1).ToString(CultureInfo.InvariantCulture));
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(release.ReleaseDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                var releaseIssueUrl = PdfPresentationHelpers.BuildIssueBrowseUrl(jiraBaseUrl, release.Key);
                _ = table.Cell()
                    .Element(PdfPresentationHelpers.StyleBodyCell)
                    .Hyperlink(releaseIssueUrl)
                    .DefaultTextStyle(static style => style.FontColor(Colors.Blue.Darken2).Underline())
                    .Text(release.Key.Value);
                _ = table.Cell()
                    .Element(PdfPresentationHelpers.StyleBodyCell)
                    .Text(release.Tasks == 0 ? "-" : release.Tasks.ToString(CultureInfo.InvariantCulture));
                if (includeComponents)
                {
                    _ = table.Cell()
                        .Element(PdfPresentationHelpers.StyleBodyCell)
                        .Text(release.Components == 0 ? "-" : release.Components.ToString(CultureInfo.InvariantCulture));
                }

                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(release.Title.Truncate(new TextLength(140)).Value);
            }
        });
    }

    private static void ComposeBugRatioSection(ColumnDescriptor column, JiraPdfReportData reportData)
    {
        if (!reportData.BugCreatedThisMonth.HasValue
            || !reportData.BugMovedToDoneThisMonth.HasValue
            || !reportData.BugRejectedThisMonth.HasValue
            || !reportData.BugFinishedThisMonth.HasValue)
        {
            return;
        }

        _ = column.Item().Text("Bug ratio").Bold().FontSize(12);

        var bugTypesLabel = reportData.Settings.BugIssueNames.Count == 0
            ? "-"
            : string.Join(", ", reportData.Settings.BugIssueNames.Select(static x => x.Value));
        var created = reportData.BugCreatedThisMonth.Value.Value;
        var finished = reportData.BugFinishedThisMonth.Value.Value;
        var resolvedRate = created == 0 ? "N/A" : $"{finished * 100.0 / created:0.##}%";

        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2.4f);
                columns.RelativeColumn(1.2f);
            });

            table.Header(header =>
            {
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Metric");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Value");
            });

            _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text("Bug issue types");
            _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(bugTypesLabel);
            _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text("Open this month");
            _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(created.ToString(CultureInfo.InvariantCulture));
            _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text("Done this month");
            _ = table.Cell()
                .Element(PdfPresentationHelpers.StyleBodyCell)
                .Text(reportData.BugMovedToDoneThisMonth.Value.Value.ToString(CultureInfo.InvariantCulture));
            _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text("Rejected this month");
            _ = table.Cell()
                .Element(PdfPresentationHelpers.StyleBodyCell)
                .Text(reportData.BugRejectedThisMonth.Value.Value.ToString(CultureInfo.InvariantCulture));
            _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text("Finished this month");
            _ = table.Cell()
                .Element(PdfPresentationHelpers.StyleBodyCell)
                .Text(reportData.BugFinishedThisMonth.Value.Value.ToString(CultureInfo.InvariantCulture));
            _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text("Resolved / Created");
            _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(resolvedRate);
        });

        ComposeIssueListItemsSection(
            column,
            "Open issues",
            reportData.BugOpenIssues,
            OPEN_ISSUE_COLOR_HEX,
            reportData.Settings.BaseUrl,
            includeCreationDate: true);
        ComposeIssueListItemsSection(
            column,
            "Done issues",
            reportData.BugDoneIssues,
            DONE_ISSUE_COLOR_HEX,
            reportData.Settings.BaseUrl,
            includeCreationDate: true);
        ComposeIssueListItemsSection(
            column,
            "Rejected issues",
            reportData.BugRejectedIssues,
            REJECTED_ISSUE_COLOR_HEX,
            reportData.Settings.BaseUrl,
            includeCreationDate: false);
    }

    private static void ComposeIssueListItemsSection(
        ColumnDescriptor column,
        string title,
        IReadOnlyList<IssueListItem> issues,
        string titleColorHex,
        JiraBaseUrl baseUrl,
        bool includeCreationDate)
    {
        _ = column.Item().Text(title).Bold().FontColor(titleColorHex);

        if (issues.Count == 0)
        {
            _ = column.Item().Text("No issues.").FontColor(Colors.Grey.Darken1);
            return;
        }

        var orderedIssues = issues
            .OrderBy(static issue => issue.Key.Value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(26);
                columns.ConstantColumn(80);
                if (includeCreationDate)
                {
                    columns.ConstantColumn(82);
                }

                columns.RelativeColumn(5);
            });

            table.Header(header =>
            {
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("#");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Jira ID");
                if (includeCreationDate)
                {
                    _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Creation Date");
                }

                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Title");
            });

            for (var i = 0; i < orderedIssues.Length; i++)
            {
                var issue = orderedIssues[i];
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text((i + 1).ToString(CultureInfo.InvariantCulture));
                var issueUrl = PdfPresentationHelpers.BuildIssueBrowseUrl(baseUrl, issue.Key);
                _ = table.Cell()
                    .Element(PdfPresentationHelpers.StyleBodyCell)
                    .Hyperlink(issueUrl)
                    .DefaultTextStyle(static style => style.FontColor(Colors.Blue.Darken2).Underline())
                    .Text(issue.Key.Value);
                if (includeCreationDate)
                {
                    var createdAtText = issue.CreatedAt.HasValue
                        ? issue.CreatedAt.Value.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                        : "-";
                    _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(createdAtText);
                }

                table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(text =>
                {
                    _ = text.Span(issue.Title.Truncate(new TextLength(140)).Value).FontColor(titleColorHex);
                });
            }
        });
    }

    private static void ComposeTransitionSection(ColumnDescriptor column, JiraPdfReportData reportData)
    {
        _ = column.Item().Text("Transition analysis").Bold().FontSize(12);

        ComposeIssueTimelineSection(
            column,
            "Issues moved to Done this month",
            reportData.DoneIssues,
            reportData.Settings.BaseUrl,
            reportData.Settings.DoneStatusName);

        if (reportData.Settings.RejectStatusName is { } rejectStatusName)
        {
            ComposeIssueTimelineSection(
                column,
                "Issues moved to Rejected this month",
                reportData.RejectedIssues,
                reportData.Settings.BaseUrl,
                rejectStatusName);
        }
    }

    private static void ComposeIssueTimelineSection(
        ColumnDescriptor column,
        string title,
        IReadOnlyList<IssueTimeline> issues,
        JiraBaseUrl baseUrl,
        StatusName targetStatusName)
    {
        _ = column.Item().Text(title).Bold();

        if (issues.Count == 0)
        {
            _ = column.Item().Text("No issues.").FontColor(Colors.Grey.Darken1);
            return;
        }

        var orderedIssues = issues
            .OrderBy(static issue => issue.Key.Value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(26);
                columns.ConstantColumn(74);
                columns.ConstantColumn(74);
                columns.ConstantColumn(64);
                columns.ConstantColumn(44);
                columns.ConstantColumn(90);
                columns.RelativeColumn(4);
            });

            table.Header(header =>
            {
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("#");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Issue");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Type");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Sub-items");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Code");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("At");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Summary");
            });

            for (var i = 0; i < orderedIssues.Length; i++)
            {
                var issue = orderedIssues[i];
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text((i + 1).ToString(CultureInfo.InvariantCulture));
                var issueUrl = PdfPresentationHelpers.BuildIssueBrowseUrl(baseUrl, issue.Key);
                _ = table.Cell()
                    .Element(PdfPresentationHelpers.StyleBodyCell)
                    .Hyperlink(issueUrl)
                    .DefaultTextStyle(static style => style.FontColor(Colors.Blue.Darken2).Underline())
                    .Text(issue.Key.Value);
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(issue.IssueType.Value);
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(issue.SubItemsCount.ToString(CultureInfo.InvariantCulture));
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(issue.HasPullRequest ? "+" : string.Empty);
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(BuildLastStatusAtText(issue, targetStatusName));
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(issue.Summary.Truncate(new TextLength(140)).Value);
            }
        });
    }

    private static void ComposePathSummarySection(ColumnDescriptor column, PathGroupsSummary summary)
    {
        _ = column.Item().Text("Path groups summary").Bold().FontSize(12);

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

    private static void ComposePathGroupsSection(
        ColumnDescriptor column,
        IReadOnlyList<PathGroup> pathGroups,
        JiraBaseUrl baseUrl)
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
            _ = column.Item().Text("TTM 75P: " + PdfPresentationHelpers.ToDurationLabel(group.TotalP75));

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
                        .Text(PdfPresentationHelpers.ToDurationLabel(transition.P75Duration));
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
        var stageColorItems = BuildStageColors(stageDurations);
        var stageColorByName = stageColorItems.ToDictionary(
            static item => item.stage,
            static item => item.colorHex,
            StringComparer.OrdinalIgnoreCase);
        var stageWeights = BuildStageWeights(stageDurations);

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

    private static List<(string stage, string colorHex)> BuildStageColors(
        List<(string stage, TimeSpan duration)> stageDurations)
    {
        var orderedStages = new List<string>();
        foreach (var (stage, _) in stageDurations)
        {
            if (!orderedStages.Contains(stage, StringComparer.OrdinalIgnoreCase))
            {
                orderedStages.Add(stage);
            }
        }

        var colorItems = new List<(string stage, string colorHex)>(orderedStages.Count);
        for (var index = 0; index < orderedStages.Count; index++)
        {
            colorItems.Add((orderedStages[index], _timelinePaletteHex[index % _timelinePaletteHex.Length]));
        }

        return colorItems;
    }

    private static List<float> BuildStageWeights(List<(string stage, TimeSpan duration)> stageDurations)
    {
        if (stageDurations.Count == 0)
        {
            return [];
        }

        return [.. stageDurations
            .Select(static segment => (float)Math.Max(0.001, Math.Max(0.0, segment.duration.TotalSeconds)))];
    }

    private static void ComposeFailuresSection(
        ColumnDescriptor column,
        IReadOnlyList<LoadFailure> failures,
        JiraBaseUrl baseUrl)
    {
        if (failures.Count == 0)
        {
            return;
        }

        _ = column.Item().Text("Failed issues").Bold().FontSize(12).FontColor(Colors.Red.Darken2);

        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(26);
                columns.ConstantColumn(86);
                columns.RelativeColumn(4);
            });

            table.Header(header =>
            {
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("#");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Issue");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Reason");
            });

            for (var i = 0; i < failures.Count; i++)
            {
                var failure = failures[i];
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text((i + 1).ToString(CultureInfo.InvariantCulture));
                var issueUrl = PdfPresentationHelpers.BuildIssueBrowseUrl(baseUrl, failure.IssueKey);
                _ = table.Cell()
                    .Element(PdfPresentationHelpers.StyleBodyCell)
                    .Hyperlink(issueUrl)
                    .DefaultTextStyle(static style => style.FontColor(Colors.Blue.Darken2).Underline())
                    .Text(failure.IssueKey.Value);
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(failure.Reason.Value);
            }
        });
    }

    private static string BuildLastStatusAtText(IssueTimeline issue, StatusName statusName)
    {
        var lastTimestamp = issue.Transitions
            .Where(transition => string.Equals(transition.To.Value, statusName.Value, StringComparison.OrdinalIgnoreCase))
            .Select(static transition => (DateTimeOffset?)transition.At)
            .OrderByDescending(static timestamp => timestamp)
            .FirstOrDefault();

        return lastTimestamp.HasValue
            ? lastTimestamp.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
            : "-";
    }
}

