namespace JiraMetrics.Presentation.Html;

/// <summary>
/// Describes one interactive HTML report table column.
/// </summary>
/// <param name="Header">Displayed column heading.</param>
/// <param name="SortType">Client-side sort value type.</param>
/// <param name="FilterPlaceholder">Filter control placeholder.</param>
/// <param name="CssClass">Optional column CSS class.</param>
/// <param name="FilterKind">Client-side filter control kind.</param>
internal sealed record TableColumn(
    string Header,
    string SortType,
    string FilterPlaceholder,
    string? CssClass = null,
    string FilterKind = "text");
