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
        ReleaseIssueReadRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var releaseFieldId = await _fieldResolver
            .ResolveFieldIdAsync(request.ReleaseDateFieldName, cancellationToken)
            .ConfigureAwait(false);
        var componentsFieldId = await _fieldResolver
            .TryResolveFieldIdAsync(request.ComponentsFieldName, cancellationToken)
            .ConfigureAwait(false);
        var resolvedHotFixRules = await _fieldResolver
            .ResolveHotFixRulesAsync(request.HotFixRules, cancellationToken)
            .ConfigureAwait(false);
        var rollbackFieldId = await _fieldResolver
            .TryResolveFieldIdAsync(request.RollbackFieldName, cancellationToken)
            .ConfigureAwait(false);
        var environmentFieldId = await _fieldResolver
            .TryResolveFieldIdAsync(request.EnvironmentFilter?.FieldName, cancellationToken)
            .ConfigureAwait(false);

        var jql = _jqlFacade.BuildReleaseIssuesQuery(request);
        var context = new ReleaseIssueMappingContext(
            releaseFieldId,
            request.ReleaseDateFieldName,
            componentsFieldId,
            request.ComponentsFieldName,
            resolvedHotFixRules,
            rollbackFieldId,
            request.RollbackFieldName,
            environmentFieldId,
            request.EnvironmentFilter?.FieldName);
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
            .SearchIssuesAsync(
                jql,
                JiraSearchFields.From("key", "summary", "created", "resolutiondate"),
                cancellationToken)
            .ConfigureAwait(false);

        return _mapperFacade.MapArchTaskItems(issues);
    }

    public async Task<IReadOnlyList<GlobalIncidentItem>> GetGlobalIncidentsForMonthAsync(
        GlobalIncidentsReportSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var startFields = await _fieldResolver.ResolveDateFieldsAsync(
            new JiraFieldName(settings.IncidentStartFieldName),
            JiraFieldName.FromNullable(settings.IncidentStartFallbackFieldName),
            cancellationToken).ConfigureAwait(false);
        var recoveryFields = await _fieldResolver.ResolveDateFieldsAsync(
            new JiraFieldName(settings.IncidentRecoveryFieldName),
            JiraFieldName.FromNullable(settings.IncidentRecoveryFallbackFieldName),
            cancellationToken).ConfigureAwait(false);
        var impactFieldId = await _fieldResolver
            .TryResolveFieldIdAsync(new JiraFieldName(settings.ImpactFieldName), cancellationToken)
            .ConfigureAwait(false);
        var urgencyFieldId = await _fieldResolver
            .TryResolveFieldIdAsync(new JiraFieldName(settings.UrgencyFieldName), cancellationToken)
            .ConfigureAwait(false);

        var additionalFieldIds = new Dictionary<JiraFieldName, JiraFieldId?>();
        foreach (var additionalFieldName in settings.AdditionalFieldNames)
        {
            var fieldName = new JiraFieldName(additionalFieldName);
            additionalFieldIds[fieldName] = await _fieldResolver
                .TryResolveFieldIdAsync(fieldName, cancellationToken)
                .ConfigureAwait(false);
        }

        var jql = _jqlFacade.BuildGlobalIncidentsQuery(settings, startFields);
        var context = new GlobalIncidentMappingContext(
            startFields,
            recoveryFields,
            impactFieldId,
            new JiraFieldName(settings.ImpactFieldName),
            urgencyFieldId,
            new JiraFieldName(settings.UrgencyFieldName),
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

