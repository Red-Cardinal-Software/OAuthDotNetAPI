namespace Application.Events.Auth;

/// <summary>
/// Domain event raised when a login attempt occurs (successful or failed).
/// </summary>
public sealed record LoginAttemptedEvent : IDomainEvent
{
    /// <summary>
    /// The user ID if the user exists, otherwise Guid.Empty.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// The username/email used in the login attempt.
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// Whether the login attempt was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The reason for failure if the attempt was unsuccessful.
    /// </summary>
    public string? FailureReason { get; init; }

    /// <summary>
    /// The IP address of the client making the login attempt.
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>
    /// The user agent string from the request.
    /// </summary>
    public string? UserAgent { get; init; }

    /// <inheritdoc />
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Whether MFA was required for this login.
    /// </summary>
    public bool MfaRequired { get; init; }

    /// <summary>
    /// Whether the account was locked as a result of this attempt.
    /// </summary>
    public bool AccountLocked { get; init; }
}