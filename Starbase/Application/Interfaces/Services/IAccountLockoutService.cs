using Domain.Entities.Security;

namespace Application.Interfaces.Services;

/// <summary>
/// Service interface for managing account lockout functionality.
/// Provides methods for tracking login attempts, managing account lockout state,
/// and implementing security policies to protect against brute force attacks.
/// </summary>
public interface IAccountLockoutService
{
    /// <summary>
    /// Records a failed login attempt for the specified user and determines
    /// if the account should be locked based on configured policies.
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <param name="username">The username attempted in the login</param>
    /// <param name="ipAddress">The IP address of the login attempt</param>
    /// <param name="userAgent">The user agent string of the login attempt</param>
    /// <param name="failureReason">The reason for the login failure</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the account was locked as a result of this attempt</returns>
    Task<bool> RecordFailedAttemptAsync(
        Guid userId,
        string username,
        string? ipAddress,
        string? userAgent,
        string failureReason,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a successful login attempt for the specified user,
    /// which resets the failed attempt counter and unlocks the account if it was
    /// locked due to failed attempts.
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <param name="username">The username used in the successful login</param>
    /// <param name="ipAddress">The IP address of the login attempt</param>
    /// <param name="userAgent">The user agent string of the login attempt</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RecordSuccessfulLoginAsync(
        Guid userId,
        string username,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the specified user account is currently locked out.
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Account lockout information if locked, null if not locked</returns>
    Task<AccountLockout?> GetAccountLockoutAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines if a user account is currently locked out.
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the account is locked out</returns>
    Task<bool> IsAccountLockedOutAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Manually locks a user account with the specified duration and reason.
    /// This is typically used by administrators for security or policy enforcement.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to lock</param>
    /// <param name="duration">Duration of the lockout (null for indefinite)</param>
    /// <param name="reason">Reason for the manual lockout</param>
    /// <param name="lockedByUserId">ID of the user performing the lockout</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task LockAccountAsync(
        Guid userId,
        TimeSpan? duration,
        string reason,
        Guid lockedByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Manually unlocks a user account and optionally resets the failed attempt counter.
    /// This is typically used by administrators to restore account access.
    /// </summary>
    /// <param name="userId">The unique identifier of the user to unlock</param>
    /// <param name="resetFailedAttempts">Whether to reset the failed attempt counter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UnlockAccountAsync(
        Guid userId,
        bool resetFailedAttempts = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the remaining lockout duration for a user account.
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Remaining lockout duration, or null if not locked</returns>
    Task<TimeSpan?> GetRemainingLockoutDurationAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent login attempts for a user within the specified time period.
    /// This can be used for security auditing and analysis.
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <param name="timePeriod">The time period to look back</param>
    /// <param name="includeSuccessful">Whether to include successful attempts</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of login attempts within the time period</returns>
    Task<IReadOnlyList<LoginAttempt>> GetRecentLoginAttemptsAsync(
        Guid userId,
        TimeSpan timePeriod,
        bool includeSuccessful = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs cleanup of old login attempt records based on configured retention policies.
    /// This should be called periodically to prevent database growth.
    /// </summary>
    /// <param name="retentionPeriod">How long to retain login attempt records</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of records cleaned up</returns>
    Task<int> CleanupOldLoginAttemptsAsync(
        TimeSpan retentionPeriod,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Automatically unlocks accounts whose lockout period has expired.
    /// This should be called periodically to process automatic unlocks.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of accounts that were automatically unlocked</returns>
    Task<int> ProcessExpiredLockoutsAsync(CancellationToken cancellationToken = default);
}