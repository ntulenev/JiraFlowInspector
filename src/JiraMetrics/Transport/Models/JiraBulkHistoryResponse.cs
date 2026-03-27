using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JiraMetrics.Transport.Models;

/// <summary>
/// Jira bulk changelog history DTO.
/// </summary>
public sealed class JiraBulkHistoryResponse
{
    /// <summary>
    /// Gets changelog creation timestamp. Jira bulk endpoint may return Unix seconds or Unix milliseconds.
    /// </summary>
    [JsonPropertyName("created")]
    public JsonElement Created { get; init; }

    /// <summary>
    /// Gets history items.
    /// </summary>
    [JsonPropertyName("items")]
    public IReadOnlyList<JiraHistoryItemResponse> Items { get; init; } = [];

    internal JiraHistoryResponse ToHistoryResponse()
    {
        return new JiraHistoryResponse
        {
            Created = ConvertCreatedToIsoString(),
            Items = Items
        };
    }

    private string? ConvertCreatedToIsoString()
    {
        return Created.ValueKind switch
        {
            JsonValueKind.Number when Created.TryGetInt64(out var unixTimestamp) =>
                ConvertUnixTimestampToIsoString(unixTimestamp),
            JsonValueKind.String => Created.GetString(),
            JsonValueKind.Number => null,
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            JsonValueKind.True => null,
            JsonValueKind.False => null,
            JsonValueKind.Object => null,
            JsonValueKind.Array => null,
            _ => null
        };
    }

    private static string? ConvertUnixTimestampToIsoString(long unixTimestamp)
    {
        try
        {
            var timestamp = Math.Abs(unixTimestamp) >= UNIX_MILLISECONDS_THRESHOLD
                ? DateTimeOffset.FromUnixTimeMilliseconds(unixTimestamp)
                : DateTimeOffset.FromUnixTimeSeconds(unixTimestamp);
            return timestamp.ToString("O", CultureInfo.InvariantCulture);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private const long UNIX_MILLISECONDS_THRESHOLD = 1_000_000_000_000;
}
