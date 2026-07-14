namespace JiraMetrics.Presentation.Html;

/// <summary>
/// Contains rendered HTML and client-side values for one table cell.
/// </summary>
/// <param name="Html">Rendered cell content.</param>
/// <param name="SortValue">Client-side sort value.</param>
/// <param name="FilterValue">Client-side filter value.</param>
internal sealed record TableCell(string Html, string SortValue, string FilterValue);
