using Domain.Entities.Security;

namespace Application.Interfaces.Services;

/// <summary>
/// Service interface for managing email-based MFA operations.
/// Handles code generation, sending, and verification for email MFA.
/// </summary>
public interface IMfaEmailService
{
    /// <summary>
    /// Sends an MFA verification code to the user's email address.
    /// </summary>
    /// <param name="challengeId">The MFA challenge ID</param>
    /// <param name="userId">The user ID</param>
    /// <param name="emailAddress">The email address to send the code to</param>
    /// <param name="ipAddress">Optional IP address for security tracking</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Success result with code expiration time</returns>
    Task<MfaEmailSendResult> SendCodeAsync(
        Guid challengeId,
        Guid userId,
        string emailAddress,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies an email MFA code against a challenge.
    /// </summary>
    /// <param name="challengeId">The MFA challenge ID</param>
    /// <param name="code">The code to verify</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Verification result</returns>
    Task<MfaEmailVerificationResult> VerifyCodeAsync(
        Guid challengeId,
        string code,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has exceeded rate limits for email codes.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Rate limit check result</returns>
    Task<MfaRateLimitResult> CheckRateLimitAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up expired email codes for maintenance.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of codes deleted</returns>
    Task<int> CleanupExpiredCodesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of sending an email MFA code.
/// </summary>
public class MfaEmailSendResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public int RemainingAttempts { get; init; }

    public static MfaEmailSendResult Successful(DateTimeOffset expiresAt, int remainingAttempts) =>
        new() { Success = true, ExpiresAt = expiresAt, RemainingAttempts = remainingAttempts };

    public static MfaEmailSendResult Failed(string errorMessage) =>
        new() { Success = false, ErrorMessage = errorMessage };
}

/// <summary>
/// Result of verifying an email MFA code.
/// </summary>
public class MfaEmailVerificationResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public int RemainingAttempts { get; init; }

    public static MfaEmailVerificationResult Successful() =>
        new() { Success = true };

    public static MfaEmailVerificationResult Failed(string errorMessage, int remainingAttempts) =>
        new() { Success = false, ErrorMessage = errorMessage, RemainingAttempts = remainingAttempts };
}

/// <summary>
/// Result of checking email MFA rate limits.
/// </summary>
public class MfaRateLimitResult
{
    public bool IsAllowed { get; init; }
    public int CodesUsed { get; init; }
    public int MaxCodesAllowed { get; init; }
    public DateTimeOffset WindowResetTime { get; init; }

    public static MfaRateLimitResult Allowed(int codesUsed, int maxAllowed, DateTimeOffset resetTime) =>
        new() { IsAllowed = true, CodesUsed = codesUsed, MaxCodesAllowed = maxAllowed, WindowResetTime = resetTime };

    public static MfaRateLimitResult Exceeded(int codesUsed, int maxAllowed, DateTimeOffset resetTime) =>
        new() { IsAllowed = false, CodesUsed = codesUsed, MaxCodesAllowed = maxAllowed, WindowResetTime = resetTime };
}