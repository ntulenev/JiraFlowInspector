using System.ComponentModel.DataAnnotations;

using FluentAssertions;

using JiraMetrics.Models.Configuration;

namespace JiraMetrics.Tests.Configuration;

public sealed class JiraOptionsTests
{
    [Fact(DisplayName = "Validation succeeds when options are valid")]
    [Trait("Category", "Unit")]
    public void ValidateWhenOptionsAreValidReturnsNoErrors()
    {
        // Arrange
        var options = new JiraOptions
        {
            BaseUrl = new Uri("https://example.atlassian.net", UriKind.Absolute),
            Email = "user@example.com",
            ApiToken = "token",
            ProjectKey = "AAA",
            DoneStatusName = "Done",
            RequiredPathStage = "Code Review",
            CreatedAfter = "2026-01-15",
            MonthLabel = "2026-02",
            RetryCount = 0
        };

        // Act
        var results = Validate(options);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact(DisplayName = "Validation fails when base URL is missing")]
    [Trait("Category", "Unit")]
    public void ValidateWhenBaseUrlIsMissingReturnsError()
    {
        // Arrange
        var options = new JiraOptions
        {
            BaseUrl = null!,
            Email = "user@example.com",
            ApiToken = "token",
            ProjectKey = "AAA",
            DoneStatusName = "Done",
            RequiredPathStage = "Code Review",
            CreatedAfter = "2026-01-15",
            MonthLabel = "2026-02",
            RetryCount = 0
        };

        // Act
        var results = Validate(options);

        // Assert
        results.Should().Contain(result => result.MemberNames.Contains("BaseUrl"));
    }

    [Fact(DisplayName = "Validation fails when month label format is invalid")]
    [Trait("Category", "Unit")]
    public void ValidateWhenMonthLabelIsInvalidReturnsError()
    {
        // Arrange
        var options = new JiraOptions
        {
            BaseUrl = new Uri("https://example.atlassian.net", UriKind.Absolute),
            Email = "user@example.com",
            ApiToken = "token",
            ProjectKey = "AAA",
            DoneStatusName = "Done",
            RequiredPathStage = "Code Review",
            CreatedAfter = "2026-01-15",
            MonthLabel = "2026/02",
            RetryCount = 0
        };

        // Act
        var results = Validate(options);

        // Assert
        results.Should().Contain(result => result.MemberNames.Contains("MonthLabel"));
    }

    [Fact(DisplayName = "Validation fails when retry count is above ten")]
    [Trait("Category", "Unit")]
    public void ValidateWhenRetryCountIsAboveRangeReturnsError()
    {
        // Arrange
        var options = new JiraOptions
        {
            BaseUrl = new Uri("https://example.atlassian.net", UriKind.Absolute),
            Email = "user@example.com",
            ApiToken = "token",
            ProjectKey = "AAA",
            DoneStatusName = "Done",
            RequiredPathStage = "Code Review",
            CreatedAfter = "2026-01-15",
            MonthLabel = "2026-02",
            RetryCount = 11
        };

        // Act
        var results = Validate(options);

        // Assert
        results.Should().Contain(result => result.MemberNames.Contains("RetryCount"));
    }

    [Fact(DisplayName = "Validation fails when created-after format is invalid")]
    [Trait("Category", "Unit")]
    public void ValidateWhenCreatedAfterIsInvalidReturnsError()
    {
        // Arrange
        var options = new JiraOptions
        {
            BaseUrl = new Uri("https://example.atlassian.net", UriKind.Absolute),
            Email = "user@example.com",
            ApiToken = "token",
            ProjectKey = "AAA",
            DoneStatusName = "Done",
            RequiredPathStage = "Code Review",
            CreatedAfter = "2026/01/15",
            MonthLabel = "2026-02",
            RetryCount = 0
        };

        // Act
        var results = Validate(options);

        // Assert
        results.Should().Contain(result => result.MemberNames.Contains("CreatedAfter"));
    }

    private static List<ValidationResult> Validate(JiraOptions options)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(options);

        Validator.TryValidateObject(options, context, results, validateAllProperties: true);

        return results;
    }
}
