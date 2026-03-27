using System.Text.Json.Serialization;

using JiraMetrics.Helpers;
using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Transport.Models;

/// <summary>
/// Jira issue key DTO.
/// </summary>
public sealed class JiraIssueKeyResponse
{
    /// <summary>
    /// Gets issue key.
    /// </summary>
    [JsonPropertyName("key")]
    public string? Key { get; init; }

    /// <summary>
    /// Gets issue fields included in search response.
    /// </summary>
    [JsonPropertyName("fields")]
    public JiraIssueFieldsResponse? Fields { get; init; }

    internal static IReadOnlyList<IssueKey> ToIssueKeys(IReadOnlyList<JiraIssueKeyResponse> issues) =>
        [.. issues
            .Where(static issue => !string.IsNullOrWhiteSpace(issue.Key))
            .Select(static issue => new IssueKey(issue.Key!.Trim()))
            .DistinctBy(static key => key.Value, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static key => key.Value, StringComparer.OrdinalIgnoreCase)];

    internal static IReadOnlyList<IssueListItem> ToIssueListItems(IReadOnlyList<JiraIssueKeyResponse> issues) =>
        [.. issues
            .Where(static issue => !string.IsNullOrWhiteSpace(issue.Key))
            .Select(issue => new IssueListItem(
                new IssueKey(issue.Key!.Trim()),
                new IssueSummary(string.IsNullOrWhiteSpace(issue.Fields?.Summary) ? "No summary" : issue.Fields.Summary),
                issue.Fields?.Created.ParseNullableDateTimeOffset()))
            .DistinctBy(static issue => issue.Key.Value, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static issue => issue.Key.Value, StringComparer.OrdinalIgnoreCase)];

    internal static IReadOnlyList<ArchTaskItem> ToArchTaskItems(IReadOnlyList<JiraIssueKeyResponse> issues) =>
        [.. issues
            .Where(static issue => !string.IsNullOrWhiteSpace(issue.Key))
            .Select(issue => new
            {
                Key = issue.Key!.Trim(),
                Title = string.IsNullOrWhiteSpace(issue.Fields?.Summary) ? "No summary" : issue.Fields.Summary,
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

    internal static IReadOnlyList<StatusIssueTypeSummary> ToStatusIssueTypeSummaries(
        IReadOnlyList<JiraIssueKeyResponse> issues)
    {
        var countsByStatus = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);

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
}
