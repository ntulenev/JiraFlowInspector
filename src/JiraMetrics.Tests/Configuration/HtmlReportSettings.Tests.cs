using System.Globalization;

using FluentAssertions;

using JiraMetrics.Models.Configuration;

namespace JiraMetrics.Tests.Configuration;

public sealed class HtmlReportSettingsTests
{
    [Fact(DisplayName = "Constructor disables auto-open by default")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenOpenAfterGenerationIsNotProvidedUsesFalse()
    {
        // Arrange
        var settings = new HtmlReportSettings();

        // Assert
        settings.OpenAfterGeneration.Should().BeFalse();
    }

    [Fact(DisplayName = "ResolveOutputPath resolves relative path to absolute dated HTML path")]
    [Trait("Category", "Unit")]
    public void ResolveOutputPathWhenPathIsRelativeReturnsAbsoluteDatedHtmlPath()
    {
        // Arrange
        var settings = new HtmlReportSettings(enabled: true, outputPath: Path.Combine("reports", "result.html"));
        var dateSuffix = DateTime.Now.ToString("dd_MM_yyyy", CultureInfo.InvariantCulture);
        var expected = Path.GetFullPath(
            Path.Combine("reports", $"result_{dateSuffix}.html"),
            Directory.GetCurrentDirectory());

        // Act
        var result = settings.ResolveOutputPath();

        // Assert
        result.Should().Be(expected);
    }

    [Fact(DisplayName = "ResolveOutputPath appends HTML extension when missing")]
    [Trait("Category", "Unit")]
    public void ResolveOutputPathWhenExtensionIsMissingAppendsHtmlExtension()
    {
        // Arrange
        var settings = new HtmlReportSettings(enabled: true, outputPath: Path.Combine("reports", "result"));
        var dateSuffix = DateTime.Now.ToString("dd_MM_yyyy", CultureInfo.InvariantCulture);
        var expected = Path.GetFullPath(
            Path.Combine("reports", $"result_{dateSuffix}.html"),
            Directory.GetCurrentDirectory());

        // Act
        var result = settings.ResolveOutputPath();

        // Assert
        result.Should().Be(expected);
    }
}
