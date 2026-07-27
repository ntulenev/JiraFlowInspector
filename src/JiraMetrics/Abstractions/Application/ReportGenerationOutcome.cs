namespace JiraMetrics.Abstractions.Application;

/// <summary>
/// Describes whether all configured report outputs were generated successfully.
/// </summary>
public enum ReportGenerationOutcome
{
    /// <summary>
    /// Every configured report renderer completed successfully.
    /// </summary>
    Succeeded,

    /// <summary>
    /// At least one configured report renderer failed.
    /// </summary>
    Failed
}
