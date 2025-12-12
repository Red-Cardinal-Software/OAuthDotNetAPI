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
    /// Updates an existing account lockout record.
    /// The entity changes will be tracked and saved when the unit of work commits.
    /// </summary>
    public Task UpdateAsync(AccountLockout accountLockout, CancellationToken cancellationToken = default)
    {
        // Entity Framework tracks changes automatically when entities are modified
        // Changes will be persisted when the unit of work commits
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the account lockout record for a user, or creates a new one if it doesn't exist.
    /// </summary>
    public async Task<AccountLockout> GetOrCreateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var existing = await GetByUserIdAsync(userId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var newLockout = AccountLockout.CreateForUser(userId);
        await AddAsync(newLockout, cancellationToken);
        return newLockout;
    }

    /// <summary>
    /// Gets all accounts that are currently locked out (active lockouts).
    /// </summary>
    public async Task<IReadOnlyList<AccountLockout>> GetActiveLockedAccountsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var results = await accountLockoutCrudOperator.GetAll()
            .Where(al => al.IsLockedOut && 
                        (al.LockoutExpiresAt == null || al.LockoutExpiresAt > now))
            .OrderByDescending(al => al.LockedOutAt)
            .ToListAsync(cancellationToken);

        return results.AsReadOnly();
    }

    /// <summary>
    /// Gets account lockouts that have expired and can be automatically unlocked.
    /// </summary>
    public async Task<IReadOnlyList<AccountLockout>> GetExpiredLockoutsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var results = await accountLockoutCrudOperator.GetAll()
            .Where(al => al.IsLockedOut && 
                        al.LockoutExpiresAt != null && 
                        al.LockoutExpiresAt <= now)
            .OrderBy(al => al.LockoutExpiresAt)
            .ToListAsync(cancellationToken);

        return results.AsReadOnly();
    }

    /// <summary>
    /// Gets all accounts that were manually locked by a specific user.
    /// </summary>
    public async Task<IReadOnlyList<AccountLockout>> GetAccountsLockedByUserAsync(Guid lockedByUserId, CancellationToken cancellationToken = default)
    {
        var results = await accountLockoutCrudOperator.GetAll()
            .Where(al => al.LockedByUserId == lockedByUserId)
            .OrderByDescending(al => al.LockedOutAt)
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
            .Select(g => new {
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