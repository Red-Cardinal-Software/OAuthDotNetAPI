using Domain.Attributes;
using Domain.Exceptions;

namespace Domain.Entities.Security;

/// <summary>
/// Represents the lockout status of a user account, implementing exponential backoff
/// and automatic unlock mechanisms to protect against brute force attacks while
/// maintaining usability. This entity enforces domain invariants and business rules
/// around account lockout behavior.
/// </summary>
[Audited]
public class AccountLockout
{
    /// <summary>
    /// Unique identifier for the account lockout record.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// The unique identifier of the user whose account is subject to lockout.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// The number of consecutive failed login attempts that led to this lockout.
    /// This value is reset when the user successfully logs in or the lockout is manually reset.
    /// </summary>
    public int FailedAttemptCount { get; private set; }

    /// <summary>
    /// Indicates whether the account is currently locked out.
    /// </summary>
    public bool IsLockedOut { get; private set; }

    /// <summary>
    /// The timestamp when the account was locked out.
    /// Null if the account is not currently locked.
    /// </summary>
    public DateTimeOffset? LockedOutAt { get; private set; }

    /// <summary>
    /// The timestamp when the account lockout will automatically expire.
    /// Null if the account is not currently locked or if manual unlock is required.
    /// </summary>
    public DateTimeOffset? LockoutExpiresAt { get; private set; }

    /// <summary>
    /// The timestamp when the failed attempt count was last incremented.
    /// Used to determine if attempts should be reset due to time passage.
    /// </summary>
    public DateTimeOffset LastFailedAttemptAt { get; private set; }

    /// <summary>
    /// The timestamp when the account lockout record was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// The timestamp when the account lockout record was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Optional reason for manual lockout (e.g., "Administrative action", "Suspicious activity").
    /// Null for automatic lockouts due to failed login attempts.
    /// </summary>
    public string? LockoutReason { get; private set; }

    /// <summary>
    /// The user ID who manually locked the account (if applicable).
    /// Null for automatic lockouts.
    /// </summary>
    public Guid? LockedByUserId { get; private set; }

    /// <summary>
    /// Private constructor for Entity Framework Core.
    /// </summary>
    private AccountLockout()
    {
    }

