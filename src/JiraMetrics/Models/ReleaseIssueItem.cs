using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models;

/// <summary>
/// Represents one release issue row in release report.
/// </summary>
public sealed record ReleaseIssueItem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReleaseIssueItem"/> class.
    /// </summary>
    /// <param name="key">Issue key.</param>
    /// <param name="title">Issue title.</param>
    /// <param name="releaseDate">Release date.</param>
    /// <param name="tasks">Count of linked work items.</param>
    /// <param name="components">Count of components for configured components field.</param>
    /// <param name="status">Current issue status.</param>
    /// <param name="componentNames">Component names for configured components field.</param>
    public ReleaseIssueItem(
        IssueKey key,
        IssueSummary title,
        DateOnly releaseDate,
        int tasks = 0,
        int components = 0,
        StatusName? status = null,
        IReadOnlyList<string>? componentNames = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(tasks);
        ArgumentOutOfRangeException.ThrowIfNegative(components);

        Key = key;
        Title = title;
        ReleaseDate = releaseDate;
        Tasks = tasks;
        Components = components;
        Status = status ?? StatusName.Unknown;
        ComponentNames = componentNames is null
            ? []
            : [.. componentNames
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Select(static value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)];
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
    /// Gets release date.
    /// </summary>
    public DateOnly ReleaseDate { get; }

    /// <summary>
    /// Gets current issue status.
    /// </summary>
    public StatusName Status { get; }

    /// <summary>
    /// Gets count of linked work items.
    /// </summary>
    public int Tasks { get; }

    /// <summary>
    /// Gets count of components for configured components field.
    /// </summary>
    public int Components { get; }

    /// <summary>
    /// Gets component names for configured components field.
    /// </summary>
    public IReadOnlyList<string> ComponentNames { get; }
}
