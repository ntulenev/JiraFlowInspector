using System.Globalization;
using System.Text.Json;

using JiraMetrics.API.FieldResolution;
using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Transport.Models;

#pragma warning disable CS1591
namespace JiraMetrics.API.Mapping;

/// <summary>
/// Maps Jira search issues into release issue rows.
/// </summary>
public sealed class ReleaseIssueMapper : IReleaseIssueMapper
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReleaseIssueMapper"/> class.
    /// </summary>
    /// <param name="fieldValueReader">Field value reader.</param>
    public ReleaseIssueMapper(JiraFieldValueReader fieldValueReader)
    {
        _fieldValueReader = fieldValueReader ?? throw new ArgumentNullException(nameof(fieldValueReader));
    }

    public IReadOnlyList<string> BuildRequestedFields(ReleaseIssueMappingContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var fields = new List<string>
        {
            "key",
            "summary",
            "status",
            "issuelinks",
            context.ReleaseFieldId
        };

        if (!string.IsNullOrWhiteSpace(context.ComponentsFieldId))
        {
            fields.Add(context.ComponentsFieldId);
        }

        foreach (var hotFixFieldId in context.HotFixRules
            .Select(static rule => rule.FieldId)
            .Where(static fieldId => !string.IsNullOrWhiteSpace(fieldId))
            .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            fields.Add(hotFixFieldId!);
        }

        if (!string.IsNullOrWhiteSpace(context.RollbackFieldId))
        {
            fields.Add(context.RollbackFieldId);
        }

        if (!string.IsNullOrWhiteSpace(context.EnvironmentFieldId))
        {
            fields.Add(context.EnvironmentFieldId);
        }

        if (!string.IsNullOrWhiteSpace(context.ComponentsFieldId)
            || !string.IsNullOrWhiteSpace(context.ComponentsFieldName))
        {
            fields.Add("components");
        }

        return fields;
    }

    public IReadOnlyList<ReleaseIssueItem> MapIssues(
        IReadOnlyList<JiraIssueKeyResponse> issues,
        ReleaseIssueMappingContext context)
    {
        ArgumentNullException.ThrowIfNull(issues);
        ArgumentNullException.ThrowIfNull(context);

        var mapped = issues
            .Where(static issue => !string.IsNullOrWhiteSpace(issue.Key))
            .Select(issue =>
            {
                var releaseDate = TryParseReleaseDate(
                    issue.Fields,
                    context.ReleaseFieldId,
                    context.ReleaseDateFieldName);
                var status = StatusName.FromNullable(issue.Fields?.Status?.Name);
                var tasks = CountAllLinkedTasks(issue.Fields);
                var componentNames = ResolveComponentNames(
                    issue.Fields,
                    context.ComponentsFieldId,
                    context.ComponentsFieldName);
                var environmentNames = ResolveEnvironmentNames(
                    issue.Fields,
                    context.EnvironmentFieldId,
                    context.EnvironmentFieldName);
                var rollbackType = ResolveRollbackPayload(
                    issue.Fields,
                    context.RollbackFieldId,
                    context.RollbackFieldName);
                var isHotFix = IsHotFixRelease(issue.Fields, context.HotFixRules);

                return (
                    key: new IssueKey(issue.Key!.Trim()),
                    title: new IssueSummary(
                        string.IsNullOrWhiteSpace(issue.Fields?.Summary)
                            ? "No summary"
                            : issue.Fields.Summary),
                    releaseDate,
                    status,
                    tasks,
                    componentNames,
                    environmentNames,
                    rollbackType,
                    isHotFix);
            })
            .Where(static item => item.releaseDate.HasValue)
            .Select(item => new ReleaseIssueItem(
                item.key,
                item.title,
                item.releaseDate!.Value,
                item.tasks,
                item.componentNames.Count,
                item.status,
                item.componentNames,
                item.environmentNames,
                item.rollbackType,
                item.isHotFix));

        return [.. mapped
            .DistinctBy(static issue => issue.Key.Value, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static issue => issue.ReleaseDate)
            .ThenBy(static issue => issue.Key.Value, StringComparer.OrdinalIgnoreCase)];
    }

    private static DateOnly? TryParseReleaseDate(
        JiraIssueFieldsResponse? fields,
        string releaseFieldId,
        string releaseDateFieldName)
    {
        if (fields?.AdditionalFields is null || fields.AdditionalFields.Count == 0)
        {
            return null;
        }

        if (!fields.AdditionalFields.TryGetValue(releaseFieldId, out var rawDate)
            && !fields.AdditionalFields.TryGetValue(releaseDateFieldName, out rawDate))
        {
            return null;
        }

        if (rawDate.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        if (rawDate.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var value = rawDate.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }

        if (DateTimeOffset.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal,
            out var timestamp))
        {
            return DateOnly.FromDateTime(timestamp.UtcDateTime);
        }

        return null;
    }

    private IReadOnlyList<string> ResolveComponentNames(
        JiraIssueFieldsResponse? fields,
        string? componentsFieldId,
        string? componentsFieldName)
    {
        if (fields?.AdditionalFields is null || fields.AdditionalFields.Count == 0)
        {
            return [];
        }

        if (_fieldValueReader.TryGetAdditionalFieldValue(
            fields.AdditionalFields,
            componentsFieldId,
            componentsFieldName,
            out var rawComponents))
        {
            var resolvedValues = _fieldValueReader.ParseComponentValues(rawComponents);
            if (resolvedValues.Count > 0)
            {
                return resolvedValues;
            }
        }

        if (fields.AdditionalFields.TryGetValue("components", out var standardComponents))
        {
            return _fieldValueReader.ParseComponentValues(standardComponents);
        }

        return [];
    }

    private IReadOnlyList<string> ResolveEnvironmentNames(
        JiraIssueFieldsResponse? fields,
        string? environmentFieldId,
        string? environmentFieldName)
    {
        if (fields?.AdditionalFields is null || fields.AdditionalFields.Count == 0)
        {
            return [];
        }

        if (!_fieldValueReader.TryGetAdditionalFieldValue(
            fields.AdditionalFields,
            environmentFieldId,
            environmentFieldName,
            out var rawEnvironments))
        {
            return [];
        }

        return _fieldValueReader.ParseRawFieldValues(rawEnvironments);
    }

    private static int CountAllLinkedTasks(JiraIssueFieldsResponse? fields)
    {
        if (fields?.IssueLinks.Count is not > 0)
        {
            return 0;
        }

        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var link in fields.IssueLinks)
        {
            if (link is null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(link.InwardIssue?.Key))
            {
                _ = keys.Add(link.InwardIssue.Key.Trim());
            }

            if (!string.IsNullOrWhiteSpace(link.OutwardIssue?.Key))
            {
                _ = keys.Add(link.OutwardIssue.Key.Trim());
            }
        }

        return keys.Count;
    }

    private bool IsHotFixRelease(
        JiraIssueFieldsResponse? fields,
        IReadOnlyList<ResolvedHotFixRule> hotFixRules)
    {
        if (fields?.AdditionalFields is null || fields.AdditionalFields.Count == 0 || hotFixRules.Count == 0)
        {
            return false;
        }

        foreach (var hotFixRule in hotFixRules)
        {
            if (!_fieldValueReader.TryGetAdditionalFieldValue(
                fields.AdditionalFields,
                hotFixRule.FieldId,
                hotFixRule.FieldName,
                out var rawValue))
            {
                continue;
            }

            if (_fieldValueReader.ParseRawFieldValues(rawValue).Any(hotFixRule.Values.Contains))
            {
                return true;
            }
        }

        return false;
    }

    private string? ResolveRollbackPayload(
        JiraIssueFieldsResponse? fields,
        string? rollbackFieldId,
        string rollbackFieldName)
    {
        if (fields?.AdditionalFields is null || fields.AdditionalFields.Count == 0)
        {
            return null;
        }

        if (!_fieldValueReader.TryGetAdditionalFieldValue(
            fields.AdditionalFields,
            rollbackFieldId,
            rollbackFieldName,
            out var rawRollback))
        {
            return null;
        }

        if (rawRollback.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        var parsedValues = _fieldValueReader.ParseRawFieldValues(rawRollback);
        if (parsedValues.Count > 0)
        {
            return string.Join(", ", parsedValues);
        }

        var rawPayload = rawRollback.ValueKind == JsonValueKind.String
            ? rawRollback.GetString()
            : rawRollback.GetRawText();
        return string.IsNullOrWhiteSpace(rawPayload) ? null : rawPayload.Trim();
    }

    private readonly JiraFieldValueReader _fieldValueReader;
}

public sealed record ReleaseIssueMappingContext(
    string ReleaseFieldId,
    string ReleaseDateFieldName,
    string? ComponentsFieldId,
    string? ComponentsFieldName,
    IReadOnlyList<ResolvedHotFixRule> HotFixRules,
    string? RollbackFieldId,
    string RollbackFieldName,
    string? EnvironmentFieldId,
    string? EnvironmentFieldName);
#pragma warning restore CS1591

