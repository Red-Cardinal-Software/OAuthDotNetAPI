namespace Application.Models;

/// <summary>
/// Represents a standard response format used across the application
/// for wrapping the result of an operation or service execution.
/// </summary>
/// <typeparam name="T">The type of data being returned in the response.</typeparam>
public class ServiceResponse<T>
{
    private int _status = 200;

    /// <summary>
    /// Gets or sets the data object of type <typeparamref name="T"/>
    /// that represents the core payload of the service response.
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the service response is successful.
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Gets or sets the message providing additional context about the result
    /// of the service operation, which can include error details or success notes.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the HTTP status code representing the outcome of the service response.
    /// When set to 400 or above, Success is automatically set to false.
    /// </summary>
    public int Status
    {
        get => _status;
        set
        {
            _status = value;
            if (value >= 400)
            {
                Success = false;
            }
        }
    }

    /// <summary>
    /// Gets or sets the metadata associated with the service response,
    /// providing additional context or supplementary information about the operation or result.
    /// Specifically, used in cases like issuing a new JWT and RefreshToken
    /// </summary>
    public object? Metadata { get; set; }
}