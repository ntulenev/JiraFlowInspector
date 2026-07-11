using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models;

/// <summary>
/// Lightweight issue-link row.
/// </summary>
/// <param name="Key">Linked issue key.</param>
/// <param name="RelationName">Jira link relation name.</param>
public sealed record IssueLinkItem(IssueKey Key, string RelationName);
