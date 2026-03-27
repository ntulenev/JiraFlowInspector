namespace JiraMetrics.Models.Configuration;

/// <summary>
/// Release report options.
/// </summary>
public sealed class ReleaseReportOptions
{
    /// <summary>
    /// Gets or sets release project key.
    /// </summary>
    public string? ReleaseProjectKey { get; init; }

    /// <summary>
    /// Gets or sets project label used in release search.
    /// </summary>
    public string? ProjectLabel { get; init; }

    /// <summary>
    /// Gets or sets release date field name.
    /// </summary>
    public string? ReleaseDateFieldName { get; init; }

    /// <summary>
    /// Gets or sets optional components field name.
    /// </summary>
    public string? ComponentsFieldName { get; init; }

    /// <summary>
    /// Gets or sets optional hot-fix marker rules in format <c>field name -&gt; values</c>.
    /// </summary>
    public Dictionary<string, string[]>? HotFixRules { get; init; }

    /// <summary>
    /// Gets or sets optional rollback field name.
    /// </summary>
    public string? RollbackFieldName { get; init; }

    /// <summary>
    /// Gets or sets optional environment field name used for release filtering.
    /// </summary>
    public string? EnvironmentFieldName { get; init; }

    /// <summary>
    /// Gets or sets optional environment field value used for release filtering.
    /// </summary>
    public string? EnvironmentFieldValue { get; init; }
}
