using Application.Common.Services;
using Application.Interfaces.Persistence;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities.Security;
using Microsoft.Extensions.Configuration;

namespace Application.Services.AccountLockout;

/// <summary>
/// Service implementation for managing account lockout functionality.
/// Provides methods for tracking login attempts, managing account lockout state,
/// and implementing security policies to protect against brute force attacks.
/// </summary>
public class AccountLockoutService(
    IAccountLockoutRepository accountLockoutRepository,
    ILoginAttemptRepository loginAttemptRepository,
    IUnitOfWork unitOfWork,
    IConfiguration configuration)
    : BaseAppService(unitOfWork), IAccountLockoutService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    /// <summary>
    /// Records a failed login attempt for the specified user and determines
    /// if the account should be locked based on configured policies.
    /// </summary>
    public async Task<bool> RecordFailedAttemptAsync(
        Guid userId,
        string username,
        string? ipAddress,
        string? userAgent,
        string failureReason,
        CancellationToken cancellationToken = default) => await RunWithCommitAsync(async () =>
    {
        // Record the failed attempt
        var failedAttempt = LoginAttempt.CreateFailed(
            userId, 
            username, 
            failureReason, 
            ipAddress, 
            userAgent);
        
        await loginAttemptRepository.AddAsync(failedAttempt, cancellationToken);

        // Only proceed with lockout logic if the attempt should count towards lockout
        if (!failedAttempt.ShouldCountTowardsLockout())
        {
            return false;
        }

        // Get lockout configuration
        var lockoutConfig = GetLockoutConfiguration();

        // Skip lockout if disabled
        if (!lockoutConfig.EnableAccountLockout)
        {
            return false;
        }

        // Get or create lockout record
        var lockout = await accountLockoutRepository.GetOrCreateAsync(userId, cancellationToken);

        // Record the failed attempt and check if account should be locked
        var wasLocked = lockout.RecordFailedAttempt(
            lockoutConfig.FailedAttemptThreshold,
            lockoutConfig.BaseLockoutDuration,
            lockoutConfig.MaxLockoutDuration,
            lockoutConfig.AttemptResetWindow);

        // Update the lockout record
        await accountLockoutRepository.UpdateAsync(lockout, cancellationToken);

        return wasLocked;
    });

    /// <summary>
    /// Records a successful login attempt for the specified user,
    /// which resets the failed attempt counter and unlocks the account if it was
    /// locked due to failed attempts.
    /// </summary>
    public async Task RecordSuccessfulLoginAsync(
        Guid userId,
        string username,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default) => await RunWithCommitAsync(async () =>
    {
        // Record the successful attempt if tracking is enabled
        var lockoutConfig = GetLockoutConfiguration();
        if (lockoutConfig.TrackLoginAttempts)
        {
            var successfulAttempt = LoginAttempt.CreateSuccessful(
                userId, 
                username, 
                ipAddress, 
                userAgent);
            
            await loginAttemptRepository.AddAsync(successfulAttempt, cancellationToken);
        }

        // Get existing lockout record if it exists
        var lockout = await accountLockoutRepository.GetByUserIdAsync(userId, cancellationToken);
        if (lockout is not null)
        {
            // Reset failed attempts and unlock if locked due to failed attempts
            lockout.RecordSuccessfulLogin();
            await accountLockoutRepository.UpdateAsync(lockout, cancellationToken);
        }
    });

    /// <summary>
    /// Checks if the specified user account is currently locked out.
    /// </summary>
    public async Task<Domain.Entities.Security.AccountLockout?> GetAccountLockoutAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var lockout = await accountLockoutRepository.GetByUserIdAsync(userId, cancellationToken);
        
        // If no lockout record exists, user is not locked
        if (lockout == null)
            return null;

        // Check if lockout has expired and auto-unlock if needed
        if (lockout.HasLockoutExpired() && lockout.IsLockedOut)
        {
            lockout.UnlockAccount(resetFailedAttempts: false);
            await accountLockoutRepository.UpdateAsync(lockout, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);
            return null;
        }

        return lockout.IsLockedOut ? lockout : null;
    }

    /// <summary>
    /// Determines if a user account is currently locked out.
    /// </summary>
    public async Task<bool> IsAccountLockedOutAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var lockout = await GetAccountLockoutAsync(userId, cancellationToken);
        return lockout?.IsLockedOut == true;
    }

    /// <summary>
    /// Manually locks a user account with the specified duration and reason.
    /// This is typically used by administrators for security or policy enforcement.
    /// </summary>
    public async Task LockAccountAsync(
        Guid userId,
        TimeSpan? duration,
        string reason,
        Guid lockedByUserId,
        CancellationToken cancellationToken = default) => await RunWithCommitAsync(async () =>
    {
        var lockout = await accountLockoutRepository.GetOrCreateAsync(userId, cancellationToken);
        
        lockout.LockAccount(duration, reason, lockedByUserId);
        
        await accountLockoutRepository.UpdateAsync(lockout, cancellationToken);
    });

    /// <summary>
    /// Manually unlocks a user account and optionally resets the failed attempt counter.
    /// This is typically used by administrators to restore account access.
    /// </summary>
    public async Task UnlockAccountAsync(
        Guid userId,
        bool resetFailedAttempts = true,
        CancellationToken cancellationToken = default) => await RunWithCommitAsync(async () =>
    {
        var lockout = await accountLockoutRepository.GetByUserIdAsync(userId, cancellationToken);
        if (lockout is not null)
        {
            lockout.UnlockAccount(resetFailedAttempts);
            await accountLockoutRepository.UpdateAsync(lockout, cancellationToken);
        }
    });

    /// <summary>
    /// Gets the remaining lockout duration for a user account.
    /// </summary>
    public async Task<TimeSpan?> GetRemainingLockoutDurationAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var lockout = await GetAccountLockoutAsync(userId, cancellationToken);
        return lockout?.GetRemainingLockoutDuration();
    }

    /// <summary>
    /// Gets recent login attempts for a user within the specified time period.
    /// This can be used for security auditing and analysis.
    /// </summary>
    public async Task<IReadOnlyList<LoginAttempt>> GetRecentLoginAttemptsAsync(
        Guid userId,
        TimeSpan timePeriod,
        bool includeSuccessful = false,
        CancellationToken cancellationToken = default)
    {
        var since = DateTimeOffset.UtcNow.Subtract(timePeriod);
        return await loginAttemptRepository.GetRecentAttemptsAsync(userId, since, includeSuccessful, cancellationToken);
    }

    /// <summary>
    /// Performs cleanup of old login attempt records based on configured retention policies.
    /// This should be called periodically to prevent database growth.
    /// </summary>
    public async Task<int> CleanupOldLoginAttemptsAsync(
        TimeSpan retentionPeriod,
        CancellationToken cancellationToken = default) => await RunWithCommitAsync(async () =>
    {
        var cutoffDate = DateTimeOffset.UtcNow.Subtract(retentionPeriod);
        return await loginAttemptRepository.DeleteOldAttemptsAsync(cutoffDate, cancellationToken);
    });

    /// <summary>
    /// Automatically unlocks accounts whose lockout period has expired.
    /// This should be called periodically to process automatic unlocks.
    /// </summary>
    public async Task<int> ProcessExpiredLockoutsAsync(CancellationToken cancellationToken = default) => await RunWithCommitAsync(async () =>
    {
        var expiredLockouts = await accountLockoutRepository.GetExpiredLockoutsAsync(cancellationToken);
        
        foreach (var lockout in expiredLockouts)
        {
            lockout.UnlockAccount(resetFailedAttempts: false);
            await accountLockoutRepository.UpdateAsync(lockout, cancellationToken);
        }

        return expiredLockouts.Count;
    });

    /// <summary>
    /// Gets account lockout configuration from application settings.
    /// </summary>
    private AccountLockoutConfiguration GetLockoutConfiguration()
    {
        return new AccountLockoutConfiguration
        {
            FailedAttemptThreshold = int.TryParse(configuration["AccountLockout:FailedAttemptThreshold"], out var threshold) ? threshold : 5,
            BaseLockoutDuration = TimeSpan.FromMinutes(int.TryParse(configuration["AccountLockout:BaseLockoutDurationMinutes"], out var baseDuration) ? baseDuration : 5),
            MaxLockoutDuration = TimeSpan.FromMinutes(int.TryParse(configuration["AccountLockout:MaxLockoutDurationMinutes"], out var maxDuration) ? maxDuration : 60),
            AttemptResetWindow = TimeSpan.FromMinutes(int.TryParse(configuration["AccountLockout:AttemptResetWindowMinutes"], out var resetWindow) ? resetWindow : 15),
            EnableAccountLockout = !bool.TryParse(configuration["AccountLockout:EnableAccountLockout"], out var enableLockout) || enableLockout,
            TrackLoginAttempts = !bool.TryParse(configuration["AccountLockout:TrackLoginAttempts"], out var trackAttempts) || trackAttempts
        };
    }

    /// <summary>
    /// Configuration class for account lockout settings.
    /// </summary>
    private class AccountLockoutConfiguration
    {
        public int FailedAttemptThreshold { get; init; }
        public TimeSpan BaseLockoutDuration { get; init; }
        public TimeSpan MaxLockoutDuration { get; init; }
        public TimeSpan AttemptResetWindow { get; init; }
        public bool EnableAccountLockout { get; init; }
        public bool TrackLoginAttempts { get; init; }
    }
}