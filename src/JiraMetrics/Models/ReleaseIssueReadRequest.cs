using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models;

/// <summary>
/// Represents a typed request for reading release issues.
/// </summary>
public sealed record ReleaseIssueReadRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReleaseIssueReadRequest"/> class.
    /// </summary>
    public ReleaseIssueReadRequest(
        ProjectKey releaseProjectKey,
        JiraLabel projectLabel,
        JiraFieldName releaseDateFieldName,
        JiraFieldName? componentsFieldName,
        IReadOnlyList<HotFixRule> hotFixRules,
        JiraFieldName rollbackFieldName,
        ReleaseEnvironmentFilter? environmentFilter)
    {
        ArgumentNullException.ThrowIfNull(hotFixRules);

        ReleaseProjectKey = releaseProjectKey;
        ProjectLabel = projectLabel;
        ReleaseDateFieldName = releaseDateFieldName;
        ComponentsFieldName = componentsFieldName;
        HotFixRules = [.. hotFixRules];
        RollbackFieldName = rollbackFieldName;
        EnvironmentFilter = environmentFilter;
    }

    /// <summary>
    /// Gets release project key.
    /// </summary>
    public ProjectKey ReleaseProjectKey { get; }

    /// <summary>
    /// Gets project label.
    /// </summary>
    public JiraLabel ProjectLabel { get; }

    /// <summary>
    /// Gets release date field name.
    /// </summary>
    public JiraFieldName ReleaseDateFieldName { get; }

    /// <summary>
    /// Gets optional components field name.
    /// </summary>
    public JiraFieldName? ComponentsFieldName { get; }

    /// <summary>
    /// Gets hot-fix rules.
    /// </summary>
    public IReadOnlyList<HotFixRule> HotFixRules { get; }

    /// <summary>
    /// Gets rollback field name.
    /// </summary>
    public JiraFieldName RollbackFieldName { get; }

    /// <summary>
    /// Gets optional environment filter.
    /// </summary>
    public ReleaseEnvironmentFilter? EnvironmentFilter { get; }
}
