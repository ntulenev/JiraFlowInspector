using System.Text.Json;


namespace JiraMetrics.Transport;

/// <summary>
/// System.Text.Json based serializer implementation.
/// </summary>
public sealed class SimpleJsonSerializer : ISerializer
{
    /// <inheritdoc />
    public string Serialize<T>(T value) => JsonSerializer.Serialize(value, _jsonOptions);

    /// <inheritdoc />
    public T? Deserialize<T>(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        return JsonSerializer.Deserialize<T>(json, _jsonOptions);
    }

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}

