using System.Text.Json;

namespace JiraMetrics.API.Mapping;

/// <summary>
/// Extracts plain text from Atlassian Document Format values.
/// </summary>
public static class AtlassianDocumentTextReader
{
    /// <summary>
    /// Attempts to read normalized text from an Atlassian document value.
    /// </summary>
    /// <param name="value">Raw Jira field value.</param>
    /// <returns>Combined document text, or <see langword="null" /> for unsupported values.</returns>
    public static string? Read(JsonElement value)
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
        AppendText(contentElement, fragments);
        var text = string.Join(
            " ",
            fragments.Where(static fragment => !string.IsNullOrWhiteSpace(fragment))).Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static void AppendText(JsonElement value, List<string> fragments)
    {
        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray())
            {
                AppendText(item, fragments);
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
            AppendText(contentElement, fragments);
        }
    }
}
