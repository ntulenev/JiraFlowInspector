namespace JiraMetrics.Models;

/// <summary>
/// Represents the outcome of loading data required for report analysis.
/// </summary>
internal abstract record ReportLoadResult
{
    private ReportLoadResult()
    {
    }

    /// <summary>
    /// Represents a successful report-data load.
    /// </summary>
    internal sealed record Success : ReportLoadResult
    {
        public Success(JiraApplicationReportData reportData)
        {
            ArgumentNullException.ThrowIfNull(reportData);
            ReportData = reportData;
        }

        public JiraApplicationReportData ReportData { get; }
    }

    /// <summary>
    /// Represents a failed report-data load.
    /// </summary>
    internal sealed record Failure : ReportLoadResult
    {
        private Failure()
        {
        }

        public static Failure Instance { get; } = new();
    }
}
