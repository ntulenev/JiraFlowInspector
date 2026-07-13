using JiraMetrics.Helpers;
using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Transport.Models;

namespace JiraMetrics.API.Mapping;

/// <summary>
/// Maps Jira search issue DTOs into lightweight domain projections.
/// </summary>
public sealed class JiraSearchIssueMapper
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JiraSearchIssueMapper"/> class.
    /// </summary>
    /// <param name="fieldValueReader">Field value reader.</param>
    public JiraSearchIssueMapper(JiraFieldValueReader fieldValueReader)
    {
        ArgumentNullException.ThrowIfNull(fieldValueReader);
        _fieldValueReader = fieldValueReader;
    }

    /// <summary>
    /// Maps search issues into distinct ordered issue keys.
    /// </summary>
    /// <param name="issues">Transport issues.</param>
    /// <returns>Distinct ordered issue keys.</returns>
    internal static IReadOnlyList<IssueKey> ToIssueKeys(IReadOnlyList<JiraIssueKeyResponse> issues) =>
        [.. issues
            .Where(static issue => !string.IsNullOrWhiteSpace(issue.Key))
            .Select(static issue => new IssueKey(issue.Key!.Trim()))
            .DistinctBy(static key => key.Value, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static key => key.Value, StringComparer.OrdinalIgnoreCase)];

    /// <summary>
    /// Maps search issues into lightweight issue list rows.
    /// </summary>
    /// <param name="issues">Transport issues.</param>
    /// <param name="context">Optional issue list mapping context.</param>
    /// <returns>Distinct ordered issue list items.</returns>
    internal IReadOnlyList<IssueListItem> ToIssueListItems(
        IReadOnlyList<JiraIssueKeyResponse> issues,
        IssueListMappingContext? context) =>
        [.. issues
            .Where(static issue => !string.IsNullOrWhiteSpace(issue.Key))
            .Select(issue => new IssueListItem(
                new IssueKey(issue.Key!.Trim()),
                new IssueSummary(
                    string.IsNullOrWhiteSpace(issue.Fields?.Summary)
                        ? "No summary"
                        : issue.Fields.Summary),
                issue.Fields?.Created.ParseNullableDateTimeOffset(),
                IsReporducedOnProd(issue, context),
                issue.Fields?.Priority?.Name,
                MapIssueLinks(issue, context),
                issue.Fields?.IssueType?.Name,
                issue.Fields?.Assignee?.DisplayName,
                issue.Fields?.Status?.Name))
            .DistinctBy(static issue => issue.Key.Value, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static issue => issue.Key.Value, StringComparer.OrdinalIgnoreCase)];

    /// <summary>
    /// Maps search issues into architecture task rows.
    /// </summary>
    /// <param name="issues">Transport issues.</param>
    /// <returns>Distinct ordered architecture task items.</returns>
    internal static IReadOnlyList<ArchTaskItem> ToArchTaskItems(IReadOnlyList<JiraIssueKeyResponse> issues) =>
        [.. issues
            .Where(static issue => !string.IsNullOrWhiteSpace(issue.Key))
            .Select(issue => new
            {
                Key = issue.Key!.Trim(),
                Title = string.IsNullOrWhiteSpace(issue.Fields?.Summary)
                    ? "No summary"
                    : issue.Fields.Summary,
                CreatedAt = issue.Fields?.Created.ParseNullableDateTimeOffset(),
                ResolvedAt = issue.Fields?.ResolutionDate.ParseNullableDateTimeOffset()
            })
            .Where(static issue => issue.CreatedAt.HasValue)
            .Select(issue => new ArchTaskItem(
                new IssueKey(issue.Key),
                new IssueSummary(issue.Title),
                issue.CreatedAt!.Value,
                issue.ResolvedAt))
            .DistinctBy(static issue => issue.Key.Value, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static issue => issue.CreatedAt)
            .ThenBy(static issue => issue.Key.Value, StringComparer.OrdinalIgnoreCase)];

    /// <summary>
    /// Maps search issues into grouped status and issue-type summaries.
    /// </summary>
    /// <param name="issues">Transport issues.</param>
    /// <returns>Ordered status summaries.</returns>
    internal static IReadOnlyList<StatusIssueTypeSummary> ToStatusIssueTypeSummaries(
        IReadOnlyList<JiraIssueKeyResponse> issues)
    {
        var countsByStatus = new Dictionary<string, Dictionary<string, int>>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var issue in issues)
        {
            var statusName = StatusName.FromNullable(issue.Fields?.Status?.Name).Value;
            var issueTypeName = IssueTypeName.FromNullable(issue.Fields?.IssueType?.Name).Value;

            if (!countsByStatus.TryGetValue(statusName, out var issueTypeCounts))
            {
                issueTypeCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                countsByStatus[statusName] = issueTypeCounts;
            }

            issueTypeCounts[issueTypeName] = issueTypeCounts.TryGetValue(issueTypeName, out var count)
                ? count + 1
                : 1;
        }

        return [.. countsByStatus
            .Select(static statusGroup =>
            {
                var issueTypeSummaries = statusGroup.Value
                    .Select(static issueTypeGroup => new IssueTypeCountSummary(
                        IssueTypeName.FromNullable(issueTypeGroup.Key),
                        new ItemCount(issueTypeGroup.Value)))
                    .OrderByDescending(static summary => summary.Count.Value)
                    .ThenBy(static summary => summary.IssueType.Value, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var totalCount = issueTypeSummaries.Sum(static summary => summary.Count.Value);

                return new StatusIssueTypeSummary(
                    StatusName.FromNullable(statusGroup.Key),
                    new ItemCount(totalCount),
                    issueTypeSummaries);
            })
            .OrderByDescending(static summary => summary.Count.Value)
            .ThenBy(static summary => summary.Status.Value, StringComparer.OrdinalIgnoreCase)];
    }

    private bool IsReporducedOnProd(JiraIssueKeyResponse issue, IssueListMappingContext? context)
    {
        if (context?.ReporducedOnProdFieldName is null || issue.Fields?.AdditionalFields is null)
        {
            return false;
        }

        if (!_fieldValueReader.TryGetAdditionalFieldValue(
            issue.Fields.AdditionalFields,
            context.ReporducedOnProdFieldId?.Value,
            context.ReporducedOnProdFieldName.Value.Value,
            out var rawValue))
        {
            return false;
        }

        if (rawValue.ValueKind is System.Text.Json.JsonValueKind.True)
        {
            return true;
        }

        if (rawValue.ValueKind is System.Text.Json.JsonValueKind.False
            or System.Text.Json.JsonValueKind.Null
            or System.Text.Json.JsonValueKind.Undefined)
        {
            return false;
        }

        var values = _fieldValueReader.ParseRawFieldValues(rawValue);
        return values.Any(static value =>
            string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "prod", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "production", StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<IssueLinkItem> MapIssueLinks(
        JiraIssueKeyResponse issue,
        IssueListMappingContext? context)
    {
        if (context?.IncludeIssueLinks != true || issue.Fields?.IssueLinks is not { Count: > 0 } issueLinks)
        {
            return [];
        }

        return [.. issueLinks
            .SelectMany(static link => new[]
            {
                CreateIssueLinkItem(link.InwardIssue?.Key, link.Type?.Inward),
                CreateIssueLinkItem(link.OutwardIssue?.Key, link.Type?.Outward)
            })
            .Where(static item => item is not null)
            .Select(static item => item!)
            .DistinctBy(static item => item.Key.Value + "|" + item.RelationName, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static item => item.Key.Value, StringComparer.OrdinalIgnoreCase)];
    }

    private static IssueLinkItem? CreateIssueLinkItem(string? key, string? relationName) =>
        string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(relationName)
            ? null
            : new IssueLinkItem(new IssueKey(key), relationName.Trim());

    private readonly JiraFieldValueReader _fieldValueReader;
}
