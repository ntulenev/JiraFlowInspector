using System.Globalization;

using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models.Configuration;

internal static class AppSettingsFactory
{
    public static AppSettings Create(JiraOptions source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var teamTasks = source.TeamTasks ?? throw new InvalidOperationException("TeamTasks section is required.");
        var requiredPathStages = CreateStageNames(teamTasks.IssueTransitions?.RequiredPathStages);

        if (requiredPathStages.Count == 0)
        {
            throw new InvalidOperationException("At least one TeamTasks:IssueTransitions:RequiredPathStages entry must be configured.");
        }

        return new AppSettings(
            new JiraBaseUrl(source.BaseUrl.ToString()),
            new JiraEmail(source.Email),
            new JiraApiToken(source.ApiToken),
            new ProjectKey(teamTasks.ProjectKey),
            new StatusName(teamTasks.DoneStatusName),
            CreateOptionalStatusName(teamTasks.RejectStatusName),
            requiredPathStages,
            ResolveReportPeriod(source),
            CreateOptionalCreatedAfterDate(source.CreatedAfter),
            CreateIssueTypeNames(teamTasks.IssueTransitions?.IssueTypes),
            NormalizeOptionalString(teamTasks.CustomFieldName),
            NormalizeOptionalString(teamTasks.CustomFieldValue),
            source.ShowTimeCalculationsInHoursOnly,
            teamTasks.IssueTransitions?.ExcludeWeekend ?? false,
            ParseExcludedDays(teamTasks.IssueTransitions?.ExcludedDays),
            CreateIssueTypeNames(teamTasks.BugRatio?.BugIssueNames),
            teamTasks.ShowGeneralStatistics,
            ResolveReleaseReport(source.ReleaseReport),
            ResolveArchTasksReport(source.ArchTasks),
            ResolveGlobalIncidentsReport(source.GlobalIncidents),
            ResolvePdfReport(source.Pdf),
            source.PullRequestFieldName);
    }

    private static StatusName? CreateOptionalStatusName(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : new StatusName(value);

    private static CreatedAfterDate? CreateOptionalCreatedAfterDate(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : new CreatedAfterDate(value);

    private static IReadOnlyList<StageName> CreateStageNames(IReadOnlyList<string>? values) =>
        values is null
            ? []
            : [.. values
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Select(static value => new StageName(value))
                .DistinctBy(static value => value.Value, StringComparer.OrdinalIgnoreCase)];

    private static IReadOnlyList<IssueTypeName> CreateIssueTypeNames(IReadOnlyList<string>? values) =>
        values is null
            ? []
            : [.. values
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Select(static value => new IssueTypeName(value))
                .DistinctBy(static value => value.Value, StringComparer.OrdinalIgnoreCase)];

    private static IReadOnlyList<DateOnly> ParseExcludedDays(IReadOnlyList<string>? values) =>
        values is null
            ? []
            : [.. values
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Select(static value => ParseExcludedDay(value.Trim()))
                .Distinct()];

    private static string? NormalizeOptionalString(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateOnly ParseExcludedDay(string value)
    {
        if (DateOnly.TryParseExact(value, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
        {
            return day;
        }

        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out day))
        {
            return day;
        }

        throw new FormatException($"Invalid excluded day '{value}'. Expected dd.MM.yyyy or yyyy-MM-dd.");
    }

    private static ReportPeriod ResolveReportPeriod(JiraOptions source)
    {
        var hasMonthLabel = !string.IsNullOrWhiteSpace(source.MonthLabel);
        var hasFrom = !string.IsNullOrWhiteSpace(source.From);
        var hasTo = !string.IsNullOrWhiteSpace(source.To);

        if (hasMonthLabel && (hasFrom || hasTo))
        {
            throw new InvalidOperationException("Use either MonthLabel or From/To, but not both.");
        }

        if (hasFrom != hasTo)
        {
            throw new InvalidOperationException("Both From and To must be provided together.");
        }

        if (hasFrom)
        {
            var fromDate = ReportPeriod.ParseConfiguredDate(source.From!, nameof(source.From));
            var toDate = ReportPeriod.ParseConfiguredDate(source.To!, nameof(source.To));
            return ReportPeriod.FromDateRange(fromDate, toDate);
        }

        if (hasMonthLabel)
        {
            return ReportPeriod.FromMonthLabel(new MonthLabel(source.MonthLabel!));
        }

        return ReportPeriod.CurrentUtcMonth();
    }

    private static ReleaseReportSettings? ResolveReleaseReport(ReleaseReportOptions? source)
    {
        if (source is null)
        {
            return null;
        }

        var hasAnyValue =
            !string.IsNullOrWhiteSpace(source.ReleaseProjectKey)
            || !string.IsNullOrWhiteSpace(source.ProjectLabel)
            || !string.IsNullOrWhiteSpace(source.ReleaseDateFieldName)
            || !string.IsNullOrWhiteSpace(source.ComponentsFieldName)
            || source.HotFixRules is { Count: > 0 }
            || !string.IsNullOrWhiteSpace(source.RollbackFieldName)
            || !string.IsNullOrWhiteSpace(source.EnvironmentFieldName)
            || !string.IsNullOrWhiteSpace(source.EnvironmentFieldValue);

        if (!hasAnyValue)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(source.ReleaseProjectKey)
            || string.IsNullOrWhiteSpace(source.ProjectLabel)
            || string.IsNullOrWhiteSpace(source.ReleaseDateFieldName))
        {
            throw new InvalidOperationException(
                "ReleaseReport requires ReleaseProjectKey, ProjectLabel, and ReleaseDateFieldName when configured.");
        }

        return new ReleaseReportSettings(
            new ProjectKey(source.ReleaseProjectKey),
            source.ProjectLabel,
            source.ReleaseDateFieldName,
            source.ComponentsFieldName,
            source.HotFixRules?.ToDictionary(
                static pair => pair.Key,
                static pair => (IReadOnlyList<string>)(pair.Value ?? []),
                StringComparer.OrdinalIgnoreCase),
            source.RollbackFieldName,
            source.EnvironmentFieldName,
            source.EnvironmentFieldValue);
    }

    private static ArchTasksReportSettings? ResolveArchTasksReport(ArchTasksReportOptions? source)
    {
        if (source is null || string.IsNullOrWhiteSpace(source.Jql))
        {
            return null;
        }

        return new ArchTasksReportSettings(source.Jql);
    }

    private static GlobalIncidentsReportSettings? ResolveGlobalIncidentsReport(GlobalIncidentsReportOptions? source)
    {
        if (source is null)
        {
            return null;
        }

        return new GlobalIncidentsReportSettings(
            source.Namespace,
            source.JqlFilter,
            source.SearchPhrase,
            source.IncidentStartFieldName,
            source.IncidentStartFallbackFieldName,
            source.IncidentRecoveryFieldName,
            source.IncidentRecoveryFallbackFieldName,
            source.ImpactFieldName,
            source.UrgencyFieldName,
            source.AdditionalFieldNames);
    }

    private static PdfReportSettings ResolvePdfReport(PdfOptions? source)
    {
        if (source is null)
        {
            return new PdfReportSettings();
        }

        return new PdfReportSettings(source.Enabled, source.OutputPath, source.OpenAfterGeneration);
    }
}
