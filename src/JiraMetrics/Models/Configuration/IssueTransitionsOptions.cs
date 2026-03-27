using System.ComponentModel.DataAnnotations;

namespace JiraMetrics.Models.Configuration;

/// <summary>
/// Issue transition-related options.
/// </summary>
public sealed class IssueTransitionsOptions
{
    /// <summary>
    /// Gets or sets required stage names in path.
    /// </summary>
    [Required]
    [MinLength(1)]
    public required IReadOnlyList<string> RequiredPathStages { get; init; }

    /// <summary>
    /// Gets or sets optional issue types filter.
    /// </summary>
    public IReadOnlyList<string>? IssueTypes { get; init; }

    /// <summary>
    /// Gets or sets whether to exclude weekends from transition durations.
    /// </summary>
    public bool ExcludeWeekend { get; init; }

    /// <summary>
    /// Gets or sets optional list of excluded days in dd.MM.yyyy or yyyy-MM-dd format.
    /// </summary>
    public IReadOnlyList<string>? ExcludedDays { get; init; }
}
