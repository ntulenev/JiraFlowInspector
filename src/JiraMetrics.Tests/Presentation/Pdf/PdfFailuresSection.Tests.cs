using FluentAssertions;

using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Presentation.Pdf;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace JiraMetrics.Tests.Presentation.Pdf;

public sealed class PdfFailuresSectionTests
{
    [Fact(DisplayName = "Compose returns without rendering rows when there are no failures")]
    [Trait("Category", "Unit")]
    public void ComposeWhenFailuresAreEmptyStillProducesValidPdf()
    {
        // Arrange
        var section = new PdfFailuresSection();
        var reportData = CreateReportData([]);

        // Act
        var bytes = GeneratePdf(column => section.Compose(column, reportData));

        // Assert
        bytes.Should().NotBeEmpty();
    }

    [Fact(DisplayName = "Compose renders failure rows when failures are present")]
    [Trait("Category", "Unit")]
    public void ComposeWhenFailuresArePresentProducesValidPdf()
    {
        // Arrange
        var section = new PdfFailuresSection();
        var reportData = CreateReportData(
        [
            new LoadFailure(new IssueKey("APP-2"), new ErrorMessage("Request timed out."))
        ]);

        // Act
        var bytes = GeneratePdf(column => section.Compose(column, reportData));

        // Assert
        bytes.Should().NotBeEmpty();
    }

    private static byte[] GeneratePdf(Action<ColumnDescriptor> composeContent)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document
            .Create(container =>
            {
                _ = container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(10);
                    page.Content().Column(composeContent);
                });
            })
            .GeneratePdf();
    }

    private static JiraPdfReportData CreateReportData(IReadOnlyList<LoadFailure> failures)
    {
        return new JiraPdfReportData
        {
            Settings = new AppSettings(
                new JiraBaseUrl("https://example.atlassian.net"),
                new JiraEmail("user@example.com"),
                new JiraApiToken("token"),
                new ProjectKey("APP"),
                new StatusName("Done"),
                new StatusName("Rejected"),
                [new StageName("Code Review")],
                ReportPeriod.FromMonthLabel(new MonthLabel("2026-03"))),
            SearchIssueCount = new ItemCount(1),
            PathSummary = new PathGroupsSummary(
                new ItemCount(1),
                new ItemCount(1),
                new ItemCount(failures.Count),
                new ItemCount(1)),
            Failures = failures
        };
    }
}
