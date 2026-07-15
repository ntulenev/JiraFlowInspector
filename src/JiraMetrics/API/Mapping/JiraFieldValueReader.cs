using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace JiraMetrics.API.Mapping;

/// <summary>
/// Reads structured and custom Jira field values from raw JSON.
/// </summary>
[SuppressMessage(
    "Performance",
    "CA1822",
    Justification = "Stateless helper is composed as a service to keep parsing behavior grouped.")]
public sealed class JiraFieldValueReader
{
    internal bool HasPullRequestInRawValue(JsonElement rawValue) =>
        PullRequestDetector.HasPullRequest(rawValue);

    internal bool TryGetAdditionalFieldValue(
        Dictionary<string, JsonElement> additionalFields,
        string? fieldId,
        string? fieldName,
        out JsonElement value)
    {
        if (!string.IsNullOrWhiteSpace(fieldId) && additionalFields.TryGetValue(fieldId, out value))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(fieldName) && additionalFields.TryGetValue(fieldName, out value))
        {
            return true;
        }

        value = default;
        return false;
    }

    internal IReadOnlyList<string> ParseRawFieldValues(JsonElement rawValue)
    {
        if (rawValue.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return [];
        }

        if (rawValue.ValueKind == JsonValueKind.Array)
        {
            var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in rawValue.EnumerateArray())
            {
                var parsed = TryGetFieldValue(item);
                if (!string.IsNullOrWhiteSpace(parsed))
                {
                    _ = values.Add(parsed.Trim());
                }
            }

            return [.. values.OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)];
        }

        var resolved = TryGetFieldValue(rawValue);
        if (string.IsNullOrWhiteSpace(resolved))
        {
            return [];
        }

        return [resolved.Trim()];
    }

    internal IReadOnlyList<string> ParseComponentValues(JsonElement rawComponents) =>
        ComponentValueReader.Read(rawComponents);

    private static string? TryGetFieldValue(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.String)
        {
            var text = value.GetString();
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            if (value.TryGetProperty("value", out var rawValue) && rawValue.ValueKind == JsonValueKind.String)
            {
                var text = rawValue.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text.Trim();
                }
            }

            if (value.TryGetProperty("name", out var rawName) && rawName.ValueKind == JsonValueKind.String)
            {
                var text = rawName.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text.Trim();
                }
            }

            if (value.TryGetProperty("id", out var rawId) && rawId.ValueKind == JsonValueKind.String)
            {
                var text = rawId.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text.Trim();
                }
            }

            var adfText = AtlassianDocumentTextReader.Read(value);
            if (!string.IsNullOrWhiteSpace(adfText))
            {
                return adfText;
            }
        }

        if (value.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
        {
            return value.GetRawText();
        }

        return null;
    }

}
