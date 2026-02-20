using FluentAssertions;

using JiraMetrics.Presentation.Pdf;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace JiraMetrics.Tests.Presentation.Pdf;

public sealed class PdfReportFileStoreTests
{
    [Fact(DisplayName = "Save throws when output path is empty")]
    [Trait("Category", "Unit")]
    public void SaveWhenOutputPathIsEmptyThrowsArgumentException()
    {
        // Arrange
        var service = new PdfReportFileStore();
        var document = CreateDocument("hello");

        // Act
        Action act = () => service.Save(" ", document);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact(DisplayName = "Save throws when document is null")]
    [Trait("Category", "Unit")]
    public void SaveWhenDocumentIsNullThrowsArgumentNullException()
    {
        // Arrange
        var service = new PdfReportFileStore();

        // Act
        Action act = () => service.Save("report.pdf", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("document");
    }

    [Fact(DisplayName = "Save creates output directory and writes PDF bytes")]
    [Trait("Category", "Integration")]
    public void SaveWhenCalledWritesPdfFile()
    {
        // Arrange
        QuestPDF.Settings.License = LicenseType.Community;

        using var tempDirectory = new TempDirectory();
        var outputPath = Path.Combine(tempDirectory.Path, "nested", "report.pdf");
        var service = new PdfReportFileStore();
        var document = CreateDocument("Generated report");

        // Act
        service.Save(outputPath, document);

        // Assert
        File.Exists(outputPath).Should().BeTrue();
        new FileInfo(outputPath).Length.Should().BeGreaterThan(0);
    }

    private static Document CreateDocument(string text)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            _ = container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(10);
                _ = page.Content().Text(text);
            });
        });
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"jfi-tests-{Guid.NewGuid():N}");
            _ = Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
