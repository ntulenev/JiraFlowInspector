using System.Globalization;

using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace JiraMetrics.Presentation.Pdf;

/// <summary>
/// Renders the release report section of the PDF output.
/// </summary>
internal sealed class PdfReleaseSection : IPdfReportSection
{
    /// <inheritdoc />
    public void Compose(ColumnDescriptor column, JiraPdfReportData reportData)
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
            .Text(
                $"Project: {releaseReport.ReleaseProjectKey.Value}    Label: {releaseReport.ProjectLabel}    Period: {reportData.Settings.ReportPeriod.Label}")
            .FontColor(Colors.Grey.Darken1);
        _ = column
            .Item()
            .Text($"Hot-fix markers: {PdfPresentationFormatting.BuildHotFixRulesText(releaseReport.HotFixRules)}")
            .FontColor(Colors.Grey.Darken1);

        if (reportData.ReleaseIssues.Count == 0)
        {
            _ = column.Item().Text("No releases found for selected period.").FontColor(Colors.Grey.Darken1);
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
                    _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(tasksText);
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
                        _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(componentsText);
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

        var componentSummaries = PdfPresentationFormatting.BuildComponentReleaseSummaries(orderedReleases);
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
                _ = table.Cell().Element(PdfPresentationHelpers.StyleBodyCell).Text(releaseCount.ToString(CultureInfo.InvariantCulture));
            }
        });
    }
}
