using JiraMetrics.Abstractions;
using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

using Microsoft.Extensions.Options;

using Spectre.Console;

namespace JiraMetrics.Presentation;

/// <summary>
/// Spectre.Console-based presentation service.
/// </summary>
public sealed class SpectreJiraPresentationService : IJiraPresentationService
{
    private static readonly char[] _pendingLoaderFrames = ['|', '/', '-', '\\'];
    private readonly object _pendingLoaderSync = new();
    private readonly SpectreStatusSection _statusSection;
    private readonly SpectreRatioSection _ratioSection;
    private readonly SpectreReleaseSection _releaseSection;
    private readonly SpectreArchTasksSection _archTasksSection;
    private readonly SpectreGlobalIncidentsSection _globalIncidentsSection;
    private readonly SpectreTransitionSection _transitionSection;
    private readonly SpectreGeneralStatisticsSection _generalStatisticsSection;
    private readonly SpectreFailuresSection _failuresSection;
    private CancellationTokenSource? _pendingLoaderCancellation;
    private Task? _pendingLoaderTask;
    private int _issueLoadTotal;
    private int _issueLoadProcessed;
    private int _issueLoadFailed;
    private int _issueLoadStep = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="SpectreJiraPresentationService"/> class.
    /// </summary>
    public SpectreJiraPresentationService()
        : this(showTimeCalculationsInHoursOnly: false)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SpectreJiraPresentationService"/> class.
    /// </summary>
    /// <param name="settings">Application settings options.</param>
    public SpectreJiraPresentationService(IOptions<AppSettings> settings)
        : this(settings is null
            ? throw new ArgumentNullException(nameof(settings))
            : settings.Value.ShowTimeCalculationsInHoursOnly)
    {
    }

    private SpectreJiraPresentationService(bool showTimeCalculationsInHoursOnly)
    {
        _statusSection = new SpectreStatusSection();
        _ratioSection = new SpectreRatioSection();
        _releaseSection = new SpectreReleaseSection();
        _archTasksSection = new SpectreArchTasksSection();
        _globalIncidentsSection = new SpectreGlobalIncidentsSection(showTimeCalculationsInHoursOnly);
        _transitionSection = new SpectreTransitionSection(showTimeCalculationsInHoursOnly);
        _generalStatisticsSection = new SpectreGeneralStatisticsSection();
        _failuresSection = new SpectreFailuresSection();
    }

    /// <inheritdoc />
    public void ShowAuthenticationStarted() => _statusSection.ShowAuthenticationStarted();

    /// <inheritdoc />
    public void ShowAuthenticationSucceeded(JiraAuthUser user)
    {
        ArgumentNullException.ThrowIfNull(user);
        _statusSection.ShowAuthenticationSucceeded(user);
    }

    /// <inheritdoc />
    public void ShowAuthenticationFailed(ErrorMessage errorMessage)
    {
        _statusSection.ShowAuthenticationFailed(errorMessage);
    }

    /// <inheritdoc />
    public void ShowReportPeriodContext(ReportPeriod reportPeriod, CreatedAfterDate? createdAfter) =>
        _statusSection.ShowReportPeriodContext(reportPeriod, createdAfter);

    /// <inheritdoc />
    public void ShowIssueSearchFailed(ErrorMessage errorMessage)
    {
        StopPendingLoader();
        _statusSection.ShowIssueSearchFailed(errorMessage);
    }

    /// <inheritdoc />
    public void ShowReportHeader(AppSettings settings, ItemCount issueCount)
    {
        ArgumentNullException.ThrowIfNull(settings);
        StopPendingLoader();
        _statusSection.ShowReportHeader(settings, issueCount);
    }

    /// <inheritdoc />
    public void ShowNoIssuesMatchedFilter()
    {
        StopPendingLoader();
        _statusSection.ShowNoIssuesMatchedFilter();
    }

    /// <inheritdoc />
    public void ShowIssueLoadingStarted(ItemCount totalIssues)
    {
        StopPendingLoader();
        _issueLoadTotal = totalIssues.Value;
        _issueLoadProcessed = 0;
        _issueLoadFailed = 0;
        _issueLoadStep = Math.Max(1, _issueLoadTotal / 10);

        AnsiConsole.MarkupLine($"[grey]Loading issue timelines:[/] 0/{_issueLoadTotal}");
    }

    /// <inheritdoc />
    public void ShowIssueLoaded(IssueKey issueKey) => UpdateIssueLoadProgress(wasFailure: false);

    /// <inheritdoc />
    public void ShowIssueFailed(IssueKey issueKey) => UpdateIssueLoadProgress(wasFailure: true);

    /// <inheritdoc />
    public void ShowIssueLoadingCompleted(ItemCount loadedIssues, ItemCount failedIssues) =>
        _statusSection.ShowIssueLoadingCompleted(loadedIssues, failedIssues);

    /// <inheritdoc />
    public void ShowProcessingStep(string message)
    {
        StopPendingLoader();
        _statusSection.ShowProcessingStep(message);
    }

    /// <inheritdoc />
    public void ShowSpacer()
    {
        StopPendingLoader();
        _statusSection.ShowSpacer();
    }

