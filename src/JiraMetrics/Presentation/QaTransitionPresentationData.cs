using System.Globalization;

using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Presentation;

/// <summary>
/// Prepared QA transition data shared by HTML and PDF presentations.
/// </summary>
internal sealed class QaTransitionPresentationData
{
    private QaTransitionPresentationData(
        int doneCodeIssueCount,
        int rejectedCodeIssueCount,
        int openBugCount,
        string openProdBugSummary,
        int doneBugCount,
        string doneProdBugSummary,
        int rejectedBugCount,
        string rejectedProdBugSummary,
        QaTransitionMetricPresentationData pickup,
        QaTransitionMetricPresentationData testing,
        QaTransitionMetricPresentationData hold,
        ItemCount analyzedIssueCount,
        decimal pickupIssuePercentage,
        bool showTimeCalculationsInHoursOnly)
    {
        DoneCodeIssueCount = doneCodeIssueCount;
        RejectedCodeIssueCount = rejectedCodeIssueCount;
        OpenBugCount = openBugCount;
        OpenProdBugSummary = openProdBugSummary;
        DoneBugCount = doneBugCount;
        DoneProdBugSummary = doneProdBugSummary;
        RejectedBugCount = rejectedBugCount;
        RejectedProdBugSummary = rejectedProdBugSummary;
        Pickup = pickup;
        Testing = testing;
        Hold = hold;
        AnalyzedIssueCount = analyzedIssueCount;
        PickupIssuePercentage = pickupIssuePercentage;
        PickupCoverageText = string.Format(
            CultureInfo.InvariantCulture,
            "{0}/{1} ({2:0.##}%)",
            pickup.IssueCount,
            analyzedIssueCount.Value,
            pickupIssuePercentage);
        PickupIssueCountText = string.Format(
            CultureInfo.InvariantCulture,
            "{0}/{1}",
            pickup.IssueCount,
            analyzedIssueCount.Value);
        PickupShareText = pickupIssuePercentage.ToString("0.##", CultureInfo.InvariantCulture) + "%";
        DurationColumnLabel = showTimeCalculationsInHoursOnly ? "Hours in QA" : "Days in QA";
        HoldDurationColumnLabel = showTimeCalculationsInHoursOnly ? "Hours on hold" : "Days on hold";
        Duration75Title = showTimeCalculationsInHoursOnly ? "Hours in QA 75P" : "Days in QA 75P";
        Duration75ColumnLabel = showTimeCalculationsInHoursOnly ? "Hours 75P" : "Days 75P";
        PickupDuration75Label = showTimeCalculationsInHoursOnly
            ? "QA In Progress Hours 75p"
            : "QA In Progress Days 75p";
        TestingDuration75Label = showTimeCalculationsInHoursOnly
            ? "QA Transition Hours 75p"
            : "QA Transition Days 75p";
        HoldDuration75Label = showTimeCalculationsInHoursOnly
            ? "QA Hold Hours 75p"
            : "QA Hold Days 75p";
    }

    public bool ShouldRender => AnalyzedIssueCount.Value > 0;

    public int DoneCodeIssueCount { get; }

    public int RejectedCodeIssueCount { get; }

    public int OpenBugCount { get; }

    public string OpenProdBugSummary { get; }

    public int DoneBugCount { get; }

    public string DoneProdBugSummary { get; }

    public int RejectedBugCount { get; }

    public string RejectedProdBugSummary { get; }

    public ItemCount AnalyzedIssueCount { get; }

    public decimal PickupIssuePercentage { get; }

    public string PickupCoverageText { get; }

    public string PickupIssueCountText { get; }

    public string PickupShareText { get; }

    public string DurationColumnLabel { get; }

    public string HoldDurationColumnLabel { get; }

    public string Duration75Title { get; }

    public string Duration75ColumnLabel { get; }

    public string PickupDuration75Label { get; }

    public string TestingDuration75Label { get; }

    public string HoldDuration75Label { get; }

    public QaTransitionMetricPresentationData Pickup { get; }

    public QaTransitionMetricPresentationData Testing { get; }

