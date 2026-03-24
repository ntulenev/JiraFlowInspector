using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JiraMetrics.API.Mapping;

/// <summary>
/// Reads structured and custom Jira field values from raw JSON.
/// </summary>
[SuppressMessage(
    "Performance",
    "CA1822",
    Justification = "Stateless helper is composed as a service to keep parsing behavior grouped.")]
public sealed partial class JiraFieldValueReader
{
    internal bool HasPullRequestInRawValue(JsonElement rawValue)
    {
        if (rawValue.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return false;
        }

        var rawText = rawValue.ValueKind == JsonValueKind.String
            ? rawValue.GetString()
            : rawValue.GetRawText();
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return false;
        }

        if (rawText.IndexOf("pullrequest", StringComparison.OrdinalIgnoreCase) < 0)
        {
            return false;
        }

        var matches = PullRequestCountPattern().Matches(rawText);
        if (matches.Count == 0)
        {
            return true;
        }

        foreach (Match match in matches)
        {
            if (!int.TryParse(
                match.Groups[1].Value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var count))
            {
                continue;
            }

            if (count > 0)
            {
                return true;
            }
        }

        return false;
    }

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

    internal IReadOnlyList<string> ParseComponentValues(JsonElement rawComponents)
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
                var value = TryGetComponentValue(item);
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
            var value = TryGetComponentValue(rawComponents);
            if (!string.IsNullOrWhiteSpace(value))
            {
                _ = values.Add(value);
            }

            return [.. values.OrderBy(static item => item, StringComparer.OrdinalIgnoreCase)];
        }

        return [];
    }

    private static string? TryGetComponentValue(JsonElement value)
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

            var adfText = TryExtractAtlassianDocumentText(value);
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

    private static string? TryExtractAtlassianDocumentText(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!value.TryGetProperty("type", out var typeElement)
            || typeElement.ValueKind != JsonValueKind.String
            || !string.Equals(typeElement.GetString(), "doc", StringComparison.OrdinalIgnoreCase)
            || !value.TryGetProperty("content", out var contentElement)
            || contentElement.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var fragments = new List<string>();
        AppendAtlassianDocumentText(contentElement, fragments);
        var text = string.Join(
            " ",
            fragments.Where(static fragment => !string.IsNullOrWhiteSpace(fragment))).Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static void AppendAtlassianDocumentText(JsonElement value, List<string> fragments)
    {
        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray())
            {
                AppendAtlassianDocumentText(item, fragments);
            }

            return;
        }

        if (value.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        if (value.TryGetProperty("text", out var textElement)
            && textElement.ValueKind == JsonValueKind.String)
        {
            var text = textElement.GetString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                fragments.Add(text.Trim());
            }
        }

        if (value.TryGetProperty("content", out var contentElement)
            && contentElement.ValueKind == JsonValueKind.Array)
        {
            AppendAtlassianDocumentText(contentElement, fragments);
        }
    }

    [GeneratedRegex(@"(?:stateCount|count)\s*""?\s*[:=]\s*(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex PullRequestCountPattern();
}
