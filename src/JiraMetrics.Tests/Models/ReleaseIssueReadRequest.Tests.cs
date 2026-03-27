using FluentAssertions;

using JiraMetrics.Models;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Models;

public sealed class ReleaseIssueReadRequestTests
{
    [Fact(DisplayName = "Constructor throws when hot-fix rules are null")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenHotFixRulesAreNullThrowsArgumentNullException()
    {
        IReadOnlyList<HotFixRule> hotFixRules = null!;

        Action act = () => _ = new ReleaseIssueReadRequest(
            new ProjectKey("REL"),
            new JiraLabel("release"),
            new JiraFieldName("Release Date"),
            new JiraFieldName("Components"),
            hotFixRules,
            new JiraFieldName("Rollback"),
            null);

        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "Constructor sets properties and copies hot-fix rules")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenArgumentsAreValidSetsProperties()
    {
        var hotFixRule = new HotFixRule(
            new JiraFieldName("Fix Type"),
            [new JiraFieldValue("Hotfix")]);
        var rules = new List<HotFixRule> { hotFixRule };
        var environmentFilter = new ReleaseEnvironmentFilter(
            new JiraFieldName("Environment"),
            new JiraFieldValue("Production"));

        var request = new ReleaseIssueReadRequest(
            new ProjectKey("REL"),
            new JiraLabel("release"),
            new JiraFieldName("Release Date"),
            new JiraFieldName("Components"),
            rules,
            new JiraFieldName("Rollback"),
            environmentFilter);
        rules.Clear();

        request.ReleaseProjectKey.Should().Be(new ProjectKey("REL"));
        request.ProjectLabel.Should().Be(new JiraLabel("release"));
        request.ReleaseDateFieldName.Should().Be(new JiraFieldName("Release Date"));
        request.ComponentsFieldName.Should().Be(new JiraFieldName("Components"));
        request.HotFixRules.Should().Equal(hotFixRule);
        request.RollbackFieldName.Should().Be(new JiraFieldName("Rollback"));
        request.EnvironmentFilter.Should().Be(environmentFilter);
    }
}