    /// <inheritdoc />
    public void ShowNoIssuesLoaded()
    {
        StopPendingLoader();
        _statusSection.ShowNoIssuesLoaded();
    }

    /// <inheritdoc />
    public void ShowNoIssuesMatchedRequiredStage()
    {
        StopPendingLoader();
        _statusSection.ShowNoIssuesMatchedRequiredStage();
    }

    /// <inheritdoc />
    public void ShowDoneIssuesTable(IReadOnlyList<IssueTimeline> issues, StatusName doneStatusName)
    {
        ArgumentNullException.ThrowIfNull(issues);
        StopPendingLoader();
        _transitionSection.ShowDoneIssuesTable(issues, doneStatusName);
    }

    /// <inheritdoc />
    public void ShowDoneDaysAtWork75PerType(
        IReadOnlyList<IssueTypeWorkDays75Summary> summaries,
        StatusName doneStatusName)
    {
        ArgumentNullException.ThrowIfNull(summaries);
        StopPendingLoader();
        _transitionSection.ShowDoneDaysAtWork75PerType(summaries, doneStatusName);
    }

    /// <inheritdoc />
    public void ShowRejectedIssuesTable(IReadOnlyList<IssueTimeline> issues, StatusName rejectStatusName)
    {
        ArgumentNullException.ThrowIfNull(issues);
        StopPendingLoader();
        _transitionSection.ShowRejectedIssuesTable(issues, rejectStatusName);
    }

    /// <inheritdoc />
    public void ShowPathGroupsSummary(PathGroupsSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        StopPendingLoader();
        _transitionSection.ShowPathGroupsSummary(summary);
    }

    /// <inheritdoc />
    public void ShowReleaseReportLoadingStarted() => StartPendingLoader("Loading release report data...");

    /// <inheritdoc />
    public void ShowGlobalIncidentsReportLoadingStarted() => StartPendingLoader("Loading global incidents report data...");

    /// <inheritdoc />
    public void ShowArchTasksReportLoadingStarted() => StartPendingLoader("Loading architecture tasks report data...");

