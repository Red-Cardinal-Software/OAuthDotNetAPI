using Application.Interfaces.Persistence;
using Application.Interfaces.Repositories;
using Domain.Entities.Security;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing AccountLockout entities.
/// Provides efficient data access methods for tracking and managing account lockouts
/// with optimized queries for security monitoring and automatic lockout management.
/// </summary>
public class AccountLockoutRepository(ICrudOperator<AccountLockout> accountLockoutCrudOperator) : IAccountLockoutRepository
{
    /// <summary>
    /// Gets the account lockout record for a specific user.
    /// </summary>
    public async Task<AccountLockout?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await accountLockoutCrudOperator.GetAll()
            .FirstOrDefaultAsync(al => al.UserId == userId, cancellationToken);
    }

    /// <summary>
    /// Adds a new account lockout record to the repository.
    /// </summary>
    public async Task AddAsync(AccountLockout accountLockout, CancellationToken cancellationToken = default)
    {
        await accountLockoutCrudOperator.AddAsync(accountLockout);
    }

    /// <summary>
    /// Gets the account lockout record for a user, or creates a new one if it doesn't exist.
    /// Handles concurrent access safely by leveraging the unique constraint on UserId.
    /// </summary>
    public async Task<AccountLockout> GetOrCreateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var existing = await GetByUserIdAsync(userId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        try
        {
            // Attempt to create a new lockout record
            var newLockout = AccountLockout.CreateForUser(userId);
            await AddAsync(newLockout, cancellationToken);
            return newLockout;
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // Another thread created the record between our check and insert
            // Return the existing record that was created by the other thread
            var existingRecord = await GetByUserIdAsync(userId, cancellationToken);

            // This should never be null at this point, but defensive programming
            return existingRecord ?? throw new InvalidOperationException(
                $"Failed to create or retrieve AccountLockout for UserId {userId}. " +
                "Unique constraint violation occurred but no existing record found.");
        }
    }

    /// <summary>
    /// Determines if a DbUpdateException was caused by a unique constraint violation.
    /// Works across different database providers by checking for common error patterns.
    /// </summary>
    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        var message = ex.InnerException?.Message ?? "";

        // Check for constraint name or common duplicate key messages (works for SQL Server, PostgreSQL, etc.)
        return message.Contains("UX_AccountLockouts_UserId") ||
               message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("UNIQUE constraint", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets accounts that are currently locked out (active lockouts) with pagination.
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of records per page (max 1000)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of active locked accounts</returns>
    public async Task<IReadOnlyList<AccountLockout>> GetActiveLockedAccountsAsync(int pageNumber = 1, int pageSize = 100, CancellationToken cancellationToken = default)
    {
        // SECURITY: Enforce maximum page size to prevent DoS
        if (pageSize > 1000) pageSize = 1000;
        if (pageSize < 1) pageSize = 1;
        if (pageNumber < 1) pageNumber = 1;

        var now = DateTimeOffset.UtcNow;
        var results = await accountLockoutCrudOperator.GetAll()
            .Where(al => al.IsLockedOut &&
                        (al.LockoutExpiresAt == null || al.LockoutExpiresAt > now))
            .OrderByDescending(al => al.LockedOutAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return results.AsReadOnly();
    }

    /// <summary>
    /// Gets account lockouts that have expired and can be automatically unlocked with pagination.
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of records per page (max 1000)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of expired lockouts</returns>
    public async Task<IReadOnlyList<AccountLockout>> GetExpiredLockoutsAsync(int pageNumber = 1, int pageSize = 100, CancellationToken cancellationToken = default)
    {
        // SECURITY: Enforce maximum page size to prevent DoS
        if (pageSize > 1000) pageSize = 1000;
        if (pageSize < 1) pageSize = 1;
        if (pageNumber < 1) pageNumber = 1;

        var now = DateTimeOffset.UtcNow;
        var results = await accountLockoutCrudOperator.GetAll()
            .Where(al => al.IsLockedOut &&
                        al.LockoutExpiresAt != null &&
                        al.LockoutExpiresAt <= now)
            .OrderBy(al => al.LockoutExpiresAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return results.AsReadOnly();
    }

    /// <summary>
    /// Gets accounts that were manually locked by a specific user with pagination.
    /// </summary>
    /// <param name="lockedByUserId">ID of the user who locked the accounts</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of records per page (max 1000)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paginated list of accounts locked by the specified user</returns>
    public async Task<IReadOnlyList<AccountLockout>> GetAccountsLockedByUserAsync(Guid lockedByUserId, int pageNumber = 1, int pageSize = 100, CancellationToken cancellationToken = default)
    {
        // SECURITY: Enforce maximum page size to prevent DoS
        if (pageSize > 1000) pageSize = 1000;
        if (pageSize < 1) pageSize = 1;
        if (pageNumber < 1) pageNumber = 1;

        var results = await accountLockoutCrudOperator.GetAll()
            .Where(al => al.LockedByUserId == lockedByUserId)
            .OrderByDescending(al => al.LockedOutAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return results.AsReadOnly();
    }

    /// <summary>
    /// Gets account lockout statistics for security reporting and monitoring.
    /// </summary>
    public async Task<Dictionary<string, object>> GetLockoutStatisticsAsync(DateTimeOffset since, CancellationToken cancellationToken = default)
    {
        var query = accountLockoutCrudOperator.GetAll()
            .Where(al => al.CreatedAt >= since);

        var now = DateTimeOffset.UtcNow;

        // Basic counts
        var totalLockouts = await query.CountAsync(cancellationToken);
        var currentlyLocked = await query.CountAsync(al => al.IsLockedOut &&
            (al.LockoutExpiresAt == null || al.LockoutExpiresAt > now), cancellationToken);
        var expiredLockouts = await query.CountAsync(al => al.IsLockedOut &&
            al.LockoutExpiresAt != null && al.LockoutExpiresAt <= now, cancellationToken);
        var manualLockouts = await query.CountAsync(al => al.LockedByUserId != null, cancellationToken);
        var automaticLockouts = totalLockouts - manualLockouts;

        // Average failed attempts before lockout (for automatic lockouts)
        var avgFailedAttempts = await query
            .Where(al => al.LockedByUserId == null && al.FailedAttemptCount > 0)
            .AverageAsync(al => (double?)al.FailedAttemptCount, cancellationToken) ?? 0.0;

        // Lockouts by hour for the last 24 hours
        var hourlyStats = await query
            .Where(al => al.CreatedAt >= DateTimeOffset.UtcNow.AddHours(-24))
            .GroupBy(al => new { al.CreatedAt.Hour, al.CreatedAt.Date })
            .Select(g => new
            {
                Hour = g.Key.Hour,
                Date = g.Key.Date,
                Count = g.Count(),
                Manual = g.Count(al => al.LockedByUserId != null)
            })
            .OrderBy(x => x.Date)
            .ThenBy(x => x.Hour)
            .ToListAsync(cancellationToken);

        // Top lockout reasons (for manual lockouts)
        var topReasons = await query
            .Where(al => al.LockoutReason != null)
            .GroupBy(al => al.LockoutReason)
            .Select(g => new { Reason = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync(cancellationToken);

        return new Dictionary<string, object>
        {
            ["TotalLockouts"] = totalLockouts,
            ["CurrentlyLocked"] = currentlyLocked,
            ["ExpiredLockouts"] = expiredLockouts,
            ["ManualLockouts"] = manualLockouts,
            ["AutomaticLockouts"] = automaticLockouts,
            ["AverageFailedAttempts"] = Math.Round(avgFailedAttempts, 2),
            ["TopLockoutReasons"] = topReasons.ToDictionary(x => x.Reason!, x => (object)x.Count),
            ["HourlyStats"] = hourlyStats.Select(x => new Dictionary<string, object>
            {
                ["Hour"] = x.Hour,
                ["Date"] = x.Date,
                ["Total"] = x.Count,
                ["Manual"] = x.Manual,
                ["Automatic"] = x.Count - x.Manual
            }).ToList()
        };
    }

    /// <summary>
    /// Deletes account lockout records for the specified user IDs.
    /// Used for cleanup when users are deleted from the system.
    /// </summary>
    public async Task<int> DeleteByUserIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
    {
        var userIdList = userIds.ToList();
        if (!userIdList.Any())
            return 0;

        var lockoutsToDelete = await accountLockoutCrudOperator.GetAll()
            .Where(al => userIdList.Contains(al.UserId))
            .ToListAsync(cancellationToken);

        accountLockoutCrudOperator.DeleteMany(lockoutsToDelete);
        return lockoutsToDelete.Count;
    }
}