    public QaTransitionMetricPresentationData Hold { get; }

    public static QaTransitionPresentationData Create(JiraReportData reportData)
    {
        ArgumentNullException.ThrowIfNull(reportData);

        var analysis = reportData.Transitions.QaTransitionAnalysis;
        var settings = reportData.Settings.QaTransitionAnalysis;
        var showHoursOnly = reportData.Settings.ShowTimeCalculationsInHoursOnly;
        var bugRatio = reportData.Ratios.Bugs;

        return new QaTransitionPresentationData(
            CountCodeIssues(reportData.Transitions.DoneIssues),
            CountCodeIssues(reportData.Transitions.RejectedIssues),
            bugRatio?.OpenIssues.Count ?? 0,
            BuildProdBugPrioritySummary(bugRatio?.OpenIssues ?? []),
            bugRatio?.DoneIssues.Count ?? 0,
            BuildProdBugPrioritySummary(bugRatio?.DoneIssues ?? []),
            bugRatio?.RejectedIssues.Count ?? 0,
            BuildProdBugPrioritySummary(bugRatio?.RejectedIssues ?? []),
            QaTransitionMetricPresentationData.Create(
                settings.PickupTransitions,
                analysis.PickupIssues,
                analysis.PickupDuration75,
                analysis.PickupDuration75PerType,
                reportData.Settings.BaseUrl,
                showHoursOnly),
            QaTransitionMetricPresentationData.Create(
                settings.TestingTransitions,
                analysis.TestingIssues,
                analysis.TestingDuration75,
                analysis.TestingDuration75PerType,
                reportData.Settings.BaseUrl,
                showHoursOnly),
            QaTransitionMetricPresentationData.Create(
                settings.HoldTransitions,
                analysis.HoldIssues,
                analysis.HoldDuration75,
                analysis.HoldDuration75PerType,
                reportData.Settings.BaseUrl,
                showHoursOnly),
            analysis.AnalyzedIssueCount,
            analysis.PickupIssuePercentage,
            showHoursOnly);
    }

    private static int CountCodeIssues(IEnumerable<IssueTimeline> issues) =>
        issues.Count(static issue => issue.HasPullRequest);

    private static string BuildProdBugPrioritySummary(IEnumerable<IssueListItem> issues)
    {
        var prodIssues = issues
            .Where(static issue => issue.ReporducedOnProd)
            .ToArray();
        var total = prodIssues.Length.ToString(CultureInfo.InvariantCulture);
        var priorityCounts = prodIssues
            .Where(static issue => !string.IsNullOrWhiteSpace(issue.Priority))
            .GroupBy(static issue => issue.Priority!, StringComparer.OrdinalIgnoreCase)
            .Select(static group => new
            {
                Priority = group.Key,
                Count = group.Count()
            })
            .OrderBy(static item => GetPrioritySortKey(item.Priority))
            .ThenBy(static item => item.Priority, StringComparer.OrdinalIgnoreCase)
            .Select(static item => string.Format(
                CultureInfo.InvariantCulture,
                "{0}: {1}",
                item.Priority,
                item.Count))
            .ToArray();

        return priorityCounts.Length == 0
            ? total
            : string.Format(CultureInfo.InvariantCulture, "{0} ({1})", total, string.Join(", ", priorityCounts));
    }

    private static int GetPrioritySortKey(string priority)
    {
        if (priority.Length >= 2
            && (priority[0] is 'P' or 'p')
            && int.TryParse(priority[1..], CultureInfo.InvariantCulture, out var priorityNumber))
        {
            return priorityNumber;
        }

        return int.MaxValue;
    }
}

/// <summary>
/// Prepared data for one QA measurement kind.
/// </summary>
internal sealed class QaTransitionMetricPresentationData
{
    private QaTransitionMetricPresentationData(
        string rulesLabel,
        int issueCount,
        TimeSpan? duration75,
        string duration75Text,
        IReadOnlyList<QaTransitionIssuePresentationData> issues,
        IReadOnlyList<QaDuration75PresentationData> duration75PerType)
    {
        RulesLabel = rulesLabel;
        IssueCount = issueCount;
        Duration75 = duration75;
        Duration75Text = duration75Text;
        Issues = issues;
        Duration75PerType = duration75PerType;
    }

