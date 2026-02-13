using FluentAssertions;

using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Models;

public sealed class JiraAuthUserTests
{
    [Fact(DisplayName = "Constructor sets properties")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenArgumentsAreValidSetsProperties()
    {
        // Arrange
        var displayName = new UserDisplayName("Jane Doe");
        var emailAddress = "user@example.com";
        var accountId = "123";

        // Act
        var user = new JiraAuthUser(displayName, emailAddress, accountId);

        // Assert
        user.DisplayName.Should().Be(displayName);
        user.EmailAddress.Should().Be(emailAddress);
        user.AccountId.Should().Be(accountId);
    }
}
