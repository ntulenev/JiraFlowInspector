using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace JiraMetrics.API.Mapping;

/// <summary>
/// Reads structured and custom Jira field values from raw JSON.
/// </summary>
[SuppressMessage(
    "Performance",
    "CA1822",
    Justification = "Stateless helper is composed as a service to keep parsing behavior grouped.")]
public sealed class JiraFieldValueReader
{
    internal bool HasPullRequestInRawValue(JsonElement rawValue) =>
        PullRequestDetector.HasPullRequest(rawValue);

    internal bool TryGetAdditionalFieldValue(
        Dictionary<string, JsonElement> additionalFields,
        string? fieldId,
        string? fieldName,
        out JsonElement value) =>
        JiraFieldValueParser.TryGetValue(additionalFields, fieldId, fieldName, out value);

    internal IReadOnlyList<string> ParseRawFieldValues(JsonElement rawValue) =>
        JiraFieldValueParser.Parse(rawValue);

    internal IReadOnlyList<string> ParseComponentValues(JsonElement rawComponents) =>
        ComponentValueReader.Read(rawComponents);

}
