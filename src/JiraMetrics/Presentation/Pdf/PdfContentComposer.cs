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
        ComposeArchTasksSection(column, reportData);
        ComposeGlobalIncidentsSection(column, reportData);
        ComposeAllTasksRatioSection(column, reportData);
        ComposeBugRatioSection(column, reportData);
        ComposeTransitionSection(column, reportData);
        ComposePathSummarySection(column, reportData.PathSummary);
        ComposePathGroupsSection(
            column,
            reportData.PathGroups,
            reportData.Settings.BaseUrl,
            reportData.Settings.ShowTimeCalculationsInHoursOnly);
        ComposeOpenIssuesByStatusSection(column, reportData);
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
        _ = column
            .Item()
            .Text($"Hot-fix markers: {BuildHotFixRulesText(releaseReport.HotFixRules)}")
            .FontColor(Colors.Grey.Darken1);

        if (reportData.ReleaseIssues.Count == 0)
        {
            _ = column.Item().Text("No releases found for selected month.").FontColor(Colors.Grey.Darken1);
            _ = column.Item().Text("Total releases: 0    Hotfix count: 0    Rollbacks count: 0").FontColor(Colors.Grey.Darken1);
            return;
        }

        var includeComponents = !string.IsNullOrWhiteSpace(releaseReport.ComponentsFieldName);
        var includeEnvironments = !string.IsNullOrWhiteSpace(releaseReport.EnvironmentFieldName);
        var jiraBaseUrl = reportData.Settings.BaseUrl;
        var orderedReleases = reportData.ReleaseIssues
            .OrderBy(static release => release.ReleaseDate)
            .ThenBy(static release => release.Key.Value, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var hotFixCount = orderedReleases.Count(static release => release.IsHotFix);
        var rollbackCount = orderedReleases.Count(static release => !string.IsNullOrWhiteSpace(release.RollbackType));

        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(26);
                columns.ConstantColumn(82);
                columns.ConstantColumn(72);
                columns.ConstantColumn(86);
                columns.ConstantColumn(52);
                if (includeComponents)
                {
                    columns.ConstantColumn(72);
                }
                if (includeEnvironments)
                {
                    columns.ConstantColumn(110);
                }

                columns.ConstantColumn(96);
                columns.RelativeColumn(4);
            });

            table.Header(header =>
            {
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("#");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Release Date");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Jira ID");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Status");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Tasks");
                if (includeComponents)
                {
                    _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Components");
                }
                if (includeEnvironments)
                {
                    _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Environments");
                }

                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Rollback type");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Title");
            });

            for (var i = 0; i < orderedReleases.Length; i++)
            {
                var release = orderedReleases[i];
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text((i + 1).ToString(CultureInfo.InvariantCulture));
                if (release.IsHotFix)
                {
                    var hotFixColor = Colors.Red.Darken2;
                    table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(text =>
                    {
                        _ = text.Span(release.ReleaseDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).FontColor(hotFixColor);
                    });
                }
                else
                {
                    _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(release.ReleaseDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
                }
                var releaseIssueUrl = PdfPresentationHelpers.BuildIssueBrowseUrl(jiraBaseUrl, release.Key);
                if (release.IsHotFix)
                {
                    var hotFixLinkColor = Colors.Red.Darken2;
                    _ = table.Cell()
                        .Element(PdfPresentationHelpers.StyleBodyCell)
                        .Hyperlink(releaseIssueUrl)
                        .DefaultTextStyle(style => style.FontColor(hotFixLinkColor).Underline())
                        .Text(release.Key.Value);
                }
                else
                {
                    _ = table.Cell()
                        .Element(PdfPresentationHelpers.StyleBodyCell)
                        .Hyperlink(releaseIssueUrl)
                        .DefaultTextStyle(static style => style.FontColor(Colors.Blue.Darken2).Underline())
                        .Text(release.Key.Value);
                }

                if (release.IsHotFix)
                {
                    var hotFixStatusColor = Colors.Red.Darken2;
                    table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(text =>
                    {
                        _ = text.Span(release.Status.Value).FontColor(hotFixStatusColor);
                    });
                }
                else
                {
                    _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(release.Status.Value);
                }

                var tasksText = release.Tasks == 0 ? "-" : release.Tasks.ToString(CultureInfo.InvariantCulture);
                if (release.IsHotFix)
                {
                    var hotFixTasksColor = Colors.Red.Darken2;
                    table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(text =>
                    {
                        _ = text.Span(tasksText).FontColor(hotFixTasksColor);
                    });
                }
                else
                {
                    _ = table.Cell()
                        .Element(PdfPresentationHelpers.StyleBodyCell)
                        .Text(tasksText);
                }
                if (includeComponents)
                {
                    var componentsText = release.Components == 0 ? "-" : release.Components.ToString(CultureInfo.InvariantCulture);
                    if (release.IsHotFix)
                    {
                        var hotFixComponentsColor = Colors.Red.Darken2;
                        table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(text =>
                        {
                            _ = text.Span(componentsText).FontColor(hotFixComponentsColor);
                        });
                    }
                    else
                    {
                        _ = table.Cell()
                            .Element(PdfPresentationHelpers.StyleBodyCell)
                            .Text(componentsText);
                    }
                }

                if (includeEnvironments)
                {
                    var environmentsText = release.EnvironmentNames.Count == 0
                        ? "-"
                        : string.Join(", ", release.EnvironmentNames);
                    if (release.IsHotFix)
                    {
                        var hotFixEnvironmentColor = Colors.Red.Darken2;
                        table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(text =>
                        {
                            _ = text.Span(environmentsText).FontColor(hotFixEnvironmentColor);
                        });
                    }
                    else
                    {
                        _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(environmentsText);
                    }
                }

                var rollbackText = string.IsNullOrWhiteSpace(release.RollbackType) ? "-" : release.RollbackType;
                if (release.IsHotFix)
                {
                    var hotFixRollbackColor = Colors.Red.Darken2;
                    table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(text =>
                    {
                        _ = text.Span(rollbackText).FontColor(hotFixRollbackColor);
                    });
                }
                else
                {
                    _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(rollbackText);
                }

                var titleText = release.Title.Truncate(new TextLength(140)).Value;
                if (release.IsHotFix)
                {
                    var hotFixTitleColor = Colors.Red.Darken2;
                    table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(text =>
                    {
                        _ = text.Span(titleText).FontColor(hotFixTitleColor);
                    });
                }
                else
                {
                    _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(titleText);
                }
            }
        });

        _ = column
            .Item()
            .Text($"Total releases: {orderedReleases.Length}    Hotfix count: {hotFixCount}    Rollbacks count: {rollbackCount}")
            .FontColor(Colors.Grey.Darken1);

        if (!includeComponents)
        {
            return;
        }

        var componentSummaries = BuildComponentReleaseSummaries(orderedReleases);
        _ = column.Item().Text("Components release table").Bold();
        if (componentSummaries.Count == 0)
        {
            _ = column.Item().Text("No components data.").FontColor(Colors.Grey.Darken1);
            return;
        }

        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(26);
                columns.RelativeColumn(3);
                columns.RelativeColumn(1);
            });

            table.Header(header =>
            {
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("#");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Component name");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Release counts");
            });

            for (var i = 0; i < componentSummaries.Count; i++)
            {
                var (componentName, releaseCount) = componentSummaries[i];
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text((i + 1).ToString(CultureInfo.InvariantCulture));
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(componentName);
                _ = table.Cell()
                    .Element(PdfPresentationHelpers.StyleBodyCell)
                    .Text(releaseCount.ToString(CultureInfo.InvariantCulture));
            }
        });
    }

    private static void ComposeArchTasksSection(ColumnDescriptor column, JiraPdfReportData reportData)
    {
        if (reportData.Settings.ArchTasksReport is not { } archTasksReport)
        {
            return;
        }

        _ = column.Item().Text("Architecture tasks report").Bold().FontSize(12);
        _ = column.Item().Text($"JQL: {archTasksReport.Jql}").FontColor(Colors.Grey.Darken1);

        var resolvedCount = reportData.ArchTasks.Count(static task => task.IsResolved);
        var openCount = reportData.ArchTasks.Count - resolvedCount;

        if (reportData.ArchTasks.Count == 0)
        {
            _ = column.Item().Text("No architecture tasks found for configured query.").FontColor(Colors.Grey.Darken1);
            _ = column.Item().Text("Total tasks: 0    Resolved: 0    Open: 0").FontColor(Colors.Grey.Darken1);
            return;
        }

        var now = DateTimeOffset.Now;
        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(26);
                columns.ConstantColumn(78);
                columns.ConstantColumn(92);
                columns.ConstantColumn(92);
                columns.ConstantColumn(70);
                columns.RelativeColumn(3.8f);
            });

            table.Header(header =>
            {
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("#");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Jira ID");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Created At");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Resolved At");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Days in work");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Title");
            });

            for (var i = 0; i < reportData.ArchTasks.Count; i++)
            {
                var task = reportData.ArchTasks[i];
                var issueUrl = PdfPresentationHelpers.BuildIssueBrowseUrl(reportData.Settings.BaseUrl, task.Key);

                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text((i + 1).ToString(CultureInfo.InvariantCulture));
                _ = table.Cell()
                    .Element(PdfPresentationHelpers.StyleBodyCell)
                    .Hyperlink(issueUrl)
                    .DefaultTextStyle(static style => style.FontColor(Colors.Blue.Darken2).Underline())
                    .Text(task.Key.Value);
                _ = table.Cell()
                    .Element(PdfPresentationHelpers.StyleBodyCell)
                    .Text(task.CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
                _ = table.Cell()
                    .Element(PdfPresentationHelpers.StyleBodyCell)
                    .Text(task.ResolvedAt.HasValue
                        ? task.ResolvedAt.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
                        : "-");
                table.Cell()
                    .Element(PdfPresentationHelpers.StyleBodyCell)
                    .Text(text =>
                    {
                        var span = text.Span(FormatCalendarDayDurationValue(task.GetElapsed(now)));
                        _ = span.FontColor(task.IsResolved ? Colors.Black : Colors.Red.Darken2);
                    });
                _ = table.Cell()
                    .Element(PdfPresentationHelpers.StyleBodyCell)
                    .Text(task.Title.Truncate(new TextLength(140)).Value);
            }
        });

        _ = column
            .Item()
            .Text($"Total tasks: {reportData.ArchTasks.Count}    Resolved: {resolvedCount}    Open: {openCount}")
            .FontColor(Colors.Grey.Darken1);
    }

    private static void ComposeGlobalIncidentsSection(ColumnDescriptor column, JiraPdfReportData reportData)
    {
        if (reportData.Settings.GlobalIncidentsReport is not { } globalIncidentsReport)
        {
            return;
        }

        _ = column.Item().Text("Global incidents report").Bold().FontSize(12);
        _ = column.Item().Text($"Namespace: {globalIncidentsReport.Namespace}").FontColor(Colors.Grey.Darken1);
        if (!string.IsNullOrWhiteSpace(globalIncidentsReport.JqlFilter))
        {
            _ = column.Item().Text($"JQL filter: {globalIncidentsReport.JqlFilter}").FontColor(Colors.Grey.Darken1);
        }
        else if (!string.IsNullOrWhiteSpace(globalIncidentsReport.SearchPhrase))
        {
            _ = column.Item().Text($"Search phrase: {globalIncidentsReport.SearchPhrase}").FontColor(Colors.Grey.Darken1);
        }

        if (globalIncidentsReport.AdditionalFieldNames.Count > 0)
        {
            _ = column
                .Item()
                .Text("Additional fields: " + string.Join(", ", globalIncidentsReport.AdditionalFieldNames))
                .FontColor(Colors.Grey.Darken1);
        }

        if (reportData.GlobalIncidents.Count == 0)
        {
            _ = column.Item().Text("No incidents found for selected month.").FontColor(Colors.Grey.Darken1);
            return;
        }

        var includeAdditionalFields = globalIncidentsReport.AdditionalFieldNames.Count > 0;
        var orderedIncidents = reportData.GlobalIncidents
            .OrderBy(static incident => incident.IncidentStartUtc)
            .ThenBy(static incident => incident.Key.Value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(26);
                columns.ConstantColumn(78);
                columns.RelativeColumn(3.6f);
                columns.ConstantColumn(92);
                columns.ConstantColumn(92);
                columns.ConstantColumn(68);
                columns.ConstantColumn(84);
                columns.ConstantColumn(72);
                if (includeAdditionalFields)
                {
                    columns.RelativeColumn(2.6f);
                }
            });

            table.Header(header =>
            {
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("#");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Jira ID");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Title");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Incident Start UTC");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Incident Recovery UTC");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Duration");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Impact");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Urgency");
                if (includeAdditionalFields)
                {
                    _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Additional fields");
                }
            });

            for (var i = 0; i < orderedIncidents.Length; i++)
            {
                var incident = orderedIncidents[i];
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text((i + 1).ToString(CultureInfo.InvariantCulture));
                var incidentUrl = PdfPresentationHelpers.BuildIssueBrowseUrl(reportData.Settings.BaseUrl, incident.Key);
                _ = table.Cell()
                    .Element(PdfPresentationHelpers.StyleBodyCell)
                    .Hyperlink(incidentUrl)
                    .DefaultTextStyle(static style => style.FontColor(Colors.Blue.Darken2).Underline())
                    .Text(incident.Key.Value);
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(incident.Title.Truncate(new TextLength(140)).Value);
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(FormatIncidentDateTimeUtc(incident.IncidentStartUtc));
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(FormatIncidentDateTimeUtc(incident.IncidentRecoveryUtc));
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(
                    FormatIncidentDuration(incident.Duration, reportData.Settings.ShowTimeCalculationsInHoursOnly));
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(incident.Impact ?? "-");
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(incident.Urgency ?? "-");
                if (includeAdditionalFields)
                {
                    var additionalFields = incident.AdditionalFields.Count == 0
                        ? "-"
                        : string.Join(
                            Environment.NewLine,
                            incident.AdditionalFields.Select(static pair => $"{pair.Key}: {pair.Value}"));
                    _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(additionalFields);
                }
            }
        });

        var totalDuration = SumIncidentDurations(orderedIncidents);
        _ = column
            .Item()
            .Text("Total duration: " + FormatIncidentDuration(totalDuration, reportData.Settings.ShowTimeCalculationsInHoursOnly))
            .FontColor(Colors.Grey.Darken1);
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

        var bugTypesLabel = reportData.Settings.BugIssueNames.Count == 0
            ? "-"
            : string.Join(", ", reportData.Settings.BugIssueNames.Select(static x => x.Value));
        ComposeRatioSection(
            column,
            "Bug ratio",
            "Bug issue types",
            bugTypesLabel,
            new ItemCount(reportData.BugOpenIssues.Count),
            reportData.BugCreatedThisMonth.Value,
            reportData.BugMovedToDoneThisMonth.Value,
            reportData.BugRejectedThisMonth.Value,
            reportData.BugFinishedThisMonth.Value);

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

    private static void ComposeAllTasksRatioSection(ColumnDescriptor column, JiraPdfReportData reportData)
    {
        if (!reportData.AllTasksCreatedThisMonth.HasValue
            || !reportData.AllTasksOpenThisMonth.HasValue
            || !reportData.AllTasksMovedToDoneThisMonth.HasValue
            || !reportData.AllTasksRejectedThisMonth.HasValue
            || !reportData.AllTasksFinishedThisMonth.HasValue)
        {
            return;
        }

        ComposeRatioSection(
            column,
            "All tasks ratio",
            "Issue types",
            "All",
            reportData.AllTasksOpenThisMonth.Value,
            reportData.AllTasksCreatedThisMonth.Value,
            reportData.AllTasksMovedToDoneThisMonth.Value,
            reportData.AllTasksRejectedThisMonth.Value,
            reportData.AllTasksFinishedThisMonth.Value);
    }

    private static void ComposeRatioSection(
        ColumnDescriptor column,
        string title,
        string scopeLabel,
        string scopeValue,
        ItemCount openThisMonth,
        ItemCount createdThisMonth,
        ItemCount movedToDoneThisMonth,
        ItemCount rejectedThisMonth,
        ItemCount finishedThisMonth)
    {
        _ = column.Item().Text(title).Bold().FontSize(12);

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

            _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(scopeLabel);
            _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(scopeValue);
            _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text("Open this month");
            _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(openThisMonth.Value.ToString(CultureInfo.InvariantCulture));
            _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text("Done this month");
            _ = table.Cell()
                .Element(PdfPresentationHelpers.StyleBodyCell)
                .Text(movedToDoneThisMonth.Value.ToString(CultureInfo.InvariantCulture));
            _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text("Rejected this month");
            _ = table.Cell()
                .Element(PdfPresentationHelpers.StyleBodyCell)
                .Text(rejectedThisMonth.Value.ToString(CultureInfo.InvariantCulture));
            _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text("Finished this month");
            _ = table.Cell()
                .Element(PdfPresentationHelpers.StyleBodyCell)
                .Text(finishedThisMonth.Value.ToString(CultureInfo.InvariantCulture));
            _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text("Finished / Created");
            _ = table.Cell()
                .Element(PdfPresentationHelpers.StyleBodyCell)
                .Text(BuildFinishedToCreatedRatioText(createdThisMonth, finishedThisMonth));
        });
    }

    private static string BuildFinishedToCreatedRatioText(ItemCount createdThisMonth, ItemCount finishedThisMonth)
    {
        return createdThisMonth.Value == 0
            ? "N/A"
            : $"{finishedThisMonth.Value * 100.0 / createdThisMonth.Value:0.##}%";
    }

    private static string BuildHotFixRulesText(IReadOnlyDictionary<string, IReadOnlyList<string>> hotFixRules)
    {
        return string.Join(
            "; ",
            hotFixRules
                .OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                .Select(static pair => $"{pair.Key} = {string.Join(", ", pair.Value)}"));
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
            reportData.Settings.DoneStatusName,
            "Done At",
            reportData.Settings.ShowTimeCalculationsInHoursOnly,
            includeCreatedAt: true,
            includeDaysAtWork: true);
        ComposeDoneDaysAtWork75PerTypeSection(
            column,
            reportData.DoneDaysAtWork75PerType,
            reportData.Settings.DoneStatusName,
            reportData.Settings.ShowTimeCalculationsInHoursOnly);

        if (reportData.Settings.RejectStatusName is { } rejectStatusName)
        {
            ComposeIssueTimelineSection(
                column,
                "Issues moved to Rejected this month",
                reportData.RejectedIssues,
                reportData.Settings.BaseUrl,
                rejectStatusName,
                "Rejected At",
                reportData.Settings.ShowTimeCalculationsInHoursOnly,
                includeCreatedAt: true,
                includeDaysAtWork: true);
        }
    }

    private static void ComposeIssueTimelineSection(
        ColumnDescriptor column,
        string title,
        IReadOnlyList<IssueTimeline> issues,
        JiraBaseUrl baseUrl,
        StatusName targetStatusName,
        string atColumnTitle,
        bool showTimeCalculationsInHoursOnly,
        bool includeCreatedAt = false,
        bool includeDaysAtWork = false)
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
                columns.RelativeColumn(4);
                if (includeCreatedAt)
                {
                    columns.ConstantColumn(82);
                }

                columns.ConstantColumn(90);
                if (includeDaysAtWork)
                {
                    columns.ConstantColumn(68);
                }
            });

            table.Header(header =>
            {
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("#");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Issue");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Type");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Sub-items");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Code");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Summary");
                if (includeCreatedAt)
                {
                    _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Created At");
                }

                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text(atColumnTitle);
                if (includeDaysAtWork)
                {
                    _ = header.Cell()
                        .Element(PdfPresentationHelpers.StyleHeaderCell)
                        .Text(GetWorkDurationColumnLabel(showTimeCalculationsInHoursOnly));
                }
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
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(issue.Summary.Truncate(new TextLength(140)).Value);
                if (includeCreatedAt)
                {
                    _ = table.Cell()
                        .Element(PdfPresentationHelpers.StyleBodyCell)
                        .Text(issue.Created.ToLocalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));
                }

                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(BuildLastStatusAtText(issue, targetStatusName));
                if (includeDaysAtWork)
                {
                    _ = table.Cell()
                        .Element(PdfPresentationHelpers.StyleBodyCell)
                        .Text(BuildWorkDurationText(issue, targetStatusName, showTimeCalculationsInHoursOnly));
                }
            }
        });
    }

    private static void ComposeDoneDaysAtWork75PerTypeSection(
        ColumnDescriptor column,
        IReadOnlyList<IssueTypeWorkDays75Summary> summaries,
        StatusName doneStatusName,
        bool showTimeCalculationsInHoursOnly)
    {
        _ = column
            .Item()
            .Text($"{GetWorkDuration75Title(showTimeCalculationsInHoursOnly)} per type (moved to {doneStatusName.Value})")
            .Bold();
        if (summaries.Count == 0)
        {
            _ = column.Item().Text("No data.").FontColor(Colors.Grey.Darken1);
            return;
        }

        var orderedSummaries = summaries
            .OrderByDescending(static item => item.DaysAtWorkP75)
            .ThenByDescending(static item => item.IssueCount.Value)
            .ThenBy(static item => item.IssueType.Value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(2f);
                columns.RelativeColumn(1f);
                columns.RelativeColumn(1.4f);
            });

            table.Header(header =>
            {
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Type");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Issues");
                _ = header.Cell()
                    .Element(PdfPresentationHelpers.StyleHeaderCell)
                    .Text(GetWorkDuration75Title(showTimeCalculationsInHoursOnly));
            });

            foreach (var summary in orderedSummaries)
            {
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(summary.IssueType.Value);
                _ = table.Cell()
                    .Element(PdfPresentationHelpers.StyleBodyCell)
                    .Text(summary.IssueCount.Value.ToString(CultureInfo.InvariantCulture));
                _ = table.Cell()
                    .Element(PdfPresentationHelpers.StyleBodyCell)
                    .Text(FormatWorkDurationValue(summary.DaysAtWorkP75, showTimeCalculationsInHoursOnly));
            }
        });
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

    private static void ComposePathGroupsSection(
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

    private static void ComposeOpenIssuesByStatusSection(ColumnDescriptor column, JiraPdfReportData reportData)
    {
        if (!reportData.Settings.ShowGeneralStatistics)
        {
            return;
        }

        _ = column.Item().Text("General statistics").Bold().FontSize(12);
        var generatedAt = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);
        _ = column.Item().Text("Data as of: " + generatedAt).FontColor(Colors.Grey.Darken1);
        _ = column.Item().Text("Scope: all not finished tasks").FontColor(Colors.Grey.Darken1);

        var excludedStatuses = reportData.Settings.RejectStatusName is { } rejectStatus
            ? $"{reportData.Settings.DoneStatusName.Value}, {rejectStatus.Value}"
            : reportData.Settings.DoneStatusName.Value;
        _ = column.Item().Text("Statuses excluded: " + excludedStatuses).FontColor(Colors.Grey.Darken1);

        if (reportData.OpenIssuesByStatus.Count == 0)
        {
            _ = column.Item().Text("No issues outside excluded statuses.").FontColor(Colors.Grey.Darken1);
            return;
        }

        var orderedStatuses = reportData.OpenIssuesByStatus
            .OrderByDescending(static summary => summary.Count.Value)
            .ThenBy(static summary => summary.Status.Value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(1.4f);
                columns.RelativeColumn(0.8f);
                columns.RelativeColumn(2.8f);
            });

            table.Header(header =>
            {
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Status");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Issues");
                _ = header.Cell().Element(PdfPresentationHelpers.StyleHeaderCell).Text("Breakdown by type");
            });

            foreach (var statusSummary in orderedStatuses)
            {
                var issueTypeBreakdown = statusSummary.IssueTypes.Count == 0
                    ? "-"
                    : string.Join(
                        Environment.NewLine,
                        statusSummary.IssueTypes
                            .OrderByDescending(static summary => summary.Count.Value)
                            .ThenBy(static summary => summary.IssueType.Value, StringComparer.OrdinalIgnoreCase)
                            .Select(summary =>
                                $"{summary.IssueType.Value} - {summary.Count.Value.ToString(CultureInfo.InvariantCulture)}"));

                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(statusSummary.Status.Value);
                _ = table.Cell()
                    .Element(PdfPresentationHelpers.StyleBodyCell)
                    .Text(statusSummary.Count.Value.ToString(CultureInfo.InvariantCulture));
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(issueTypeBreakdown);
            }
        });
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

    private static IReadOnlyList<(string componentName, int releaseCount)> BuildComponentReleaseSummaries(
        IReadOnlyList<ReleaseIssueItem> releases)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var release in releases)
        {
            foreach (var componentName in release.ComponentNames)
            {
                if (string.IsNullOrWhiteSpace(componentName))
                {
                    continue;
                }

                var normalized = componentName.Trim();
                counts[normalized] = counts.TryGetValue(normalized, out var currentCount)
                    ? currentCount + 1
                    : 1;
            }
        }

        return [.. counts
            .Select(static pair => (componentName: pair.Key, releaseCount: pair.Value))
            .OrderByDescending(static pair => pair.releaseCount)
            .ThenBy(static pair => pair.componentName, StringComparer.OrdinalIgnoreCase)];
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

    private static string BuildWorkDurationText(
        IssueTimeline issue,
        StatusName targetStatusName,
        bool showTimeCalculationsInHoursOnly)
    {
        var targetTransitionIndex = issue.Transitions
            .Select(static (transition, index) => (transition, index))
            .Where(item => string.Equals(item.transition.To.Value, targetStatusName.Value, StringComparison.OrdinalIgnoreCase))
            .Select(static item => item.index)
            .DefaultIfEmpty(-1)
            .Max();
        if (targetTransitionIndex < 0)
        {
            return "-";
        }

        var workDuration = issue.Transitions
            .Take(targetTransitionIndex + 1)
            .Aggregate(TimeSpan.Zero, static (sum, transition) => sum + transition.SincePrevious);

        return FormatWorkDurationValue(workDuration, showTimeCalculationsInHoursOnly);
    }

    private static string FormatIncidentDateTimeUtc(DateTimeOffset? value) =>
        value.HasValue
            ? value.Value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
            : "-";

    private static string FormatIncidentDuration(TimeSpan? duration, bool showTimeCalculationsInHoursOnly)
    {
        if (!duration.HasValue || duration.Value < TimeSpan.Zero)
        {
            return "-";
        }

        if (showTimeCalculationsInHoursOnly)
        {
            return DurationLabel.FromDuration(duration.Value, showTimeCalculationsInHoursOnly: true).Value;
        }

        var totalMinutes = (int)Math.Round(duration.Value.TotalMinutes, MidpointRounding.AwayFromZero);
        var days = totalMinutes / (24 * 60);
        var hours = totalMinutes % (24 * 60) / 60;
        var minutes = totalMinutes % 60;

        if (days > 0)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}d {1}h {2}m", days, hours, minutes);
        }

        if (hours > 0)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}h {1}m", hours, minutes);
        }

        return string.Format(CultureInfo.InvariantCulture, "{0}m", minutes);
    }

    private static string FormatWorkDurationValue(TimeSpan duration, bool showTimeCalculationsInHoursOnly) =>
        (showTimeCalculationsInHoursOnly ? duration.TotalHours : duration.TotalDays)
        .ToString("0.##", CultureInfo.InvariantCulture);

    private static string FormatCalendarDayDurationValue(TimeSpan duration) =>
        Math.Max(0, duration.TotalDays).ToString("0.##", CultureInfo.InvariantCulture);

    private static string GetWorkDurationColumnLabel(bool showTimeCalculationsInHoursOnly) =>
        showTimeCalculationsInHoursOnly ? "Hours at work" : "Days at work";

    private static string GetWorkDuration75Title(bool showTimeCalculationsInHoursOnly) =>
        showTimeCalculationsInHoursOnly ? "Hours at Work 75P" : "Days at Work 75P";

    private static TimeSpan? SumIncidentDurations(IReadOnlyList<GlobalIncidentItem> incidents)
    {
        var durations = incidents
            .Select(static incident => incident.Duration)
            .Where(static duration => duration.HasValue && duration.Value >= TimeSpan.Zero)
            .Select(static duration => duration!.Value)
            .ToList();

        if (durations.Count == 0)
        {
            return null;
        }

        return durations.Aggregate(TimeSpan.Zero, static (sum, duration) => sum + duration);
    }
}

