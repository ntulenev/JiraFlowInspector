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
    private readonly IssueSearchSnapshotLoader _issueSearchSnapshotLoader;
    private readonly JiraReportContextLoader _reportContextLoader;
    private readonly JiraIssueTimelineLoader _issueTimelineLoader;
    private readonly Dictionary<string, IssueSearchSnapshot> _issueSearchSnapshots =
        new(StringComparer.Ordinal);

    public JiraApplicationDataFacade(
        IJiraApiClient apiClient,
        IssueSearchSnapshotLoader issueSearchSnapshotLoader,
        JiraReportContextLoader reportContextLoader,
        JiraIssueTimelineLoader issueTimelineLoader)
    {
        ArgumentNullException.ThrowIfNull(apiClient);
        ArgumentNullException.ThrowIfNull(issueSearchSnapshotLoader);
        ArgumentNullException.ThrowIfNull(reportContextLoader);
        ArgumentNullException.ThrowIfNull(issueTimelineLoader);
        _apiClient = apiClient;
        _issueSearchSnapshotLoader = issueSearchSnapshotLoader;
        _reportContextLoader = reportContextLoader;
        _issueTimelineLoader = issueTimelineLoader;
    }

    public Task<JiraAuthUser> GetCurrentUserAsync(CancellationToken cancellationToken) =>
        _apiClient.GetCurrentUserAsync(cancellationToken);

    public Task<JiraReportContext> LoadReportContextAsync(
        AppSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return settings.CreatedAfter is null
            ? LoadReportContextWithAllTasksSnapshotAsync(settings, cancellationToken)
            : _reportContextLoader.LoadAsync(settings, cancellationToken);
    }

    public Task<IssueRatioSnapshot> LoadIssueRatioAsync(
        AppSettings settings,
        IReadOnlyList<IssueTypeName> issueTypes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(issueTypes);

        return LoadIssueRatioFromSnapshotAsync(settings, issueTypes, cancellationToken);
    }

    public Task<IssueTimelineLoadResult> LoadIssueTimelinesAsync(
        IReadOnlyList<IssueKey> issueKeys,
        IReadOnlyList<IssueKey> rejectIssueKeys,
        CancellationToken cancellationToken) =>
        _issueTimelineLoader.LoadAsync(issueKeys, rejectIssueKeys, cancellationToken);

    private async Task<JiraReportContext> LoadReportContextWithAllTasksSnapshotAsync(
        AppSettings settings,
        CancellationToken cancellationToken)
    {
        var allTasksSnapshot = await GetIssueSearchSnapshotAsync(settings, [], cancellationToken)
            .ConfigureAwait(false);
        return await _reportContextLoader.LoadAsync(settings, cancellationToken, allTasksSnapshot)
            .ConfigureAwait(false);
    }

    private async Task<IssueRatioSnapshot> LoadIssueRatioFromSnapshotAsync(
        AppSettings settings,
        IReadOnlyList<IssueTypeName> issueTypes,
        CancellationToken cancellationToken)
    {
        var searchSnapshot = await GetIssueSearchSnapshotAsync(settings, issueTypes, cancellationToken)
            .ConfigureAwait(false);
        return JiraIssueRatioLoader.Build(searchSnapshot);
    }

    private async Task<IssueSearchSnapshot> GetIssueSearchSnapshotAsync(
        AppSettings settings,
        IReadOnlyList<IssueTypeName> issueTypes,
        CancellationToken cancellationToken)
    {
        var cacheKey = BuildIssueSearchSnapshotCacheKey(issueTypes);
        if (_issueSearchSnapshots.TryGetValue(cacheKey, out var cachedSnapshot))
        {
            return cachedSnapshot;
        }

        var snapshot = await _issueSearchSnapshotLoader
            .LoadAsync(settings, issueTypes, cancellationToken)
            .ConfigureAwait(false);
        _issueSearchSnapshots[cacheKey] = snapshot;
        return snapshot;
    }

    private static string BuildIssueSearchSnapshotCacheKey(IReadOnlyList<IssueTypeName> issueTypes)
    {
        if (issueTypes.Count == 0)
        {
            return string.Empty;
        }

        return string.Join(
            "|",
            issueTypes
                .Select(static issueType => issueType.Value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase));
    }
}
