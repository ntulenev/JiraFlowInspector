using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Abstractions;

/// <summary>
/// Presents diagnostics, failures, and secondary summaries.
/// </summary>
public interface IJiraDiagnosticsPresenter
{
    /// <summary>
    /// Shows open issue counts grouped by status and issue type.
    /// </summary>
    /// <param name="statusSummaries">Status summaries.</param>
    /// <param name="doneStatusName">Done status excluded from the summary.</param>
    /// <param name="rejectStatusName">Optional reject status excluded from the summary.</param>
    void ShowOpenIssuesByStatusSummary(
        IReadOnlyList<StatusIssueTypeSummary> statusSummaries,
        StatusName doneStatusName,
        StatusName? rejectStatusName);

    /// <summary>
    /// Shows failed issue rows.
    /// </summary>
    /// <param name="failures">Failures to display.</param>
    void ShowFailures(IReadOnlyList<LoadFailure> failures);

    /// <summary>
    /// Shows a spacer line between sections.
    /// </summary>
    void ShowSpacer();
}
