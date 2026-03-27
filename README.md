# JiraFlowInspector

<img src="FlowLogo.png" alt="logo" width="250">

JiraFlowInspector is a console analytics utility for Jira workflows.
It analyzes how issues move across statuses, highlights bug/release metrics, and can export the same report to PDF.

## Features

- Jira Cloud authentication with `Email` + `ApiToken`.
- Transition analytics for issues moved to `Done` in a selected month.
- Optional reject flow support (`RejectStatusName`).
- Optional bug ratio report with open/done/rejected/finished metrics.
- Optional release report by label, custom release date field, and optional environment filter.
- Optional architecture tasks report driven by custom JQL or JQL template.
- Optional global incidents report by namespace/project and JQL filter.
- P75 transition timing per path group.
- Timeline diagrams in console and PDF.
- Optional exclusion of weekends and specific calendar days from duration calculation.
- Optional hours-only display for duration and work-time metrics.
- Optional custom field filter (for team-level filtering).
- Optional retry policy for transient Jira API failures.
- Optional PDF export (QuestPDF), including clickable Jira links.

## What The App Does

1. Loads `Jira` settings from `appsettings.json`.
2. Authenticates with Jira.
3. Resolves reporting period from `MonthLabel` (or current UTC month if omitted).
4. Loads keys for issues that:
   `status CHANGED TO "<DoneStatusName>"` in month range
   and currently have `status = "<DoneStatusName>"`.
5. Optionally loads keys for `RejectStatusName` with the same final-status rule.
6. Optionally loads release issues for the month.
7. Optionally loads architecture tasks for the report.
8. Optionally loads global incidents for the month.
9. Optionally loads bug-ratio datasets.
10. Loads changelogs for selected issues and builds transition timelines.
11. Applies issue-type and required-stage filters.
12. Shows console sections.
13. Optionally writes PDF report.

## Important Behavior Rules

- Final status is required for done/rejected searches:
  issues that were moved to Done (or Reject) and later reopened are excluded.
- `CreatedAfter` applies to transition analytics key search only.
- `CustomFieldName` + `CustomFieldValue` are applied only when both are provided.
- Required stages use case-insensitive substring matching against both transition `From` and `To` statuses.
- Path grouping for transition analytics is built only from issues with detected code activity (`HasPullRequest = true`).

## Report Sections

### Console Output

- Report context:
  month, optional created-after date.
- Release report (optional):
  all releases in `MonthLabel` by `ProjectLabel`, with tasks/components/environment details.
- Architecture tasks report (optional):
  tasks loaded by configured JQL, with `Created At`, `Resolved At`, and `Days in work`.
  Open tasks use `Created At -> now`; resolved tasks use `Created At -> Resolved At`.
  Open items are highlighted in red in console and PDF.
- Global incidents report (optional):
  incidents in `MonthLabel` by configured namespace/project and optional JQL filter.
- Bug ratio (optional):
  open/done/rejected/finished counts and finished/created rate.
- Bug ratio details (optional):
  separate tables for Open, Done, Rejected issues.
- Transition analysis:
  done table, optional rejected table.
- Path group summary:
  successful, matched stage, failed, path groups, and filter note
  that only tasks with code artefacts are included.
- Path groups:
  path, issue list, timeline diagram, P75 transition table.
- Failed issues table (when any request fails per issue).

All list tables include `#` index column.

### PDF Output

When `Jira:Pdf:Enabled` is `true`, PDF includes:

- Header (`Jira Analytics`, generation timestamp, project, done status, month, optional created-after/custom-field filter).
- Release report (if configured).
- Architecture tasks report (if configured).
- Global incidents report (if configured).
- Bug ratio (if configured) and bug detail tables.
- Transition analysis tables (Done and optional Rejected).
- Path groups summary.
- Path groups with timeline diagrams and P75 tables.
- Failed issues (if any).

Jira issue identifiers are clickable links in PDF sections:
release table, bug detail tables, done/rejected tables, path-group issue list, and failures table.

## Metrics Logic

### Transition Analytics

- Source set:
  issues moved to `DoneStatusName` during month and currently in that status.
- If `RejectStatusName` configured:
  rejected source set is loaded similarly.
- Issue types are filtered after loading timelines.
- Required path stages are enforced after issue-type filtering.
- Path groups are built from filtered issues with code activity only.
- P75 is calculated per transition index within each path group.

### Bug Ratio

`BugIssueNames` defines which issue types are treated as bugs.

