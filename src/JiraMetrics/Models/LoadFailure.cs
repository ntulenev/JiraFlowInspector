using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models;

/// <summary>
/// Represents a failed issue load operation.
/// </summary>
public sealed record LoadFailure
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoadFailure"/> class.
    /// </summary>
    /// <param name="issueKey">Issue key that failed.</param>
    /// <param name="reason">Failure reason.</param>
    public LoadFailure(IssueKey issueKey, ErrorMessage reason)
    {
        IssueKey = issueKey;
        Reason = reason;
    }

    /// <summary>
    /// Gets the failed issue key.
    /// </summary>
    public IssueKey IssueKey { get; }

    /// <summary>
    /// Gets the failure reason.
    /// </summary>
    public ErrorMessage Reason { get; }
}
