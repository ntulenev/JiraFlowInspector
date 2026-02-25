using FluentAssertions;

using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;

namespace JiraMetrics.Tests.Configuration;

public sealed class ReleaseReportSettingsTests
{
    [Fact(DisplayName = "Constructor uses default hot-fix marker rule when values are not provided")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenHotFixRulesAreMissingUsesDefaults()
    {
        // Act
        var settings = new ReleaseReportSettings(
            new ProjectKey("RLS"),
            "Processing",
            "Change completion date");

        // Assert
        settings.HotFixRules.Should().ContainSingle();
        settings.HotFixRules.Should().ContainKey("Change type");
        settings.HotFixRules["Change type"].Should().BeEquivalentTo(["Emergency"]);
        settings.RollbackFieldName.Should().Be("Rollback type");
    }

    [Fact(DisplayName = "Constructor uses configured hot-fix marker rules")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenHotFixRulesAreProvidedUsesConfiguredValues()
    {
        // Act
        var settings = new ReleaseReportSettings(
            new ProjectKey("RLS"),
            "Processing",
            "Change completion date",
            componentsFieldName: "Components",
            hotFixRules: new Dictionary<string, IReadOnlyList<string>>
            {
                ["Change type"] = ["Emergency"],
                ["Change reason"] = ["Repair", "Mitigation"]
            },
            rollbackFieldName: "Rollback category");

        // Assert
        settings.HotFixRules.Should().HaveCount(2);
        settings.HotFixRules["Change type"].Should().BeEquivalentTo(["Emergency"]);
        settings.HotFixRules["Change reason"].Should().BeEquivalentTo(["Mitigation", "Repair"]);
        settings.RollbackFieldName.Should().Be("Rollback category");
    }
}
