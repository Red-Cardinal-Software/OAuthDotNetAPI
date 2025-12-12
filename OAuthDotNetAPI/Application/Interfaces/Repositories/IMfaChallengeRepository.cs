using Domain.Entities.Security;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for managing MFA challenges.
/// Provides data access methods for temporary MFA verification challenges during authentication.
/// </summary>
public interface IMfaChallengeRepository
{
    /// <summary>
    /// Gets an MFA challenge by its unique identifier.
    /// </summary>
    /// <param name="id">The challenge ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The MFA challenge if found, otherwise null</returns>
    Task<MfaChallenge?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an MFA challenge by its challenge token.
    /// </summary>
    /// <param name="challengeToken">The challenge token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The MFA challenge if found, otherwise null</returns>
    Task<MfaChallenge?> GetByChallengeTokenAsync(string challengeToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active (non-completed, non-invalid) challenges for a user.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of active MFA challenges</returns>
    Task<IReadOnlyList<MfaChallenge>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent challenges for a user within a time period.
    /// Used for rate limiting and security monitoring.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="since">Start time for the search period</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of recent MFA challenges</returns>
    Task<IReadOnlyList<MfaChallenge>> GetRecentByUserIdAsync(Guid userId, DateTimeOffset since, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets challenges that have expired and can be cleaned up.
    /// </summary>
    /// <param name="expiredBefore">Expiration threshold</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of expired challenges</returns>
    Task<IReadOnlyList<MfaChallenge>> GetExpiredChallengesAsync(DateTimeOffset expiredBefore, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of active challenges for a user.
    /// Used for rate limiting.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of active challenges</returns>
    Task<int> GetActiveChallengeCountAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of challenges created by a user within a time period.
    /// Used for rate limiting challenge creation.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="since">Start time for counting</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of challenges created since the specified time</returns>
    Task<int> GetChallengeCountSinceAsync(Guid userId, DateTimeOffset since, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new MFA challenge to the repository.
    /// </summary>
    /// <param name="mfaChallenge">The MFA challenge to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddAsync(MfaChallenge mfaChallenge, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an MFA challenge from the repository.
    /// </summary>
    /// <param name="mfaChallenge">The MFA challenge to remove</param>
    void Remove(MfaChallenge mfaChallenge);

    /// <summary>
    /// Invalidates all active challenges for a user.
    /// Used when a user successfully completes authentication or security events occur.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of challenges invalidated</returns>
    Task<int> InvalidateAllUserChallengesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes expired challenges from the database.
    /// Used for periodic cleanup operations.
    /// </summary>
    /// <param name="expiredBefore">Delete challenges expired before this time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of challenges removed</returns>
    Task<int> DeleteExpiredChallengesAsync(DateTimeOffset expiredBefore, CancellationToken cancellationToken = default);
}
