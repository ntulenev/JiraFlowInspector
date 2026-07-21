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
        var generatedAt = new DateTimeOffset(2026, 2, 3, 23, 59, 58, TimeSpan.FromHours(2));
        var expected = Path.GetFullPath(
            Path.Combine("reports", "result_03_02_2026.html"),
            Directory.GetCurrentDirectory());

        // Act
        var result = settings.ResolveOutputPath(generatedAt);

        // Assert
        result.Should().Be(expected);
    }

    [Fact(DisplayName = "ResolveOutputPath appends HTML extension when missing")]
    [Trait("Category", "Unit")]
    public void ResolveOutputPathWhenExtensionIsMissingAppendsHtmlExtension()
    {
        // Arrange
        var settings = new HtmlReportSettings(enabled: true, outputPath: Path.Combine("reports", "result"));
        var generatedAt = new DateTimeOffset(2026, 2, 3, 23, 59, 58, TimeSpan.FromHours(2));
        var expected = Path.GetFullPath(
            Path.Combine("reports", "result_03_02_2026.html"),
            Directory.GetCurrentDirectory());

        // Act
        var result = settings.ResolveOutputPath(generatedAt);

        // Assert
        result.Should().Be(expected);
    }
}
