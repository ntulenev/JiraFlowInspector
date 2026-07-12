using FluentAssertions;

using JiraMetrics.API;
using JiraMetrics.API.Mapping;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Transport.Models;

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
        fixture.MapperFacade.Setup(mapper => mapper.MapIssueListItems(
                It.IsAny<IReadOnlyList<JiraIssueKeyResponse>>(),
                It.Is<IssueListMappingContext>(context =>
                    context.ReporducedOnProdFieldId == fieldId
                    && context.ReporducedOnProdFieldName == fieldName
                    && !context.IncludeIssueLinks)))
            .Returns([]);

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
        fixture.MapperFacade.VerifyAll();
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
        fixture.MapperFacade.Setup(mapper => mapper.MapIssueListItems(
                It.IsAny<IReadOnlyList<JiraIssueKeyResponse>>(),
                It.Is<IssueListMappingContext>(context =>
                    context.ReporducedOnProdFieldId == null
                    && context.ReporducedOnProdFieldName == fieldName)))
            .Returns([]);

        // Act
        var result = await fixture.Sut.GetIssuesCreatedThisMonthAsync(
            fixture.ProjectKey,
            fixture.IssueTypes,
            fixture.Token,
            fieldName);

        // Assert
        result.Should().BeEmpty();
        fixture.SearchExecutor.VerifyAll();
        fixture.MapperFacade.VerifyAll();
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
        fixture.MapperFacade.Setup(mapper => mapper.MapIssueListItems(
                It.IsAny<IReadOnlyList<JiraIssueKeyResponse>>(),
                It.Is<IssueListMappingContext>(context =>
                    context.ReporducedOnProdFieldId == null
                    && context.ReporducedOnProdFieldName == null
                    && context.IncludeIssueLinks)))
            .Returns([]);

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
        fixture.MapperFacade.VerifyAll();
    }

    [Fact(DisplayName = "Constructor rejects null collaborators")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenCollaboratorIsNullThrowsArgumentNullException()
    {
        // Arrange
        var searchExecutor = new Mock<IJiraSearchExecutor>(MockBehavior.Strict).Object;
        var jqlFacade = new Mock<IJiraJqlFacade>(MockBehavior.Strict).Object;
        var fieldResolver = new Mock<IJiraFieldResolver>(MockBehavior.Strict).Object;
        var mapperFacade = new Mock<IJiraMapperFacade>(MockBehavior.Strict).Object;

        // Act
        Action nullSearchExecutor = () => _ = new JiraIssueSearchClient(null!, jqlFacade, fieldResolver, mapperFacade);
        Action nullJqlFacade = () => _ = new JiraIssueSearchClient(searchExecutor, null!, fieldResolver, mapperFacade);
        Action nullFieldResolver = () => _ = new JiraIssueSearchClient(searchExecutor, jqlFacade, null!, mapperFacade);
        Action nullMapperFacade = () => _ = new JiraIssueSearchClient(searchExecutor, jqlFacade, fieldResolver, null!);

        // Assert
        nullSearchExecutor.Should().Throw<ArgumentNullException>();
        nullJqlFacade.Should().Throw<ArgumentNullException>();
        nullFieldResolver.Should().Throw<ArgumentNullException>();
        nullMapperFacade.Should().Throw<ArgumentNullException>();
    }

    private sealed class Fixture
    {
        public Fixture()
        {
            Sut = new JiraIssueSearchClient(
                SearchExecutor.Object,
                JqlFacade.Object,
                FieldResolver.Object,
                MapperFacade.Object);
        }

        public Mock<IJiraSearchExecutor> SearchExecutor { get; } = new(MockBehavior.Strict);

        public Mock<IJiraJqlFacade> JqlFacade { get; } = new(MockBehavior.Strict);

        public Mock<IJiraFieldResolver> FieldResolver { get; } = new(MockBehavior.Strict);

        public Mock<IJiraMapperFacade> MapperFacade { get; } = new(MockBehavior.Strict);

        public JiraIssueSearchClient Sut { get; }

        public ProjectKey ProjectKey { get; } = new("APP");

        public IReadOnlyList<IssueTypeName> IssueTypes { get; } = [new IssueTypeName("Story")];

        public CancellationToken Token { get; } = new(canceled: false);
    }
}
