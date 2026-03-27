using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Abstractions.Presentation;

/// <summary>
/// Presents shared workflow status, progress, and summary messages.
/// </summary>
public interface IJiraStatusPresenter
{
    /// <summary>
    /// Shows authentication start message.
    /// </summary>
    void ShowAuthenticationStarted();

    /// <summary>
    /// Shows successful authentication message.
    /// </summary>
    /// <param name="user">Authenticated user.</param>
    void ShowAuthenticationSucceeded(JiraAuthUser user);

    /// <summary>
    /// Shows authentication failure message.
    /// </summary>
    /// <param name="errorMessage">Error details.</param>
    void ShowAuthenticationFailed(ErrorMessage errorMessage);

    /// <summary>
    /// Shows selected report period and optional created-after filter.
    /// </summary>
    /// <param name="reportPeriod">Report period.</param>
    /// <param name="createdAfter">Optional created-after filter.</param>
    void ShowReportPeriodContext(ReportPeriod reportPeriod, CreatedAfterDate? createdAfter);

    /// <summary>
    /// Shows issue search failure message.
    /// </summary>
    /// <param name="errorMessage">Error details.</param>
    void ShowIssueSearchFailed(ErrorMessage errorMessage);

    /// <summary>
    /// Shows top-level report header.
    /// </summary>
    /// <param name="settings">Application settings.</param>
    /// <param name="issueCount">Issue count for the report.</param>
    void ShowReportHeader(AppSettings settings, ItemCount issueCount);

    /// <summary>
    /// Shows message that no issues matched the search filter.
    /// </summary>
    void ShowNoIssuesMatchedFilter();

    /// <summary>
    /// Shows short workflow progress message.
    /// </summary>
    /// <param name="message">Step description.</param>
    void ShowProcessingStep(string message);

    /// <summary>
    /// Shows a spacer line between sections.
    /// </summary>
    void ShowSpacer();

    /// <summary>
    /// Shows message that no issues were loaded successfully.
    /// </summary>
    void ShowNoIssuesLoaded();

    /// <summary>
    /// Shows message that no issues matched the required path stages.
    /// </summary>
    void ShowNoIssuesMatchedRequiredStage();

    /// <summary>
    /// Shows final execution and transport telemetry summary.
    /// </summary>
    /// <param name="totalDuration">Total application duration.</param>
    /// <param name="requestTelemetry">Aggregated Jira request telemetry.</param>
    void ShowExecutionSummary(TimeSpan totalDuration, JiraRequestTelemetrySummary requestTelemetry);
}

