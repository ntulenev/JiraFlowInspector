namespace JiraMetrics.Models.Configuration;

/// <summary>
/// Raw options for automated test coverage calculation.
/// </summary>
public sealed class TestCoverageOptions
{
    /// <summary>
    /// Gets or sets whether automated test coverage should be calculated.
    /// </summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Gets or sets issue types included into coverage denominator.
    /// </summary>
    public IReadOnlyList<string>? IssueTypes { get; init; }

    /// <summary>
    /// Gets or sets Jira project key that contains automated test tasks.
    /// </summary>
    public string? TestProjectKey { get; init; }

    /// <summary>
    /// Gets or sets issue-link relation name that marks tested tasks.
    /// </summary>
    public string? LinkName { get; init; }
}
