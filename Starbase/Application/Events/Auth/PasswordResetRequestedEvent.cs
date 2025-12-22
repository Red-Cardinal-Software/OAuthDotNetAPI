namespace Application.Events.Auth;

/// <summary>
/// Domain event raised when a password reset is requested.
/// </summary>
public sealed record PasswordResetRequestedEvent : IDomainEvent
{
    /// <summary>
    /// The user ID if the user exists, otherwise Guid.Empty.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// The email address used for the reset request.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// The IP address of the client.
    /// </summary>
    public string? IpAddress { get; init; }

    /// <inheritdoc />
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Whether the user exists in the system.
    /// Note: For security, the API response should be the same regardless.
    /// </summary>
    public bool UserExists { get; init; }
}