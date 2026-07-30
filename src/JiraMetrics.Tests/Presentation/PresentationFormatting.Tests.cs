using FluentAssertions;

using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Presentation;

namespace JiraMetrics.Tests.Presentation;

public sealed class PresentationFormattingTests
{
    [Fact(DisplayName = "BuildIssueBrowseUrl builds one shared Jira issue URL")]
    public void BuildIssueBrowseUrlWhenCalledBuildsExpectedUrl()
    {
        var result = PresentationFormatting.BuildIssueBrowseUrl(
            new JiraBaseUrl("https://example.atlassian.net"),
            new IssueKey("APP-42"));

        result.Should().Be("https://example.atlassian.net/browse/APP-42");
    }

    [Fact(DisplayName = "Optional local date formatting uses a dash for missing values")]
    public void FormatLocalDateAndTimeWhenValueIsMissingReturnsDash()
    {
        PresentationFormatting.FormatLocalDateTime(null).Should().Be("-");
        PresentationFormatting.FormatLocalDate(null).Should().Be("-");
    }

    [Theory(DisplayName = "Work duration formatting includes its configured unit")]
    [InlineData(false, "1.5 days")]
    [InlineData(true, "36 hours")]
    public void FormatWorkDurationValueWhenCalledIncludesUnit(
        bool showTimeCalculationsInHoursOnly,
        string expected)
    {
        var result = PresentationFormatting.FormatWorkDurationValue(
            TimeSpan.FromHours(36),
            showTimeCalculationsInHoursOnly);

        result.Should().Be(expected);
    }

}
