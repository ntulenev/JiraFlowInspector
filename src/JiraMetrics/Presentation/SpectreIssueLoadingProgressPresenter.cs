using JiraMetrics.Models.ValueObjects;

using Spectre.Console;

namespace JiraMetrics.Presentation;

/// <summary>
/// Owns issue-loading progress state and the console pending-operation animation.
/// </summary>
internal sealed class SpectreIssueLoadingProgressPresenter : IJiraIssueLoadingProgressPresenter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SpectreIssueLoadingProgressPresenter"/> class.
    /// </summary>
    /// <param name="statusSection">Console status section used for completion and spacing output.</param>
    public SpectreIssueLoadingProgressPresenter(SpectreStatusSection statusSection)
    {
        _statusSection = statusSection ?? throw new ArgumentNullException(nameof(statusSection));
    }

    /// <inheritdoc />
    public void ShowIssueLoadingStarted(ItemCount totalIssues)
    {
        Stop();
        lock (_pendingLoaderSync)
        {
            _issueLoadTotal = totalIssues.Value;
            _issueLoadProcessed = 0;
            _issueLoadFailed = 0;
            _issueLoadStep = Math.Max(1, _issueLoadTotal / 10);
        }

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
        Stop();
        _statusSection.ShowIssueLoadingCompleted(loadedIssues, failedIssues);
    }

    /// <inheritdoc />
    public void ShowSpacer()
    {
        Stop();
        _statusSection.ShowSpacer();
    }

    /// <summary>
    /// Starts a pending-operation animation or writes a static message when animation is unavailable.
    /// </summary>
    /// <param name="message">Pending operation description.</param>
    public void ShowPending(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        Stop();

        if (!CanAnimatePendingLoader())
        {
            AnsiConsole.MarkupLine($"[grey]{Markup.Escape(message)}[/]");
            return;
        }

        StartPendingLoader(() => $"[grey]{Markup.Escape(message)}[/]");
    }

    /// <summary>
    /// Stops the active progress or pending-operation animation.
    /// </summary>
    public void Stop()
    {
        CancellationTokenSource? cancellation;
        Task? task;

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
            task?.GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
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

    private void UpdateIssueLoadProgress(bool wasFailure)
    {
        lock (_pendingLoaderSync)
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

    private void StartPendingLoader(Func<string> messageFactory)
    {
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
                    await Task.Delay(120, cancellation.Token).ConfigureAwait(false);
                }
            }, cancellation.Token);
        }
    }

    private static bool CanAnimatePendingLoader() =>
        !Console.IsOutputRedirected && AnsiConsole.Console.GetType().Name != "TestConsole";

    private static readonly char[] _pendingLoaderFrames = ['|', '/', '-', '\\'];
    private readonly Lock _pendingLoaderSync = new();
    private readonly SpectreStatusSection _statusSection;
    private CancellationTokenSource? _pendingLoaderCancellation;
    private Task? _pendingLoaderTask;
    private int _issueLoadTotal;
    private int _issueLoadProcessed;
    private int _issueLoadFailed;
    private int _issueLoadStep = 1;
}
