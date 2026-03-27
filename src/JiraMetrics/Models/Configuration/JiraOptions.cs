using System.ComponentModel.DataAnnotations;

using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Models.Configuration;

/// <summary>
/// Raw configuration options bound from the <c>Jira</c> section in <c>appsettings.json</c>.
/// </summary>
public sealed class JiraOptions : IValidatableObject
{
    /// <summary>
    /// Gets or sets Jira base URL.
    /// </summary>
    [Required]
    public required Uri BaseUrl { get; init; }

    /// <summary>
    /// Gets or sets Jira account email.
    /// </summary>
    [Required]
    [MinLength(1)]
    public required string Email { get; init; }

    /// <summary>
    /// Gets or sets Jira API token.
    /// </summary>
    [Required]
    [MinLength(1)]
    public required string ApiToken { get; init; }

    /// <summary>
    /// Gets or sets team-tasks analytics settings.
    /// </summary>
    [Required]
    public required TeamTasksOptions TeamTasks { get; init; }

    /// <summary>
    /// Gets or sets month label.
    /// </summary>
    [RegularExpression(@"^\d{4}-\d{2}$")]
    public string? MonthLabel { get; init; }

    /// <summary>
    /// Gets or sets explicit report start date in dd.MM.yyyy or yyyy-MM-dd format.
    /// </summary>
    public string? From { get; init; }

    /// <summary>
    /// Gets or sets explicit report end date in dd.MM.yyyy or yyyy-MM-dd format.
    /// </summary>
    public string? To { get; init; }

    /// <summary>
    /// Gets or sets optional lower bound for issue creation date in yyyy-MM-dd format.
    /// </summary>
    [RegularExpression(@"^\d{4}-\d{2}-\d{2}$")]
    public string? CreatedAfter { get; init; }

    /// <summary>
    /// Gets or sets whether all time calculations should be shown strictly in hours.
    /// </summary>
    public bool ShowTimeCalculationsInHoursOnly { get; init; }

    /// <summary>
    /// Gets or sets number of retries for transient Jira API errors.
    /// </summary>
    [Range(0, 10)]
    public int RetryCount { get; init; }

    /// <summary>
    /// Gets or sets pull request field name or id used for code-activity detection.
    /// </summary>
    public string? PullRequestFieldName { get; init; }

    /// <summary>
    /// Gets or sets optional release report settings.
    /// </summary>
    public ReleaseReportOptions? ReleaseReport { get; init; }

    /// <summary>
    /// Gets or sets optional architecture tasks report settings.
    /// </summary>
    public ArchTasksReportOptions? ArchTasks { get; init; }

    /// <summary>
    /// Gets or sets optional global incidents report settings.
    /// </summary>
    public GlobalIncidentsReportOptions? GlobalIncidents { get; init; }

    /// <summary>
    /// Gets or sets PDF report settings.
    /// </summary>
    public PdfOptions Pdf { get; init; } = new();

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var hasMonthLabel = !string.IsNullOrWhiteSpace(MonthLabel);
        var hasFrom = !string.IsNullOrWhiteSpace(From);
        var hasTo = !string.IsNullOrWhiteSpace(To);

        if (hasMonthLabel && (hasFrom || hasTo))
        {
            yield return new ValidationResult(
                "Use either MonthLabel or From/To, but not both.",
                [nameof(MonthLabel), nameof(From), nameof(To)]);
        }

        if (hasFrom != hasTo)
        {
            yield return new ValidationResult(
                "Both From and To must be provided together.",
                [nameof(From), nameof(To)]);
            yield break;
        }

        if (!hasFrom)
        {
            yield break;
        }

        if (!ReportPeriod.TryParseConfiguredDate(From, out var fromDate))
        {
            yield return new ValidationResult(
                "From must match dd.MM.yyyy or yyyy-MM-dd format.",
                [nameof(From)]);
        }

        if (!ReportPeriod.TryParseConfiguredDate(To, out var toDate))
        {
            yield return new ValidationResult(
                "To must match dd.MM.yyyy or yyyy-MM-dd format.",
                [nameof(To)]);
        }

        if (ReportPeriod.TryParseConfiguredDate(From, out fromDate)
            && ReportPeriod.TryParseConfiguredDate(To, out toDate)
            && fromDate > toDate)
        {
            yield return new ValidationResult(
                "From must be less than or equal to To.",
                [nameof(From), nameof(To)]);
        }
    }
}
