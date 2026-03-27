using System.Globalization;

#pragma warning disable CA1822

using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

using Spectre.Console;

namespace JiraMetrics.Presentation;

internal sealed class SpectreStatusSection
{
    public void ShowAuthenticationStarted() => AnsiConsole.MarkupLine("[grey]Authenticating with Jira...[/]");

    public void ShowAuthenticationSucceeded(JiraAuthUser user)
    {
        AnsiConsole.MarkupLine($"[green]Auth succeeded for user:[/] {Markup.Escape(user.DisplayName.Value)}");
    }

    public void ShowAuthenticationFailed(ErrorMessage errorMessage)
    {
        AnsiConsole.MarkupLine($"[red]Auth failed:[/] {Markup.Escape(errorMessage.Value)}");
    }

    public void ShowReportPeriodContext(ReportPeriod reportPeriod, CreatedAfterDate? createdAfter)
    {
        var periodLabel = reportPeriod.IsMonthBased ? "Month label" : "Date range";
        AnsiConsole.MarkupLine($"[grey]{periodLabel}:[/] {Markup.Escape(reportPeriod.Label)}");
        if (createdAfter is { } value)
        {
            AnsiConsole.MarkupLine($"[grey]Created after:[/] {Markup.Escape(value.ToString())}");
        }
    }

    public void ShowIssueSearchFailed(ErrorMessage errorMessage)
    {
        AnsiConsole.MarkupLine($"[red]Failed to load issues:[/] {Markup.Escape(errorMessage.Value)}");
    }

    public void ShowReportHeader(AppSettings settings, ItemCount issueCount)
    {
        AnsiConsole.Write(
            new Rule($"[bold cyan]Jira Transition Analytics[/] - [bold]{issueCount.Value} issue(s)[/]")
                .RuleStyle("grey")
                .LeftJustified());

        AnsiConsole.MarkupLine(
            $"[grey]Filter:[/] project = {Markup.Escape(settings.ProjectKey.Value)}, moved to {Markup.Escape(settings.DoneStatusName.Value)} during {Markup.Escape(settings.ReportPeriod.Label)}");
        if (settings.CreatedAfter is { } createdAfter)
        {
            AnsiConsole.MarkupLine($"[grey]Created after:[/] {Markup.Escape(createdAfter.ToString())}");
        }

        if (settings.IssueTypes.Count > 0)
        {
            var issueTypes = string.Join(", ", settings.IssueTypes.Select(static issueType => issueType.Value));
            AnsiConsole.MarkupLine($"[grey]Issue types:[/] {Markup.Escape(issueTypes)}");
        }

        if (!string.IsNullOrWhiteSpace(settings.CustomFieldName)
            && !string.IsNullOrWhiteSpace(settings.CustomFieldValue))
        {
            AnsiConsole.MarkupLine(
                $"[grey]Filtered by:[/] {Markup.Escape(settings.CustomFieldName)} = {Markup.Escape(settings.CustomFieldValue)}");
        }

        var requiredStages = settings.RequiredPathStages.Count == 0
            ? "-"
            : string.Join(", ", settings.RequiredPathStages.Select(static stage => stage.Value));
        AnsiConsole.MarkupLine($"[grey]Required stages in path:[/] {Markup.Escape(requiredStages)}");
    }

    public void ShowNoIssuesMatchedFilter() => AnsiConsole.MarkupLine("[yellow]No issues matched this filter.[/]");

    public void ShowIssueLoadingCompleted(ItemCount loadedIssues, ItemCount failedIssues) =>
        AnsiConsole.MarkupLine($"[green]Issue loading completed:[/] loaded = {loadedIssues.Value}, failed = {failedIssues.Value}");

    public void ShowProcessingStep(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        AnsiConsole.MarkupLine($"[grey]{Markup.Escape(message)}[/]");
    }

    public void ShowSpacer() => AnsiConsole.WriteLine();

    public void ShowNoIssuesLoaded() => AnsiConsole.MarkupLine("[red]No issues were loaded successfully.[/]");

    public void ShowNoIssuesMatchedRequiredStage() =>
        AnsiConsole.MarkupLine("[yellow]No issues matched the required stages in path.[/]");

    public void ShowExecutionSummary(TimeSpan totalDuration, JiraRequestTelemetrySummary requestTelemetry)
    {
        AnsiConsole.MarkupLine($"[green]Execution completed:[/] {Markup.Escape(SpectrePresentationFormatting.FormatExecutionDuration(totalDuration))}");
        AnsiConsole.MarkupLine(
            $"[grey]Jira HTTP:[/] requests = {requestTelemetry.RequestCount}, retries = {requestTelemetry.RetryCount}, response payload = {Markup.Escape(SpectrePresentationFormatting.FormatBytes(requestTelemetry.ResponseBytes))}, latency = {Markup.Escape(SpectrePresentationFormatting.FormatExecutionDuration(requestTelemetry.TotalDuration))}");

        if (requestTelemetry.Endpoints.Count == 0)
        {
            return;
        }

        var table = new Table()
            .RoundedBorder()
            .BorderColor(Color.Grey)
            .AddColumn("[bold]Endpoint[/]")
            .AddColumn("[bold]Requests[/]")
            .AddColumn("[bold]Retries[/]")
            .AddColumn("[bold]Payload[/]")
            .AddColumn("[bold]Total[/]")
            .AddColumn("[bold]Avg[/]")
            .AddColumn("[bold]Max[/]");

        foreach (var endpoint in requestTelemetry.Endpoints)
        {
            var averageDuration = endpoint.RequestCount == 0
                ? TimeSpan.Zero
                : TimeSpan.FromTicks(endpoint.TotalDuration.Ticks / endpoint.RequestCount);
            _ = table.AddRow(
                Markup.Escape($"{endpoint.Method} {endpoint.Endpoint}"),
                endpoint.RequestCount.ToString(CultureInfo.InvariantCulture),
                endpoint.RetryCount.ToString(CultureInfo.InvariantCulture),
                Markup.Escape(SpectrePresentationFormatting.FormatBytes(endpoint.ResponseBytes)),
                Markup.Escape(SpectrePresentationFormatting.FormatExecutionDuration(endpoint.TotalDuration)),
                Markup.Escape(SpectrePresentationFormatting.FormatExecutionDuration(averageDuration)),
                Markup.Escape(SpectrePresentationFormatting.FormatExecutionDuration(endpoint.MaxDuration)));
        }

        AnsiConsole.Write(table);
    }
}
