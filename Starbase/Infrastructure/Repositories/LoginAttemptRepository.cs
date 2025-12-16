using Application.Interfaces.Persistence;
using Application.Interfaces.Repositories;
using Domain.Entities.Security;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing LoginAttempt entities.
/// Provides efficient data access methods for tracking and querying login attempts
/// with optimized queries for security monitoring and analysis.
/// </summary>
public class LoginAttemptRepository(ICrudOperator<LoginAttempt> loginAttemptCrudOperator) : ILoginAttemptRepository
{
    /// <summary>
    /// Adds a new login attempt record to the repository.
    /// </summary>
    public async Task AddAsync(LoginAttempt loginAttempt, CancellationToken cancellationToken = default)
    {
        await loginAttemptCrudOperator.AddAsync(loginAttempt);
    }

    /// <summary>
    /// Gets recent login attempts for a specific user within a time window.
    /// Uses optimized indexing for efficient querying of large datasets.
    /// </summary>
    public async Task<IReadOnlyList<LoginAttempt>> GetRecentAttemptsAsync(
        Guid userId,
        DateTimeOffset since,
        bool includeSuccessful = false,
        CancellationToken cancellationToken = default)
    {
        var query = loginAttemptCrudOperator.GetAll()
            .Where(la => la.UserId == userId && la.AttemptedAt >= since);

        if (!includeSuccessful)
        {
            query = query.Where(la => !la.IsSuccessful);
        }

        var results = await query
            .OrderByDescending(la => la.AttemptedAt)
            .ToListAsync(cancellationToken);

        return results.AsReadOnly();
    }

    /// <summary>
    /// Gets the count of failed login attempts for a user within a time window.
    /// Optimized query that only counts records without loading entities.
    /// </summary>
    public async Task<int> GetFailedAttemptCountAsync(
        Guid userId,
        DateTimeOffset since,
        CancellationToken cancellationToken = default)
    {
        return await loginAttemptCrudOperator.GetAll()
            .Where(la => la.UserId == userId &&
                        la.AttemptedAt >= since &&
                        !la.IsSuccessful)
            .CountAsync(cancellationToken);
    }

    /// <summary>
    /// Gets recent login attempts from a specific IP address.
    /// Useful for detecting potential attacks from specific sources.
    /// </summary>
    public async Task<IReadOnlyList<LoginAttempt>> GetRecentAttemptsByIpAsync(
        string ipAddress,
        DateTimeOffset since,
        bool includeSuccessful = false,
        CancellationToken cancellationToken = default)
    {
        var query = loginAttemptCrudOperator.GetAll()
            .Where(la => la.IpAddress == ipAddress && la.AttemptedAt >= since);

        if (!includeSuccessful)
        {
            query = query.Where(la => !la.IsSuccessful);
        }

        var results = await query
            .OrderByDescending(la => la.AttemptedAt)
            .ToListAsync(cancellationToken);

        return results.AsReadOnly();
    }

    /// <summary>
    /// Gets login attempts for a specific username, regardless of whether
    /// the username corresponds to an existing user account.
    /// </summary>
    public async Task<IReadOnlyList<LoginAttempt>> GetAttemptsByUsernameAsync(
        string username,
        DateTimeOffset since,
        bool includeSuccessful = false,
        CancellationToken cancellationToken = default)
    {
        var query = loginAttemptCrudOperator.GetAll()
            .Where(la => la.AttemptedUsername == username.ToLowerInvariant() &&
                        la.AttemptedAt >= since);

        if (!includeSuccessful)
        {
            query = query.Where(la => !la.IsSuccessful);
        }

        var results = await query
            .OrderByDescending(la => la.AttemptedAt)
            .ToListAsync(cancellationToken);

        return results.AsReadOnly();
    }

    /// <summary>
    /// Deletes login attempt records older than the specified date.
    /// Used for cleanup and data retention compliance.
    /// Uses batch deletion for performance with large datasets.
    /// </summary>
    public async Task<int> DeleteOldAttemptsAsync(
        DateTimeOffset olderThan,
        CancellationToken cancellationToken = default)
    {
        var oldAttempts = await loginAttemptCrudOperator.GetAll()
            .Where(la => la.AttemptedAt < olderThan)
            .ToListAsync(cancellationToken);

        loginAttemptCrudOperator.DeleteMany(oldAttempts);
        return oldAttempts.Count;
    }

    /// <summary>
    /// Gets login attempt statistics for security reporting.
    /// Provides aggregated data for monitoring and analysis.
    /// </summary>
    public async Task<Dictionary<string, object>> GetLoginStatisticsAsync(
        DateTimeOffset since,
        CancellationToken cancellationToken = default)
    {
        var query = loginAttemptCrudOperator.GetAll()
            .Where(la => la.AttemptedAt >= since);

        // Get basic counts
        var totalAttempts = await query.CountAsync(cancellationToken);
        var successfulAttempts = await query.CountAsync(la => la.IsSuccessful, cancellationToken);
        var failedAttempts = totalAttempts - successfulAttempts;

        // Get unique users and IPs
        var uniqueUsers = await query
            .Where(la => la.UserId != Guid.Empty)
            .Select(la => la.UserId)
            .Distinct()
            .CountAsync(cancellationToken);

        var uniqueIPs = await query
            .Where(la => la.IpAddress != null)
            .Select(la => la.IpAddress)
            .Distinct()
            .CountAsync(cancellationToken);

        // Get top failure reasons
        var topFailureReasons = await query
            .Where(la => !la.IsSuccessful && la.FailureReason != null)
            .GroupBy(la => la.FailureReason)
            .Select(g => new { Reason = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync(cancellationToken);

        // Get attempts by hour for the last 24 hours
        var hourlyStats = await query
            .Where(la => la.AttemptedAt >= DateTimeOffset.UtcNow.AddHours(-24))
            .GroupBy(la => new { la.AttemptedAt.Hour, la.AttemptedAt.Date })
            .Select(g => new
            {
                Hour = g.Key.Hour,
                Date = g.Key.Date,
                Count = g.Count(),
                Failed = g.Count(la => !la.IsSuccessful)
            })
            .OrderBy(x => x.Date)
            .ThenBy(x => x.Hour)
            .ToListAsync(cancellationToken);

        return new Dictionary<string, object>
        {
            ["TotalAttempts"] = totalAttempts,
            ["SuccessfulAttempts"] = successfulAttempts,
            ["FailedAttempts"] = failedAttempts,
            ["SuccessRate"] = totalAttempts > 0 ? (double)successfulAttempts / totalAttempts : 0.0,
            ["UniqueUsers"] = uniqueUsers,
            ["UniqueIPs"] = uniqueIPs,
            ["TopFailureReasons"] = topFailureReasons.ToDictionary(x => x.Reason!, x => (object)x.Count),
            ["HourlyStats"] = hourlyStats.Select(x => new Dictionary<string, object>
            {
                ["Hour"] = x.Hour,
                ["Date"] = x.Date,
                ["Total"] = x.Count,
                ["Failed"] = x.Failed,
                ["Success"] = x.Count - x.Failed
            }).ToList()
        };
    }
}