    /// <summary>
    /// Creates a new account lockout tracking record for a user.
    /// Initially, the account is not locked out.
    /// </summary>
    /// <param name="userId">The unique identifier of the user</param>
    /// <returns>A new AccountLockout instance</returns>
    public static AccountLockout CreateForUser(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new InvalidLockoutParametersException("User ID cannot be empty");

        var now = DateTimeOffset.UtcNow;

        return new AccountLockout
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FailedAttemptCount = 0,
            IsLockedOut = false,
            LockedOutAt = null,
            LockoutExpiresAt = null,
            LastFailedAttemptAt = now,
            CreatedAt = now,
            UpdatedAt = now,
            LockoutReason = null,
            LockedByUserId = null
        };
    }

    /// <summary>
    /// Records a failed login attempt and potentially locks the account
    /// based on the configured thresholds and lockout strategy.
    /// </summary>
    /// <param name="lockoutThreshold">Number of failed attempts before lockout</param>
    /// <param name="baseLockoutDuration">Base duration for lockout</param>
    /// <param name="maxLockoutDuration">Maximum lockout duration</param>
    /// <param name="resetWindow">Time window after which failed attempts are reset</param>
    /// <returns>True if the account was locked out as a result of this attempt</returns>
    public bool RecordFailedAttempt(
        int lockoutThreshold,
        TimeSpan baseLockoutDuration,
        TimeSpan maxLockoutDuration,
        TimeSpan resetWindow)
    {
        ValidateLockoutParameters(lockoutThreshold, baseLockoutDuration, maxLockoutDuration, resetWindow);

        var now = DateTimeOffset.UtcNow;

        // If the account is currently locked and hasn't expired, don't increment
        if (IsLockedOut && (LockoutExpiresAt == null || LockoutExpiresAt > now))
        {
            return false; // Already locked
        }

        // If enough time has passed since the last failed attempt, reset the counter
        if (now - LastFailedAttemptAt > resetWindow)
        {
            FailedAttemptCount = 0;
        }

        // Increment the failed attempt count
        FailedAttemptCount++;
        LastFailedAttemptAt = now;
        UpdatedAt = now;

        // Check if we should lock the account
        if (FailedAttemptCount >= lockoutThreshold)
        {
            LockAccount(CalculateLockoutDuration(baseLockoutDuration, maxLockoutDuration), null, null);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Manually locks the account with a specified duration and reason.
    /// </summary>
    /// <param name="duration">Duration of the lockout (null for indefinite)</param>
    /// <param name="reason">Reason for the manual lockout</param>
    /// <param name="lockedByUserId">ID of the user who performed the lockout</param>
    public void LockAccount(TimeSpan? duration, string? reason, Guid? lockedByUserId)
    {
        var now = DateTimeOffset.UtcNow;

        IsLockedOut = true;
        LockedOutAt = now;
        LockoutExpiresAt = duration.HasValue ? now.Add(duration.Value) : null;
        LockoutReason = reason?.Trim();
        LockedByUserId = lockedByUserId;
        UpdatedAt = now;
    }

    /// <summary>
    /// Unlocks the account and resets the failed attempt counter.
    /// </summary>
    /// <param name="resetFailedAttempts">Whether to reset the failed attempt counter</param>
    public void UnlockAccount(bool resetFailedAttempts = true)
    {
        var now = DateTimeOffset.UtcNow;

        IsLockedOut = false;
        LockedOutAt = null;
        LockoutExpiresAt = null;
        LockoutReason = null;
        LockedByUserId = null;
        UpdatedAt = now;

        if (resetFailedAttempts)
        {
            FailedAttemptCount = 0;
        }
    }

    /// <summary>
    /// Records a successful login and resets the failed attempt counter.
    /// </summary>
    public void RecordSuccessfulLogin()
    {
        var now = DateTimeOffset.UtcNow;

        FailedAttemptCount = 0;
        UpdatedAt = now;

        // If account was locked due to failed attempts (not manual), unlock it
        if (IsLockedOut && LockedByUserId == null)
        {
            UnlockAccount(resetFailedAttempts: false); // Already reset above
        }
    }

    /// <summary>
    /// Checks if the account lockout has automatically expired.
    /// </summary>
    /// <returns>True if the lockout has expired</returns>
    public bool HasLockoutExpired()
    {
        if (!IsLockedOut || LockoutExpiresAt == null)
            return false;

        return DateTimeOffset.UtcNow >= LockoutExpiresAt;
    }

    /// <summary>
    /// Gets the remaining lockout duration, if any.
    /// </summary>
    /// <returns>Remaining lockout duration, or null if not locked or indefinite</returns>
    public TimeSpan? GetRemainingLockoutDuration()
    {
        if (!IsLockedOut || LockoutExpiresAt == null)
            return null;

        var remaining = LockoutExpiresAt.Value - DateTimeOffset.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    /// <summary>
    /// Calculates the lockout duration using exponential backoff strategy.
    /// </summary>
    /// <param name="baseDuration">Base lockout duration</param>
    /// <param name="maxDuration">Maximum allowed lockout duration</param>
    /// <returns>Calculated lockout duration</returns>
    private TimeSpan CalculateLockoutDuration(TimeSpan baseDuration, TimeSpan maxDuration)
    {
        // Exponential backoff: base * 2^(attempts - threshold)
        // For example: 1 min, 2 min, 4 min, 8 min, etc.
        var multiplier = Math.Pow(2, Math.Max(0, FailedAttemptCount - 3)); // Start exponential after 3 attempts
        var calculatedDuration = TimeSpan.FromMilliseconds(baseDuration.TotalMilliseconds * multiplier);

        return calculatedDuration > maxDuration ? maxDuration : calculatedDuration;
    }

    /// <summary>
    /// Validates lockout parameters to ensure they are within acceptable ranges.
    /// </summary>
    private static void ValidateLockoutParameters(
        int lockoutThreshold,
        TimeSpan baseLockoutDuration,
        TimeSpan maxLockoutDuration,
        TimeSpan resetWindow)
    {
        if (lockoutThreshold <= 0)
            throw new InvalidLockoutParametersException("Lockout threshold must be positive");

        if (lockoutThreshold > 100)
            throw new InvalidLockoutParametersException("Lockout threshold cannot exceed 100 attempts");

        if (baseLockoutDuration <= TimeSpan.Zero)
            throw new InvalidLockoutParametersException("Base lockout duration must be positive");

        if (maxLockoutDuration < baseLockoutDuration)
            throw new InvalidLockoutParametersException("Maximum lockout duration cannot be less than base duration");

        if (resetWindow <= TimeSpan.Zero)
            throw new InvalidLockoutParametersException("Reset window must be positive");

        if (maxLockoutDuration > TimeSpan.FromDays(30))
            throw new InvalidLockoutParametersException("Maximum lockout duration cannot exceed 30 days");
    }
}