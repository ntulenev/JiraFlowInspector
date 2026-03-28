using JiraMetrics.Models;

namespace JiraMetrics.Logic;

/// <summary>
/// Loads created/open/finished issue ratios for a configured issue-type filter.
/// </summary>
internal sealed class JiraIssueRatioLoader
{
    public static IssueRatioSnapshot Build(IssueSearchSnapshot searchSnapshot)
    {
        ArgumentNullException.ThrowIfNull(searchSnapshot);
        return searchSnapshot.BuildRatioSnapshot();
    }
}
