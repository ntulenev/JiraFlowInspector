namespace JiraMetrics.Abstractions.Application;

/// <summary>
/// Runs the Jira analytics application workflow.
/// </summary>
public interface IJiraApplication
{
    /// <summary>
    /// Executes the application workflow.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Asynchronous operation result.</returns>
    Task RunAsync(CancellationToken cancellationToken = default);
}

