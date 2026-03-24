using JiraMetrics.Abstractions;
using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Logic;

/// <summary>
/// Facade over Jira data-loading workflow steps used by the application.
/// </summary>
internal sealed class JiraApplicationDataFacade : IJiraApplicationDataFacade
{
    private readonly IJiraApiClient _apiClient;
    private readonly JiraReportContextLoader _reportContextLoader;
    private readonly JiraIssueRatioLoader _issueRatioLoader;
    private readonly JiraIssueTimelineLoader _issueTimelineLoader;

    public JiraApplicationDataFacade(
        IJiraApiClient apiClient,
        JiraReportContextLoader reportContextLoader,
        JiraIssueRatioLoader issueRatioLoader,
        JiraIssueTimelineLoader issueTimelineLoader)
    {
        ArgumentNullException.ThrowIfNull(apiClient);
        ArgumentNullException.ThrowIfNull(reportContextLoader);
        ArgumentNullException.ThrowIfNull(issueRatioLoader);
        ArgumentNullException.ThrowIfNull(issueTimelineLoader);
        _apiClient = apiClient;
        _reportContextLoader = reportContextLoader;
        _issueRatioLoader = issueRatioLoader;
        _issueTimelineLoader = issueTimelineLoader;
    }

    public Task<JiraAuthUser> GetCurrentUserAsync(CancellationToken cancellationToken) =>
        _apiClient.GetCurrentUserAsync(cancellationToken);

    public Task<JiraReportContext> LoadReportContextAsync(
        AppSettings settings,
        CancellationToken cancellationToken) =>
        _reportContextLoader.LoadAsync(settings, cancellationToken);

    public Task<IssueRatioSnapshot> LoadIssueRatioAsync(
        AppSettings settings,
        IReadOnlyList<IssueTypeName> issueTypes,
        CancellationToken cancellationToken) =>
        _issueRatioLoader.LoadAsync(settings, issueTypes, cancellationToken);

    public Task<IssueTimelineLoadResult> LoadIssueTimelinesAsync(
        IReadOnlyList<IssueKey> issueKeys,
        IReadOnlyList<IssueKey> rejectIssueKeys,
        CancellationToken cancellationToken) =>
        _issueTimelineLoader.LoadAsync(issueKeys, rejectIssueKeys, cancellationToken);
}
