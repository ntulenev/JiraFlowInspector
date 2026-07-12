using System.ComponentModel;
using System.Text;

using FluentAssertions;

using JiraMetrics.Presentation.Html;

namespace JiraMetrics.Tests.Presentation.Html;

public sealed class HtmlReportInfrastructureTests
{
    [Fact(DisplayName = "Save creates nested directories and writes UTF-8 without BOM")]
    [Trait("Category", "Integration")]
    public void SaveWhenDirectoryDoesNotExistCreatesFileWithoutBom()
    {
        // Arrange
        var root = CreateTemporaryDirectory();
        var outputPath = Path.Combine(root, "nested", "report.html");
        const string html = "<h1>Отчёт</h1>";
        var sut = new HtmlReportFileStore();

        try
        {
            // Act
            sut.Save(outputPath, html);

            // Assert
            File.ReadAllText(outputPath, Encoding.UTF8).Should().Be(html);
            var bytes = File.ReadAllBytes(outputPath);
            bytes.AsSpan().StartsWith(Encoding.UTF8.Preamble).Should().BeFalse();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact(DisplayName = "Save overwrites an existing report")]
    [Trait("Category", "Integration")]
    public void SaveWhenFileExistsOverwritesItsContents()
    {
        // Arrange
        var root = CreateTemporaryDirectory();
        var outputPath = Path.Combine(root, "report.html");
        File.WriteAllText(outputPath, "old content");
        var sut = new HtmlReportFileStore();

        try
        {
            // Act
            sut.Save(outputPath, "new content");

            // Assert
            File.ReadAllText(outputPath).Should().Be("new content");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact(DisplayName = "Save validates path and HTML arguments")]
    [Trait("Category", "Unit")]
    public void SaveWhenArgumentIsInvalidThrowsArgumentException()
    {
        // Arrange
        var sut = new HtmlReportFileStore();

        // Act
        Action emptyPath = () => sut.Save(" ", "html");
        Action nullHtml = () => sut.Save("report.html", null!);

        // Assert
        emptyPath.Should().Throw<ArgumentException>();
        nullHtml.Should().Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Open validates the report path")]
    [Trait("Category", "Unit")]
    public void OpenWhenPathIsEmptyThrowsArgumentException()
    {
        // Arrange
        var sut = new HtmlReportLauncher();

        // Act
        Action act = () => sut.Open(" ");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Open propagates shell error for a missing report")]
    [Trait("Category", "Integration")]
    public void OpenWhenReportDoesNotExistThrowsWin32Exception()
    {
        // Arrange
        var missingPath = Path.Combine(
            Path.GetTempPath(),
            $"JiraMetrics-{Guid.NewGuid():N}",
            "missing-report.html");
        var sut = new HtmlReportLauncher();

        // Act
        Action act = () => sut.Open(missingPath);

        // Assert
        act.Should().Throw<Win32Exception>();
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"JiraMetrics.Tests-{Guid.NewGuid():N}");
        _ = Directory.CreateDirectory(path);
        return path;
    }
}
