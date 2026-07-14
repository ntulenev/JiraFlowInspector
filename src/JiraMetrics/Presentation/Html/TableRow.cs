namespace JiraMetrics.Presentation.Html;

/// <summary>
/// Contains cells and optional styling for one report table row.
/// </summary>
/// <param name="Cells">Ordered row cells.</param>
/// <param name="CssClass">Optional row CSS class.</param>
internal sealed record TableRow(IReadOnlyList<TableCell> Cells, string? CssClass = null);
