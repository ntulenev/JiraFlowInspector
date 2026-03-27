namespace JiraMetrics.Models.Configuration;

/// <summary>
/// Represents validated global incidents report settings.
/// </summary>
public sealed record GlobalIncidentsReportSettings
{

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalIncidentsReportSettings"/> class.
    /// </summary>
    /// <param name="namespaceName">Jira namespace or project used for incidents search.</param>
    /// <param name="jqlFilter">Optional raw JQL clause used to filter incidents.</param>
    /// <param name="searchPhrase">Optional free-text phrase used to filter incidents.</param>
    /// <param name="incidentStartFieldName">Incident start field name.</param>
    /// <param name="incidentStartFallbackFieldName">Fallback incident start field name used when the primary field is empty.</param>
    /// <param name="incidentRecoveryFieldName">Incident recovery field name.</param>
    /// <param name="incidentRecoveryFallbackFieldName">Fallback incident recovery field name used when the primary field is empty.</param>
    /// <param name="impactFieldName">Impact field name.</param>
    /// <param name="urgencyFieldName">Urgency field name.</param>
    /// <param name="additionalFieldNames">Optional additional field names shown in the report.</param>
    public GlobalIncidentsReportSettings(
        string? namespaceName = null,
        string? jqlFilter = null,
        string? searchPhrase = null,
        string? incidentStartFieldName = null,
        string? incidentStartFallbackFieldName = null,
        string? incidentRecoveryFieldName = null,
        string? incidentRecoveryFallbackFieldName = null,
        string? impactFieldName = null,
        string? urgencyFieldName = null,
        IReadOnlyList<string>? additionalFieldNames = null)
    {
        Namespace = string.IsNullOrWhiteSpace(namespaceName) ? DEFAULT_NAMESPACE : namespaceName.Trim();
        JqlFilter = string.IsNullOrWhiteSpace(jqlFilter) ? null : jqlFilter.Trim();
        SearchPhrase = string.IsNullOrWhiteSpace(searchPhrase) ? null : searchPhrase.Trim();
        IncidentStartFieldName = string.IsNullOrWhiteSpace(incidentStartFieldName)
            ? DEFAULT_INCIDENT_START_FIELD_NAME
            : incidentStartFieldName.Trim();
        IncidentStartFallbackFieldName = string.IsNullOrWhiteSpace(incidentStartFallbackFieldName)
            ? null
            : incidentStartFallbackFieldName.Trim();
        IncidentRecoveryFieldName = string.IsNullOrWhiteSpace(incidentRecoveryFieldName)
            ? DEFAULT_INCIDENT_RECOVERY_FIELD_NAME
            : incidentRecoveryFieldName.Trim();
        IncidentRecoveryFallbackFieldName = string.IsNullOrWhiteSpace(incidentRecoveryFallbackFieldName)
            ? null
            : incidentRecoveryFallbackFieldName.Trim();
        ImpactFieldName = string.IsNullOrWhiteSpace(impactFieldName)
            ? DEFAULT_IMPACT_FIELD_NAME
            : impactFieldName.Trim();
        UrgencyFieldName = string.IsNullOrWhiteSpace(urgencyFieldName)
            ? DEFAULT_URGENCY_FIELD_NAME
            : urgencyFieldName.Trim();
        AdditionalFieldNames = additionalFieldNames is null
            ? []
            : [.. additionalFieldNames
                .Where(static value => !string.IsNullOrWhiteSpace(value))
                .Select(static value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// Gets Jira namespace or project used for incidents search.
    /// </summary>
    public string Namespace { get; }

    /// <summary>
    /// Gets optional raw JQL clause used to filter incidents.
    /// </summary>
    public string? JqlFilter { get; }

    /// <summary>
    /// Gets optional free-text phrase used to filter incidents.
    /// </summary>
    public string? SearchPhrase { get; }

    /// <summary>
    /// Gets incident start field name.
    /// </summary>
    public string IncidentStartFieldName { get; }

    /// <summary>
    /// Gets fallback incident start field name.
    /// </summary>
    public string? IncidentStartFallbackFieldName { get; }

    /// <summary>
    /// Gets incident recovery field name.
    /// </summary>
    public string IncidentRecoveryFieldName { get; }

    /// <summary>
    /// Gets fallback incident recovery field name.
    /// </summary>
    public string? IncidentRecoveryFallbackFieldName { get; }

    /// <summary>
    /// Gets impact field name.
    /// </summary>
    public string ImpactFieldName { get; }

    /// <summary>
    /// Gets urgency field name.
    /// </summary>
    public string UrgencyFieldName { get; }

    /// <summary>
    /// Gets optional additional field names shown in the report.
    /// </summary>
    public IReadOnlyList<string> AdditionalFieldNames { get; }
    private const string DEFAULT_NAMESPACE = "Incidents";
    private const string DEFAULT_INCIDENT_START_FIELD_NAME = "Incident Start date/time UTC";
    private const string DEFAULT_INCIDENT_RECOVERY_FIELD_NAME = "Incident Recovery date/time UTC";
    private const string DEFAULT_IMPACT_FIELD_NAME = "Impact";
    private const string DEFAULT_URGENCY_FIELD_NAME = "Urgency";
}
