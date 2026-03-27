using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models;

/// <summary>
/// Represents one architecture task row for reporting.
/// </summary>
public sealed record ArchTaskItem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ArchTaskItem"/> class.
    /// </summary>
    /// <param name="key">Issue key.</param>
    /// <param name="title">Issue title.</param>
    /// <param name="createdAt">Issue creation timestamp.</param>
    /// <param name="resolvedAt">Optional issue resolution timestamp.</param>
    public ArchTaskItem(
        IssueKey key,
        IssueSummary title,
        DateTimeOffset createdAt,
        DateTimeOffset? resolvedAt = null)
    {
        Key = key;
        Title = title;
        CreatedAt = createdAt;
        ResolvedAt = resolvedAt;
    }

    /// <summary>
    /// Gets issue key.
    /// </summary>
    public IssueKey Key { get; }

    /// <summary>
    /// Gets issue title.
    /// </summary>
    public IssueSummary Title { get; }

    /// <summary>
    /// Gets issue creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// Gets issue resolution timestamp when available.
    /// </summary>
    public DateTimeOffset? ResolvedAt { get; }

    /// <summary>
    /// Gets whether issue is resolved.
    /// </summary>
    public bool IsResolved => ResolvedAt.HasValue;

    /// <summary>
    /// Calculates elapsed duration from creation until resolution or the supplied current moment.
    /// </summary>
    /// <param name="now">Current timestamp used for unresolved issues.</param>
    /// <returns>Elapsed duration.</returns>
    public TimeSpan GetElapsed(DateTimeOffset now)
    {
        var finishAt = ResolvedAt ?? now;
        return finishAt <= CreatedAt ? TimeSpan.Zero : finishAt - CreatedAt;
    }
}
