using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models.Configuration;

/// <summary>
/// Validated settings for automated test coverage calculation.
/// </summary>
public sealed record TestCoverageSettings
{
    private const string DEFAULT_TEST_PROJECT_KEY = "QA";
    private const string DEFAULT_LINK_NAME = "is tested by";
    private static readonly IssueTypeName[] _defaultIssueTypes = [new("SuperTask")];

    /// <summary>
    /// Initializes a new instance of the <see cref="TestCoverageSettings"/> class.
    /// </summary>
    /// <param name="enabled">Whether automated test coverage should be calculated.</param>
    /// <param name="issueTypes">Issue types included into coverage denominator.</param>
    /// <param name="testProjectKey">Jira project key that contains automated test tasks.</param>
    /// <param name="linkName">Issue-link relation name that marks tested tasks.</param>
    public TestCoverageSettings(
        bool enabled = true,
        IReadOnlyList<IssueTypeName>? issueTypes = null,
        ProjectKey? testProjectKey = null,
        string? linkName = null)
    {
        Enabled = enabled;
        IssueTypes = issueTypes is { Count: > 0 } ? [.. issueTypes] : [.. _defaultIssueTypes];
        TestProjectKey = testProjectKey ?? new ProjectKey(DEFAULT_TEST_PROJECT_KEY);
        LinkName = string.IsNullOrWhiteSpace(linkName) ? DEFAULT_LINK_NAME : linkName.Trim();
    }

    /// <summary>
    /// Gets whether automated test coverage should be calculated.
    /// </summary>
    public bool Enabled { get; }

    /// <summary>
    /// Gets issue types included into coverage denominator.
    /// </summary>
    public IReadOnlyList<IssueTypeName> IssueTypes { get; }

    /// <summary>
    /// Gets Jira project key that contains automated test tasks.
    /// </summary>
    public ProjectKey TestProjectKey { get; }

    /// <summary>
    /// Gets issue-link relation name that marks tested tasks.
    /// </summary>
    public string LinkName { get; }
}
