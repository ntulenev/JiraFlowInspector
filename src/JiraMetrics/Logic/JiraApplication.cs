using JiraMetrics.Abstractions;
using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

using Microsoft.Extensions.Options;

namespace JiraMetrics.Logic;

/// <summary>
/// Default application workflow implementation.
/// </summary>
public sealed class JiraApplication : IJiraApplication
{
    private readonly AppSettings _settings;
    private readonly IJiraApiClient _apiClient;
    private readonly IJiraLogicService _logicService;
    private readonly IJiraPresentationService _presentationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="JiraApplication"/> class.
    /// </summary>
    /// <param name="settings">Application settings options.</param>
    /// <param name="apiClient">Jira API client.</param>
    /// <param name="logicService">Domain logic service.</param>
    /// <param name="presentationService">Presentation service.</param>
    public JiraApplication(
        IOptions<AppSettings> settings,
        IJiraApiClient apiClient,
        IJiraLogicService logicService,
        IJiraPresentationService presentationService)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings.Value ?? throw new ArgumentException("App settings value is required.", nameof(settings));
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logicService = logicService ?? throw new ArgumentNullException(nameof(logicService));
        _presentationService = presentationService ?? throw new ArgumentNullException(nameof(presentationService));
    }

    /// <summary>
    /// Executes the application flow.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _presentationService.ShowAuthenticationStarted();

        try
        {
            var user = await _apiClient.GetCurrentUserAsync(cancellationToken).ConfigureAwait(false);
            _presentationService.ShowAuthenticationSucceeded(user);
        }
        catch (Exception ex)
        {
            _presentationService.ShowAuthenticationFailed(ErrorMessage.FromException(ex));
            throw;
        }

        IReadOnlyList<IssueKey> issueKeys;
        try
        {
            issueKeys = await _apiClient.GetIssueKeysMovedToDoneThisMonthAsync(
                _settings.ProjectKey,
                _settings.DoneStatusName,
                _settings.CreatedAfter,
                cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            _presentationService.ShowIssueSearchFailed(ErrorMessage.FromException(ex));
            return;
        }
        catch (InvalidOperationException ex)
        {
            _presentationService.ShowIssueSearchFailed(ErrorMessage.FromException(ex));
            return;
        }
        catch (System.Text.Json.JsonException ex)
        {
            _presentationService.ShowIssueSearchFailed(ErrorMessage.FromException(ex));
            return;
        }

        _presentationService.ShowReportHeader(_settings, new ItemCount(issueKeys.Count));

        if (issueKeys.Count == 0)
        {
            _presentationService.ShowNoIssuesMatchedFilter();
            return;
        }

        var issues = new List<IssueTimeline>();
        var failures = new List<LoadFailure>();

        foreach (var issueKey in issueKeys)
        {
            try
            {
                var issue = await _apiClient.GetIssueTimelineAsync(issueKey, cancellationToken).ConfigureAwait(false);
                issues.Add(issue);
                _presentationService.ShowIssueLoaded(issueKey);
            }
            catch (HttpRequestException ex)
            {
                failures.Add(new LoadFailure(issueKey, ErrorMessage.FromException(ex)));
                _presentationService.ShowIssueFailed(issueKey);
            }
            catch (InvalidOperationException ex)
            {
                failures.Add(new LoadFailure(issueKey, ErrorMessage.FromException(ex)));
                _presentationService.ShowIssueFailed(issueKey);
            }
            catch (System.Text.Json.JsonException ex)
            {
                failures.Add(new LoadFailure(issueKey, ErrorMessage.FromException(ex)));
                _presentationService.ShowIssueFailed(issueKey);
            }
        }

        _presentationService.ShowSpacer();

        if (issues.Count == 0)
        {
            _presentationService.ShowNoIssuesLoaded();
            _presentationService.ShowFailures(failures);
            return;
        }

        var issuesByType = _logicService.FilterIssuesByIssueTypes(issues, _settings.IssueTypes);
        if (issuesByType.Count == 0)
        {
            _presentationService.ShowNoIssuesMatchedFilter();
            _presentationService.ShowFailures(failures);
            return;
        }

        var filteredIssues = _logicService.FilterIssuesByRequiredStage(issuesByType, _settings.RequiredPathStage);
        if (filteredIssues.Count == 0)
        {
            _presentationService.ShowNoIssuesMatchedRequiredStage();
            _presentationService.ShowFailures(failures);
            return;
        }

        _presentationService.ShowDoneIssuesTable(filteredIssues, _settings.DoneStatusName);
        _presentationService.ShowSpacer();

        var groups = _logicService.BuildPathGroups(filteredIssues);
        _presentationService.ShowPathGroupsSummary(new PathGroupsSummary(
            new ItemCount(issues.Count),
            new ItemCount(filteredIssues.Count),
            new ItemCount(failures.Count),
            new ItemCount(groups.Count)));
        _presentationService.ShowSpacer();
        _presentationService.ShowPathGroups(groups);

        if (failures.Count > 0)
        {
            _presentationService.ShowSpacer();
            _presentationService.ShowFailures(failures);
        }
    }
}
