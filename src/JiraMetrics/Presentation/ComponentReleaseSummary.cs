using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Presentation;

/// <summary>
/// Prepared release count for one component.
/// </summary>
internal sealed class ComponentReleaseSummary
{
    /// <summary>
    /// Initializes a component release summary.
    /// </summary>
    /// <param name="componentName">Normalized component name.</param>
    /// <param name="releaseCount">Number of releases associated with the component.</param>
    public ComponentReleaseSummary(
        string componentName,
        ItemCount releaseCount)
    {
        ComponentName = componentName;
        ReleaseCount = releaseCount;
    }

    /// <summary>
    /// Gets the normalized component name.
    /// </summary>
    public string ComponentName { get; }

    /// <summary>
    /// Gets the number of releases associated with the component.
    /// </summary>
    public ItemCount ReleaseCount { get; }
}
