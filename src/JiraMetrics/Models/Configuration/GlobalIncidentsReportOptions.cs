namespace JiraMetrics.Models.Configuration;

/// <summary>
/// Global incidents report options.
/// </summary>
public sealed class GlobalIncidentsReportOptions
{
    /// <summary>
    /// Gets or sets Jira namespace or project used for incidents search.
    /// </summary>
    public string Namespace { get; init; } = "Incidents";

    /// <summary>
    /// Gets or sets optional raw JQL clause used to filter incidents.
    /// </summary>
    public string? JqlFilter { get; init; }

    /// <summary>
    /// Gets or sets optional free-text phrase used to filter incidents.
    /// </summary>
    public string? SearchPhrase { get; init; }

    /// <summary>
    /// Gets or sets incident start field name.
    /// </summary>
    public string IncidentStartFieldName { get; init; } = "Incident Start date/time UTC";

    /// <summary>
    /// Gets or sets fallback incident start field name used when the primary field is empty.
    /// </summary>
    public string? IncidentStartFallbackFieldName { get; init; }

    /// <summary>
    /// Gets or sets incident recovery field name.
    /// </summary>
    public string IncidentRecoveryFieldName { get; init; } = "Incident Recovery date/time UTC";

    /// <summary>
    /// Gets or sets fallback incident recovery field name used when the primary field is empty.
    /// </summary>
    public string? IncidentRecoveryFallbackFieldName { get; init; }

    /// <summary>
    /// Gets or sets impact field name.
    /// </summary>
    public string ImpactFieldName { get; init; } = "Impact";

    /// <summary>
    /// Gets or sets urgency field name.
    /// </summary>
    public string UrgencyFieldName { get; init; } = "Urgency";

    /// <summary>
    /// Gets or sets optional additional field names shown in the report.
    /// </summary>
    public IReadOnlyList<string>? AdditionalFieldNames { get; init; }
}
