namespace JiraMetrics.Abstractions.Application;

/// <summary>
/// Aggregates report presentation and rendering services used by the application workflow.
/// </summary>
public interface IJiraApplicationReportingFacade :
    IJiraStatusPresenter,
    IJiraReportSectionsPresenter,
    IJiraAnalysisPresenter,
    IJiraDiagnosticsPresenter,
    IPdfReportRenderer
{
    /// <summary>
    /// Shows a spacer line between report sections.
    /// </summary>
    new void ShowSpacer();
}
