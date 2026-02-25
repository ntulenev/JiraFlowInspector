using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models.Configuration;

/// <summary>
/// Represents validated release report settings.
/// </summary>
public sealed record ReleaseReportSettings
{
    private const string DEFAULT_HOT_FIX_FIELD_NAME = "Change type";
    private const string DEFAULT_HOT_FIX_FIELD_VALUE = "Emergency";

    /// <summary>
    /// Initializes a new instance of the <see cref="ReleaseReportSettings"/> class.
    /// </summary>
    /// <param name="releaseProjectKey">Release project key.</param>
    /// <param name="projectLabel">Project label used in release search.</param>
    /// <param name="releaseDateFieldName">Release date field name.</param>
    /// <param name="componentsFieldName">Optional components field name.</param>
    /// <param name="hotFixRules">Optional hot-fix marker rules in format <c>field name -&gt; values</c>. Defaults to <c>Change type -&gt; Emergency</c>.</param>
    public ReleaseReportSettings(
        ProjectKey releaseProjectKey,
        string projectLabel,
        string releaseDateFieldName,
        string? componentsFieldName = null,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? hotFixRules = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectLabel);
        ArgumentException.ThrowIfNullOrWhiteSpace(releaseDateFieldName);

        ReleaseProjectKey = releaseProjectKey;
        ProjectLabel = projectLabel.Trim();
        ReleaseDateFieldName = releaseDateFieldName.Trim();
        ComponentsFieldName = string.IsNullOrWhiteSpace(componentsFieldName) ? null : componentsFieldName.Trim();
        HotFixRules = NormalizeHotFixRules(hotFixRules);
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

    /// <summary>
    /// Gets hot-fix marker rules in format <c>field name -&gt; values</c>.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> HotFixRules { get; }

    private static Dictionary<string, IReadOnlyList<string>> NormalizeHotFixRules(
        IReadOnlyDictionary<string, IReadOnlyList<string>>? source)
    {
        var normalized = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);

        if (source is not null)
        {
            foreach (var (rawFieldName, rawValues) in source)
            {
                if (string.IsNullOrWhiteSpace(rawFieldName) || rawValues is null)
                {
                    continue;
                }

                var values = rawValues
                    .Where(static value => !string.IsNullOrWhiteSpace(value))
                    .Select(static value => value.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                if (values.Length == 0)
                {
                    continue;
                }

                normalized[rawFieldName.Trim()] = values;
            }
        }

        if (normalized.Count == 0)
        {
            normalized[DEFAULT_HOT_FIX_FIELD_NAME] = [DEFAULT_HOT_FIX_FIELD_VALUE];
        }

        return normalized;
    }
}
