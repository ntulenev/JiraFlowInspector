using System.Globalization;
using System.Text.Json;

using JiraMetrics.API.Mapping;
using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Transport.Models;

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

    public async Task<IReadOnlyList<IssueListItem>> GetUnresolved30DaysTasksAsync(
        Unresolved30DaysTasksReportSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var issues = await _searchExecutor
            .SearchIssuesAsync(
                new JqlQuery(settings.Jql),
                JiraSearchFields.From("key", "summary", "created", "issuetype", "assignee", "status"),
                cancellationToken)
            .ConfigureAwait(false);

        return _mapperFacade.MapIssueListItems(issues);
    }

    public async Task<IReadOnlyList<RoadmapItem>> GetRoadmapItemsAsync(
        RoadmapReportSettings settings,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var roadmapFieldId = await _fieldResolver
            .ResolveFieldIdAsync(new JiraFieldName(settings.RoadmapFieldName), cancellationToken)
            .ConfigureAwait(false);
        var startDateField = await ResolveRoadmapDateFieldAsync(
            settings.StartDateFieldName,
            cancellationToken).ConfigureAwait(false);
        var endDateField = await ResolveRoadmapDateFieldAsync(
            settings.EndDateFieldName,
            cancellationToken).ConfigureAwait(false);
        var issues = await _searchExecutor
            .SearchIssuesAsync(
                new JqlQuery(settings.Jql),
                JiraSearchFields.From(
                    "key",
                    "summary",
                    "status",
                    roadmapFieldId.Value,
                    startDateField.FieldId.Value,
                    endDateField.FieldId.Value),
                cancellationToken)
            .ConfigureAwait(false);

        return [.. issues.Select(issue => MapRoadmapItem(
            issue,
            roadmapFieldId,
            startDateField,
            endDateField))];
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

    private static RoadmapItem MapRoadmapItem(
        JiraIssueKeyResponse issue,
        JiraFieldId roadmapFieldId,
        RoadmapDateField startDateField,
        RoadmapDateField endDateField)
    {
        var key = new IssueKey(issue.Key ?? throw new InvalidOperationException("Roadmap issue key is missing."));
        var fields = issue.Fields ?? throw new InvalidOperationException($"Roadmap issue '{key.Value}' fields are missing.");
        var summary = new IssueSummary(fields.Summary ?? key.Value);
        var status = string.IsNullOrWhiteSpace(fields.Status?.Name) ? "-" : fields.Status.Name.Trim();
        var additionalFields = fields.AdditionalFields;

        return new RoadmapItem(
            key,
            summary,
            status,
            ReadDropdown(additionalFields, roadmapFieldId.Value),
            ReadDate(additionalFields, startDateField),
            ReadDate(additionalFields, endDateField));
    }

    private async Task<RoadmapDateField> ResolveRoadmapDateFieldAsync(
        string configuredField,
        CancellationToken cancellationToken)
    {
        if (TryParseIntervalFieldReference(configuredField, out var intervalField))
        {
            return intervalField;
        }

        var fieldId = await _fieldResolver
            .ResolveFieldIdAsync(new JiraFieldName(configuredField), cancellationToken)
            .ConfigureAwait(false);
        return new RoadmapDateField(fieldId, null);
    }

    private static bool TryParseIntervalFieldReference(
        string configuredField,
        out RoadmapDateField field)
    {
        var value = configuredField.Trim();
        var fieldIdEnd = value.IndexOf("][", StringComparison.Ordinal);
        if (!value.StartsWith("cf[", StringComparison.OrdinalIgnoreCase)
            || fieldIdEnd < 4
            || !value.EndsWith(']'))
        {
            field = default;
            return false;
        }

        var numericId = value[3..fieldIdEnd];
        var component = value[(fieldIdEnd + 2)..^1];
        if (!numericId.All(char.IsDigit))
        {
            field = default;
            return false;
        }

        var jsonPropertyName = component.ToUpperInvariant() switch
        {
            "STARTDATE" => "start",
            "ENDDATE" => "end",
            _ => null
        };
        if (jsonPropertyName is null)
        {
            field = default;
            return false;
        }

        field = new RoadmapDateField(new JiraFieldId($"customfield_{numericId}"), jsonPropertyName);
        return true;
    }

    private static string? ReadDropdown(Dictionary<string, JsonElement>? fields, string fieldId)
    {
        if (fields is null || !fields.TryGetValue(fieldId, out var value))
        {
            return null;
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            return Normalize(value.GetString());
        }

        if (value.ValueKind == JsonValueKind.Object)
        {
            if (value.TryGetProperty("value", out var optionValue))
            {
                return Normalize(optionValue.GetString());
            }

            if (value.TryGetProperty("name", out var optionName))
            {
                return Normalize(optionName.GetString());
            }
        }

        return null;
    }

    private static DateOnly? ReadDate(
        Dictionary<string, JsonElement>? fields,
        RoadmapDateField field)
    {
        if (fields is null
            || !fields.TryGetValue(field.FieldId.Value, out var value))
        {
            return null;
        }

        if (field.JsonPropertyName is { } propertyName)
        {
            return ReadIntervalDate(value, propertyName);
        }

        return ReadDateValue(value);
    }

    private static DateOnly? ReadIntervalDate(JsonElement value, string propertyName)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            return value.TryGetProperty(propertyName, out var propertyValue)
                ? ReadDateValue(propertyValue)
                : null;
        }

        if (value.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(value.GetString()))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(value.GetString()!);
            return document.RootElement.TryGetProperty(propertyName, out var propertyValue)
                ? ReadDateValue(propertyValue)
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static DateOnly? ReadDateValue(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var text = value.GetString();
        if (DateOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }

        return DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var timestamp)
            ? DateOnly.FromDateTime(timestamp.Date)
            : null;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private readonly record struct RoadmapDateField(JiraFieldId FieldId, string? JsonPropertyName);
    private readonly IJiraSearchExecutor _searchExecutor;
    private readonly IJiraJqlFacade _jqlFacade;
    private readonly IJiraFieldResolver _fieldResolver;
    private readonly IJiraMapperFacade _mapperFacade;
}
