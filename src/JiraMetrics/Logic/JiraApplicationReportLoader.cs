using System.Text.Json;

using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Logic;

/// <summary>
/// Loads report context and auxiliary ratio data used before detailed analysis starts.
/// </summary>
internal sealed class JiraApplicationReportLoader : IJiraApplicationReportLoader
{
    public JiraApplicationReportLoader(
        AppSettings settings,
        IJiraApplicationDataFacade dataFacade,
        IJiraApplicationReportingFacade reportingFacade)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(dataFacade);
        ArgumentNullException.ThrowIfNull(reportingFacade);

        _settings = settings;
        _dataFacade = dataFacade;
        _reportingFacade = reportingFacade;
    }

    public Task<JiraAuthUser> GetReportUserAsync(CancellationToken cancellationToken) =>
        _dataFacade.GetCurrentUserAsync(cancellationToken);

    public async Task<JiraApplicationReportData?> TryLoadAsync(CancellationToken cancellationToken)
    {
        _reportingFacade.ShowReportPeriodContext(_settings.ReportPeriod, _settings.CreatedAfter);
        _reportingFacade.ShowSpacer();

        ShowOptionalReportLoadingStarted();
        _reportingFacade.ShowAllTasksRatioLoadingStarted();

        var reportContextTask = TryLoadSearchDataAsync(
            () => _dataFacade.LoadReportContextAsync(_settings, cancellationToken));
        var allTasksRatioTask = TryLoadSearchDataAsync(
            () => _dataFacade.LoadIssueRatioAsync(_settings, [], cancellationToken));
        var bugRatioTask = StartBugRatioLoading(cancellationToken);

        var reportContext = await reportContextTask.ConfigureAwait(false);
        if (reportContext is null)
        {
            return null;
        }

        ShowOptionalReports(reportContext);

        var allTasksRatio = await LoadAndShowAllTasksRatioAsync(allTasksRatioTask).ConfigureAwait(false);
        if (allTasksRatio is null)
        {
            return null;
        }

        var bugRatio = await LoadAndShowBugRatioAsync(bugRatioTask).ConfigureAwait(false);
        if (bugRatioTask is not null && bugRatio is null)
        {
            return null;
        }

        return new JiraApplicationReportData(reportContext, allTasksRatio, bugRatio);
    }

    private void ShowOptionalReportLoadingStarted()
    {
        if (_settings.ReleaseReport is not null)
        {
            _reportingFacade.ShowReleaseReportLoadingStarted();
        }

        if (_settings.ArchTasksReport is not null)
        {
            _reportingFacade.ShowArchTasksReportLoadingStarted();
        }

        if (_settings.GlobalIncidentsReport is not null)
        {
            _reportingFacade.ShowGlobalIncidentsReportLoadingStarted();
        }
    }

    private void ShowOptionalReports(JiraReportContext reportContext)
    {
        if (_settings.ReleaseReport is { } releaseReportSettings)
        {
            _reportingFacade.ShowSpacer();
            _reportingFacade.ShowReleaseReport(
                releaseReportSettings,
                _settings.ReportPeriod,
                reportContext.ReleaseIssues);
            _reportingFacade.ShowSpacer();
        }

        if (_settings.ArchTasksReport is { } archTasksReportSettings)
        {
            _reportingFacade.ShowArchTasksReport(
                archTasksReportSettings,
                reportContext.ArchTasks);
            _reportingFacade.ShowSpacer();
        }

        if (_settings.GlobalIncidentsReport is { } globalIncidentsReportSettings)
        {
            _reportingFacade.ShowGlobalIncidentsReport(
                globalIncidentsReportSettings,
                _settings.ReportPeriod,
                reportContext.GlobalIncidents);
            _reportingFacade.ShowSpacer();
        }
    }

    private Task<IssueRatioSnapshot?>? StartBugRatioLoading(CancellationToken cancellationToken)
    {
        if (_settings.BugIssueNames.Count == 0)
        {
            return null;
        }

        _reportingFacade.ShowBugRatioLoadingStarted(_settings.BugIssueNames);
        return TryLoadSearchDataAsync(
            () => _dataFacade.LoadIssueRatioAsync(
                _settings,
                _settings.BugIssueNames,
                cancellationToken));
    }

    private async Task<IssueRatioSnapshot?> LoadAndShowAllTasksRatioAsync(Task<IssueRatioSnapshot?> allTasksRatioTask)
    {
        var allTasksRatio = await allTasksRatioTask.ConfigureAwait(false);
        if (allTasksRatio is null)
        {
            return null;
        }

        _reportingFacade.ShowAllTasksRatioLoadingCompleted(allTasksRatio);
        _reportingFacade.ShowAllTasksRatio(
            _settings.CustomFieldName,
            _settings.CustomFieldValue,
            allTasksRatio);
        _reportingFacade.ShowSpacer();

        return allTasksRatio;
    }

    private async Task<IssueRatioSnapshot?> LoadAndShowBugRatioAsync(Task<IssueRatioSnapshot?>? bugRatioTask)
    {
        if (bugRatioTask is null)
        {
            return null;
        }

        var bugRatio = await bugRatioTask.ConfigureAwait(false);
        if (bugRatio is null)
        {
            return null;
        }

        _reportingFacade.ShowBugRatioLoadingCompleted(bugRatio);
        _reportingFacade.ShowBugRatio(
            _settings.BugIssueNames,
            _settings.CustomFieldName,
            _settings.CustomFieldValue,
            bugRatio);
        _reportingFacade.ShowSpacer();

        return bugRatio;
    }

    private async Task<T?> TryLoadSearchDataAsync<T>(Func<Task<T>> loadAsync)
        where T : class
    {
        try
        {
            return await loadAsync().ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            _reportingFacade.ShowIssueSearchFailed(ErrorMessage.FromException(ex));
        }
        catch (InvalidOperationException ex)
        {
            _reportingFacade.ShowIssueSearchFailed(ErrorMessage.FromException(ex));
        }
        catch (JsonException ex)
        {
            _reportingFacade.ShowIssueSearchFailed(ErrorMessage.FromException(ex));
        }

        return null;
    }

    private readonly AppSettings _settings;
    private readonly IJiraApplicationDataFacade _dataFacade;
    private readonly IJiraApplicationReportingFacade _reportingFacade;
}
