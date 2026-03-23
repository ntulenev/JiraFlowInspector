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
        settings.IncidentStartFallbackFieldName.Should().BeNull();
        settings.IncidentRecoveryFieldName.Should().Be("Incident Recovery date/time UTC");
        settings.IncidentRecoveryFallbackFieldName.Should().BeNull();
        settings.ImpactFieldName.Should().Be("Impact");
        settings.UrgencyFieldName.Should().Be("Urgency");
        settings.JqlFilter.Should().BeNull();
        settings.SearchPhrase.Should().BeNull();
        settings.AdditionalFieldNames.Should().BeEmpty();
    }

    [Fact(DisplayName = "Constructor trims JQL filter when it is provided")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenJqlFilterIsProvidedStoresTrimmedValue()
    {
        // Act
        var settings = new GlobalIncidentsReportSettings(jqlFilter: "  summary ~ \"downtime\"  ");

        // Assert
        settings.JqlFilter.Should().Be("summary ~ \"downtime\"");
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

    [Fact(DisplayName = "Constructor trims fallback field names")]
    [Trait("Category", "Unit")]
    public void ConstructorWhenFallbackFieldNamesAreProvidedStoresTrimmedValues()
    {
        // Act
        var settings = new GlobalIncidentsReportSettings(
            incidentStartFallbackFieldName: "  Incident Start date/time user timezone  ",
            incidentRecoveryFallbackFieldName: "  Incident Recovery date/time user timezone  ");

        // Assert
        settings.IncidentStartFallbackFieldName.Should().Be("Incident Start date/time user timezone");
        settings.IncidentRecoveryFallbackFieldName.Should().Be("Incident Recovery date/time user timezone");
    }
}
