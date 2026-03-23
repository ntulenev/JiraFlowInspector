using FluentAssertions;

using JiraMetrics.Models;
using JiraMetrics.Models.Configuration;
using JiraMetrics.Models.ValueObjects;
using JiraMetrics.Presentation;

using Microsoft.Extensions.Options;

using Spectre.Console;
using Spectre.Console.Testing;

namespace JiraMetrics.Tests.Presentation;

public sealed class SpectreJiraPresentationServiceTests
{
    [Fact(DisplayName = "ShowAuthenticationSucceeded throws when user is null")]
    [Trait("Category", "Unit")]
    public void ShowAuthenticationSucceededWhenUserIsNullThrowsArgumentNullException()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();
        JiraAuthUser user = null!;

        // Act
        Action act = () => service.ShowAuthenticationSucceeded(user);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "ShowReportHeader throws when settings are null")]
    [Trait("Category", "Unit")]
    public void ShowReportHeaderWhenSettingsAreNullThrowsArgumentNullException()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();
        AppSettings settings = null!;

        // Act
        Action act = () => service.ShowReportHeader(settings, new ItemCount(0));

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "ShowReportPeriodContext writes month first and created-after when provided")]
    [Trait("Category", "Unit")]
    public async Task ShowReportPeriodContextWhenCreatedAfterProvidedWritesMonthThenCreatedAfter()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();

        // Act
        var output = await RunWithTestConsoleAsync(console =>
        {
            service.ShowReportPeriodContext(new MonthLabel("2026-02"), new CreatedAfterDate("2026-01-01"));
            return Task.FromResult(console.Output);
        });

        // Assert
        var monthIndex = output.IndexOf("Month label:", StringComparison.Ordinal);
        var createdAfterIndex = output.IndexOf("Created after:", StringComparison.Ordinal);
        monthIndex.Should().BeGreaterThanOrEqualTo(0);
        createdAfterIndex.Should().BeGreaterThanOrEqualTo(0);
        monthIndex.Should().BeLessThan(createdAfterIndex);
    }

    [Fact(DisplayName = "ShowReportPeriodContext writes only month when created-after is missing")]
    [Trait("Category", "Unit")]
    public async Task ShowReportPeriodContextWhenCreatedAfterMissingWritesOnlyMonth()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();

        // Act
        var output = await RunWithTestConsoleAsync(console =>
        {
            service.ShowReportPeriodContext(new MonthLabel("2026-02"), null);
            return Task.FromResult(console.Output);
        });

        // Assert
        output.Should().Contain("Month label:");
        output.Should().NotContain("Created after:");
    }

    [Fact(DisplayName = "ShowDoneIssuesTable throws when issues are null")]
    [Trait("Category", "Unit")]
    public void ShowDoneIssuesTableWhenIssuesAreNullThrowsArgumentNullException()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();
        IReadOnlyList<IssueTimeline> issues = null!;

        // Act
        Action act = () => service.ShowDoneIssuesTable(issues, new StatusName("Done"));

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "ShowPathGroupsSummary throws when summary is null")]
    [Trait("Category", "Unit")]
    public void ShowPathGroupsSummaryWhenSummaryIsNullThrowsArgumentNullException()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();
        PathGroupsSummary summary = null!;

        // Act
        Action act = () => service.ShowPathGroupsSummary(summary);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "ShowPathGroupsSummary writes code artefacts filter info")]
    [Trait("Category", "Unit")]
    public async Task ShowPathGroupsSummaryWhenCalledWritesFilterInfo()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();

        // Act
        var output = await RunWithTestConsoleAsync(console =>
        {
            service.ShowPathGroupsSummary(
                new PathGroupsSummary(
                    new ItemCount(10),
                    new ItemCount(8),
                    new ItemCount(2),
                    new ItemCount(3)));
            return Task.FromResult(console.Output);
        });

        // Assert
        output.Should().Contain("Successful:");
        output.Should().Contain("Path groups:");
        output.Should().Contain("Filter:");
        output.Should().Contain("only tasks with code artefacts");
    }

    [Fact(DisplayName = "ShowPathGroups throws when groups are null")]
    [Trait("Category", "Unit")]
    public void ShowPathGroupsWhenGroupsAreNullThrowsArgumentNullException()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();
        IReadOnlyList<PathGroup> groups = null!;

        // Act
        Action act = () => service.ShowPathGroups(groups);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "ShowFailures throws when failures are null")]
    [Trait("Category", "Unit")]
    public void ShowFailuresWhenFailuresAreNullThrowsArgumentNullException()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();
        IReadOnlyList<LoadFailure> failures = null!;

        // Act
        Action act = () => service.ShowFailures(failures);

        // Assert
        act.Should()
            .Throw<ArgumentNullException>();
    }

    [Fact(DisplayName = "ShowAuthenticationSucceeded writes user display name")]
    [Trait("Category", "Unit")]
    public async Task ShowAuthenticationSucceededWhenCalledWritesDisplayName()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();

        // Act
        var output = await RunWithTestConsoleAsync(console =>
        {
            service.ShowAuthenticationSucceeded(new JiraAuthUser(new UserDisplayName("Jane Doe"), null, null));
            return Task.FromResult(console.Output);
        });

        // Assert
        output.Should().Contain("Auth succeeded for user:");
        output.Should().Contain("Jane Doe");
    }

    [Fact(DisplayName = "ShowFailures writes failed issues table when failures exist")]
    [Trait("Category", "Unit")]
    public async Task ShowFailuresWhenFailuresExistWritesTable()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();
        var failures = new List<LoadFailure>
        {
            new(new IssueKey("AAA-1"), new ErrorMessage("boom"))
        };

        // Act
        var output = await RunWithTestConsoleAsync(console =>
        {
            service.ShowFailures(failures);
            return Task.FromResult(console.Output);
        });

        // Assert
        output.Should().Contain("Failed issues");
        output.Should().Contain("#");
        output.Should().Contain("AAA-1");
        output.Should().Contain("boom");
    }

    [Fact(DisplayName = "ShowFailures writes nothing when list is empty")]
    [Trait("Category", "Unit")]
    public async Task ShowFailuresWhenListIsEmptyWritesNothing()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();

        // Act
        var output = await RunWithTestConsoleAsync(console =>
        {
            service.ShowFailures([]);
            return Task.FromResult(console.Output);
        });

        // Assert
        output.Should().BeEmpty();
    }

    [Fact(DisplayName = "ShowDoneIssuesTable writes issue type column and value")]
    [Trait("Category", "Unit")]
    public async Task ShowDoneIssuesTableWhenCalledWritesIssueType()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();
        var transitions = new List<TransitionEvent>
        {
            new(new StatusName("Open"), new StatusName("Done"), DateTimeOffset.UtcNow, TimeSpan.FromHours(2))
        };
        var issue = new IssueTimeline(
            new IssueKey("AAA-1"),
            new IssueTypeName("Bug"),
            new IssueSummary("Fix login"),
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow,
            transitions,
            new PathKey("OPEN->DONE"),
            new PathLabel("Open -> Done"),
            3,
            true);

        // Act
        var output = await RunWithTestConsoleAsync(console =>
        {
            service.ShowDoneIssuesTable([issue], new StatusName("Done"));
            return Task.FromResult(console.Output);
        });

        // Assert
        output.Should().Contain("#");
        output.Should().Contain("Type");
        output.Should().Contain("Sub-ite");
        output.Should().Contain("Code");
        output.Should().Contain("Created");
        output.Should().Contain("Done At");
        output.Should().Contain("Days at");
        output.Should().Contain("Bug");
        output.Should().Contain("3");
        output.Should().Contain("+");
    }

    [Fact(DisplayName = "ShowReportHeader writes issue type filter when configured")]
    [Trait("Category", "Unit")]
    public async Task ShowReportHeaderWhenIssueTypesAreConfiguredWritesIssueTypesLine()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();
        var settings = new AppSettings(
            new JiraBaseUrl("https://example.atlassian.net"),
            new JiraEmail("user@example.com"),
            new JiraApiToken("token"),
            new ProjectKey("AAA"),
            new StatusName("Done"),
            new StatusName("Reject"),
            [new StageName("Code Review")],
            new MonthLabel("2026-02"),
            null,
            [new IssueTypeName("Bug"), new IssueTypeName("Story")],
            customFieldName: null,
            customFieldValue: null,
            excludeWeekend: false);

        // Act
        var output = await RunWithTestConsoleAsync(console =>
        {
            service.ShowReportHeader(settings, new ItemCount(2));
            return Task.FromResult(console.Output);
        });

        // Assert
        output.Should().Contain("Issue types:");
        output.Should().Contain("Bug, Story");
    }

    [Fact(DisplayName = "ShowBugRatio writes bug ratio section")]
    [Trait("Category", "Unit")]
    public async Task ShowBugRatioWhenCalledWritesBugRatioSection()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();

        // Act
        var output = await RunWithTestConsoleAsync(console =>
        {
            service.ShowBugRatio(
                [new IssueTypeName("Bug")],
                "NOVA Team",
                "Processing",
                new ItemCount(8),
                new ItemCount(4),
                new ItemCount(1),
                new ItemCount(5),
                [new IssueListItem(new IssueKey("AAA-1"), new IssueSummary("Open issue"), new DateTimeOffset(2026, 2, 3, 10, 0, 0, TimeSpan.Zero))],
                [new IssueListItem(new IssueKey("AAA-2"), new IssueSummary("Done issue"), new DateTimeOffset(2026, 2, 5, 10, 0, 0, TimeSpan.Zero))],
                [new IssueListItem(new IssueKey("AAA-3"), new IssueSummary("Rejected issue"))]);
            return Task.FromResult(console.Output);
        });

        // Assert
        output.Should().Contain("Bug ratio");
        output.Should().Contain("Filtered by:");
        output.Should().Contain("NOVA Team = Processing");
        output.Should().NotContain("Created this month");
        output.Should().Contain("Open this month");
        output.Should().Contain("Done this month");
        output.Should().Contain("Rejected this month");
        output.Should().Contain("Finished this month");
        output.Should().Contain("62.5%");
        output.Should().Contain("Open issues");
        output.Should().Contain("Done issues");
        output.Should().Contain("Rejected issues");
        output.Should().Contain("Jira ID");
        output.Should().Contain("Creation Date");
        output.Should().Contain("Title");
        output.Should().Contain("#");
        output.Should().Contain("AAA-1");
        output.Should().Contain("AAA-2");
        output.Should().Contain("AAA-3");
        output.Should().Contain("2026-02-03");
        output.Should().Contain("2026-02-05");
    }

    [Fact(DisplayName = "ShowAllTasksRatio writes summary without details")]
    [Trait("Category", "Unit")]
    public async Task ShowAllTasksRatioWhenCalledWritesSummaryOnly()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();

        // Act
        var output = await RunWithTestConsoleAsync(console =>
        {
            service.ShowAllTasksRatio(
                "ADF Team",
                "Processing",
                new ItemCount(47),
                new ItemCount(33),
                new ItemCount(31),
                new ItemCount(37),
                new ItemCount(68));
            return Task.FromResult(console.Output);
        });

        // Assert
        output.Should().Contain("All tasks ratio");
        output.Should().Contain("Filtered by:");
        output.Should().Contain("ADF Team = Processing");
        output.Should().Contain("Issue types");
        output.Should().Contain("All");
        output.Should().Contain("Open this month");
        output.Should().Contain("Done this month");
        output.Should().Contain("Rejected this month");
        output.Should().Contain("Finished this month");
        output.Should().Contain("144.68%");
        output.Should().NotContain("Open issues");
        output.Should().NotContain("Done issues");
        output.Should().NotContain("Rejected issues");
    }

    [Fact(DisplayName = "ShowRejectedIssuesTable writes reject issues table")]
    [Trait("Category", "Unit")]
    public async Task ShowRejectedIssuesTableWhenCalledWritesRejectRows()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();
        var transitions = new List<TransitionEvent>
        {
            new(new StatusName("Open"), new StatusName("Reject"), DateTimeOffset.UtcNow, TimeSpan.FromHours(2))
        };
        var issue = new IssueTimeline(
            new IssueKey("AAA-9"),
            new IssueTypeName("Task"),
            new IssueSummary("Reject me"),
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow,
            transitions,
            new PathKey("OPEN->REJECT"),
            new PathLabel("Open -> Reject"));

        // Act
        var output = await RunWithTestConsoleAsync(console =>
        {
            service.ShowRejectedIssuesTable([issue], new StatusName("Reject"));
            return Task.FromResult(console.Output);
        });

        // Assert
        output.Should().Contain("Issues moved to Rejected this month");
        output.Should().Contain("Created");
        output.Should().Contain("Rejected At");
        output.Should().Contain("Days at");
        output.Should().Contain("AAA-9");
    }

    [Fact(DisplayName = "ShowDoneDaysAtWork75PerType writes days-at-work percentile table")]
    [Trait("Category", "Unit")]
    public async Task ShowDoneDaysAtWork75PerTypeWhenCalledWritesRows()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();

        // Act
        var output = await RunWithTestConsoleAsync(console =>
        {
            service.ShowDoneDaysAtWork75PerType(
                [
                    new IssueTypeWorkDays75Summary(
                        new IssueTypeName("Task"),
                        new ItemCount(4),
                        TimeSpan.FromDays(2.5))
                ],
                new StatusName("Done"));
            return Task.FromResult(console.Output);
        });

        // Assert
        output.Should().Contain("Days at Work 75P per type");
        output.Should().Contain("Done");
        output.Should().Contain("Type");
        output.Should().Contain("Issues");
        output.Should().Contain("Days at Work 75P");
        output.Should().Contain("Task");
        output.Should().Contain("4");
        output.Should().Contain("2.5");
    }

    [Fact(DisplayName = "ShowDoneDaysAtWork75PerType writes hours when strict hours mode is enabled")]
    [Trait("Category", "Unit")]
    public async Task ShowDoneDaysAtWork75PerTypeWhenStrictHoursModeEnabledWritesHours()
    {
        // Arrange
        var service = new SpectreJiraPresentationService(Options.Create(new AppSettings(
            new JiraBaseUrl("https://example.atlassian.net"),
            new JiraEmail("user@example.com"),
            new JiraApiToken("token"),
            new ProjectKey("AAA"),
            new StatusName("Done"),
            null,
            [new StageName("Code Review")],
            new MonthLabel("2026-02"),
            showTimeCalculationsInHoursOnly: true)));

        // Act
        var output = await RunWithTestConsoleAsync(console =>
        {
            service.ShowDoneDaysAtWork75PerType(
                [
                    new IssueTypeWorkDays75Summary(
                        new IssueTypeName("Task"),
                        new ItemCount(4),
                        TimeSpan.FromDays(2.5))
                ],
                new StatusName("Done"));
            return Task.FromResult(console.Output);
        });

        // Assert
        output.Should().Contain("Hours at Work 75P per type");
        output.Should().Contain("Hours at Work 75P");
        output.Should().Contain("60");
        output.Should().NotContain("Days at Work 75P");
    }

    [Fact(DisplayName = "ShowDoneDaysAtWork75PerType writes empty state when there is no data")]
    [Trait("Category", "Unit")]
    public async Task ShowDoneDaysAtWork75PerTypeWhenNoItemsWritesNoData()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();

        // Act
        var output = await RunWithTestConsoleAsync(console =>
        {
            service.ShowDoneDaysAtWork75PerType([], new StatusName("Done"));
            return Task.FromResult(console.Output);
        });

        // Assert
        output.Should().Contain("Days at Work 75P per type");
        output.Should().Contain("No data.");
    }

    [Fact(DisplayName = "ShowRejectedIssuesTable writes empty state when list is empty")]
    [Trait("Category", "Unit")]
    public async Task ShowRejectedIssuesTableWhenListIsEmptyWritesNoIssues()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();

        // Act
        var output = await RunWithTestConsoleAsync(console =>
        {
            service.ShowRejectedIssuesTable([], new StatusName("Reject"));
            return Task.FromResult(console.Output);
        });

        // Assert
        output.Should().Contain("Issues moved to Rejected this month");
        output.Should().Contain("No issues");
    }

    [Fact(DisplayName = "ShowBugRatioLoadingStarted and completed write loader lines")]
    [Trait("Category", "Unit")]
    public async Task ShowBugRatioLoadingMessagesWhenCalledWriteOutput()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();

        // Act
        var output = await RunWithTestConsoleAsync(console =>
        {
            service.ShowBugRatioLoadingStarted([new IssueTypeName("Bug")]);
            service.ShowBugRatioLoadingCompleted(new ItemCount(3), new ItemCount(2), new ItemCount(1), new ItemCount(3));
            return Task.FromResult(console.Output);
        });

        // Assert
        output.Should().Contain("Loading bug ratio data");
        output.Should().Contain("Bug");
        output.Should().Contain("Bug ratio data loaded");
        output.Should().Contain("created = 3");
        output.Should().Contain("done = 2");
        output.Should().Contain("rejected = 1");
        output.Should().Contain("finished = 3");
    }

    [Fact(DisplayName = "ShowAllTasksRatioLoadingStarted and completed write loader lines")]
    [Trait("Category", "Unit")]
    public async Task ShowAllTasksRatioLoadingMessagesWhenCalledWriteOutput()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();

        // Act
        var output = await RunWithTestConsoleAsync(console =>
        {
            service.ShowAllTasksRatioLoadingStarted();
            service.ShowAllTasksRatioLoadingCompleted(new ItemCount(47), new ItemCount(31), new ItemCount(37), new ItemCount(68));
            return Task.FromResult(console.Output);
        });

        // Assert
        output.Should().Contain("Loading all tasks ratio data");
        output.Should().Contain("All tasks ratio data loaded");
        output.Should().Contain("created = 47");
        output.Should().Contain("done = 31");
        output.Should().Contain("rejected = 37");
        output.Should().Contain("finished =");
        output.Should().Contain("68");
    }

    [Fact(DisplayName = "ShowReleaseReportLoadingStarted writes loader line")]
    [Trait("Category", "Unit")]
    public async Task ShowReleaseReportLoadingStartedWhenCalledWritesOutput()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();

        // Act
        var output = await RunWithTestConsoleAsync(console =>
        {
            service.ShowReleaseReportLoadingStarted();
            return Task.FromResult(console.Output);
        });

        // Assert
        output.Should().Contain("Loading release report data...");
    }

    [Fact(DisplayName = "ShowGlobalIncidentsReportLoadingStarted writes loader line")]
    [Trait("Category", "Unit")]
    public async Task ShowGlobalIncidentsReportLoadingStartedWhenCalledWritesOutput()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();

        // Act
        var output = await RunWithTestConsoleAsync(console =>
        {
            service.ShowGlobalIncidentsReportLoadingStarted();
            return Task.FromResult(console.Output);
        });

        // Assert
        output.Should().Contain("Loading global incidents report data...");
    }

    [Fact(DisplayName = "ShowReleaseReport writes release table")]
    [Trait("Category", "Unit")]
    public async Task ShowReleaseReportWhenCalledWritesReleaseRows()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();
        var settings = new ReleaseReportSettings(
            new ProjectKey("RLS"),
            "Processing",
            "Change completion date");

        // Act
        var output = await RunWithTestConsoleAsync(console =>
        {
            service.ShowReleaseReport(
                settings,
                new MonthLabel("2026-02"),
                [new ReleaseIssueItem(
                    new IssueKey("RLS-1"),
                    new IssueSummary("Release 1"),
                    new DateOnly(2026, 2, 14),
                    tasks: 3,
                    status: new StatusName("Ready for Prod"))]);
            return Task.FromResult(console.Output);
        });

        // Assert
        output.Should().Contain("Release report");
        output.Should().Contain("All releases by label");
        output.Should().Contain("Processing");
        output.Should().Contain("Hot-fix markers:");
        output.Should().Contain("Change type = Emergency");
        output.Should().Contain("#");
        output.Should().Contain("Release Date");
        output.Should().Contain("Status");
        output.Should().Contain("Tasks");
        output.Should().Contain("Rollback");
        output.Should().Contain("type");
        output.Should().Contain("RLS-1");
        output.Should().Contain("Release 1");
        output.Should().Contain("Ready for");
        output.Should().Contain("Prod");
        output.Should().Contain("3");
        output.Should().Contain("2026-02-14");
        output.Should().Contain("Total releases:");
        output.Should().Contain("Hotfix count:");
        output.Should().Contain("Rollbacks count:");
        output.Should().Contain("1");
        output.Should().Contain("0");
        output.Should().NotContain("Components field:");
        output.Should().NotContain("Components release table");
    }

    [Fact(DisplayName = "ShowReleaseReport writes components column when configured")]
    [Trait("Category", "Unit")]
    public async Task ShowReleaseReportWhenComponentsFieldIsConfiguredWritesComponents()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();
        var settings = new ReleaseReportSettings(
            new ProjectKey("RLS"),
            "Processing",
            "Change completion date",
            "Components",
            environmentFieldName: "customfield_10865",
            environmentFieldValue: "P005");

        // Act
        var output = await RunWithTestConsoleAsync(console =>
        {
            service.ShowReleaseReport(
                settings,
                new MonthLabel("2026-02"),
                [
                    new ReleaseIssueItem(
                        new IssueKey("RLS-1"),
                        new IssueSummary("Release 1"),
                        new DateOnly(2026, 2, 14),
                        tasks: 3,
                        components: 2,
                        status: new StatusName("In QA"),
                        componentNames: ["Flux", "Nebula PostgreSQL Database"],
                        environmentNames: ["P005", "S005"],
                        rollbackType: "Full rollback"),
                    new ReleaseIssueItem(
                        new IssueKey("RLS-2"),
                        new IssueSummary("Release 2"),
                        new DateOnly(2026, 2, 20),
                        tasks: 1,
                        components: 1,
                        status: new StatusName("Done"),
                        componentNames: ["Flux"],
                        environmentNames: ["P005"])
                ]);
            return Task.FromResult(console.Output);
        });

        // Assert
        output.Should().Contain("Components field:");
        output.Should().Contain("Hot-fix markers:");
        output.Should().Contain("Components");
        output.Should().Contain("Environments");
        output.Should().Contain("Status");
        output.Should().Contain("Tasks");
        output.Should().Contain("In QA");
        output.Should().Contain("2");
        output.Should().Contain("P005");
        output.Should().Contain("S005");
        output.Should().Contain("Components release table");
        output.Should().Contain("Component name");
        output.Should().Contain("Release counts");
        output.Should().Contain("Total releases:");
        output.Should().Contain("Hotfix count:");
        output.Should().Contain("Rollbacks count:");
        output.Should().Contain("Full");
        output.Should().Contain("rollback");
        output.Should().Contain("Flux");
        output.Should().Contain("Nebula PostgreSQL Database");

        var fluxIndex = output.IndexOf("Flux", StringComparison.Ordinal);
        var adfIndex = output.IndexOf("Nebula PostgreSQL Database", StringComparison.Ordinal);
        fluxIndex.Should().BeGreaterThanOrEqualTo(0);
        adfIndex.Should().BeGreaterThanOrEqualTo(0);
        fluxIndex.Should().BeLessThan(adfIndex);

        var componentsSectionIndex = output.IndexOf("Components release table", StringComparison.Ordinal);
        componentsSectionIndex.Should().BeGreaterThanOrEqualTo(0);
        var componentsSection = output[componentsSectionIndex..];
        componentsSection.Should().Contain("#");
    }

    [Fact(DisplayName = "ShowReleaseReport renders dash for zero tasks and components")]
    [Trait("Category", "Unit")]
    public async Task ShowReleaseReportWhenCountsAreZeroRendersDash()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();
        var settings = new ReleaseReportSettings(
            new ProjectKey("RLS"),
            "Processing",
            "Change completion date",
            "Components",
            environmentFieldName: "customfield_10865",
            environmentFieldValue: "P005");

        // Act
        var output = await RunWithTestConsoleAsync(console =>
        {
            service.ShowReleaseReport(
                settings,
                new MonthLabel("2026-02"),
                [new ReleaseIssueItem(
                    new IssueKey("RLS-2"),
                    new IssueSummary("Release 2"),
                    new DateOnly(2026, 2, 15),
                    tasks: 0,
                    components: 0,
                    status: new StatusName("Open"),
                    componentNames: [],
                    environmentNames: [])]);
            return Task.FromResult(console.Output);
        });

        // Assert
        output.Should().Contain("RLS-2");
        output.Should().Contain("Open");
        output.Should().Contain("-");
        output.Should().Contain("Environments");
        output.Should().Contain("Components release table");
        output.Should().Contain("No components data.");
        output.Should().Contain("Total releases:");
        output.Should().Contain("Hotfix count:");
        output.Should().Contain("Rollbacks count:");
    }

    [Fact(DisplayName = "ShowGlobalIncidentsReport writes incident rows")]
    [Trait("Category", "Unit")]
    public async Task ShowGlobalIncidentsReportWhenCalledWritesIncidentRows()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();
        var settings = new GlobalIncidentsReportSettings(
            namespaceName: "Incidents",
            jqlFilter: "(labels = ORX OR summary ~ \"ORX\") AND (summary ~ \"disab*\" OR summary ~ \"downtime\")",
            additionalFieldNames: ["Business Impact"]);

        // Act
        var output = await RunWithTestConsoleAsync(console =>
        {
            service.ShowGlobalIncidentsReport(
                settings,
                new MonthLabel("2026-03"),
                [
                    new GlobalIncidentItem(
                        new IssueKey("INC-11861"),
                        new IssueSummary("NOVA - ORX disabled 10/03/2026"),
                        new DateTimeOffset(2026, 3, 10, 2, 34, 0, TimeSpan.Zero),
                        new DateTimeOffset(2026, 3, 10, 3, 23, 0, TimeSpan.Zero),
                        impact: "Significant / Large",
                        urgency: "High",
                        additionalFields: new Dictionary<string, string?>
                        {
                            ["Business Impact"] = "ORX live feed unavailable"
                        })
                ]);
            return Task.FromResult(console.Output);
        });

        // Assert
        output.Should().Contain("Global incidents report");
        output.Should().Contain("Namespace:");
        output.Should().Contain("Incidents");
        output.Should().Contain("JQL filter:");
        output.Should().Contain("labels = ORX");
        output.Should().Contain("downtime");
        output.Should().Contain("Jira ID");
        output.Should().Contain("Start");
        output.Should().Contain("Recov");
        output.Should().Contain("UTC");
        output.Should().Contain("Durat");
        output.Should().Contain("Impact");
        output.Should().Contain("Urgen");
        output.Should().Contain("Addit");
        output.Should().Contain("INC-118");
        output.Should().Contain("NOVA -");
        output.Should().Contain("ORX");
        output.Should().Contain("2026-03");
        output.Should().Contain("02:34");
        output.Should().Contain("03:23");
        output.Should().Contain("49m");
        output.Should().Contain("Total duration:");
        output.Should().Contain("Signif");
        output.Should().Contain("Large");
        output.Should().Contain("High");
        output.Should().Contain("Business");
        output.Should().Contain("live");
    }

    [Fact(DisplayName = "ShowGlobalIncidentsReport writes hours when strict hours mode is enabled")]
    [Trait("Category", "Unit")]
    public async Task ShowGlobalIncidentsReportWhenStrictHoursModeEnabledWritesHours()
    {
        // Arrange
        var service = new SpectreJiraPresentationService(Options.Create(new AppSettings(
            new JiraBaseUrl("https://example.atlassian.net"),
            new JiraEmail("user@example.com"),
            new JiraApiToken("token"),
            new ProjectKey("AAA"),
            new StatusName("Done"),
            null,
            [new StageName("Code Review")],
            new MonthLabel("2026-03"),
            showTimeCalculationsInHoursOnly: true)));
        var settings = new GlobalIncidentsReportSettings(namespaceName: "Incidents");

        // Act
        var output = await RunWithTestConsoleAsync(console =>
        {
            service.ShowGlobalIncidentsReport(
                settings,
                new MonthLabel("2026-03"),
                [
                    new GlobalIncidentItem(
                        new IssueKey("INC-11861"),
                        new IssueSummary("NOVA - ORX disabled 10/03/2026"),
                        new DateTimeOffset(2026, 3, 10, 2, 34, 0, TimeSpan.Zero),
                        new DateTimeOffset(2026, 3, 10, 3, 23, 0, TimeSpan.Zero))
                ]);
            return Task.FromResult(console.Output);
        });

        // Assert
        output.Should().Contain("0.82h");
        output.Should().NotContain("49m");
    }

    [Fact(DisplayName = "ShowOpenIssuesByStatusSummary writes status and issue type breakdown")]
    [Trait("Category", "Unit")]
    public async Task ShowOpenIssuesByStatusSummaryWhenCalledWritesGroupedCounts()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();

        // Act
        var output = await RunWithTestConsoleAsync(console =>
        {
            service.ShowOpenIssuesByStatusSummary(
                [
                    new StatusIssueTypeSummary(
                        new StatusName("QA"),
                        new ItemCount(25),
                        [
                            new IssueTypeCountSummary(new IssueTypeName("UserStory"), new ItemCount(20)),
                            new IssueTypeCountSummary(new IssueTypeName("SubTask"), new ItemCount(5))
                        ])
                ],
                new StatusName("Done"),
                new StatusName("Reject"));
            return Task.FromResult(console.Output);
        });

        // Assert
        output.Should().Contain("General statistics");
        output.Should().Contain("Data as of:");
        output.Should().Contain("Scope:");
        output.Should().Contain("all not finished tasks");
        output.Should().Contain("Statuses excluded:");
        output.Should().Contain("Done, Reject");
        output.Should().Contain("Status");
        output.Should().Contain("Issues");
        output.Should().Contain("Breakdown by type");
        output.Should().Contain("QA");
        output.Should().Contain("25");
        output.Should().Contain("UserStory - 20");
        output.Should().Contain("SubTask - 5");
    }

    [Fact(DisplayName = "ShowOpenIssuesByStatusSummary writes empty state when no data")]
    [Trait("Category", "Unit")]
    public async Task ShowOpenIssuesByStatusSummaryWhenNoItemsWritesEmptyState()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();

        // Act
        var output = await RunWithTestConsoleAsync(console =>
        {
            service.ShowOpenIssuesByStatusSummary([], new StatusName("Done"), new StatusName("Reject"));
            return Task.FromResult(console.Output);
        });

        // Assert
        output.Should().Contain("General statistics");
        output.Should().Contain("Data as of:");
        output.Should().Contain("Scope:");
        output.Should().Contain("all not finished tasks");
        output.Should().Contain("No issues outside excluded statuses.");
    }

    [Fact(DisplayName = "Issue loading progress does not print Jira IDs")]
    [Trait("Category", "Unit")]
    public async Task ShowIssueLoadingProgressWhenCalledDoesNotPrintIssueIds()
    {
        // Arrange
        var service = new SpectreJiraPresentationService();

        // Act
        var output = await RunWithTestConsoleAsync(console =>
        {
            service.ShowIssueLoadingStarted(new ItemCount(3));
            service.ShowIssueLoaded(new IssueKey("AAA-1"));
            service.ShowIssueFailed(new IssueKey("AAA-2"));
            service.ShowIssueLoaded(new IssueKey("AAA-3"));
            service.ShowIssueLoadingCompleted(new ItemCount(2), new ItemCount(1));
            return Task.FromResult(console.Output);
        });

        // Assert
        output.Should().Contain("Loading issue timelines");
        output.Should().Contain("Issue loading completed");
        output.Should().NotContain("AAA-1");
        output.Should().NotContain("AAA-2");
        output.Should().NotContain("AAA-3");
    }

    private static async Task<T> RunWithTestConsoleAsync<T>(Func<TestConsole, Task<T>> action)
    {
        var original = AnsiConsole.Console;
        var console = new TestConsole();
        AnsiConsole.Console = console;

        try
        {
            return await action(console);
        }
        finally
        {
            AnsiConsole.Console = original;
        }
    }
}

