using FluentAssertions;

using JiraMetrics.Transport;
using JiraMetrics.Transport.Models;

namespace JiraMetrics.Tests.Transport;

public sealed class SimpleJsonSerializerTests
{
    [Fact(DisplayName = "Deserialize returns DTO when JSON is valid")]
    [Trait("Category", "Unit")]
    public void DeserializeWhenJsonIsValidReturnsDto()
    {
        // Arrange
        var serializer = new SimpleJsonSerializer();
        var json = "{\"displayname\":\"Jane Doe\",\"emailaddress\":\"user@example.test\",\"accountid\":\"123\"}";

        // Act
        var result = serializer.Deserialize<JiraCurrentUserResponse>(json);

        // Assert
        result.Should().NotBeNull();
        result!.DisplayName.Should().Be("Jane Doe");
        result.EmailAddress.Should().Be("user@example.test");
        result.AccountId.Should().Be("123");
    }

    [Fact(DisplayName = "Deserialize returns null when JSON literal is null")]
    [Trait("Category", "Unit")]
    public void DeserializeWhenJsonLiteralIsNullReturnsNull()
    {
        // Arrange
        var serializer = new SimpleJsonSerializer();

        // Act
        var result = serializer.Deserialize<JiraCurrentUserResponse>("null");

        // Assert
        result.Should().BeNull();
    }

    [Fact(DisplayName = "Deserialize throws when JSON is null")]
    [Trait("Category", "Unit")]
    public void DeserializeWhenJsonIsNullThrowsArgumentNullException()
    {
        // Arrange
        var serializer = new SimpleJsonSerializer();
        string json = null!;

        // Act
        Action act = () => _ = serializer.Deserialize<JiraCurrentUserResponse>(json);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }
}
