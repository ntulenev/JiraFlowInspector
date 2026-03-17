using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models;

/// <summary>
/// Represents one incident row in the global incidents report.
/// </summary>
public sealed record GlobalIncidentItem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalIncidentItem"/> class.
    /// </summary>
    /// <param name="key">Issue key.</param>
    /// <param name="title">Issue title.</param>
    /// <param name="incidentStartUtc">Incident start timestamp in UTC.</param>
    /// <param name="incidentRecoveryUtc">Incident recovery timestamp in UTC.</param>
    /// <param name="impact">Impact value.</param>
    /// <param name="urgency">Urgency value.</param>
    /// <param name="additionalFields">Optional additional field values keyed by configured field name.</param>
    public GlobalIncidentItem(
        IssueKey key,
        IssueSummary title,
        DateTimeOffset? incidentStartUtc,
        DateTimeOffset? incidentRecoveryUtc,
        string? impact = null,
        string? urgency = null,
        IReadOnlyDictionary<string, string?>? additionalFields = null)
    {
        Key = key;
        Title = title;
        IncidentStartUtc = incidentStartUtc;
        IncidentRecoveryUtc = incidentRecoveryUtc;
        Impact = string.IsNullOrWhiteSpace(impact) ? null : impact.Trim();
        Urgency = string.IsNullOrWhiteSpace(urgency) ? null : urgency.Trim();
        Duration = incidentStartUtc.HasValue
            && incidentRecoveryUtc.HasValue
            && incidentRecoveryUtc.Value >= incidentStartUtc.Value
                ? incidentRecoveryUtc.Value - incidentStartUtc.Value
                : null;
        AdditionalFields = additionalFields is null
            ? []
            : new Dictionary<string, string>(
                additionalFields
                    .Where(static pair => !string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
                    .OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        static pair => pair.Key.Trim(),
                        static pair => pair.Value!.Trim(),
                        StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets issue key.
    /// </summary>
    public IssueKey Key { get; }

    /// <summary>
    /// Gets issue title.
    /// </summary>
    public IssueSummary Title { get; }

    /// <summary>
    /// Gets incident start timestamp in UTC.
    /// </summary>
    public DateTimeOffset? IncidentStartUtc { get; }

    /// <summary>
    /// Gets incident recovery timestamp in UTC.
    /// </summary>
    public DateTimeOffset? IncidentRecoveryUtc { get; }

    /// <summary>
    /// Gets incident duration when both start and recovery timestamps are available.
    /// </summary>
    public TimeSpan? Duration { get; }

    /// <summary>
    /// Gets impact value.
    /// </summary>
    public string? Impact { get; }

    /// <summary>
    /// Gets urgency value.
    /// </summary>
    public string? Urgency { get; }

    /// <summary>
    /// Gets additional configured field values keyed by configured field name.
    /// </summary>
    public IReadOnlyDictionary<string, string> AdditionalFields { get; }
}
