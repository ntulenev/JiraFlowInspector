using System.Text.Json;

namespace JiraMetrics.API.Mapping;

/// <summary>
/// Resolves and parses general-purpose Jira field values.
/// </summary>
public static class JiraFieldValueParser
{
    /// <summary>
    /// Finds an additional field by id, then by display name.
    /// </summary>
    /// <param name="additionalFields">Additional Jira fields.</param>
    /// <param name="fieldId">Optional Jira field id.</param>
    /// <param name="fieldName">Optional Jira field display name.</param>
    /// <param name="value">Resolved raw value.</param>
    /// <returns><see langword="true" /> when a matching field exists.</returns>
    public static bool TryGetValue(
        Dictionary<string, JsonElement> additionalFields,
        string? fieldId,
        string? fieldName,
        out JsonElement value)
    {
        ArgumentNullException.ThrowIfNull(additionalFields);

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

    /// <summary>
    /// Parses normalized, distinct values from a raw Jira field.
    /// </summary>
    /// <param name="rawValue">Raw Jira field value.</param>
    /// <returns>Parsed values ordered case-insensitively.</returns>
    public static IReadOnlyList<string> Parse(JsonElement rawValue)
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
                var parsed = TryParseSingleValue(item);
                if (!string.IsNullOrWhiteSpace(parsed))
                {
                    _ = values.Add(parsed.Trim());
                }
            }

            return [.. values.OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)];
        }

        var resolved = TryParseSingleValue(rawValue);
        if (string.IsNullOrWhiteSpace(resolved))
        {
            return [];
        }

        return [resolved.Trim()];
    }

    private static string? TryParseSingleValue(JsonElement value)
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

            var documentText = AtlassianDocumentTextReader.Read(value);
            if (!string.IsNullOrWhiteSpace(documentText))
            {
                return documentText;
            }
        }

        if (value.ValueKind is JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False)
        {
            return value.GetRawText();
        }

        return null;
    }
}
