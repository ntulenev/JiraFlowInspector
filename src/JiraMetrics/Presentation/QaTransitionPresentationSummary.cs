using System.Globalization;

using JiraMetrics.Models;

namespace JiraMetrics.Presentation;

/// <summary>
/// Shared presentation formatting for QA transition report sections.
/// </summary>
internal static class QaTransitionPresentationSummary
{
    public static int CountCodeIssues(IEnumerable<IssueTimeline> issues) =>
        issues.Count(static issue => issue.HasPullRequest);

    public static string BuildProdBugPrioritySummary(IEnumerable<IssueListItem> issues)
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

    public static string BuildCoverageText(QaTransitionAnalysis analysis) =>
        string.Format(
            CultureInfo.InvariantCulture,
            "{0}/{1} ({2:0.##}%)",
            analysis.PickupIssues.Count,
            analysis.AnalyzedIssueCount.Value,
            analysis.PickupIssuePercentage);

    public static string BuildRulesLabel(IReadOnlyList<TransitionMeasurementRule> rules) =>
        string.Join("; ", rules.Select(static rule => rule.Label));

    public static string FormatDuration(
        TimeSpan? duration,
        bool showTimeCalculationsInHoursOnly) =>
        duration is null
            ? "-"
            : PresentationFormatting.FormatWorkDurationValue(
                duration.Value,
                showTimeCalculationsInHoursOnly);

    public static string GetDurationColumnLabel(bool showTimeCalculationsInHoursOnly) =>
        showTimeCalculationsInHoursOnly ? "Hours in QA" : "Days in QA";

    public static string GetHoldDurationColumnLabel(bool showTimeCalculationsInHoursOnly) =>
        showTimeCalculationsInHoursOnly ? "Hours on hold" : "Days on hold";

    public static string GetDuration75Title(bool showTimeCalculationsInHoursOnly) =>
        showTimeCalculationsInHoursOnly ? "Hours in QA 75P" : "Days in QA 75P";

    public static string GetPickupDuration75Label(bool showTimeCalculationsInHoursOnly) =>
        showTimeCalculationsInHoursOnly ? "QA In Progress Hours 75p" : "QA In Progress Days 75p";

    public static string GetTestingDuration75Label(bool showTimeCalculationsInHoursOnly) =>
        showTimeCalculationsInHoursOnly ? "QA Transition Hours 75p" : "QA Transition Days 75p";

    public static string GetHoldDuration75Label(bool showTimeCalculationsInHoursOnly) =>
        showTimeCalculationsInHoursOnly ? "QA Hold Hours 75p" : "QA Hold Days 75p";

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