- Open this month:
  bug issues created in month and not in finished set.
- Done this month:
  bug issues moved to `DoneStatusName` in month and currently in done status.
- Rejected this month:
  bug issues moved to `RejectStatusName` in month and currently in rejected status.
- Finished this month:
  union of Done and Rejected issue keys.
- Finished / Created:
  `finished / created * 100`.

### Release Report

Release query uses:

- `project = ReleaseProjectKey`
- `labels = ProjectLabel`
- `ReleaseDateFieldName` in `MonthLabel` range.
- optional `EnvironmentFieldName = EnvironmentFieldValue`

Per release row:

- `Status`:
  current Jira issue status.
- `Tasks`:
  count of all linked work items (both inward/outward links).
- `Components`:
  count from configured components field; fallback to standard Jira `components`.
  Supports array/string/object custom-field payloads.
- `Environments`:
  values from configured environment field.
  Supports array/string/object custom-field payloads.
- `Rollback type`:
  payload from configured rollback field; if empty then `-`.
- `0` task/component values are displayed as `-`.
- Release totals:
  `Total releases`, `Hotfix count`, and `Rollbacks count` are shown after release table.
- Hot-fix detection:
  uses `HotFixRules` dictionary (`field -> values[]`).
  If release matches any configured rule, release row fields are rendered in red.

When `ReleaseReport.ComponentsFieldName` is configured, report also shows
`Components release table` after the release table with:

- `Component name`
- `Release counts` (ordered descending)

### Architecture Tasks Report

Architecture tasks query uses the raw `ArchTasks.Jql` value.

- You can provide a fixed JQL query.
- You can also provide a JQL template using `{{MonthResolvedClause}}`.
  The placeholder is replaced with the current `MonthLabel` range clause for resolved date filtering.

Per task row:

- `Created At`:
  Jira issue creation timestamp.
- `Resolved At`:
  Jira resolution timestamp, or `-` when task is still open.
- `Days in work`:
  `Resolved At - Created At` for resolved tasks, or `now - Created At` for open tasks.
- Open tasks are rendered in red in the `Days in work` column.
- Summary line shows `Total tasks`, `Resolved`, and `Open`.

### Global Incidents Report

Global incidents query uses:

- `project = GlobalIncidents.Namespace`
- `IncidentStartFieldName` in `MonthLabel` range
- if `IncidentStartFallbackFieldName` is configured:
  incident also matches when fallback start field is in `MonthLabel` range
- optional raw `JqlFilter` clause
- fallback text-term filters derived from `SearchPhrase` when `JqlFilter` is not configured

Per incident row:

- `Incident Start UTC`
  first non-empty value from `IncidentStartFieldName`, then `IncidentStartFallbackFieldName`
- `Incident Recovery UTC`
  first non-empty value from `IncidentRecoveryFieldName`, then `IncidentRecoveryFallbackFieldName`
- `Duration` (recovery minus start when both timestamps are present)
- `Impact`
- `Urgency`
- optional configured `AdditionalFieldNames` rendered in one column

## Duration Calculation

Transition durations come from time between consecutive status changes.

- If `ExcludeWeekend = true`, Saturday/Sunday time is removed.
- Any date in `ExcludedDays` is removed.
- If `ShowTimeCalculationsInHoursOnly = true`, duration values are rendered strictly in hours.
  This affects incident durations, work-duration columns in transition tables, P75 per-type output, and path-group TTM/P75 labels.
- Supported date formats in `ExcludedDays`:
  `dd.MM.yyyy` and `yyyy-MM-dd`.

## Pull Request Detection

Code activity (`HasPullRequest`) is detected from issue additional fields by searching for pull request data.
The detector checks configured pull request field (`PullRequestFieldName`);

## Configuration (`appsettings.json`)

All options live under `Jira`.

- `BaseUrl` (`string`, required):
  Jira base URL, for example `https://company.atlassian.net`.
- `Email` (`string`, required):
  Jira account email.
- `ApiToken` (`string`, required):
  Jira API token.
- `PullRequestFieldName` (`string`, optional):
  Jira field for detecting pull request activity.
- `TeamTasks` (`object`, required):
  transition and bug report settings.
- `TeamTasks.ProjectKey` (`string`, required):
  project used for transition and bug queries.
- `TeamTasks.DoneStatusName` (`string`, required):
  done status name.
- `TeamTasks.RejectStatusName` (`string`, optional):
  rejected status name.
- `TeamTasks.CustomFieldName` (`string`, optional):
  custom field name for filtering.
