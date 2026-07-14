using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.API.Mapping;

/// <summary>
/// Parses Jira interval-field references used by roadmap date settings.
/// </summary>
internal static class RoadmapFieldReferenceParser
{
    /// <summary>
    /// Attempts to parse a configured interval-field reference.
    /// </summary>
    /// <param name="configuredField">Configured Jira field reference.</param>
    /// <param name="field">Parsed field reference when successful.</param>
    /// <returns><see langword="true" /> when the reference uses a supported interval component.</returns>
    public static bool TryParseIntervalField(
        string configuredField,
        out RoadmapDateFieldReference field)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configuredField);

        var value = configuredField.Trim();
        var fieldIdEnd = value.IndexOf("][", StringComparison.Ordinal);
        if (!value.StartsWith("cf[", StringComparison.OrdinalIgnoreCase)
            || fieldIdEnd < 4
            || !value.EndsWith(']'))
        {
            field = default;
            return false;
        }

        var numericId = value[3..fieldIdEnd];
        var component = value[(fieldIdEnd + 2)..^1];
        if (!numericId.All(char.IsDigit))
        {
            field = default;
            return false;
        }

        var jsonPropertyName = component.ToUpperInvariant() switch
        {
            "STARTDATE" => "start",
            "ENDDATE" => "end",
            _ => null
        };
        if (jsonPropertyName is null)
        {
            field = default;
            return false;
        }

        field = new RoadmapDateFieldReference(
            new JiraFieldId($"customfield_{numericId}"),
            jsonPropertyName);
        return true;
    }
}