    public string RulesLabel { get; }

    public int IssueCount { get; }

    public TimeSpan? Duration75 { get; }

    public string Duration75Text { get; }

    public IReadOnlyList<QaTransitionIssuePresentationData> Issues { get; }

    public IReadOnlyList<QaDuration75PresentationData> Duration75PerType { get; }

    public static QaTransitionMetricPresentationData Create(
        IReadOnlyList<TransitionMeasurementRule> rules,
        IReadOnlyList<TransitionMeasurementIssue> issues,
        TimeSpan? duration75,
        IReadOnlyList<IssueTypeDuration75Summary> duration75PerType,
        JiraBaseUrl baseUrl,
        bool showHoursOnly)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(issues);
        ArgumentNullException.ThrowIfNull(duration75PerType);

        return new QaTransitionMetricPresentationData(
            string.Join("; ", rules.Select(static rule => rule.Label)),
            issues.Count,
            duration75,
            FormatDuration(duration75, showHoursOnly),
            [.. issues
                .OrderByDescending(static item => item.Duration)
                .ThenBy(static item => item.Issue.Key.Value, StringComparer.OrdinalIgnoreCase)
                .Select(item => QaTransitionIssuePresentationData.Create(item, baseUrl, showHoursOnly))],
            [.. duration75PerType
                .OrderByDescending(static summary => summary.DurationP75)
                .ThenByDescending(static summary => summary.IssueCount.Value)
                .ThenBy(static summary => summary.IssueType.Value, StringComparer.OrdinalIgnoreCase)
                .Select(summary => QaDuration75PresentationData.Create(summary, showHoursOnly))]);
    }

    private static string FormatDuration(TimeSpan? duration, bool showHoursOnly) =>
        duration is null
            ? "-"
            : PresentationFormatting.FormatWorkDurationValue(duration.Value, showHoursOnly);
}

/// <summary>
/// Prepared QA issue row.
/// </summary>
internal sealed record QaTransitionIssuePresentationData(
    IssueKey Key,
    string IssueUrl,
    IssueTypeName IssueType,
    int SubItemsCount,
    bool HasPullRequest,
    IssueSummary Summary,
    string RuleLabel,
    DateTimeOffset TransitionAt,
    string TransitionAtText,
    TimeSpan Duration,
    string DurationText)
{
    public static QaTransitionIssuePresentationData Create(
        TransitionMeasurementIssue measurement,
        JiraBaseUrl baseUrl,
        bool showHoursOnly)
    {
        ArgumentNullException.ThrowIfNull(measurement);

        return new QaTransitionIssuePresentationData(
            measurement.Issue.Key,
            PresentationFormatting.BuildIssueBrowseUrl(baseUrl, measurement.Issue.Key),
            measurement.Issue.IssueType,
            measurement.Issue.SubItemsCount,
            measurement.Issue.HasPullRequest,
            measurement.Issue.Summary,
            measurement.Rule.Label,
            measurement.TransitionAt,
            PresentationFormatting.FormatLocalDateTime(measurement.TransitionAt),
            measurement.Duration,
            PresentationFormatting.FormatWorkDurationValue(measurement.Duration, showHoursOnly));
    }
}

/// <summary>
/// Prepared QA P75 row for one issue type.
/// </summary>
internal sealed record QaDuration75PresentationData(
    IssueTypeName IssueType,
    ItemCount IssueCount,
    TimeSpan Duration,
    string DurationText)
{
    public static QaDuration75PresentationData Create(
        IssueTypeDuration75Summary summary,
        bool showHoursOnly)
    {
        ArgumentNullException.ThrowIfNull(summary);

        return new QaDuration75PresentationData(
            summary.IssueType,
            summary.IssueCount,
            summary.DurationP75,
            PresentationFormatting.FormatWorkDurationValue(summary.DurationP75, showHoursOnly));
    }
}
