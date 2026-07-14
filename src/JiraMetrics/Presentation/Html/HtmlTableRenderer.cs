using System.Globalization;
using System.Text;

namespace JiraMetrics.Presentation.Html;

/// <summary>
/// Renders interactive report tables used by HTML report sections.
/// </summary>
internal static class HtmlTableRenderer
{
    public static TableCell BuildTextCell(string text, IFormattable? sortValue = null) =>
        new(
            HtmlPresentationHelpers.Encode(text),
            sortValue is null ? text : sortValue.ToString(null, CultureInfo.InvariantCulture),
            text);

    public static TableCell BuildLinkCell(string text, string url)
    {
        var encodedUrl = HtmlPresentationHelpers.EncodeAttribute(url);
        var encodedText = HtmlPresentationHelpers.Encode(text);
        return new TableCell(
            $"<a href=\"{encodedUrl}\" target=\"_blank\" rel=\"noreferrer\">{encodedText}</a>",
            text,
            text);
    }

    public static string BuildTableSection(
        string sectionId,
        string title,
        string emptyMessage,
        IReadOnlyList<TableColumn> columns,
        IReadOnlyList<TableRow> rows,
        int? defaultSortColumn,
        string defaultSortDirection = "asc",
        bool compact = false)
    {
        var containerClass = compact ? "table-section compact-section" : "table-section";
        var html = new StringBuilder();
        _ = html.AppendLine(string.Concat("<section class=\"", containerClass, "\" id=\"", HtmlPresentationHelpers.EncodeAttribute(sectionId), "\">"));
        _ = html.AppendLine(string.Concat("  <div class=\"section-header\"><h2>", HtmlPresentationHelpers.Encode(title), "</h2></div>"));
        _ = html.AppendLine("  <div class=\"table-panel\" data-table-panel>");
        _ = html.AppendLine("    <div class=\"table-controls\">");
        _ = html.AppendLine("      <input class=\"search\" data-table-search type=\"search\" placeholder=\"Search this table\">");
        _ = html.AppendLine("      <button class=\"button\" data-table-reset type=\"button\">Reset Filters</button>");
        _ = html.AppendLine("    </div>");
        _ = html.AppendLine("    <div class=\"table-wrap\"><div class=\"scroll\">");
        _ = html.AppendLine(string.Concat(
            "      <table class=\"report-table\" data-default-sort-column=\"",
            defaultSortColumn.HasValue ? defaultSortColumn.Value.ToString(CultureInfo.InvariantCulture) : string.Empty,
            "\" data-default-sort-direction=\"",
            HtmlPresentationHelpers.EncodeAttribute(defaultSortDirection),
            "\">"));
        _ = html.AppendLine("        <thead><tr>");

        for (var columnIndex = 0; columnIndex < columns.Count; columnIndex++)
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
        for (var columnIndex = 0; columnIndex < columns.Count; columnIndex++)
        {
            var column = columns[columnIndex];
            var thClass = string.IsNullOrWhiteSpace(column.CssClass) ? string.Empty : $" class=\"{HtmlPresentationHelpers.EncodeAttribute(column.CssClass)}\"";
            _ = html.Append(string.Concat("          <th", thClass, ">"));
            if (string.Equals(column.FilterKind, "date-range", StringComparison.Ordinal))
            {
                _ = html.Append(string.Concat(
                    "<div class=\"range-filter\"><input class=\"filter-input\" data-filter-column=\"",
                    columnIndex.ToString(CultureInfo.InvariantCulture),
                    "\" data-filter-operator=\"min\" aria-label=\"",
                    HtmlPresentationHelpers.EncodeAttribute(column.Header),
                    " from\" type=\"date\"><input class=\"filter-input\" data-filter-column=\"",
                    columnIndex.ToString(CultureInfo.InvariantCulture),
                    "\" data-filter-operator=\"max\" aria-label=\"",
                    HtmlPresentationHelpers.EncodeAttribute(column.Header),
                    " to\" type=\"date\"></div>"));
            }
            else if (string.Equals(column.FilterKind, "multi-select", StringComparison.Ordinal))
            {
                _ = html.Append(string.Concat(
                    "<div class=\"multi-select\" data-multi-select data-filter-column=\"",
                    columnIndex.ToString(CultureInfo.InvariantCulture),
                    "\" data-multi-select-placeholder=\"",
                    HtmlPresentationHelpers.EncodeAttribute(column.FilterPlaceholder),
                    "\"><button class=\"multi-select-button\" data-multi-select-toggle type=\"button\" aria-expanded=\"false\"><span data-multi-select-label>",
                    HtmlPresentationHelpers.Encode(column.FilterPlaceholder),
                    "</span><span class=\"multi-select-chevron\" aria-hidden=\"true\"></span></button><div class=\"multi-select-menu\" data-multi-select-menu hidden></div></div>"));
            }
            else
            {
                _ = html.Append(string.Concat(
                    "<input class=\"filter-input\" data-filter-column=\"",
                    columnIndex.ToString(CultureInfo.InvariantCulture),
                    "\" placeholder=\"",
                    HtmlPresentationHelpers.EncodeAttribute(column.FilterPlaceholder),
                    "\" type=\"search\">"));
            }

            _ = html.AppendLine("</th>");
        }

        _ = html.AppendLine("        </tr></thead><tbody>");
        if (rows.Count == 0)
        {
            _ = html.AppendLine(string.Concat(
                "          <tr class=\"empty\"><td class=\"empty-cell\" colspan=\"",
                columns.Count.ToString(CultureInfo.InvariantCulture),
                "\">",
                HtmlPresentationHelpers.Encode(emptyMessage),
                "</td></tr>"));
        }
        else
        {
            foreach (var row in rows)
            {
                var rowClass = string.IsNullOrWhiteSpace(row.CssClass) ? string.Empty : $" class=\"{HtmlPresentationHelpers.EncodeAttribute(row.CssClass)}\"";
                _ = html.AppendLine(string.Concat("          <tr", rowClass, ">"));
                foreach (var cell in row.Cells)
                {
                    _ = html.AppendLine(string.Concat(
                        "            <td data-sort='",
                        HtmlPresentationHelpers.EncodeAttribute(cell.SortValue),
                        "' data-filter='",
                        HtmlPresentationHelpers.EncodeAttribute(cell.FilterValue),
                        "'>",
                        cell.Html,
                        "</td>"));
                }

                _ = html.AppendLine("          </tr>");
            }
        }

        _ = html.AppendLine("        </tbody></table>");
        _ = html.AppendLine("    </div></div>");
        _ = html.AppendLine("  </div>");
        _ = html.AppendLine("</section>");
        return html.ToString();
    }
}

internal sealed record TableColumn(
    string Header,
    string SortType,
    string FilterPlaceholder,
    string? CssClass = null,
    string FilterKind = "text");

internal sealed record TableCell(string Html, string SortValue, string FilterValue);

internal sealed record TableRow(IReadOnlyList<TableCell> Cells, string? CssClass = null);
