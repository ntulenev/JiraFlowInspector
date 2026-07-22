using System.Globalization;

using JiraMetrics.Models;

namespace JiraMetrics.Presentation;

/// <summary>
/// Shared presentation formatting for QA transition report sections.
/// </summary>
internal static class QaTransitionPresentationSummary
{
    /// <summary>
    /// Counts issues that have pull-request activity.
    /// </summary>
    /// <param name="issues">Issues to inspect.</param>
    /// <returns>Number of issues with pull-request activity.</returns>
    public static int CountCodeIssues(IEnumerable<IssueTimeline> issues) =>
        issues.Count(static issue => issue.HasPullRequest);

    /// <summary>
    /// Builds a production-bug count with a priority breakdown.
    /// </summary>
    /// <param name="issues">Issue rows to summarize.</param>
    /// <returns>Formatted production-bug count and priority breakdown.</returns>
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

    /// <summary>
    /// Builds the QA pickup coverage value.
    /// </summary>
    /// <param name="analysis">QA transition analysis.</param>
    /// <returns>Formatted covered, total, and percentage values.</returns>
    public static string BuildCoverageText(QaTransitionAnalysis analysis) =>
        string.Format(
            CultureInfo.InvariantCulture,
            "{0}/{1} ({2:0.##}%)",
            analysis.PickupIssues.Count,
            analysis.AnalyzedIssueCount.Value,
            analysis.PickupIssuePercentage);

    /// <summary>
    /// Joins transition measurement rules into a display label.
    /// </summary>
    /// <param name="rules">Transition rules in display order.</param>
    /// <returns>Semicolon-separated rule labels.</returns>
    public static string BuildRulesLabel(IReadOnlyList<TransitionMeasurementRule> rules) =>
        string.Join("; ", rules.Select(static rule => rule.Label));

    /// <summary>
    /// Formats an optional QA duration using the configured unit.
    /// </summary>
    /// <param name="duration">Optional duration.</param>
    /// <param name="showTimeCalculationsInHoursOnly">Whether to display hours instead of days.</param>
    /// <returns>Formatted duration, or a dash when no value is available.</returns>
    public static string FormatDuration(
        TimeSpan? duration,
        bool showTimeCalculationsInHoursOnly) =>
        duration is null
            ? "-"
            : PresentationFormatting.FormatWorkDurationValue(
                duration.Value,
                showTimeCalculationsInHoursOnly);

    /// <summary>
    /// Gets the default QA duration column label.
    /// </summary>
    /// <param name="showTimeCalculationsInHoursOnly">Whether the column contains hours instead of days.</param>
    /// <returns>QA duration column label.</returns>
    public static string GetDurationColumnLabel(bool showTimeCalculationsInHoursOnly) =>
        showTimeCalculationsInHoursOnly ? "Hours in QA" : "Days in QA";

    /// <summary>
    /// Gets the QA hold duration column label.
    /// </summary>
    /// <param name="showTimeCalculationsInHoursOnly">Whether the column contains hours instead of days.</param>
    /// <returns>QA hold duration column label.</returns>
    public static string GetHoldDurationColumnLabel(bool showTimeCalculationsInHoursOnly) =>
        showTimeCalculationsInHoursOnly ? "Hours on hold" : "Days on hold";

    /// <summary>
    /// Gets the QA duration percentile title.
    /// </summary>
    /// <param name="showTimeCalculationsInHoursOnly">Whether the percentile is expressed in hours instead of days.</param>
    /// <returns>QA duration percentile title.</returns>
    public static string GetDuration75Title(bool showTimeCalculationsInHoursOnly) =>
        showTimeCalculationsInHoursOnly ? "Hours in QA 75P" : "Days in QA 75P";

    /// <summary>
    /// Gets the QA pickup percentile label.
    /// </summary>
    /// <param name="showTimeCalculationsInHoursOnly">Whether the percentile is expressed in hours instead of days.</param>
    /// <returns>QA pickup percentile label.</returns>
    public static string GetPickupDuration75Label(bool showTimeCalculationsInHoursOnly) =>
        showTimeCalculationsInHoursOnly ? "QA In Progress Hours 75p" : "QA In Progress Days 75p";

    /// <summary>
    /// Gets the QA testing percentile label.
    /// </summary>
    /// <param name="showTimeCalculationsInHoursOnly">Whether the percentile is expressed in hours instead of days.</param>
    /// <returns>QA testing percentile label.</returns>
    public static string GetTestingDuration75Label(bool showTimeCalculationsInHoursOnly) =>
        showTimeCalculationsInHoursOnly ? "QA Transition Hours 75p" : "QA Transition Days 75p";

    /// <summary>
    /// Gets the QA hold percentile label.
    /// </summary>
    /// <param name="showTimeCalculationsInHoursOnly">Whether the percentile is expressed in hours instead of days.</param>
    /// <returns>QA hold percentile label.</returns>
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
