using System.Globalization;

using FluentAssertions;

using JiraMetrics.Models.Configuration;

namespace JiraMetrics.Tests.Configuration;

public sealed class PdfReportSettingsTests
{
    [Fact(DisplayName = "ResolveOutputPath resolves relative path to absolute path")]
    [Trait("Category", "Unit")]
    public void ResolveOutputPathWhenPathIsRelativeReturnsAbsolutePath()
    {
        // Arrange
        var settings = new PdfReportSettings(enabled: true, outputPath: Path.Combine("reports", "result.pdf"));
        var dateSuffix = DateTime.Now.ToString("dd_MM_yyyy", CultureInfo.InvariantCulture);
        var expected = Path.GetFullPath(
            Path.Combine("reports", $"result_{dateSuffix}.pdf"),
            Directory.GetCurrentDirectory());

        // Act
        var result = settings.ResolveOutputPath();

        // Assert
        result.Should().Be(expected);
    }

    [Fact(DisplayName = "ResolveOutputPath trims output path")]
    [Trait("Category", "Unit")]
    public void ResolveOutputPathWhenPathHasPaddingTrimsPath()
    {
        // Arrange
        var settings = new PdfReportSettings(enabled: true, outputPath: "  report.pdf  ");
        var dateSuffix = DateTime.Now.ToString("dd_MM_yyyy", CultureInfo.InvariantCulture);
        var expected = Path.GetFullPath(
            $"report_{dateSuffix}.pdf",
            Directory.GetCurrentDirectory());

        // Act
        var result = settings.ResolveOutputPath();

        // Assert
        result.Should().Be(expected);
    }

    [Fact(DisplayName = "ResolveOutputPath uses default path when output path is missing")]
    [Trait("Category", "Unit")]
    public void ResolveOutputPathWhenPathIsMissingUsesDefault()
    {
        // Arrange
        var settings = new PdfReportSettings(enabled: true, outputPath: " ");
        var dateSuffix = DateTime.Now.ToString("dd_MM_yyyy", CultureInfo.InvariantCulture);
        var expected = Path.GetFullPath(
            $"jiraflowinspector-report_{dateSuffix}.pdf",
            Directory.GetCurrentDirectory());

        // Act
        var result = settings.ResolveOutputPath();

        // Assert
        result.Should().Be(expected);
    }
}
