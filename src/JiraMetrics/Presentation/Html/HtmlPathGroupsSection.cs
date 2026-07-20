using System.Globalization;
using System.Text;

using JiraMetrics.Models;

using static JiraMetrics.Presentation.Html.HtmlTableRenderer;

namespace JiraMetrics.Presentation.Html;

/// <summary>
/// Renders transition path summary and detail HTML sections.
/// </summary>
internal sealed class HtmlPathGroupsSection : IHtmlReportSection
{
    /// <inheritdoc />
    public string Compose(JiraReportData reportData)
    {
        var html = new StringBuilder();
        _ = html.Append(BuildPathSummaryTable(reportData.Transitions.PathSummary));
        _ = html.Append(BuildPathGroupsTable(reportData));
        return html.ToString();
    }

    private static string BuildPathSummaryTable(PathGroupsSummary summary) =>
        BuildTableSection(
            "path-summary",
            "Path Groups Summary",
            "No path summary available.",
            MetricColumns,
            [
                BuildMetricRow("Successful", summary.SuccessfulCount.Value),
                BuildMetricRow("Matched stage", summary.MatchedStageCount.Value),
                BuildMetricRow("Failed", summary.FailedCount.Value),
                BuildMetricRow("Path groups", summary.PathGroupCount.Value)
            ],
            defaultSortColumn: 0,
            compact: true);

    private static string BuildPathGroupsTable(JiraReportData reportData)
    {
        var columns = new[]
        {
            new TableColumn("#", "number", "#", "narrow"),
            new TableColumn("Issues", "number", "Issues"),
            new TableColumn("TTM 75P", "number", "TTM 75P"),
            new TableColumn("Transition Len", "number", "Transition Len")
        };
        var html = new StringBuilder();
        _ = html.AppendLine("<section class=\"table-section\" id=\"path-groups\">");
        _ = html.AppendLine("  <div class=\"section-header\"><h2>Path Groups</h2></div>");
        _ = html.AppendLine("  <div class=\"table-panel\" data-table-panel>");
        _ = html.AppendLine("    <div class=\"table-controls\">");
        _ = html.AppendLine("      <input class=\"search\" data-table-search type=\"search\" placeholder=\"Search this table\">");
        _ = html.AppendLine("      <button class=\"button\" data-table-reset type=\"button\">Reset Filters</button>");
        _ = html.AppendLine("    </div>");
        _ = html.AppendLine("    <div class=\"table-wrap\"><div class=\"scroll\">");
        _ = html.AppendLine("      <table class=\"report-table path-table\" data-default-sort-column=\"1\" data-default-sort-direction=\"desc\">");
        _ = html.AppendLine("        <thead><tr>");

        for (var columnIndex = 0; columnIndex < columns.Length; columnIndex++)
        {
            var column = columns[columnIndex];
            var thClass = string.IsNullOrWhiteSpace(column.CssClass) ? string.Empty : $" class=\"{HtmlPresentationHelpers.EncodeAttribute(column.CssClass)}\"";
            _ = html.AppendLine(string.Concat(
                "          <th",
                thClass,
                "><button class=\"th-button\" data-sort-column=\"",
                columnIndex.ToString(CultureInfo.InvariantCulture),
                "\" data-sort-type=\"",
                HtmlPresentationHelpers.EncodeAttribute(column.SortType),
                "\" type=\"button\"><span>",
                HtmlPresentationHelpers.Encode(column.Header),
                "</span><span class=\"sort-indicator\"></span></button></th>"));
        }

        _ = html.AppendLine("        </tr><tr class=\"filters\">");
        foreach (var column in columns)
        {
            var thClass = string.IsNullOrWhiteSpace(column.CssClass) ? string.Empty : $" class=\"{HtmlPresentationHelpers.EncodeAttribute(column.CssClass)}\"";
            _ = html.AppendLine(string.Concat(
                "          <th",
                thClass,
                "><input class=\"filter-input\" data-filter-column placeholder=\"",
                HtmlPresentationHelpers.EncodeAttribute(column.FilterPlaceholder),
                "\" type=\"search\"></th>"));
        }

        _ = html.AppendLine("        </tr></thead><tbody>");
        if (reportData.Transitions.PathGroups.Count == 0)
        {
            _ = html.AppendLine("          <tr class=\"empty\"><td class=\"empty-cell\" colspan=\"4\">No path groups.</td></tr>");
        }
        else
        {
            for (var index = 0; index < reportData.Transitions.PathGroups.Count; index++)
            {
                var group = reportData.Transitions.PathGroups[index];
                var groupNumber = index + 1;
                var filterValue = string.Join(
                    " ",
                    new[] { group.PathLabel.Value }.Concat(group.Issues.Select(static issue => issue.Key.Value)));
                _ = html.AppendLine("          <tr class=\"path-group-row\" data-toggle-detail>");
                _ = html.AppendLine(string.Concat(
                    "            <td data-sort='",
                    groupNumber.ToString(CultureInfo.InvariantCulture),
                    "' data-filter='",
                    HtmlPresentationHelpers.EncodeAttribute(filterValue),
                    "'><button class=\"row-toggle\" type=\"button\" aria-label=\"Toggle path group details\">+</button> ",
                    groupNumber.ToString(CultureInfo.InvariantCulture),
                    "</td>"));
                _ = html.AppendLine(string.Concat("            <td data-sort='", group.Issues.Count.ToString(CultureInfo.InvariantCulture), "' data-filter='", HtmlPresentationHelpers.EncodeAttribute(filterValue), "'>", group.Issues.Count.ToString(CultureInfo.InvariantCulture), "</td>"));
                _ = html.AppendLine(string.Concat("            <td data-sort='", group.TotalP75.TotalMinutes.ToString(CultureInfo.InvariantCulture), "' data-filter='", HtmlPresentationHelpers.EncodeAttribute(filterValue), "'>", HtmlPresentationHelpers.Encode(PresentationFormatting.ToDurationLabel(group.TotalP75, reportData.Settings.ShowTimeCalculationsInHoursOnly)), "</td>"));
                _ = html.AppendLine(string.Concat("            <td data-sort='", group.P75Transitions.Count.ToString(CultureInfo.InvariantCulture), "' data-filter='", HtmlPresentationHelpers.EncodeAttribute(filterValue), "'>", group.P75Transitions.Count.ToString(CultureInfo.InvariantCulture), "</td>"));
                _ = html.AppendLine("          </tr>");
                _ = html.AppendLine("          <tr class=\"detail-row\" hidden><td colspan=\"4\">");
                _ = html.Append(BuildPathGroupDetails(group, reportData));
                _ = html.AppendLine("          </td></tr>");
            }
        }

        _ = html.AppendLine("        </tbody></table>");
        _ = html.AppendLine("    </div></div>");
        _ = html.AppendLine("  </div>");
        _ = html.AppendLine("</section>");
        return html.ToString();
    }

