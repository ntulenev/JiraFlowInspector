using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models;

/// <summary>
/// Current roadmap data for a Jira issue.
/// </summary>
public sealed record RoadmapItem(
    IssueKey Key,
    IssueSummary Summary,
    string Status,
    string? Roadmap,
    DateOnly? StartDate,
    DateOnly? EndDate);
