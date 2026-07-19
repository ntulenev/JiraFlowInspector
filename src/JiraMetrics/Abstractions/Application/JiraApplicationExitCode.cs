namespace JiraMetrics.Abstractions.Application;

/// <summary>
/// Defines process exit codes produced by the Jira analytics application.
/// </summary>
public enum JiraApplicationExitCode
{
    /// <summary>
    /// The application completed successfully.
    /// </summary>
    Success = 0,

    /// <summary>
    /// Data required to generate the report could not be loaded.
    /// </summary>
    ReportLoadFailed = 1,

    /// <summary>
    /// Application execution was canceled while the host was stopping.
    /// </summary>
    Canceled = 2
}
