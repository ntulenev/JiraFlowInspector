namespace JiraMetrics.Abstractions;

/// <summary>
/// Provides a deserialization abstraction.
/// </summary>
public interface ISerializer
{
    /// <summary>
    /// Deserializes JSON into a <typeparamref name="T"/> instance.
    /// </summary>
    /// <typeparam name="T">Target type.</typeparam>
    /// <param name="json">JSON payload.</param>
    /// <returns>Deserialized instance or null when the JSON literal is <c>null</c>.</returns>
    T? Deserialize<T>(string json);
}