    private static string BuildPathGroupDetails(PathGroup group, JiraReportData reportData)
    {
        var html = new StringBuilder();
        var maxDuration = group.P75Transitions.Count == 0
            ? 0
            : group.P75Transitions.Max(static transition => Math.Max(0.001, transition.P75Duration.TotalMinutes));

        _ = html.AppendLine("            <div class=\"path-detail\">");
        _ = html.AppendLine(string.Concat("              <div class=\"path-label\">", HtmlPresentationHelpers.Encode(group.PathLabel.Value), "</div>"));
        _ = html.AppendLine("              <div class=\"path-detail-grid\">");
        _ = html.AppendLine("                <div>");
        _ = html.AppendLine("                  <h3>Tasks</h3>");
        _ = html.AppendLine("                  <div class=\"detail-list\">");
        foreach (var issue in group.Issues.OrderBy(static issue => issue.Key.Value, StringComparer.OrdinalIgnoreCase))
        {
            var issueUrl = HtmlPresentationHelpers.BuildIssueBrowseUrl(reportData.Settings.BaseUrl, issue.Key);
            _ = html.AppendLine(string.Concat(
                "                    <div><a href=\"",
                HtmlPresentationHelpers.EncodeAttribute(issueUrl),
                "\" target=\"_blank\" rel=\"noreferrer\">",
                HtmlPresentationHelpers.Encode(issue.Key.Value),
                "</a><span>",
                HtmlPresentationHelpers.Encode(issue.IssueType.Value),
                "</span><span>",
                HtmlPresentationHelpers.Encode(issue.Summary.Value),
                "</span></div>"));
        }

        _ = html.AppendLine("                  </div>");
        _ = html.AppendLine("                </div>");
        _ = html.AppendLine("                <div>");
        _ = html.AppendLine("                  <h3>Transitions</h3>");
        if (group.P75Transitions.Count == 0)
        {
            _ = html.AppendLine("                  <p class=\"empty-section\">No transitions in this path.</p>");
        }
        else
        {
            _ = html.AppendLine("                  <div class=\"transition-bars\">");
            foreach (var transition in group.P75Transitions)
            {
                var duration = transition.P75Duration < TimeSpan.Zero ? TimeSpan.Zero : transition.P75Duration;
                var width = maxDuration <= 0
                    ? 0
                    : Math.Clamp(duration.TotalMinutes / maxDuration * 100, 4, 100);
                _ = html.AppendLine("                    <div class=\"transition-bar-row\">");
                _ = html.AppendLine(string.Concat(
                    "                      <div class=\"transition-label\">",
                    HtmlPresentationHelpers.Encode(transition.From.Value),
                    " -> ",
                    HtmlPresentationHelpers.Encode(transition.To.Value),
                    "</div>"));
                _ = html.AppendLine("                      <div class=\"bar-track\">");
                _ = html.AppendLine(string.Concat(
                    "                        <div class=\"bar-fill\" style=\"width:",
                    width.ToString("0.##", CultureInfo.InvariantCulture),
                    "%\"></div>"));
                _ = html.AppendLine("                      </div>");
                _ = html.AppendLine(string.Concat(
                    "                      <div class=\"duration-value\">",
                    HtmlPresentationHelpers.Encode(FormatDurationWithHours(duration, reportData.Settings.ShowTimeCalculationsInHoursOnly)),
                    "</div>"));
                _ = html.AppendLine("                    </div>");
            }

            _ = html.AppendLine("                  </div>");
        }

        _ = html.AppendLine("                </div>");
        _ = html.AppendLine("              </div>");
        _ = html.AppendLine("            </div>");
        return html.ToString();
    }

    private static string FormatDurationWithHours(TimeSpan duration, bool showTimeCalculationsInHoursOnly)
    {
        var durationLabel = PresentationFormatting.ToDurationLabel(duration, showTimeCalculationsInHoursOnly);
        var hoursLabel = duration.TotalHours.ToString("0.##", CultureInfo.InvariantCulture) + "h";
        return showTimeCalculationsInHoursOnly
            ? durationLabel
            : $"{durationLabel} ({hoursLabel})";
    }
}
