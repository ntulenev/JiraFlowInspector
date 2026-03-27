using System.Globalization;
using System.Text.Json;

using JiraMetrics.API.FieldResolution;
using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Transport.Models;

#pragma warning disable CS1591
namespace JiraMetrics.API.Mapping;

/// <summary>
/// Maps Jira search issues into global incident rows.
/// </summary>
public sealed class GlobalIncidentMapper : IGlobalIncidentMapper
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalIncidentMapper"/> class.
    /// </summary>
    /// <param name="fieldValueReader">Field value reader.</param>
    public GlobalIncidentMapper(JiraFieldValueReader fieldValueReader)
    {
        _fieldValueReader = fieldValueReader ?? throw new ArgumentNullException(nameof(fieldValueReader));
    }

    public IReadOnlyList<string> BuildRequestedFields(GlobalIncidentMappingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var fields = new List<string>
        {
            "key",
            "summary"
        };

        foreach (var incidentStartField in context.IncidentStartFields)
        {
            AddFieldIfNeeded(fields, incidentStartField.FieldId);
        }

        foreach (var incidentRecoveryField in context.IncidentRecoveryFields)
        {
            AddFieldIfNeeded(fields, incidentRecoveryField.FieldId);
        }

        AddFieldIfNeeded(fields, context.ImpactFieldId);
        AddFieldIfNeeded(fields, context.UrgencyFieldId);

        foreach (var additionalFieldId in context.AdditionalFieldIds.Values)
        {
            AddFieldIfNeeded(fields, additionalFieldId);
        }

        return fields;
    }

    public IReadOnlyList<GlobalIncidentItem> MapIssues(
        IReadOnlyList<JiraIssueKeyResponse> issues,
        GlobalIncidentMappingContext context)
    {
        ArgumentNullException.ThrowIfNull(issues);
        ArgumentNullException.ThrowIfNull(context);

        var incidents = issues
            .Where(static issue => !string.IsNullOrWhiteSpace(issue.Key))
            .Select(issue =>
            {
                var startedAt = TryParseConfiguredDateTimeField(issue.Fields, context.IncidentStartFields);
                if (!startedAt.HasValue)
                {
                    return null;
                }

                var recoveredAt = TryParseConfiguredDateTimeField(
                    issue.Fields,
                    context.IncidentRecoveryFields);
                var impact = ResolveFieldDisplayValue(
                    issue.Fields,
                    context.ImpactFieldId,
                    context.ImpactFieldName);
                var urgency = ResolveFieldDisplayValue(
                    issue.Fields,
                    context.UrgencyFieldId,
                    context.UrgencyFieldName);
                var additionalFields = ResolveAdditionalFieldValues(issue.Fields, context.AdditionalFieldIds);

                return new GlobalIncidentItem(
                    new IssueKey(issue.Key!.Trim()),
                    new IssueSummary(
                        string.IsNullOrWhiteSpace(issue.Fields?.Summary)
                            ? "No summary"
                            : issue.Fields.Summary),
                    startedAt,
                    recoveredAt,
                    impact,
                    urgency,
                    additionalFields);
            })
            .Where(static item => item is not null)
            .Cast<GlobalIncidentItem>();

        return [.. incidents
            .DistinctBy(static incident => incident.Key.Value, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static incident => incident.IncidentStartUtc)
            .ThenBy(static incident => incident.Key.Value, StringComparer.OrdinalIgnoreCase)];
    }

    private static void AddFieldIfNeeded(List<string> fields, string? fieldId)
    {
        if (!string.IsNullOrWhiteSpace(fieldId))
        {
            fields.Add(fieldId);
        }
    }

    private DateTimeOffset? TryParseConfiguredDateTimeField(
        JiraIssueFieldsResponse? fields,
        IReadOnlyList<ResolvedJiraField> fieldCandidates)
    {
        foreach (var fieldCandidate in fieldCandidates)
        {
            var resolvedDateTime = TryParseConfiguredDateTimeField(
                fields,
                fieldCandidate.FieldId,
                fieldCandidate.FieldName);
            if (resolvedDateTime.HasValue)
            {
                return resolvedDateTime;
            }
        }

        return null;
    }

    private DateTimeOffset? TryParseConfiguredDateTimeField(
        JiraIssueFieldsResponse? fields,
        string? fieldId,
        string? fieldName)
    {
        if (fields?.AdditionalFields is null || fields.AdditionalFields.Count == 0)
        {
            return null;
        }

        if (!_fieldValueReader.TryGetAdditionalFieldValue(
            fields.AdditionalFields,
            fieldId,
            fieldName,
            out var rawDateTime))
        {
            return null;
        }

        var resolvedValues = _fieldValueReader.ParseRawFieldValues(rawDateTime);
        if (resolvedValues.Count == 0 || string.IsNullOrWhiteSpace(resolvedValues[0]))
        {
            return null;
        }

        var resolvedValue = resolvedValues[0];

        if (DateTimeOffset.TryParseExact(
                resolvedValue,
                "yyyy-MM-dd HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var exactUtcDateTime))
        {
            return exactUtcDateTime;
        }

        if (DateTimeOffset.TryParse(
                resolvedValue,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsedDateTime))
        {
            return parsedDateTime;
        }

        if (DateOnly.TryParse(
            resolvedValue,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var dateOnly))
        {
            return new DateTimeOffset(dateOnly.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        }

        return null;
    }

    private string? ResolveFieldDisplayValue(
        JiraIssueFieldsResponse? fields,
        string? fieldId,
        string? fieldName)
    {
        if (fields?.AdditionalFields is null || fields.AdditionalFields.Count == 0)
        {
            return null;
        }

        if (!_fieldValueReader.TryGetAdditionalFieldValue(
            fields.AdditionalFields,
            fieldId,
            fieldName,
            out var rawValue))
        {
            return null;
        }

        var parsedValues = _fieldValueReader.ParseRawFieldValues(rawValue);
        if (parsedValues.Count > 0)
        {
            return string.Join(", ", parsedValues);
        }

        var rawPayload = rawValue.ValueKind == JsonValueKind.String
            ? rawValue.GetString()
            : rawValue.GetRawText();
        return string.IsNullOrWhiteSpace(rawPayload) ? null : rawPayload.Trim();
    }

    private Dictionary<string, string?> ResolveAdditionalFieldValues(
        JiraIssueFieldsResponse? fields,
        IReadOnlyDictionary<string, string?> additionalFieldIds)
    {
        if (additionalFieldIds.Count == 0)
        {
            return new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        }

        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (fieldName, fieldId) in additionalFieldIds)
        {
            values[fieldName] = ResolveFieldDisplayValue(fields, fieldId, fieldName);
        }

        return values;
    }

    private readonly JiraFieldValueReader _fieldValueReader;
}

public sealed record GlobalIncidentMappingContext(
    IReadOnlyList<ResolvedJiraField> IncidentStartFields,
    IReadOnlyList<ResolvedJiraField> IncidentRecoveryFields,
    string? ImpactFieldId,
    string ImpactFieldName,
    string? UrgencyFieldId,
    string UrgencyFieldName,
    IReadOnlyDictionary<string, string?> AdditionalFieldIds);
#pragma warning restore CS1591

