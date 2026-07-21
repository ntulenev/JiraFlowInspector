using FluentAssertions;

using JiraMetrics.Models.Configuration;

namespace JiraMetrics.Tests.Configuration;

public sealed class PdfReportSettingsTests
{
    [Fact(DisplayName = "Constructor enables auto-open by default")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenOpenAfterGenerationIsNotProvidedUsesTrue()
    {
        // Arrange
        var settings = new PdfReportSettings();

        // Assert
        settings.OpenAfterGeneration.Should().BeTrue();
    }

    [Fact(DisplayName = "Constructor stores configured auto-open flag")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenOpenAfterGenerationIsProvidedStoresValue()
    {
        // Arrange
        var settings = new PdfReportSettings(enabled: true, outputPath: "report.pdf", openAfterGeneration: false);

        // Assert
        settings.OpenAfterGeneration.Should().BeFalse();
    }

    [Fact(DisplayName = "ResolveOutputPath resolves relative path to absolute path")]
    [Trait("Category", "Unit")]
    public void ResolveOutputPathWhenPathIsRelativeReturnsAbsolutePath()
    {
        // Arrange
        var settings = new PdfReportSettings(enabled: true, outputPath: Path.Combine("reports", "result.pdf"));
        var generatedAt = new DateTimeOffset(2026, 2, 3, 23, 59, 58, TimeSpan.FromHours(2));
        var expected = Path.GetFullPath(
            Path.Combine("reports", "result_03_02_2026.pdf"),
            Directory.GetCurrentDirectory());

        // Act
        var result = settings.ResolveOutputPath(generatedAt);

        // Assert
        result.Should().Be(expected);
    }

    [Fact(DisplayName = "ResolveOutputPath trims output path")]
    [Trait("Category", "Unit")]
    public void ResolveOutputPathWhenPathHasPaddingTrimsPath()
    {
        // Arrange
        var settings = new PdfReportSettings(enabled: true, outputPath: "  report.pdf  ");
        var generatedAt = new DateTimeOffset(2026, 2, 3, 23, 59, 58, TimeSpan.FromHours(2));
        var expected = Path.GetFullPath(
            "report_03_02_2026.pdf",
            Directory.GetCurrentDirectory());

        // Act
        var result = settings.ResolveOutputPath(generatedAt);

        // Assert
        result.Should().Be(expected);
    }

    [Fact(DisplayName = "ResolveOutputPath uses default path when output path is missing")]
    [Trait("Category", "Unit")]
    public void ResolveOutputPathWhenPathIsMissingUsesDefault()
    {
        // Arrange
        var settings = new PdfReportSettings(enabled: true, outputPath: " ");
        var generatedAt = new DateTimeOffset(2026, 2, 3, 23, 59, 58, TimeSpan.FromHours(2));
        var expected = Path.GetFullPath(
            "jiraflowinspector-report_03_02_2026.pdf",
            Directory.GetCurrentDirectory());

        // Act
        var result = settings.ResolveOutputPath(generatedAt);

        // Assert
        result.Should().Be(expected);
    }

    [Fact(DisplayName = "ResolveOutputPath prepends filename prefix")]
    [Trait("Category", "Unit")]
    public void ResolveOutputPathWhenPrefixIsProvidedPrependsPrefix()
    {
        // Arrange
        var settings = new PdfReportSettings(enabled: true, outputPath: Path.Combine("reports", "result.pdf"));
        var generatedAt = new DateTimeOffset(2026, 2, 3, 23, 59, 58, TimeSpan.FromHours(2));
        var expected = Path.GetFullPath(
            Path.Combine("reports", "CustomTransition_result_03_02_2026.pdf"),
            Directory.GetCurrentDirectory());

        // Act
        var result = settings.ResolveOutputPath(generatedAt, "CustomTransition");

        // Assert
        result.Should().Be(expected);
    }
}
