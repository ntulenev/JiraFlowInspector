namespace JiraMetrics.Abstractions;

/// <summary>
/// Compatibility facade that aggregates all Jira presentation contracts.
/// </summary>
public interface IJiraPresentationService :
    IJiraStatusPresenter,
    IJiraIssueLoadingProgressPresenter,
    IJiraReportSectionsPresenter,
    IJiraAnalysisPresenter,
    IJiraDiagnosticsPresenter
{
}
