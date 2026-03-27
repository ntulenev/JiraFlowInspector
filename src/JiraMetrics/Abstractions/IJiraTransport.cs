namespace JiraMetrics.Abstractions;

/// <summary>
/// Abstraction for Jira HTTP transport and JSON handling.
/// </summary>
public interface IJiraTransport
{
    /// <summary>
    /// Issues a GET request and deserializes the JSON response.
    /// </summary>
    /// <typeparam name="TDto">DTO type to deserialize.</typeparam>
    /// <param name="url">Relative or absolute URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Deserialized DTO or null if response body is null.</returns>
    Task<TDto?> GetAsync<TDto>(Uri url, CancellationToken cancellationToken);

    /// <summary>
    /// Issues a POST request with a JSON payload and deserializes the JSON response.
    /// </summary>
    /// <typeparam name="TRequest">Request DTO type.</typeparam>
    /// <typeparam name="TDto">Response DTO type.</typeparam>
    /// <param name="url">Relative or absolute URL.</param>
    /// <param name="request">Request payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Deserialized DTO or null if response body is null.</returns>
    Task<TDto?> PostAsync<TRequest, TDto>(Uri url, TRequest request, CancellationToken cancellationToken);
}
