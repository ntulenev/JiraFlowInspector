using FluentAssertions;

using JiraMetrics.Models.Configuration;

namespace JiraMetrics.Tests.Configuration;

public sealed class GlobalIncidentsReportSettingsTests
{
    [Fact(DisplayName = "Constructor uses defaults when values are omitted")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenValuesAreOmittedUsesDefaults()
    {
        // Act
        var settings = new GlobalIncidentsReportSettings();

        // Assert
        settings.Namespace.Should().Be("Incidents");
        settings.IncidentStartFieldName.Should().Be("Incident Start date/time UTC");
        settings.IncidentRecoveryFieldName.Should().Be("Incident Recovery date/time UTC");
        settings.ImpactFieldName.Should().Be("Impact");
        settings.UrgencyFieldName.Should().Be("Urgency");
        settings.SearchPhrase.Should().BeNull();
        settings.AdditionalFieldNames.Should().BeEmpty();
    }

    [Fact(DisplayName = "Constructor normalizes additional field names")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenAdditionalFieldNamesAreProvidedNormalizesValues()
    {
        // Act
        var settings = new GlobalIncidentsReportSettings(
            additionalFieldNames: ["  Business Impact  ", "Incident resolution", "Business Impact"]);

        // Assert
        settings.AdditionalFieldNames.Should().ContainInOrder("Business Impact", "Incident resolution");
    }
}