- `TeamTasks.CustomFieldValue` (`string`, optional):
  custom field value for filtering.
- `TeamTasks.IssueTransitions` (`object`, required):
  transition analysis settings.
- `TeamTasks.IssueTransitions.RequiredPathStages` (`string[]`, required, at least one):
  all stages must be present in issue transition path.
- `TeamTasks.IssueTransitions.IssueTypes` (`string[]`, optional):
  allowed issue types for transition analysis.
- `TeamTasks.IssueTransitions.ExcludeWeekend` (`bool`, optional, default `false`):
  exclude weekend time from durations.
- `TeamTasks.IssueTransitions.ExcludedDays` (`string[]`, optional):
  exact days excluded from durations.
- `TeamTasks.BugRatio` (`object`, optional):
  bug ratio settings.
- `TeamTasks.BugRatio.BugIssueNames` (`string[]`, optional):
  issue types treated as bug-like.
- `ReleaseReport` (`object`, optional):
  release report settings.
- `ArchTasks` (`object`, optional):
  architecture-tasks report settings.
- `ArchTasks.Jql` (`string`, required when `ArchTasks` used):
  raw JQL or JQL template used to load architecture-review items.
  Example:
  `project = CORE AND type = "Architecture Review" AND (resolved IS EMPTY OR {{MonthResolvedClause}}) ORDER BY created ASC`
- `ReleaseReport.ReleaseProjectKey` (`string`, required when `ReleaseReport` used):
  project containing release issues.
- `ReleaseReport.ProjectLabel` (`string`, required when `ReleaseReport` used):
  label filter for releases.
- `ReleaseReport.ReleaseDateFieldName` (`string`, required when `ReleaseReport` used):
  Jira field display name storing release date.
- `ReleaseReport.ComponentsFieldName` (`string`, optional):
  Jira field name for components counting.
- `ReleaseReport.EnvironmentFieldName` (`string`, optional):
  Jira field name or Jira field id used both for release filtering and for displaying environments in the release table.
- `ReleaseReport.EnvironmentFieldValue` (`string`, optional):
  Jira environment value used for release filtering.
- `ReleaseReport.RollbackFieldName` (`string`, optional, default `Rollback type`):
  Jira field name for rollback payload in release table.
- `ReleaseReport.HotFixRules` (`object`, optional, default `{ "Change type": ["Emergency"] }`):
  hot-fix rules dictionary where key is Jira field name and value is list of accepted marker values.
  Issue is treated as hot-fix when any rule matches.

Notes:

- Jira can use different environment fields in different projects.
- In some projects, the environment data can be stored in a field with display name like `Environment`.
- In other projects, the same data can be stored in a different field, for example `Environments`, or only be reliably addressable by custom field id.
- If release report returns `No releases found for selected month`, verify `ReleaseProjectKey`, `MonthLabel`, and the actual Jira field used for environments in that project.
- `Pdf` (`object`, optional):
  PDF settings.
- `GlobalIncidents` (`object`, optional):
  global incidents report settings.
- `GlobalIncidents.Namespace` (`string`, optional, default `Incidents`):
  Jira namespace/project used for incidents search.
- `GlobalIncidents.JqlFilter` (`string`, optional):
  raw JQL clause appended to the global-incidents query.
  When configured, it takes precedence over `SearchPhrase`.
- `GlobalIncidents.SearchPhrase` (`string`, optional):
  legacy free-text filter split into Jira `text ~ "<term>*"` terms.
  Used only when `JqlFilter` is not configured.
- `GlobalIncidents.IncidentStartFieldName` (`string`, optional, default `Incident Start date/time UTC`):
  primary Jira field name storing incident start.
- `GlobalIncidents.IncidentStartFallbackFieldName` (`string`, optional):
  fallback Jira field name used when primary start field is empty.
- `GlobalIncidents.IncidentRecoveryFieldName` (`string`, optional, default `Incident Recovery date/time UTC`):
  primary Jira field name storing incident recovery.
- `GlobalIncidents.IncidentRecoveryFallbackFieldName` (`string`, optional):
  fallback Jira field name used when primary recovery field is empty.
- `GlobalIncidents.ImpactFieldName` (`string`, optional, default `Impact`):
  Jira field name used for impact output.
- `GlobalIncidents.UrgencyFieldName` (`string`, optional, default `Urgency`):
  Jira field name used for urgency output.
- `GlobalIncidents.AdditionalFieldNames` (`string[]`, optional):
  extra Jira field names shown in an aggregated `Additional fields` column.
