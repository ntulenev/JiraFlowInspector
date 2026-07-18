using FluentAssertions;

using JiraMetrics.API;
using JiraMetrics.Models.ValueObjects;

using Moq;

namespace JiraMetrics.Tests.API;

public sealed class JiraIssueSearchClientTests
{
    [Fact(DisplayName = "GetIssuesCreatedThisMonth uses resolved custom field id")]
    [Trait("Category", "Unit")]
    public async Task GetIssuesCreatedThisMonthWhenFieldResolvesUsesFieldIdAndMappingContext()
    {
        // Arrange
        var fixture = new Fixture();
        var fieldName = new JiraFieldName("Reproduced on prod");
        var fieldId = new JiraFieldId("customfield_12345");
        var query = new JqlQuery("project = APP");

        fixture.JqlFacade.Setup(facade => facade.BuildCreatedIssuesQuery(fixture.ProjectKey, fixture.IssueTypes))
            .Returns(query);
        fixture.FieldResolver.Setup(resolver => resolver.TryResolveFieldIdAsync(fieldName, fixture.Token))
            .ReturnsAsync(fieldId);
        fixture.SearchExecutor.Setup(executor => executor.SearchIssuesAsync(
                query,
                It.Is<JiraSearchFields>(fields => fields.Contains(fieldId.Value) && !fields.Contains(fieldName.Value)),
                fixture.Token))
            .ReturnsAsync([]);
        // Act
        var result = await fixture.Sut.GetIssuesCreatedThisMonthAsync(
            fixture.ProjectKey,
            fixture.IssueTypes,
            fixture.Token,
            fieldName);

        // Assert
        result.Should().BeEmpty();
        fixture.FieldResolver.VerifyAll();
        fixture.SearchExecutor.VerifyAll();
    }

    [Fact(DisplayName = "GetIssuesCreatedThisMonth falls back to custom field name when id is unresolved")]
    [Trait("Category", "Unit")]
    public async Task GetIssuesCreatedThisMonthWhenFieldIsUnresolvedUsesFieldName()
    {
        // Arrange
        var fixture = new Fixture();
        var fieldName = new JiraFieldName("Reproduced on prod");
        var query = new JqlQuery("project = APP");

        fixture.JqlFacade.Setup(facade => facade.BuildCreatedIssuesQuery(fixture.ProjectKey, fixture.IssueTypes))
            .Returns(query);
        fixture.FieldResolver.Setup(resolver => resolver.TryResolveFieldIdAsync(fieldName, fixture.Token))
            .ReturnsAsync((JiraFieldId?)null);
        fixture.SearchExecutor.Setup(executor => executor.SearchIssuesAsync(
                query,
                It.Is<JiraSearchFields>(fields => fields.Contains(fieldName.Value)),
                fixture.Token))
            .ReturnsAsync([]);
        // Act
        var result = await fixture.Sut.GetIssuesCreatedThisMonthAsync(
            fixture.ProjectKey,
            fixture.IssueTypes,
            fixture.Token,
            fieldName);

        // Assert
        result.Should().BeEmpty();
        fixture.SearchExecutor.VerifyAll();
    }

    [Fact(DisplayName = "GetIssuesMovedToDoneThisMonth requests issue links without resolving a custom field")]
    [Trait("Category", "Unit")]
    public async Task GetIssuesMovedToDoneThisMonthWhenLinksRequestedIncludesIssueLinksField()
    {
        // Arrange
        var fixture = new Fixture();
        var done = new StatusName("Done");
        var query = new JqlQuery("status changed to Done");

        fixture.JqlFacade.Setup(facade => facade.BuildMovedToDoneIssuesQuery(
                fixture.ProjectKey,
                done,
                fixture.IssueTypes))
            .Returns(query);
        fixture.SearchExecutor.Setup(executor => executor.SearchIssuesAsync(
                query,
                It.Is<JiraSearchFields>(fields => fields.Contains("issuelinks")),
                fixture.Token))
            .ReturnsAsync([]);
        // Act
        var result = await fixture.Sut.GetIssuesMovedToDoneThisMonthAsync(
            fixture.ProjectKey,
            done,
            fixture.IssueTypes,
            fixture.Token,
            includeIssueLinks: true);

        // Assert
        result.Should().BeEmpty();
        fixture.FieldResolver.Verify(
            resolver => resolver.TryResolveFieldIdAsync(It.IsAny<JiraFieldName>(), fixture.Token),
            Times.Never);
    }

    [Fact(DisplayName = "Constructor rejects null collaborators")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenCollaboratorIsNullThrowsArgumentNullException()
    {
        // Arrange
        var searchExecutor = new Mock<IJiraSearchExecutor>(MockBehavior.Strict).Object;
        var jqlFacade = new Mock<IJiraJqlFacade>(MockBehavior.Strict).Object;
        var fieldResolver = new Mock<IJiraFieldResolver>(MockBehavior.Strict).Object;

        // Act
        Action nullSearchExecutor = () => _ = new JiraIssueSearchClient(null!, jqlFacade, fieldResolver);
        Action nullJqlFacade = () => _ = new JiraIssueSearchClient(searchExecutor, null!, fieldResolver);
        Action nullFieldResolver = () => _ = new JiraIssueSearchClient(searchExecutor, jqlFacade, null!);

        // Assert
        nullSearchExecutor.Should().Throw<ArgumentNullException>();
        nullJqlFacade.Should().Throw<ArgumentNullException>();
        nullFieldResolver.Should().Throw<ArgumentNullException>();
    }

    private sealed class Fixture
    {
        public Fixture()
        {
            Sut = new JiraIssueSearchClient(
                SearchExecutor.Object,
                JqlFacade.Object,
                FieldResolver.Object);
        }

        public Mock<IJiraSearchExecutor> SearchExecutor { get; } = new(MockBehavior.Strict);

        public Mock<IJiraJqlFacade> JqlFacade { get; } = new(MockBehavior.Strict);

        public Mock<IJiraFieldResolver> FieldResolver { get; } = new(MockBehavior.Strict);

        public JiraIssueSearchClient Sut { get; }

        public ProjectKey ProjectKey { get; } = new("APP");

        public IReadOnlyList<IssueTypeName> IssueTypes { get; } = [new IssueTypeName("Story")];

        public CancellationToken Token { get; } = new(canceled: false);
    }
}
