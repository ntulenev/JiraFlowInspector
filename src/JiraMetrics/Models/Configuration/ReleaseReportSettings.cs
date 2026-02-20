using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models.Configuration;

/// <summary>
/// Represents validated release report settings.
/// </summary>
public sealed record ReleaseReportSettings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReleaseReportSettings"/> class.
    /// </summary>
    /// <param name="releaseProjectKey">Release project key.</param>
    /// <param name="projectLabel">Project label used in release search.</param>
    /// <param name="releaseDateFieldName">Release date field name.</param>
    /// <param name="componentsFieldName">Optional components field name.</param>
    public ReleaseReportSettings(
        ProjectKey releaseProjectKey,
        string projectLabel,
        string releaseDateFieldName,
        string? componentsFieldName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectLabel);
        ArgumentException.ThrowIfNullOrWhiteSpace(releaseDateFieldName);

        ReleaseProjectKey = releaseProjectKey;
        ProjectLabel = projectLabel.Trim();
        ReleaseDateFieldName = releaseDateFieldName.Trim();
        ComponentsFieldName = string.IsNullOrWhiteSpace(componentsFieldName) ? null : componentsFieldName.Trim();
    }

    /// <summary>
    /// Gets release project key.
    /// </summary>
    public ProjectKey ReleaseProjectKey { get; }

    /// <summary>
    /// Gets project label used in release search.
    /// </summary>
    public string ProjectLabel { get; }

    /// <summary>
    /// Gets release date field name.
    /// </summary>
    public string ReleaseDateFieldName { get; }

    /// <summary>
    /// Gets optional components field name.
    /// </summary>
    public string? ComponentsFieldName { get; }
}