- `Pdf.Enabled` (`bool`, optional, default `true`):
  enables PDF generation.
- `Pdf.OpenAfterGeneration` (`bool`, optional, default `true`):
  opens generated PDF in the system default viewer after save.
- `Pdf.OutputPath` (`string`, optional, default `jiraflowinspector-report.pdf`):
  output file path.
  Actual file name gets date suffix `_<dd_MM_yyyy>` before extension.
- `ShowTimeCalculationsInHoursOnly` (`bool`, optional, default `false`):
  renders duration values strictly in hours instead of day-based work metrics/default duration labels.
- `MonthLabel` (`string`, optional, format `yyyy-MM`):
  reporting month, defaults to current UTC month.
- `CreatedAfter` (`string`, optional, format `yyyy-MM-dd`):
  created-date lower bound for transition source query.
- `RetryCount` (`int`, optional, range `0..10`, default `0`):
  retry attempts for transient transport failures.

## Example Configuration

```json
{
  "Jira": {
    "BaseUrl": "https://your-company.atlassian.net",
    "Email": "your-email@company.com",
    "ApiToken": "your-jira-api-token",
    "PullRequestFieldName": "customfield_123",
    "TeamTasks": {
      "ProjectKey": "AAA",
      "DoneStatusName": "Done",
      "RejectStatusName": "Reject",
      "IssueTransitions": {
        "RequiredPathStages": [ "Code Review", "Release Candidate" ],
        "IssueTypes": [ "Task", "Bug", "Subtask" ],
        "ExcludeWeekend": true,
        "ExcludedDays": [
          "01.01.2026",
          "02.01.2026"
        ]
      },
      "BugRatio": {
        "BugIssueNames": [ "Bug" ]
      },
      "CustomFieldName": "Team",
      "CustomFieldValue": "Team1"
    },
    "ReleaseReport": {
      "ReleaseProjectKey": "RLS",
      "ProjectLabel": "AAA",
      "ReleaseDateFieldName": "Change completion date",
      "ComponentsFieldName": "Components",
      "EnvironmentFieldName": "your-environment-field-name-or-id",
      "EnvironmentFieldValue": "your-environment-filter-value",
      "RollbackFieldName": "Rollback type",
      "HotFixRules": {
        "Change type": [ "Emergency" ],
        "Change reason": [ "Repair", "Mitigation" ]
      }
    },
    "ArchTasks": {
      "Jql": "project = CORE AND type = \"Architecture Review\" AND (resolved IS EMPTY OR {{MonthResolvedClause}}) ORDER BY created ASC"
    },
    "GlobalIncidents": {
      "Namespace": "Incidents",
      "JqlFilter": "(\"Incident categorization\" = SERVICE OR labels = SERVICE OR summary ~ \"SERVICE\") AND (summary ~ \"disab*\" OR summary ~ \"unavail*\" OR summary ~ \"downtime\")",
      "IncidentStartFieldName": "Incident Start date/time UTC",
      "IncidentStartFallbackFieldName": "Incident Start date/time user timezone",
      "IncidentRecoveryFieldName": "Incident Recovery date/time UTC",
      "IncidentRecoveryFallbackFieldName": "Incident Recovery date/time user timezone",
      "ImpactFieldName": "Impact",
      "UrgencyFieldName": "Urgency",
      "AdditionalFieldNames": [ "Business Impact" ]
    },
    "Pdf": {
      "Enabled": true,
      "OpenAfterGeneration": true,
      "OutputPath": "jiraflowinspector-report.pdf"
    },
    "ShowTimeCalculationsInHoursOnly": false,
    "CreatedAfter": "2026-01-01",
    "MonthLabel": "2026-02",
    "RetryCount": 0
  }
}
```

## Build And Run

Prerequisite: .NET SDK with support for `net10.0`.

```bash
dotnet restore src/JiraMetrics.slnx
dotnet run --project src/JiraMetrics/JiraMetrics.csproj
```

Run tests:

```bash
dotnet test src/JiraMetrics.slnx
```


## Output Screenshot

>For demonstration purposes, the program output shown in the screenshots uses synthetic data to avoid exposing information from real repositories and users.


### Console
<img src="Jira1.png" alt="Output1">
<img src="Jira2.png" alt="Output2">

### PDF
<img src="Jira3.png" alt="Output3">
<img src="Jira4.png" alt="Output4">
<img src="Jira5.png" alt="Output5">
