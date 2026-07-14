using System.Globalization;
using System.Text.Json;

using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Transport.Models;

namespace JiraMetrics.API.Mapping;

/// <summary>
/// Maps Jira search issues into roadmap rows.
/// </summary>
public sealed class RoadmapItemMapper : IRoadmapItemMapper
{
    /// <inheritdoc />
    public JiraSearchFields BuildRequestedFields(RoadmapMappingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        return JiraSearchFields.From(
            "key",
            "summary",
            "status",
            context.RoadmapFieldId.Value,
            context.StartDateField.FieldId.Value,
            context.EndDateField.FieldId.Value);
    }

    /// <inheritdoc />
    public IReadOnlyList<RoadmapItem> MapIssues(
        IReadOnlyList<JiraIssueKeyResponse> issues,
        RoadmapMappingContext context)
    {
        ArgumentNullException.ThrowIfNull(issues);
        ArgumentNullException.ThrowIfNull(context);

        return [.. issues.Select(issue => MapIssue(issue, context))];
    }

    private static RoadmapItem MapIssue(
        JiraIssueKeyResponse issue,
        RoadmapMappingContext context)
    {
        var key = new IssueKey(
            issue.Key ?? throw new InvalidOperationException("Roadmap issue key is missing."));
        var fields = issue.Fields
            ?? throw new InvalidOperationException($"Roadmap issue '{key.Value}' fields are missing.");
        var summary = new IssueSummary(fields.Summary ?? key.Value);
        var status = string.IsNullOrWhiteSpace(fields.Status?.Name) ? "-" : fields.Status.Name.Trim();
        var additionalFields = fields.AdditionalFields;

        return new RoadmapItem(
            key,
            summary,
            status,
            ReadDropdown(additionalFields, context.RoadmapFieldId.Value),
            ReadDate(additionalFields, context.StartDateField),
            ReadDate(additionalFields, context.EndDateField));
    }

    private static string? ReadDropdown(Dictionary<string, JsonElement>? fields, string fieldId)
    {
        if (fields is null || !fields.TryGetValue(fieldId, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            return Normalize(value.GetString());
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            if (value.TryGetProperty("value", out var optionValue))
            {
                return Normalize(optionValue.GetString());
            }

            if (value.TryGetProperty("name", out var optionName))
            {
                return Normalize(optionName.GetString());
            }
        }

        return null;
    }

    private static DateOnly? ReadDate(
        Dictionary<string, JsonElement>? fields,
        RoadmapDateFieldReference field)
    {
        if (fields is null || !fields.TryGetValue(field.FieldId.Value, out var value))
        {
            return null;
        }

        return field.JsonPropertyName is { } propertyName
            ? ReadIntervalDate(value, propertyName)
            : ReadDateValue(value);
    }

    private static DateOnly? ReadIntervalDate(JsonElement value, string propertyName)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            return value.TryGetProperty(propertyName, out var propertyValue)
                ? ReadDateValue(propertyValue)
                : null;
        }

        if (value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(value.GetString()!);
            return document.RootElement.TryGetProperty(propertyName, out var propertyValue)
                ? ReadDateValue(propertyValue)
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static DateOnly? ReadDateValue(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var text = value.GetString();
        if (DateOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }

        return DateTimeOffset.TryParse(
            text,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var timestamp)
                ? DateOnly.FromDateTime(timestamp.Date)
                : null;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
