using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Logic;

/// <summary>
/// Loads created/open/finished issue ratios for a configured issue-type filter.
/// </summary>
internal sealed class JiraIssueRatioLoader
{
    public static IssueRatioSnapshot Build(IssueSearchSnapshot searchSnapshot)
    {
        ArgumentNullException.ThrowIfNull(searchSnapshot);

        var doneKeys = searchSnapshot.DoneIssues
            .Select(static issue => issue.Key.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rejectedKeys = searchSnapshot.RejectedIssues
            .Select(static issue => issue.Key.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var finishedKeys = doneKeys
            .Union(rejectedKeys, StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var openIssues = (IReadOnlyList<IssueListItem>)[.. searchSnapshot.CreatedIssues
            .Where(issue => !finishedKeys.Contains(issue.Key.Value))
            .OrderBy(static issue => issue.Key.Value, StringComparer.OrdinalIgnoreCase)];

        return new IssueRatioSnapshot(
            new ItemCount(searchSnapshot.CreatedIssues.Count),
            new ItemCount(openIssues.Count),
            new ItemCount(searchSnapshot.DoneIssues.Count),
            new ItemCount(searchSnapshot.RejectedIssues.Count),
            new ItemCount(finishedKeys.Count),
            openIssues,
            searchSnapshot.DoneIssues,
            searchSnapshot.RejectedIssues);
    }
}
