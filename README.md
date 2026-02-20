# JiraFlowInspector

<img src="FlowLogo.png" alt="logo" width="250">

JiraFlowInspector is a console analytics utility for tracking Jira issue transition flow.
It helps detect bottlenecks and compare transition paths with P75 timing.



## How the service works
1. Loads configuration from `appsettings.json` (`Jira` section).
2. Authenticates against Jira Cloud API using basic auth (`Email` + `ApiToken`).
3. Fetches issues that changed status to the configured done status since start of month.
4. Loads each issue changelog and builds transition timelines.
5. Optionally filters issues by configured Jira issue types.
6. Filters issues by required stages presence in the transition path.
7. Groups issues by transition path.
8. Calculates P75 transition duration per path.
9. Renders output tables:
   - Issues moved to done
   - Bug ratio (optional)
   - Path groups with P75 timeline
   - Failure report (if any)

## appsettings.json parameters
### `Jira` section
Application options are under the `Jira` object.

- `BaseUrl` (`string`, required): Jira base URL (for example `https://your-company.atlassian.net`).
- `Email` (`string`, required): Jira account email used for authentication.
- `ApiToken` (`string`, required): Jira API token used for authentication.
- `ProjectKey` (`string`, required): Jira project key used in JQL filter.
- `DoneStatusName` (`string`, required): Target done status name used in JQL filter.
- `RejectStatusName` (`string`, optional): Status name treated as rejected in bug ratio calculations (for example `Reject`).
- `IssueTransitions` (`object`, required): Settings for transition-path analytics.
- `IssueTransitions.RequiredPathStages` (`string[]`, required): Stages that must all be present in the issue transition path.
- `IssueTransitions.IssueTypes` (`string[]`, optional): Allowed Jira issue types filter (for example `["Bug", "Story"]`). When omitted or empty, all issue types are included.
- `IssueTransitions.ExcludeWeekend` (`bool`, optional): When `true`, Saturday/Sunday time is excluded from transition durations; defaults to `false`.
- `IssueTransitions.ExcludedDays` (`string[]`, optional): List of excluded dates (`dd.MM.yyyy` or `yyyy-MM-dd`); time spent on those days is excluded from transition durations.
- `BugRatio` (`object`, optional): Settings for bug-ratio report section.
- `BugRatio.BugIssueNames` (`string[]`, optional): Issue types treated as bugs (for example `["Bug"]`). When configured, report prints bug counts created in the selected month, moved to done, moved to rejected, and finished (`done + rejected`) in the selected month.
  Bug ratio section also prints three details tables (`Open issues`, `Done issues`, `Rejected issues`) with `Jira ID` and `Title`.
- `CustomFieldName` (`string`, optional): Custom field name used for filtering (for example `Team`). Applied only when both name and value are provided.
- `CustomFieldValue` (`string`, optional): Custom field value used for filtering (for example `Import`). Applied only when both name and value are provided.
- `MonthLabel` (`string`, optional): Month used to filter issues moved to done (`yyyy-MM`); defaults to current UTC month when omitted.
- `CreatedAfter` (`string`, optional): Lower bound for issue creation date (`yyyy-MM-dd`); adds `created >= "<date>"` to JQL when provided.
- `RetryCount` (`int`, optional): Number of retries for transient Jira API failures (`0..10`, default `0`).

## Example configuration
```json
{
  "Jira": {
    "BaseUrl": "https://your-company.atlassian.net",
    "Email": "your-email@company.com",
    "ApiToken": "your-jira-api-token",
    "ProjectKey": "ABC",
    "DoneStatusName": "Done",
    "RejectStatusName": "Reject",
    "IssueTransitions": {
      "RequiredPathStages": ["Code Review", "QA"],
      "IssueTypes": ["Bug", "Story"],
      "ExcludeWeekend": false,
      "ExcludedDays": ["10.01.2026", "05.02.2026"]
    },
    "BugRatio": {
      "BugIssueNames": ["Bug"]
    },
    "CustomFieldName": "Team",
    "CustomFieldValue": "Import",
    "CreatedAfter": "2026-01-01",
    "MonthLabel": "2026-02",
    "RetryCount": 2
  }
}
```

## Output

<img src="Screenshot.png" alt="Output">
