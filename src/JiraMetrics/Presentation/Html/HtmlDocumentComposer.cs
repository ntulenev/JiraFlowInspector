using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using JiraMetrics.Models;

namespace JiraMetrics.Presentation.Html;

/// <summary>
/// Composes the complete HTML report document from embedded templates and report content.
/// </summary>
internal static partial class HtmlDocumentComposer
{
    public static string Compose(JiraReportData reportData, string contentHtml)
    {
        ArgumentNullException.ThrowIfNull(reportData);
        ArgumentNullException.ThrowIfNull(contentHtml);

        return ApplyTemplate(
            HtmlTemplateLoader.LoadReportTemplate(),
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["__PROJECT__"] = HtmlPresentationHelpers.Encode(reportData.Settings.ProjectKey.Value),
                ["__GENERATED_AT__"] = HtmlPresentationHelpers.Encode(
                    reportData.RunContext.GeneratedAt.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture)),
                ["__PERIOD__"] = HtmlPresentationHelpers.Encode(reportData.Settings.ReportPeriod.Label),
                ["__DONE_STATUS__"] = HtmlPresentationHelpers.Encode(reportData.Settings.DoneStatusName.Value),
                ["__SEARCH_ISSUES__"] = reportData.Source.SearchIssueCount.Value.ToString(CultureInfo.InvariantCulture),
                ["__DONE_ISSUES__"] = reportData.Transitions.DoneIssues.Count.ToString(CultureInfo.InvariantCulture),
                ["__REJECTED_ISSUES__"] = reportData.Transitions.RejectedIssues.Count.ToString(CultureInfo.InvariantCulture),
                ["__PATH_GROUPS__"] = reportData.Transitions.PathSummary.PathGroupCount.Value.ToString(CultureInfo.InvariantCulture),
                ["__FAILED_ISSUES__"] = reportData.Failures.Count.ToString(CultureInfo.InvariantCulture),
                ["__NAV__"] = BuildNavigation(contentHtml),
                ["__CONTENT__"] = contentHtml,
                ["__STYLES__"] = HtmlTemplateLoader.LoadReportStyles(),
                ["__SCRIPT__"] = HtmlTemplateLoader.LoadReportScript()
            });
    }

    private static string ApplyTemplate(string template, IReadOnlyDictionary<string, string> tokens)
    {
        var result = template;
        foreach (var token in tokens)
        {
            result = result.Replace(token.Key, token.Value, StringComparison.Ordinal);
        }

        return result;
    }

    private static string BuildNavigation(string contentHtml)
    {
        var sectionMatches = SectionHeadingRegex().Matches(contentHtml);
        if (sectionMatches.Count == 0)
        {
            return string.Empty;
        }

        var html = new StringBuilder();
        _ = html.AppendLine("<aside class=\"report-nav\" aria-label=\"Report sections\">");
        _ = html.AppendLine("  <div class=\"report-nav-title\">Sections</div>");
        _ = html.AppendLine("  <nav>");
        foreach (Match match in sectionMatches)
        {
            var sectionId = match.Groups["id"].Value;
            if (_qaChildSectionIds.Contains(sectionId))
            {
                continue;
            }

            if (string.Equals(sectionId, QA_PARENT_SECTION_ID, StringComparison.Ordinal))
            {
                _ = html.AppendLine("    <div class=\"report-nav-group\" data-nav-group=\"qa\">");
                AppendNavigationLink(html, match, "report-nav-parent", 6);
                _ = html.AppendLine("      <div class=\"report-nav-children\" aria-label=\"QA Snapshot subsections\">");
                foreach (Match childMatch in sectionMatches)
                {
                    if (_qaChildSectionIds.Contains(childMatch.Groups["id"].Value))
                    {
                        AppendNavigationLink(html, childMatch, "report-nav-child", 8);
                    }
                }

                _ = html.AppendLine("      </div>");
                _ = html.AppendLine("    </div>");
                continue;
            }

            AppendNavigationLink(html, match, cssClass: null, indentation: 4);
        }

        _ = html.AppendLine("  </nav>");
        _ = html.AppendLine("</aside>");
        return html.ToString();
    }

    private static void AppendNavigationLink(
        StringBuilder html,
        Match sectionMatch,
        string? cssClass,
        int indentation)
    {
        var sectionId = sectionMatch.Groups["id"].Value;
        var title = TagRegex().Replace(sectionMatch.Groups["title"].Value, string.Empty).Trim();
        var classAttribute = cssClass is null
            ? string.Empty
            : string.Concat(" class=\"", cssClass, "\"");

        _ = html.Append(' ', indentation).AppendLine(string.Concat(
            "<a",
            classAttribute,
            " href=\"#",
            HtmlPresentationHelpers.EncodeAttribute(sectionId),
            "\">",
            HtmlPresentationHelpers.Encode(title),
            "</a>"));
    }

    private const string QA_PARENT_SECTION_ID = "qa-summary";

    private static readonly HashSet<string> _qaChildSectionIds = new(StringComparer.Ordinal)
    {
        "qa-pickup-summary",
        "qa-pickup-75",
        "qa-testing-issues",
        "qa-testing-75",
        "qa-hold-summary",
        "qa-hold-issues",
        "qa-hold-75",
        "test-coverage"
    };

    [GeneratedRegex("<section\\s+class=\"[^\"]*table-section[^\"]*\"\\s+id=\"(?<id>[^\"]+)\">\\s*<div\\s+class=\"section-header\"><h2>(?<title>.*?)</h2></div>", RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex SectionHeadingRegex();

    [GeneratedRegex("<.*?>", RegexOptions.CultureInvariant)]
    private static partial Regex TagRegex();
}
