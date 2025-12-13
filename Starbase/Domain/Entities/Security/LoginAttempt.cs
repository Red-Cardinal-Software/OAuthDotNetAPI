using Domain.Exceptions;

namespace Domain.Entities.Security;

/// <summary>
/// Represents a login attempt made by a user, used for tracking failed login attempts
/// and implementing account lockout mechanisms to prevent brute force attacks.
/// This entity follows Domain-Driven Design principles with protected state mutations
/// and invariant enforcement.
/// </summary>
public class LoginAttempt
{
    /// <summary>
    /// Unique identifier for the login attempt.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The unique identifier of the user who made the login attempt.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// The username or email used in the login attempt.
    /// This is stored for auditing purposes and to handle cases where
    /// the username doesn't correspond to an existing user.
    /// </summary>
    public string AttemptedUsername { get; private set; } = string.Empty;

    /// <summary>
    /// The IP address from which the login attempt was made.
    /// Used for security auditing and potential IP-based lockout mechanisms.
    /// </summary>
    public string? IpAddress { get; private set; }

    /// <summary>
    /// The user agent string from the login attempt request.
    /// Used for security auditing and detecting potential automated attacks.
    /// </summary>
    public string? UserAgent { get; private set; }

    /// <summary>
    /// Indicates whether the login attempt was successful.
    /// </summary>
    public bool IsSuccessful { get; private set; }

    /// <summary>
    /// The reason for login failure, if the attempt was unsuccessful.
    /// Examples: "Invalid credentials", "Account locked", "Account disabled"
    /// </summary>
    public string? FailureReason { get; private set; }

    /// <summary>
    /// The timestamp when the login attempt was made.
    /// </summary>
    public DateTimeOffset AttemptedAt { get; private set; }

    /// <summary>
    /// Additional metadata about the attempt in JSON format.
    /// Can include information like geolocation, device fingerprint, etc.
    /// </summary>
    public string? Metadata { get; private set; }

    /// <summary>
    /// Private constructor for Entity Framework Core.
    /// </summary>
    private LoginAttempt()
    {
    }

    /// <summary>
    /// Creates a new login attempt record for a successful login.
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <param name="attemptedUsername">The username used in the attempt</param>
    /// <param name="ipAddress">The IP address of the request</param>
    /// <param name="userAgent">The user agent string</param>
    /// <param name="metadata">Optional metadata in JSON format</param>
    /// <returns>A new LoginAttempt instance representing a successful login</returns>
    public static LoginAttempt CreateSuccessful(
        Guid userId,
        string attemptedUsername,
        string? ipAddress = null,
        string? userAgent = null,
        string? metadata = null)
    {
        ValidateInputs(userId, attemptedUsername);

        return new LoginAttempt
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AttemptedUsername = attemptedUsername.Trim(),
            IpAddress = ipAddress?.Trim(),
            UserAgent = userAgent?.Trim(),
            IsSuccessful = true,
            FailureReason = null,
            AttemptedAt = DateTimeOffset.UtcNow,
            Metadata = metadata?.Trim()
        };
    }

    /// <summary>
    /// Creates a new login attempt record for a failed login.
    /// </summary>
    /// <param name="userId">The unique identifier of the user (use Empty for non-existent users)</param>
    /// <param name="attemptedUsername">The username used in the attempt</param>
    /// <param name="failureReason">The reason for the login failure</param>
    /// <param name="ipAddress">The IP address of the request</param>
    /// <param name="userAgent">The user agent string</param>
    /// <param name="metadata">Optional metadata in JSON format</param>
    /// <returns>A new LoginAttempt instance representing a failed login</returns>
    public static LoginAttempt CreateFailed(
        Guid userId,
        string attemptedUsername,
        string failureReason,
        string? ipAddress = null,
        string? userAgent = null,
        string? metadata = null)
    {
        ValidateInputs(userId, attemptedUsername);

        if (string.IsNullOrWhiteSpace(failureReason))
            throw new InvalidLoginAttemptException("Failure reason cannot be empty for failed login attempts");

        return new LoginAttempt
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AttemptedUsername = attemptedUsername.Trim(),
            IpAddress = ipAddress?.Trim(),
            UserAgent = userAgent?.Trim(),
            IsSuccessful = false,
            FailureReason = failureReason.Trim(),
            AttemptedAt = DateTimeOffset.UtcNow,
            Metadata = metadata?.Trim()
        };
    }

    /// <summary>
    /// Validates common inputs for login attempt creation.
    /// </summary>
    /// <param name="userId">The user identifier to validate</param>
    /// <param name="attemptedUsername">The username to validate</param>
    /// <exception cref="InvalidLoginAttemptException">Thrown when validation fails</exception>
    private static void ValidateInputs(Guid userId, string attemptedUsername)
    {
        if (string.IsNullOrWhiteSpace(attemptedUsername))
            throw new InvalidLoginAttemptException("Attempted username cannot be empty");

        if (attemptedUsername.Length > 255)
            throw new InvalidLoginAttemptException("Attempted username cannot exceed 255 characters");
    }

    /// <summary>
    /// Determines if this attempt represents a security-relevant failure
    /// that should count towards account lockout.
    /// </summary>
    /// <returns>True if this failure should count towards lockout</returns>
    public bool ShouldCountTowardsLockout()
    {
        return !IsSuccessful &&
               UserId != Guid.Empty &&
               FailureReason?.Contains("Invalid credentials", StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <summary>
    /// Checks if this attempt occurred within the specified time window.
    /// </summary>
    /// <param name="timeWindow">The time window to check against</param>
    /// <returns>True if the attempt occurred within the time window</returns>
    public bool IsWithinTimeWindow(TimeSpan timeWindow)
    {
        return DateTimeOffset.UtcNow - AttemptedAt <= timeWindow;
    }
}