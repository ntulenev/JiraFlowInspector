using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

#pragma warning disable CS1591

namespace JiraMetrics.Abstractions;

/// <summary>
/// Presents diagnostics, failures, and secondary summaries.
/// </summary>
public interface IJiraDiagnosticsPresenter
{
    void ShowOpenIssuesByStatusSummary(
        IReadOnlyList<StatusIssueTypeSummary> statusSummaries,
        StatusName doneStatusName,
        StatusName? rejectStatusName);

    void ShowFailures(IReadOnlyList<LoadFailure> failures);

    void ShowSpacer();
}
