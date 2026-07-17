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
                    DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture)),
                ["__PERIOD__"] = HtmlPresentationHelpers.Encode(reportData.Settings.ReportPeriod.Label),
                ["__DONE_STATUS__"] = HtmlPresentationHelpers.Encode(reportData.Settings.DoneStatusName.Value),
                ["__SEARCH_ISSUES__"] = reportData.Source.SearchIssueCount.Value.ToString(CultureInfo.InvariantCulture),
                ["__DONE_ISSUES__"] = reportData.DoneIssues.Count.ToString(CultureInfo.InvariantCulture),
                ["__REJECTED_ISSUES__"] = reportData.RejectedIssues.Count.ToString(CultureInfo.InvariantCulture),
                ["__PATH_GROUPS__"] = reportData.PathSummary.PathGroupCount.Value.ToString(CultureInfo.InvariantCulture),
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
            var title = TagRegex().Replace(match.Groups["title"].Value, string.Empty).Trim();
            _ = html.AppendLine(string.Concat(
                "    <a href=\"#",
                HtmlPresentationHelpers.EncodeAttribute(sectionId),
                "\">",
                HtmlPresentationHelpers.Encode(title),
                "</a>"));
        }

        _ = html.AppendLine("  </nav>");
        _ = html.AppendLine("</aside>");
        return html.ToString();
    }

    [GeneratedRegex("<section\\s+class=\"[^\"]*table-section[^\"]*\"\\s+id=\"(?<id>[^\"]+)\">\\s*<div\\s+class=\"section-header\"><h2>(?<title>.*?)</h2></div>", RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex SectionHeadingRegex();

    [GeneratedRegex("<.*?>", RegexOptions.CultureInvariant)]
    private static partial Regex TagRegex();
}
