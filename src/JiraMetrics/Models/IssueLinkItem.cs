using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models;

/// <summary>
/// Lightweight issue-link row.
/// </summary>
public sealed record IssueLinkItem(IssueKey Key, string RelationName);
