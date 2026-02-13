# JiraFlowInspector

JiraFlowInspector is a console analytics utility for tracking Jira issue transition flow.
It helps detect bottlenecks and compare transition paths with P75 timing.

## Project structure
- `src/JiraMetrics/Abstractions`: Service contracts/interfaces.
- `src/JiraMetrics/Models`: Domain models (one model per file).
- `src/JiraMetrics/Models/ValueObjects`: Domain primitive wrappers.
- `src/JiraMetrics/Models/Configuration`: Options/configuration models and validation.
- `src/JiraMetrics/Logic`: Configuration + analytics + orchestration logic.
- `src/JiraMetrics/Transport`: Jira API transport client and response DTOs.
- `src/JiraMetrics/Presentation`: Console UI rendering (Spectre.Console).
- `src/JiraMetrics.Tests/Logic`: Unit tests split per tested type.

## How the service works
1. Loads configuration from `appsettings.json` (`Jira` section).
2. Authenticates against Jira Cloud API using basic auth (`Email` + `ApiToken`).
3. Fetches issues that changed status to the configured done status since start of month.
4. Loads each issue changelog and builds transition timelines.
5. Filters issues by required stage presence in the transition path.
6. Groups issues by transition path.
7. Calculates P75 transition duration per path.
8. Renders output tables:
   - Issues moved to done
   - Path groups with P75 timeline
   - Failure report (if any)

## appsettings.json parameters
All settings are under the `Jira` object.

- `BaseUrl` (`string`): Jira base URL (for example `https://your-company.atlassian.net`).
- `Email` (`string`): Jira account email used for authentication.
- `ApiToken` (`string`): Jira API token used for authentication.
- `ProjectKey` (`string`): Jira project key used in JQL filter.
- `DoneStatusName` (`string`): Target done status name used in JQL filter.
- `RequiredPathStage` (`string`): Stage that must be present in the issue transition path.
- `MonthLabel` (`string`): Label shown in the console filter summary.

## Example configuration
```json
{
  "Jira": {
    "BaseUrl": "https://your-company.atlassian.net",
    "Email": "your-email@company.com",
    "ApiToken": "your-jira-api-token",
    "ProjectKey": "",
    "DoneStatusName": "Done",
    "RequiredPathStage": "Code Review",
    "MonthLabel": "2026-02"
  }
}
```

## Run
```bash
dotnet run --project src/JiraMetrics/JiraMetrics.csproj
```

## Test
```bash
dotnet test src/JiraMetrics.slnx
```
