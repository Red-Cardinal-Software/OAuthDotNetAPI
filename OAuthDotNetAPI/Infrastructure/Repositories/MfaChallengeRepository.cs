using Application.Interfaces.Persistence;
using Application.Interfaces.Repositories;
using Domain.Entities.Security;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository implementation for managing MfaChallenge entities.
/// Provides efficient data access methods for temporary MFA verification challenges
/// with optimized queries for authentication flows and security monitoring.
/// </summary>
public class MfaChallengeRepository(ICrudOperator<MfaChallenge> mfaChallengeCrudOperator) : IMfaChallengeRepository
{
    /// <summary>
    /// Gets an MFA challenge by its unique identifier.
    /// </summary>
    public async Task<MfaChallenge?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await mfaChallengeCrudOperator.GetAll()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    /// <summary>
    /// Gets an MFA challenge by its challenge token.
    /// </summary>
    public async Task<MfaChallenge?> GetByChallengeTokenAsync(string challengeToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(challengeToken))
            return null;

        return await mfaChallengeCrudOperator.GetAll()
            .FirstOrDefaultAsync(c => c.ChallengeToken == challengeToken, cancellationToken);
    }

    /// <summary>
    /// Gets all active (non-completed, non-invalid) challenges for a user.
    /// </summary>
    public async Task<IReadOnlyList<MfaChallenge>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        
        var challenges = await mfaChallengeCrudOperator.GetAll()
            .Where(c => c.UserId == userId 
                && !c.IsCompleted 
                && !c.IsInvalid 
                && c.ExpiresAt > now)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        return challenges.AsReadOnly();
    }

    /// <summary>
    /// Gets recent challenges for a user within a time period.
    /// Used for rate limiting and security monitoring.
    /// </summary>
    public async Task<IReadOnlyList<MfaChallenge>> GetRecentByUserIdAsync(Guid userId, DateTimeOffset since, CancellationToken cancellationToken = default)
    {
        var challenges = await mfaChallengeCrudOperator.GetAll()
            .Where(c => c.UserId == userId && c.CreatedAt >= since)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        return challenges.AsReadOnly();
    }

    /// <summary>
    /// Gets challenges that have expired and can be cleaned up.
    /// </summary>
    public async Task<IReadOnlyList<MfaChallenge>> GetExpiredChallengesAsync(DateTimeOffset expiredBefore, CancellationToken cancellationToken = default)
    {
        var challenges = await mfaChallengeCrudOperator.GetAll()
            .Where(c => c.ExpiresAt < expiredBefore)
            .OrderBy(c => c.ExpiresAt)
            .ToListAsync(cancellationToken);

        return challenges.AsReadOnly();
    }

    /// <summary>
    /// Gets the count of active challenges for a user.
    /// Used for rate limiting.
    /// </summary>
    public async Task<int> GetActiveChallengeCountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        
        return await mfaChallengeCrudOperator.GetAll()
            .CountAsync(c => c.UserId == userId 
                && !c.IsCompleted 
                && !c.IsInvalid 
                && c.ExpiresAt > now, cancellationToken);
    }

    /// <summary>
    /// Gets the count of challenges created by a user within a time period.
    /// Used for rate limiting challenge creation.
    /// </summary>
    public async Task<int> GetChallengeCountSinceAsync(Guid userId, DateTimeOffset since, CancellationToken cancellationToken = default)
    {
        return await mfaChallengeCrudOperator.GetAll()
            .CountAsync(c => c.UserId == userId && c.CreatedAt >= since, cancellationToken);
    }

    /// <summary>
    /// Adds a new MFA challenge to the repository.
    /// </summary>
    public async Task AddAsync(MfaChallenge mfaChallenge, CancellationToken cancellationToken = default)
    {
        await mfaChallengeCrudOperator.AddAsync(mfaChallenge);
    }


    /// <summary>
    /// Removes an MFA challenge from the repository.
    /// </summary>
    public void Remove(MfaChallenge mfaChallenge)
    {
        mfaChallengeCrudOperator.Delete(mfaChallenge);
    }

    /// <summary>
    /// Invalidates all active challenges for a user.
    /// Used when a user successfully completes authentication or security events occur.
    /// </summary>
    public async Task<int> InvalidateAllUserChallengesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        
        var activeChallenges = await mfaChallengeCrudOperator.GetAll()
            .Where(c => c.UserId == userId 
                && !c.IsCompleted 
                && !c.IsInvalid 
                && c.ExpiresAt > now)
            .ToListAsync(cancellationToken);

        foreach (var challenge in activeChallenges)
        {
            challenge.Invalidate();
        }

        return activeChallenges.Count;
    }

    /// <summary>
    /// Removes expired challenges from the database.
    /// Used for periodic cleanup operations.
    /// </summary>
    public async Task<int> DeleteExpiredChallengesAsync(DateTimeOffset expiredBefore, CancellationToken cancellationToken = default)
    {
        var expiredChallenges = await mfaChallengeCrudOperator.GetAll()
            .Where(c => c.ExpiresAt < expiredBefore)
            .ToListAsync(cancellationToken);

        foreach (var challenge in expiredChallenges)
        {
            mfaChallengeCrudOperator.Delete(challenge);
        }

        return expiredChallenges.Count;
    }
}
