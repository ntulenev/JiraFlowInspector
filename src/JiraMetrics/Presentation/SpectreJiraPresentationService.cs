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
        StopAllLoaders();
        _statusSection.ShowIssueSearchFailed(errorMessage);
    }

    /// <inheritdoc />
    public void ShowReportHeader(AppSettings settings, ItemCount issueCount)
    {
        ArgumentNullException.ThrowIfNull(settings);
        StopAllLoaders();
        _statusSection.ShowReportHeader(settings, issueCount);
    }

    /// <inheritdoc />
    public void ShowNoIssuesMatchedFilter()
    {
        StopAllLoaders();
        _statusSection.ShowNoIssuesMatchedFilter();
    }

    /// <inheritdoc />
    public void ShowIssueLoadingStarted(ItemCount totalIssues)
    {
        StopAllLoaders();
        _issueLoadTotal = totalIssues.Value;
        _issueLoadProcessed = 0;
        _issueLoadFailed = 0;
        _issueLoadStep = Math.Max(1, _issueLoadTotal / 10);

        if (CanAnimatePendingLoader())
        {
            StartPendingLoader(BuildIssueLoadProgressMessage);
            return;
        }

        AnsiConsole.MarkupLine(BuildIssueLoadProgressMessage());
    }

    /// <inheritdoc />
    public void ShowIssueLoaded(IssueKey issueKey) => UpdateIssueLoadProgress(wasFailure: false);

    /// <inheritdoc />
    public void ShowIssueFailed(IssueKey issueKey) => UpdateIssueLoadProgress(wasFailure: true);

    /// <inheritdoc />
    public void ShowIssueLoadingCompleted(ItemCount loadedIssues, ItemCount failedIssues)
    {
        StopAllLoaders();
        _statusSection.ShowIssueLoadingCompleted(loadedIssues, failedIssues);
    }

    /// <inheritdoc />
    public void ShowProcessingStep(string message)
    {
        StopAllLoaders();
        _statusSection.ShowProcessingStep(message);
    }

    /// <inheritdoc />
    public void ShowSpacer()
    {
        StopAllLoaders();
        _statusSection.ShowSpacer();
    }

    /// <inheritdoc />
    public void ShowNoIssuesLoaded()
    {
        StopAllLoaders();
        _statusSection.ShowNoIssuesLoaded();
    }

    /// <inheritdoc />
    public void ShowNoIssuesMatchedRequiredStage()
    {
        StopAllLoaders();
        _statusSection.ShowNoIssuesMatchedRequiredStage();
    }

    /// <inheritdoc />
    public void ShowDoneIssuesTable(IReadOnlyList<IssueTimeline> issues, StatusName doneStatusName)
    {
        ArgumentNullException.ThrowIfNull(issues);
        StopAllLoaders();
        _transitionSection.ShowDoneIssuesTable(issues, doneStatusName);
    }

    /// <inheritdoc />
    public void ShowDoneDaysAtWork75PerType(
        IReadOnlyList<IssueTypeWorkDays75Summary> summaries,
        StatusName doneStatusName)
    {
        ArgumentNullException.ThrowIfNull(summaries);
        StopAllLoaders();
        _transitionSection.ShowDoneDaysAtWork75PerType(summaries, doneStatusName);
    }

    /// <inheritdoc />
    public void ShowRejectedIssuesTable(IReadOnlyList<IssueTimeline> issues, StatusName rejectStatusName)
    {
        ArgumentNullException.ThrowIfNull(issues);
        StopAllLoaders();
        _transitionSection.ShowRejectedIssuesTable(issues, rejectStatusName);
    }

    /// <inheritdoc />
    public void ShowPathGroupsSummary(PathGroupsSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        StopAllLoaders();
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
        StopAllLoaders();
        _releaseSection.ShowReleaseReport(settings, reportPeriod, releases);
    }

    /// <inheritdoc />
    public void ShowArchTasksReport(
        ArchTasksReportSettings settings,
        IReadOnlyList<ArchTaskItem> tasks)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(tasks);
        StopAllLoaders();
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
        StopAllLoaders();
        _globalIncidentsSection.ShowGlobalIncidentsReport(settings, reportPeriod, incidents);
    }

    /// <inheritdoc />
    public void ShowAllTasksRatioLoadingStarted() => StartPendingLoader("Loading all tasks ratio data...");

    /// <inheritdoc />
    public void ShowAllTasksRatioLoadingCompleted(IssueRatioSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        StopAllLoaders();
        _ratioSection.ShowAllTasksRatioLoadingCompleted(snapshot);
    }

    /// <inheritdoc />
    public void ShowAllTasksRatio(
        string? customFieldName,
        string? customFieldValue,
        IssueRatioSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        StopAllLoaders();
        _ratioSection.ShowAllTasksRatio(
            customFieldName,
            customFieldValue,
            snapshot);
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
    public void ShowBugRatioLoadingCompleted(IssueRatioSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        StopAllLoaders();
        _ratioSection.ShowBugRatioLoadingCompleted(snapshot);
    }

    /// <inheritdoc />
    public void ShowBugRatio(
        IReadOnlyList<IssueTypeName> bugIssueNames,
        string? customFieldName,
        string? customFieldValue,
        IssueRatioSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(bugIssueNames);
        ArgumentNullException.ThrowIfNull(snapshot);
        StopAllLoaders();
        _ratioSection.ShowBugRatio(
            bugIssueNames,
            customFieldName,
            customFieldValue,
            snapshot);
    }

    /// <inheritdoc />
    public void ShowOpenIssuesByStatusSummary(
        IReadOnlyList<StatusIssueTypeSummary> statusSummaries,
        StatusName doneStatusName,
        StatusName? rejectStatusName)
    {
        ArgumentNullException.ThrowIfNull(statusSummaries);
        StopAllLoaders();
        _generalStatisticsSection.ShowOpenIssuesByStatusSummary(statusSummaries, doneStatusName, rejectStatusName);
    }

    /// <inheritdoc />
    public void ShowPathGroups(IReadOnlyList<PathGroup> groups)
    {
        ArgumentNullException.ThrowIfNull(groups);
        StopAllLoaders();
        _transitionSection.ShowPathGroups(groups);
    }

    /// <inheritdoc />
    public void ShowExecutionSummary(TimeSpan totalDuration, JiraRequestTelemetrySummary requestTelemetry)
    {
        ArgumentNullException.ThrowIfNull(requestTelemetry);
        StopAllLoaders();
        _statusSection.ShowExecutionSummary(totalDuration, requestTelemetry);
    }

    /// <inheritdoc />
    public void ShowFailures(IReadOnlyList<LoadFailure> failures)
    {
        ArgumentNullException.ThrowIfNull(failures);
        StopAllLoaders();
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

        if (_pendingLoaderCancellation is not null)
        {
            return;
        }

        if (_issueLoadProcessed == _issueLoadTotal
            || _issueLoadProcessed % _issueLoadStep == 0)
        {
            AnsiConsole.MarkupLine(BuildIssueLoadProgressMessage());
        }
    }

    private string BuildIssueLoadProgressMessage()
    {
        if (_issueLoadTotal <= 0)
        {
            return "[grey]Loading issue timelines:[/] 0/0";
        }

        if (_issueLoadProcessed == 0 && _issueLoadFailed == 0)
        {
            return $"[grey]Loading issue timelines:[/] 0/{_issueLoadTotal}";
        }

        var percent = _issueLoadProcessed * 100.0 / _issueLoadTotal;
        return $"[grey]Loading issue timelines:[/] {_issueLoadProcessed}/{_issueLoadTotal} ({percent:0}%)  [grey]failed:[/] {_issueLoadFailed}";
    }

    private void StartPendingLoader(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        StopAllLoaders();

        if (!CanAnimatePendingLoader())
        {
            AnsiConsole.MarkupLine($"[grey]{Markup.Escape(message)}[/]");
            return;
        }

        StartPendingLoader(() => $"[grey]{Markup.Escape(message)}[/]");
    }

    private void StartPendingLoader(Func<string> messageFactory)
    {
        ArgumentNullException.ThrowIfNull(messageFactory);

        StopAllLoaders();

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
                        AnsiConsole.Markup($"\r{messageFactory()} {_pendingLoaderFrames[index]}");
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

    private void StopAllLoaders() => StopPendingLoader();

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
}

