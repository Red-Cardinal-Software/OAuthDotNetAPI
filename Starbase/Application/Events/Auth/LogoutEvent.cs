namespace Application.Events.Auth;

/// <summary>
/// Domain event raised when a user logs out.
/// </summary>
public sealed record LogoutEvent : IDomainEvent
{
    /// <summary>
    /// The user ID of the user logging out.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// The username of the user logging out.
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
    /// Whether this was a forced logout (e.g., from admin action or security event).
    /// </summary>
    public bool Forced { get; init; }
}