using JiraMetrics.API.Mapping;
using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.API;

internal sealed class JiraReportDataClient : IJiraReportDataClient
{

    public JiraReportDataClient(
        IJiraSearchExecutor searchExecutor,
        IJiraJqlFacade jqlFacade,
        IJiraFieldResolver fieldResolver,
        IJiraMapperFacade mapperFacade)
    {
        ArgumentNullException.ThrowIfNull(searchExecutor);
        ArgumentNullException.ThrowIfNull(jqlFacade);
        ArgumentNullException.ThrowIfNull(fieldResolver);
        ArgumentNullException.ThrowIfNull(mapperFacade);

        _searchExecutor = searchExecutor;
        _jqlFacade = jqlFacade;
        _fieldResolver = fieldResolver;
        _mapperFacade = mapperFacade;
    }

    public async Task<IReadOnlyList<ReleaseIssueItem>> GetReleaseIssuesForMonthAsync(
        ProjectKey releaseProjectKey,
        string projectLabel,
        string releaseDateFieldName,
        string? componentsFieldName,
        IReadOnlyDictionary<string, IReadOnlyList<string>> hotFixRules,
        string rollbackFieldName,
        string? environmentFieldName,
        string? environmentFieldValue,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectLabel);
        ArgumentException.ThrowIfNullOrWhiteSpace(releaseDateFieldName);
        ArgumentNullException.ThrowIfNull(hotFixRules);
        ArgumentException.ThrowIfNullOrWhiteSpace(rollbackFieldName);

        var releaseFieldId = await _fieldResolver
            .ResolveFieldIdAsync(releaseDateFieldName, cancellationToken)
            .ConfigureAwait(false);
        var componentsFieldId = await _fieldResolver
            .TryResolveFieldIdAsync(componentsFieldName, cancellationToken)
            .ConfigureAwait(false);
        var resolvedHotFixRules = await _fieldResolver
            .ResolveHotFixRulesAsync(hotFixRules, cancellationToken)
            .ConfigureAwait(false);
        var rollbackFieldId = await _fieldResolver
            .TryResolveFieldIdAsync(rollbackFieldName, cancellationToken)
            .ConfigureAwait(false);
        var environmentFieldId = await _fieldResolver
            .TryResolveFieldIdAsync(environmentFieldName, cancellationToken)
            .ConfigureAwait(false);

        var jql = _jqlFacade.BuildReleaseIssuesQuery(
            releaseProjectKey,
            projectLabel,
            releaseDateFieldName,
            environmentFieldName,
            environmentFieldValue);
        var context = new ReleaseIssueMappingContext(
            releaseFieldId,
            releaseDateFieldName,
            componentsFieldId,
            componentsFieldName,
            resolvedHotFixRules,
            rollbackFieldId,
            rollbackFieldName,
            environmentFieldId,
            environmentFieldName);
        var issues = await _searchExecutor
            .SearchIssuesAsync(
                jql,
                _mapperFacade.BuildReleaseRequestedFields(context),
                cancellationToken)
            .ConfigureAwait(false);

        return _mapperFacade.MapReleaseIssues(issues, context);
    }

    public async Task<IReadOnlyList<ArchTaskItem>> GetArchTasksAsync(
        ArchTasksReportSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var jql = _jqlFacade.BuildArchTasksQuery(settings);
        var issues = await _searchExecutor
            .SearchIssuesAsync(jql, ["key", "summary", "created", "resolutiondate"], cancellationToken)
            .ConfigureAwait(false);

        return _mapperFacade.MapArchTaskItems(issues);
    }

    public async Task<IReadOnlyList<GlobalIncidentItem>> GetGlobalIncidentsForMonthAsync(
        GlobalIncidentsReportSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var startFields = await _fieldResolver.ResolveDateFieldsAsync(
            settings.IncidentStartFieldName,
            settings.IncidentStartFallbackFieldName,
            cancellationToken).ConfigureAwait(false);
        var recoveryFields = await _fieldResolver.ResolveDateFieldsAsync(
            settings.IncidentRecoveryFieldName,
            settings.IncidentRecoveryFallbackFieldName,
            cancellationToken).ConfigureAwait(false);
        var impactFieldId = await _fieldResolver
            .TryResolveFieldIdAsync(settings.ImpactFieldName, cancellationToken)
            .ConfigureAwait(false);
        var urgencyFieldId = await _fieldResolver
            .TryResolveFieldIdAsync(settings.UrgencyFieldName, cancellationToken)
            .ConfigureAwait(false);

        var additionalFieldIds = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var additionalFieldName in settings.AdditionalFieldNames)
        {
            additionalFieldIds[additionalFieldName] = await _fieldResolver
                .TryResolveFieldIdAsync(additionalFieldName, cancellationToken)
                .ConfigureAwait(false);
        }

        var jql = _jqlFacade.BuildGlobalIncidentsQuery(settings, startFields);
        var context = new GlobalIncidentMappingContext(
            startFields,
            recoveryFields,
            impactFieldId,
            settings.ImpactFieldName,
            urgencyFieldId,
            settings.UrgencyFieldName,
            additionalFieldIds);
        var issues = await _searchExecutor
            .SearchIssuesAsync(
                jql,
                _mapperFacade.BuildGlobalIncidentRequestedFields(context),
                cancellationToken)
            .ConfigureAwait(false);

        return _mapperFacade.MapGlobalIncidents(issues, context);
    }
    private readonly IJiraSearchExecutor _searchExecutor;
    private readonly IJiraJqlFacade _jqlFacade;
    private readonly IJiraFieldResolver _fieldResolver;
    private readonly IJiraMapperFacade _mapperFacade;
}

