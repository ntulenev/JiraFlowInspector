using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace JiraMetrics.API.Mapping;

/// <summary>
/// Detects pull request activity in Jira development field payloads.
/// </summary>
public static partial class PullRequestDetector
{
    /// <summary>
    /// Determines whether a raw Jira field contains pull request activity.
    /// </summary>
    /// <param name="rawValue">Raw Jira field value.</param>
    /// <returns><see langword="true" /> when pull request activity is present.</returns>
    public static bool HasPullRequest(JsonElement rawValue)
    {
        if (rawValue.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return false;
        }

        var rawText = rawValue.ValueKind == JsonValueKind.String
            ? rawValue.GetString()
            : rawValue.GetRawText();
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return false;
        }

        if (rawText.IndexOf("pullrequest", StringComparison.OrdinalIgnoreCase) < 0)
        {
            return false;
        }

        var matches = PullRequestCountPattern().Matches(rawText);
        if (matches.Count == 0)
        {
            return true;
        }

        foreach (Match match in matches)
        {
            if (!int.TryParse(
                match.Groups[1].Value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var count))
            {
                continue;
            }

            if (count > 0)
            {
                return true;
            }
        }

        return false;
    }

    [GeneratedRegex(@"(?:stateCount|count)\s*""?\s*[:=]\s*(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex PullRequestCountPattern();
}
