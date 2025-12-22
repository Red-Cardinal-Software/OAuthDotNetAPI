namespace Application.Events.Auth;

/// <summary>
/// Domain event raised when a token is refreshed.
/// </summary>
public sealed record TokenRefreshedEvent : IDomainEvent
{
    /// <summary>
    /// The user ID of the user refreshing the token.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// The username of the user.
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// The IP address of the client.
    /// </summary>
    public string? IpAddress { get; init; }

    /// <inheritdoc />
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Whether the refresh was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The reason for failure if unsuccessful.
    /// </summary>
    public string? FailureReason { get; init; }
}