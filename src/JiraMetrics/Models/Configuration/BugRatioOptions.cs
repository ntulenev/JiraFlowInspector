namespace JiraMetrics.Models.Configuration;

/// <summary>
/// Bug ratio options.
/// </summary>
public sealed class BugRatioOptions
{
    /// <summary>
    /// Gets or sets optional issue types that should be treated as bug-like issues.
    /// </summary>
    public IReadOnlyList<string>? BugIssueNames { get; init; }

    /// <summary>
    /// Gets or sets optional field name or id that marks bugs reproduced on production.
    /// </summary>
    public string? ReporducedOnProd { get; init; }
}
