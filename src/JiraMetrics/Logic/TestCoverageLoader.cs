using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;

namespace JiraMetrics.Logic;

/// <summary>
/// Loads automated test coverage for completed issues.
/// </summary>
internal sealed class TestCoverageLoader
{
    public TestCoverageLoader(IJiraIssueSearchClient issueSearchClient)
    {
        ArgumentNullException.ThrowIfNull(issueSearchClient);
        _issueSearchClient = issueSearchClient;
    }

    public async Task<TestCoverageSnapshot> LoadAsync(
        AppSettings settings,
        TestCoverageSettings coverageSettings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(coverageSettings);

        if (!coverageSettings.Enabled)
        {
            return TestCoverageSnapshot.Empty;
        }

        var doneIssues = await _issueSearchClient.GetIssuesMovedToDoneThisMonthAsync(
                settings.ProjectKey,
                settings.DoneStatusName,
                coverageSettings.IssueTypes,
                cancellationToken,
                includeIssueLinks: true)
            .ConfigureAwait(false);

        var coveredIssues = doneIssues
            .Where(issue => IsCoveredByAutomatedTest(issue, coverageSettings))
            .ToArray();

        return new TestCoverageSnapshot(doneIssues, coveredIssues);
    }

    private static bool IsCoveredByAutomatedTest(
        IssueListItem issue,
        TestCoverageSettings coverageSettings) =>
        issue.IssueLinks.Any(link =>
            string.Equals(link.RelationName, coverageSettings.LinkName, StringComparison.OrdinalIgnoreCase)
            && IsIssueFromProject(link.Key.Value, coverageSettings.TestProjectKey.Value));

    private static bool IsIssueFromProject(string issueKey, string projectKey) =>
        issueKey.StartsWith(projectKey + "-", StringComparison.OrdinalIgnoreCase);

    private readonly IJiraIssueSearchClient _issueSearchClient;
}
