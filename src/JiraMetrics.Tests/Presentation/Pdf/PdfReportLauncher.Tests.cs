using FluentAssertions;

using JiraMetrics.Presentation.Pdf;

namespace JiraMetrics.Tests.Presentation.Pdf;

public sealed class PdfReportLauncherTests
{
    [Theory(DisplayName = "Open throws when path is null, empty, or whitespace")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [Trait("Category", "Unit")]
    public void OpenWhenPathIsInvalidThrowsArgumentException(string? pdfPath)
    {
        // Arrange
        var launcher = new PdfReportLauncher();

        // Act
        Action act = () => launcher.Open(pdfPath!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
