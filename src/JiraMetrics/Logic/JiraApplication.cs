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
    private readonly IPdfReportRenderer _pdfReportRenderer;

    /// <summary>
    /// Initializes a new instance of the <see cref="JiraApplication"/> class.
    /// </summary>
    /// <param name="settings">Application settings options.</param>
    /// <param name="apiClient">Jira API client.</param>
    /// <param name="logicService">Domain logic service.</param>
    /// <param name="presentationService">Presentation service.</param>
    /// <param name="pdfReportRenderer">PDF report renderer.</param>
    public JiraApplication(
        IOptions<AppSettings> settings,
        IJiraApiClient apiClient,
        IJiraLogicService logicService,
        IJiraPresentationService presentationService,
        IPdfReportRenderer pdfReportRenderer)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings.Value ?? throw new ArgumentException("App settings value is required.", nameof(settings));
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logicService = logicService ?? throw new ArgumentNullException(nameof(logicService));
        _presentationService = presentationService ?? throw new ArgumentNullException(nameof(presentationService));
        _pdfReportRenderer = pdfReportRenderer ?? throw new ArgumentNullException(nameof(pdfReportRenderer));
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

        _presentationService.ShowReportPeriodContext(_settings.MonthLabel, _settings.CreatedAfter);
        _presentationService.ShowSpacer();

        IReadOnlyList<IssueKey> issueKeys;
        IReadOnlyList<IssueKey> rejectIssueKeys = [];
        ItemCount? bugCreatedThisMonth = null;
        ItemCount? bugMovedToDoneThisMonth = null;
        ItemCount? bugRejectedThisMonth = null;
        ItemCount? bugFinishedThisMonth = null;
        IReadOnlyList<IssueListItem> bugOpenIssues = [];
        IReadOnlyList<IssueListItem> bugDoneIssues = [];
        IReadOnlyList<IssueListItem> bugRejectedIssues = [];
        IReadOnlyList<StatusIssueTypeSummary> openIssuesByStatus = [];
        IReadOnlyList<ReleaseIssueItem> releaseIssues = [];
        try
        {
            issueKeys = await _apiClient.GetIssueKeysMovedToDoneThisMonthAsync(
                _settings.ProjectKey,
                _settings.DoneStatusName,
                _settings.CreatedAfter,
                cancellationToken).ConfigureAwait(false);

            if (_settings.RejectStatusName is { } rejectStatusName)
            {
                rejectIssueKeys = await _apiClient.GetIssueKeysMovedToDoneThisMonthAsync(
                    _settings.ProjectKey,
                    rejectStatusName,
                    _settings.CreatedAfter,
                    cancellationToken).ConfigureAwait(false);
            }

            if (_settings.ReleaseReport is { } releaseReport)
            {
                _presentationService.ShowReleaseReportLoadingStarted();

                releaseIssues = await _apiClient.GetReleaseIssuesForMonthAsync(
                    releaseReport.ReleaseProjectKey,
                    releaseReport.ProjectLabel,
                    releaseReport.ReleaseDateFieldName,
                    releaseReport.ComponentsFieldName,
                    releaseReport.HotFixRules,
                    releaseReport.RollbackFieldName,
                    cancellationToken).ConfigureAwait(false);
            }

            if (_settings.ShowGeneralStatistics)
            {
                openIssuesByStatus = await _apiClient.GetIssueCountsByStatusExcludingDoneAndRejectAsync(
                    _settings.ProjectKey,
                    _settings.DoneStatusName,
                    _settings.RejectStatusName,
                    cancellationToken).ConfigureAwait(false);
            }
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

        if (_settings.ReleaseReport is { } releaseReportSettings)
        {
            _presentationService.ShowSpacer();
            _presentationService.ShowReleaseReport(releaseReportSettings, _settings.MonthLabel, releaseIssues);
            _presentationService.ShowSpacer();
        }

        if (_settings.BugIssueNames.Count > 0)
        {
            try
            {
                _presentationService.ShowBugRatioLoadingStarted(_settings.BugIssueNames);

                var bugCreatedIssues = await _apiClient.GetIssuesCreatedThisMonthAsync(
                    _settings.ProjectKey,
                    _settings.BugIssueNames,
                    cancellationToken).ConfigureAwait(false);
                bugDoneIssues = await _apiClient.GetIssuesMovedToDoneThisMonthAsync(
                    _settings.ProjectKey,
                    _settings.DoneStatusName,
                    _settings.BugIssueNames,
                    cancellationToken).ConfigureAwait(false);

                if (_settings.RejectStatusName is { } rejectStatusName)
                {
                    bugRejectedIssues = await _apiClient.GetIssuesMovedToDoneThisMonthAsync(
                        _settings.ProjectKey,
                        rejectStatusName,
                        _settings.BugIssueNames,
                        cancellationToken).ConfigureAwait(false);
                }

                var bugDoneKeys = bugDoneIssues
                    .Select(static issue => issue.Key.Value)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var bugRejectedKeys = bugRejectedIssues
                    .Select(static issue => issue.Key.Value)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var bugFinishedKeys = bugDoneKeys
                    .Union(bugRejectedKeys, StringComparer.OrdinalIgnoreCase)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                bugOpenIssues = [.. bugCreatedIssues
                    .Where(issue => !bugFinishedKeys.Contains(issue.Key.Value))
                    .OrderBy(static issue => issue.Key.Value, StringComparer.OrdinalIgnoreCase)];
                bugCreatedThisMonth = new ItemCount(bugCreatedIssues.Count);
                bugMovedToDoneThisMonth = new ItemCount(bugDoneIssues.Count);
                bugRejectedThisMonth = new ItemCount(bugRejectedIssues.Count);
                bugFinishedThisMonth = new ItemCount(bugFinishedKeys.Count);

                _presentationService.ShowBugRatioLoadingCompleted(
                    bugCreatedThisMonth.Value,
                    bugMovedToDoneThisMonth.Value,
                    bugRejectedThisMonth.Value,
                    bugFinishedThisMonth.Value);
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
        }

        if (bugCreatedThisMonth.HasValue
            && bugMovedToDoneThisMonth.HasValue
            && bugRejectedThisMonth.HasValue
            && bugFinishedThisMonth.HasValue)
        {
            _presentationService.ShowBugRatio(
                _settings.BugIssueNames,
                _settings.CustomFieldName,
                _settings.CustomFieldValue,
                bugCreatedThisMonth.Value,
                bugMovedToDoneThisMonth.Value,
                bugRejectedThisMonth.Value,
                bugFinishedThisMonth.Value,
                bugOpenIssues,
                bugDoneIssues,
                bugRejectedIssues);
            _presentationService.ShowSpacer();
        }

        _presentationService.ShowReportHeader(_settings, new ItemCount(issueKeys.Count));
        var openIssuesSummaryShown = false;

        void ShowOpenIssuesSummary()
        {
            if (openIssuesSummaryShown)
            {
                return;
            }

            if (!_settings.ShowGeneralStatistics)
            {
                return;
            }

            _presentationService.ShowOpenIssuesByStatusSummary(
                openIssuesByStatus,
                _settings.DoneStatusName,
                _settings.RejectStatusName);
            _presentationService.ShowSpacer();
            openIssuesSummaryShown = true;
        }

        if (issueKeys.Count == 0)
        {
            _presentationService.ShowNoIssuesMatchedFilter();
            ShowOpenIssuesSummary();
            return;
        }

        _presentationService.ShowIssueLoadingStarted(new ItemCount(issueKeys.Count));

        var issues = new List<IssueTimeline>();
        var rejectIssues = new List<IssueTimeline>();
        var loadedIssuesByKey = new Dictionary<string, IssueTimeline>(StringComparer.OrdinalIgnoreCase);
        var failures = new List<LoadFailure>();

        foreach (var issueKey in issueKeys)
        {
            try
            {
                var issue = await _apiClient.GetIssueTimelineAsync(issueKey, cancellationToken).ConfigureAwait(false);
                issues.Add(issue);
                loadedIssuesByKey[issue.Key.Value] = issue;
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

        if (_settings.RejectStatusName is not null && rejectIssueKeys.Count > 0)
        {
            foreach (var issueKey in rejectIssueKeys)
            {
                if (loadedIssuesByKey.TryGetValue(issueKey.Value, out var loadedIssue))
                {
                    rejectIssues.Add(loadedIssue);
                    continue;
                }

                try
                {
                    var issue = await _apiClient.GetIssueTimelineAsync(issueKey, cancellationToken).ConfigureAwait(false);
                    rejectIssues.Add(issue);
                    loadedIssuesByKey[issue.Key.Value] = issue;
                }
                catch (HttpRequestException ex)
                {
                    failures.Add(new LoadFailure(issueKey, ErrorMessage.FromException(ex)));
                }
                catch (InvalidOperationException ex)
                {
                    failures.Add(new LoadFailure(issueKey, ErrorMessage.FromException(ex)));
                }
                catch (System.Text.Json.JsonException ex)
                {
                    failures.Add(new LoadFailure(issueKey, ErrorMessage.FromException(ex)));
                }
            }
        }

        _presentationService.ShowIssueLoadingCompleted(new ItemCount(issues.Count), new ItemCount(failures.Count));
        _presentationService.ShowSpacer();

        if (issues.Count == 0)
        {
            _presentationService.ShowNoIssuesLoaded();
            _presentationService.ShowFailures(failures);
            ShowOpenIssuesSummary();
            return;
        }

        var issuesByType = _logicService.FilterIssuesByIssueTypes(issues, _settings.IssueTypes);
        if (issuesByType.Count == 0)
        {
            _presentationService.ShowNoIssuesMatchedFilter();
            _presentationService.ShowFailures(failures);
            ShowOpenIssuesSummary();
            return;
        }

        var filteredIssues = _logicService.FilterIssuesByRequiredStage(issuesByType, _settings.RequiredPathStages);
        if (filteredIssues.Count == 0)
        {
            _presentationService.ShowNoIssuesMatchedRequiredStage();
            _presentationService.ShowFailures(failures);
            ShowOpenIssuesSummary();
            return;
        }

        var doneDaysAtWork75PerType = _logicService.BuildDaysAtWork75PerType(filteredIssues, _settings.DoneStatusName);

        _presentationService.ShowDoneIssuesTable(filteredIssues, _settings.DoneStatusName);
        _presentationService.ShowSpacer();
        _presentationService.ShowDoneDaysAtWork75PerType(doneDaysAtWork75PerType, _settings.DoneStatusName);
        _presentationService.ShowSpacer();

        IReadOnlyList<IssueTimeline> filteredRejectedIssues = [];
        if (_settings.RejectStatusName is { } rejectStatus)
        {
            var rejectIssuesByType = _logicService.FilterIssuesByIssueTypes(rejectIssues, _settings.IssueTypes);
            filteredRejectedIssues = _logicService.FilterIssuesByRequiredStage(rejectIssuesByType, _settings.RequiredPathStages);
            _presentationService.ShowRejectedIssuesTable(filteredRejectedIssues, rejectStatus);
            _presentationService.ShowSpacer();
        }

        var groupedIssues = filteredIssues
            .Where(static issue => issue.HasPullRequest)
            .ToList();
        var groups = _logicService.BuildPathGroups(groupedIssues);
        var pathGroupsSummary = new PathGroupsSummary(
            new ItemCount(issues.Count),
            new ItemCount(groupedIssues.Count),
            new ItemCount(failures.Count),
            new ItemCount(groups.Count));
        _presentationService.ShowPathGroupsSummary(pathGroupsSummary);
        _presentationService.ShowSpacer();
        _presentationService.ShowPathGroups(groups);
        ShowOpenIssuesSummary();

        _pdfReportRenderer.RenderReport(new JiraPdfReportData
        {
            Settings = _settings,
            SearchIssueCount = new ItemCount(issueKeys.Count),
            ReleaseIssues = releaseIssues,
            BugCreatedThisMonth = bugCreatedThisMonth,
            BugMovedToDoneThisMonth = bugMovedToDoneThisMonth,
            BugRejectedThisMonth = bugRejectedThisMonth,
            BugFinishedThisMonth = bugFinishedThisMonth,
            BugOpenIssues = bugOpenIssues,
            BugDoneIssues = bugDoneIssues,
            BugRejectedIssues = bugRejectedIssues,
            OpenIssuesByStatus = openIssuesByStatus,
            DoneIssues = filteredIssues,
            DoneDaysAtWork75PerType = doneDaysAtWork75PerType,
            RejectedIssues = filteredRejectedIssues,
            PathSummary = pathGroupsSummary,
            PathGroups = groups,
            Failures = failures
        });

        if (failures.Count > 0)
        {
            _presentationService.ShowSpacer();
            _presentationService.ShowFailures(failures);
        }
    }
}
