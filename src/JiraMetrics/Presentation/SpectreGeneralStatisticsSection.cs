using System.Globalization;

#pragma warning disable CA1822

using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

using Spectre.Console;

namespace JiraMetrics.Presentation;

internal sealed class SpectreGeneralStatisticsSection
{
    public void ShowOpenIssuesByStatusSummary(
        IReadOnlyList<StatusIssueTypeSummary> statusSummaries,
        StatusName doneStatusName,
        StatusName? rejectStatusName)
    {
        var excludedStatuses = rejectStatusName is { } rejectStatus
            ? $"{doneStatusName.Value}, {rejectStatus.Value}"
            : doneStatusName.Value;
        var generatedAt = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);

        AnsiConsole.MarkupLine("[bold]General statistics[/]");
        AnsiConsole.MarkupLine($"[grey]Data as of:[/] {Markup.Escape(generatedAt)}");
        AnsiConsole.MarkupLine("[grey]Scope:[/] all not finished tasks");
        AnsiConsole.MarkupLine($"[grey]Statuses excluded:[/] {Markup.Escape(excludedStatuses)}");

        if (statusSummaries.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No issues outside excluded statuses.[/]");
            return;
        }

        var table = new Table()
            .RoundedBorder()
            .BorderColor(Color.Grey)
            .AddColumn("[bold]Status[/]")
            .AddColumn("[bold]Issues[/]")
            .AddColumn("[bold]Breakdown by type[/]");

        foreach (var statusSummary in statusSummaries
                     .OrderByDescending(static summary => summary.Count.Value)
                     .ThenBy(static summary => summary.Status.Value, StringComparer.OrdinalIgnoreCase))
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

            _ = table.AddRow(
                Markup.Escape(statusSummary.Status.Value),
                statusSummary.Count.Value.ToString(CultureInfo.InvariantCulture),
                Markup.Escape(issueTypeBreakdown));
        }

        AnsiConsole.Write(table);
    }
}
