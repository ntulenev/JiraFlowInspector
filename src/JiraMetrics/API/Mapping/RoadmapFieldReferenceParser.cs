using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.API.Mapping;

internal static class RoadmapFieldReferenceParser
{
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