    /// <inheritdoc />
    public void ShowReleaseReport(
        ReleaseReportSettings settings,
        ReportPeriod reportPeriod,
        IReadOnlyList<ReleaseIssueItem> releases)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(releases);
        StopPendingLoader();
        _releaseSection.ShowReleaseReport(settings, reportPeriod, releases);
    }

    /// <inheritdoc />
    public void ShowArchTasksReport(
        ArchTasksReportSettings settings,
        IReadOnlyList<ArchTaskItem> tasks)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(tasks);
        StopPendingLoader();
        _archTasksSection.ShowArchTasksReport(settings, tasks);
    }

    /// <inheritdoc />
    public void ShowGlobalIncidentsReport(
        GlobalIncidentsReportSettings settings,
        ReportPeriod reportPeriod,
        IReadOnlyList<GlobalIncidentItem> incidents)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(incidents);
        StopPendingLoader();
        _globalIncidentsSection.ShowGlobalIncidentsReport(settings, reportPeriod, incidents);
    }

    /// <inheritdoc />
    public void ShowAllTasksRatioLoadingStarted() => StartPendingLoader("Loading all tasks ratio data...");

    /// <inheritdoc />
    public void ShowAllTasksRatioLoadingCompleted(
        ItemCount createdThisMonth,
        ItemCount movedToDoneThisMonth,
        ItemCount rejectedThisMonth,
        ItemCount finishedThisMonth)
    {
        StopPendingLoader();
        _ratioSection.ShowAllTasksRatioLoadingCompleted(
            createdThisMonth,
            movedToDoneThisMonth,
            rejectedThisMonth,
            finishedThisMonth);
    }

    /// <inheritdoc />
    public void ShowAllTasksRatio(
        string? customFieldName,
        string? customFieldValue,
        ItemCount createdThisMonth,
        ItemCount openThisMonth,
        ItemCount movedToDoneThisMonth,
        ItemCount rejectedThisMonth,
        ItemCount finishedThisMonth)
    {
        StopPendingLoader();
        _ratioSection.ShowAllTasksRatio(
            customFieldName,
            customFieldValue,
            createdThisMonth,
            openThisMonth,
            movedToDoneThisMonth,
            rejectedThisMonth,
            finishedThisMonth);
    }

    /// <inheritdoc />
    public void ShowBugRatioLoadingStarted(IReadOnlyList<IssueTypeName> bugIssueNames)
    {
        ArgumentNullException.ThrowIfNull(bugIssueNames);

        var bugTypes = bugIssueNames.Count == 0
            ? "-"
            : string.Join(", ", bugIssueNames.Select(static issueType => issueType.Value));
        StartPendingLoader($"Loading bug ratio data for: {bugTypes}");
    }

    /// <inheritdoc />
    public void ShowBugRatioLoadingCompleted(
        ItemCount createdThisMonth,
        ItemCount movedToDoneThisMonth,
        ItemCount rejectedThisMonth,
        ItemCount finishedThisMonth)
    {
        StopPendingLoader();
        _ratioSection.ShowBugRatioLoadingCompleted(
            createdThisMonth,
            movedToDoneThisMonth,
            rejectedThisMonth,
            finishedThisMonth);
    }

    /// <inheritdoc />
    public void ShowBugRatio(
        IReadOnlyList<IssueTypeName> bugIssueNames,
        string? customFieldName,
        string? customFieldValue,
        ItemCount createdThisMonth,
        ItemCount movedToDoneThisMonth,
        ItemCount rejectedThisMonth,
        ItemCount finishedThisMonth,
        IReadOnlyList<IssueListItem> openIssues,
        IReadOnlyList<IssueListItem> doneIssues,
        IReadOnlyList<IssueListItem> rejectedIssues)
    {
        ArgumentNullException.ThrowIfNull(bugIssueNames);
        ArgumentNullException.ThrowIfNull(openIssues);
        ArgumentNullException.ThrowIfNull(doneIssues);
        ArgumentNullException.ThrowIfNull(rejectedIssues);
        StopPendingLoader();
        _ratioSection.ShowBugRatio(
            bugIssueNames,
            customFieldName,
            customFieldValue,
            createdThisMonth,
            movedToDoneThisMonth,
            rejectedThisMonth,
            finishedThisMonth,
            openIssues,
            doneIssues,
            rejectedIssues);
    }

    /// <inheritdoc />
    public void ShowOpenIssuesByStatusSummary(
        IReadOnlyList<StatusIssueTypeSummary> statusSummaries,
        StatusName doneStatusName,
        StatusName? rejectStatusName)
    {
        ArgumentNullException.ThrowIfNull(statusSummaries);
        StopPendingLoader();
        _generalStatisticsSection.ShowOpenIssuesByStatusSummary(statusSummaries, doneStatusName, rejectStatusName);
    }

    /// <inheritdoc />
    public void ShowPathGroups(IReadOnlyList<PathGroup> groups)
    {
        ArgumentNullException.ThrowIfNull(groups);
        StopPendingLoader();
        _transitionSection.ShowPathGroups(groups);
    }

    /// <inheritdoc />
    public void ShowExecutionSummary(TimeSpan totalDuration, JiraRequestTelemetrySummary requestTelemetry)
    {
        ArgumentNullException.ThrowIfNull(requestTelemetry);
        StopPendingLoader();
        _statusSection.ShowExecutionSummary(totalDuration, requestTelemetry);
    }

    /// <inheritdoc />
    public void ShowFailures(IReadOnlyList<LoadFailure> failures)
    {
        ArgumentNullException.ThrowIfNull(failures);
        StopPendingLoader();
        _failuresSection.ShowFailures(failures);
    }

    private void UpdateIssueLoadProgress(bool wasFailure)
    {
        if (_issueLoadTotal <= 0)
        {
            return;
        }

        _issueLoadProcessed++;
        if (wasFailure)
        {
            _issueLoadFailed++;
        }

        if (_issueLoadProcessed == _issueLoadTotal
            || _issueLoadProcessed % _issueLoadStep == 0)
        {
            var percent = _issueLoadProcessed * 100.0 / _issueLoadTotal;
            AnsiConsole.MarkupLine(
                $"[grey]Loading issue timelines:[/] {_issueLoadProcessed}/{_issueLoadTotal} ({percent:0}%)  [grey]failed:[/] {_issueLoadFailed}");
        }
    }

    private void StartPendingLoader(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        StopPendingLoader();

        if (!CanAnimatePendingLoader())
        {
            AnsiConsole.MarkupLine($"[grey]{Markup.Escape(message)}[/]");
            return;
        }

        var cancellation = new CancellationTokenSource();
        lock (_pendingLoaderSync)
        {
            _pendingLoaderCancellation = cancellation;
            _pendingLoaderTask = Task.Run(async () =>
            {
                var index = 0;

                while (!cancellation.Token.IsCancellationRequested)
                {
                    lock (_pendingLoaderSync)
                    {
                        AnsiConsole.Markup($"\r[grey]{Markup.Escape(message)} {_pendingLoaderFrames[index]}[/]");
                    }

                    index = (index + 1) % _pendingLoaderFrames.Length;

                    try
                    {
                        await Task.Delay(120, cancellation.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                }
            }, cancellation.Token);
        }
    }

    private void StopPendingLoader()
    {
        CancellationTokenSource? cancellation = null;
        Task? task = null;

        lock (_pendingLoaderSync)
        {
            if (_pendingLoaderCancellation is null)
            {
                return;
            }

            cancellation = _pendingLoaderCancellation;
            task = _pendingLoaderTask;
            _pendingLoaderCancellation = null;
            _pendingLoaderTask = null;
        }

        cancellation.Cancel();
        try
        {
            _ = task?.Wait(250);
        }
        catch (AggregateException)
        {
        }
        finally
        {
            cancellation.Dispose();
        }

        lock (_pendingLoaderSync)
        {
            AnsiConsole.WriteLine();
        }
    }

    private static bool CanAnimatePendingLoader() =>
        !Console.IsOutputRedirected && AnsiConsole.Console.GetType().Name != "TestConsole";
}
