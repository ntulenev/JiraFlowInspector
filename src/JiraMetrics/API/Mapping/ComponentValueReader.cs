using System.Text.Json;

namespace JiraMetrics.API.Mapping;

/// <summary>
/// Reads component names from Jira standard and custom field values.
/// </summary>
public static class ComponentValueReader
{
    /// <summary>
    /// Parses normalized, distinct component names.
    /// </summary>
    /// <param name="rawComponents">Raw Jira components field value.</param>
    /// <returns>Component names ordered case-insensitively.</returns>
    public static IReadOnlyList<string> Read(JsonElement rawComponents)
    {
        if (rawComponents.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return [];
        }

        var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (rawComponents.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in rawComponents.EnumerateArray())
            {
                var value = TryGetValue(item);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _ = values.Add(value);
                }
            }

            return [.. values.OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)];
        }

        if (rawComponents.ValueKind == JsonValueKind.String)
        {
            var raw = rawComponents.GetString();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return [];
            }

            return [.. raw
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(static item => item.Trim())
                .Where(static item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static item => item, StringComparer.OrdinalIgnoreCase)];
        }

        if (rawComponents.ValueKind == JsonValueKind.Object)
        {
            var value = TryGetValue(rawComponents);
            if (!string.IsNullOrWhiteSpace(value))
            {
                _ = values.Add(value);
            }

            return [.. values.OrderBy(static item => item, StringComparer.OrdinalIgnoreCase)];
        }

        return [];
    }

    private static string? TryGetValue(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.String)
        {
            var text = value.GetString();
            return string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

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

        return null;
    }
}
