using JiraMetrics.Abstractions;
using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Transport.Models;

using Microsoft.Extensions.Options;

#pragma warning disable CS1591
namespace JiraMetrics.API.Mapping;

/// <summary>
/// Maps Jira issue responses into issue timelines.
/// </summary>
public sealed class IssueTimelineMapper : IIssueTimelineMapper
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IssueTimelineMapper"/> class.
    /// </summary>
    /// <param name="transitionBuilder">Transition builder.</param>
    /// <param name="settings">Application settings.</param>
    /// <param name="fieldValueReader">Field value reader.</param>
    public IssueTimelineMapper(
        ITransitionBuilder transitionBuilder,
        IOptions<AppSettings> settings,
        JiraFieldValueReader fieldValueReader)
    {
        _transitionBuilder = transitionBuilder ?? throw new ArgumentNullException(nameof(transitionBuilder));
        ArgumentNullException.ThrowIfNull(settings);
        _fieldValueReader = fieldValueReader ?? throw new ArgumentNullException(nameof(fieldValueReader));
        var resolved = settings.Value;
        _pullRequestFieldName = resolved.PullRequestFieldName ?? string.Empty;
    }

    public IssueTimeline Map(JiraIssueResponse response, IssueKey fallbackKey)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (response.Fields is null)
        {
            throw new InvalidOperationException("Response missing fields.");
        }

        if (string.IsNullOrWhiteSpace(response.Fields.Created)
            || !DateTimeOffset.TryParse(response.Fields.Created, out var created))
        {
            throw new InvalidOperationException("Issue created date is missing.");
        }

        var transitions = ParseTransitions(response.Changelog?.Histories ?? [], created);

        var endTime = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(response.Fields.ResolutionDate)
            && DateTimeOffset.TryParse(response.Fields.ResolutionDate, out var parsedResolutionDate))
        {
            endTime = parsedResolutionDate;
        }

        if (endTime < created)
        {
            endTime = created;
        }

        return new IssueTimeline(
            !string.IsNullOrWhiteSpace(response.Key) ? new IssueKey(response.Key.Trim()) : fallbackKey,
            IssueTypeName.FromNullable(response.Fields.IssueType?.Name),
            new IssueSummary(
                string.IsNullOrWhiteSpace(response.Fields.Summary)
                    ? "No summary"
                    : response.Fields.Summary),
            created,
            endTime,
            transitions,
            PathKey.FromTransitions(transitions),
            PathLabel.FromTransitions(transitions),
            response.Fields.Subtasks.Count,
            HasPullRequest(response.Fields));
    }

    private IReadOnlyList<TransitionEvent> ParseTransitions(
        IReadOnlyList<JiraHistoryResponse> histories,
        DateTimeOffset created)
    {
        var rawTransitions = new List<(DateTimeOffset At, StatusName From, StatusName To)>();

        foreach (var history in histories)
        {
            if (string.IsNullOrWhiteSpace(history.Created)
                || !DateTimeOffset.TryParse(history.Created, out var at))
            {
                continue;
            }

            foreach (var item in history.Items.Where(static item =>
                         string.Equals(item.Field, "status", StringComparison.OrdinalIgnoreCase)))
            {
                rawTransitions.Add((
                    at,
                    StatusName.FromNullable(item.FromStatus),
                    StatusName.FromNullable(item.ToStatus)));
            }
        }

        return _transitionBuilder.BuildTransitions(rawTransitions, created);
    }

    private bool HasPullRequest(JiraIssueFieldsResponse fields)
    {
        ArgumentNullException.ThrowIfNull(fields);

        if (fields.AdditionalFields is null || fields.AdditionalFields.Count == 0)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(_pullRequestFieldName)
            && fields.AdditionalFields.TryGetValue(_pullRequestFieldName, out var configuredPullRequestField)
            && _fieldValueReader.HasPullRequestInRawValue(configuredPullRequestField))
        {
            return true;
        }

        foreach (var rawValue in fields.AdditionalFields.Values)
        {
            if (_fieldValueReader.HasPullRequestInRawValue(rawValue))
            {
                return true;
            }
        }

        return false;
    }

    private readonly ITransitionBuilder _transitionBuilder;
    private readonly JiraFieldValueReader _fieldValueReader;
    private readonly string _pullRequestFieldName;
}
#pragma warning restore CS1591
