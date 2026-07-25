using JiraMetrics.Helpers;
using JiraMetrics.Logic;
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
    /// <param name="runContext">Context shared by the current report run.</param>
    public IssueTimelineMapper(
        TransitionBuilder transitionBuilder,
        IOptions<AppSettings> settings,
        ReportRunContext runContext)
    {
        _transitionBuilder = transitionBuilder ?? throw new ArgumentNullException(nameof(transitionBuilder));
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(runContext);
        var resolved = settings.Value;
        _pullRequestFieldName = resolved.PullRequestFieldName ?? string.Empty;
        _reportGeneratedAt = runContext.GeneratedAt;
    }

    public IssueTimeline Map(JiraIssueResponse response, IssueKey fallbackKey)
    {
        ArgumentNullException.ThrowIfNull(response);

        if (response.Fields is null)
        {
            throw new InvalidOperationException("Response missing fields.");
        }

        if (response.Fields.Created.ParseNullableDateTimeOffset() is not { } created)
        {
            throw new InvalidOperationException("Issue created date is missing.");
        }

        var transitions = ParseTransitions(response.Changelog?.Histories ?? [], created);

        var endTime = response.Fields.ResolutionDate.ParseNullableDateTimeOffset()
            ?? _reportGeneratedAt;

        return IssueTimeline.Create(
            !string.IsNullOrWhiteSpace(response.Key) ? new IssueKey(response.Key.Trim()) : fallbackKey,
            IssueTypeName.FromNullable(response.Fields.IssueType?.Name),
            new IssueSummary(
                string.IsNullOrWhiteSpace(response.Fields.Summary)
                    ? "No summary"
                    : response.Fields.Summary),
            created,
            transitions,
            endTime,
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
            if (history.Created.ParseNullableDateTimeOffset() is not { } at)
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
            && PullRequestDetector.HasPullRequest(configuredPullRequestField))
        {
            return true;
        }

        foreach (var rawValue in fields.AdditionalFields.Values)
        {
            if (PullRequestDetector.HasPullRequest(rawValue))
            {
                return true;
            }
        }

        return false;
    }

    private readonly TransitionBuilder _transitionBuilder;
    private readonly string _pullRequestFieldName;
    private readonly DateTimeOffset _reportGeneratedAt;
}
#pragma warning restore CS1591

