using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

#pragma warning disable CS1591

namespace JiraMetrics.Abstractions;

/// <summary>
/// Presents shared workflow status, progress, and summary messages.
/// </summary>
public interface IJiraStatusPresenter
{
    void ShowAuthenticationStarted();

    void ShowAuthenticationSucceeded(JiraAuthUser user);

    void ShowAuthenticationFailed(ErrorMessage errorMessage);

    void ShowReportPeriodContext(ReportPeriod reportPeriod, CreatedAfterDate? createdAfter);

    void ShowIssueSearchFailed(ErrorMessage errorMessage);

    void ShowReportHeader(AppSettings settings, ItemCount issueCount);

    void ShowNoIssuesMatchedFilter();

    void ShowProcessingStep(string message);

    void ShowSpacer();

    void ShowNoIssuesLoaded();

    void ShowNoIssuesMatchedRequiredStage();

    void ShowExecutionSummary(TimeSpan totalDuration, JiraRequestTelemetrySummary requestTelemetry);
}
