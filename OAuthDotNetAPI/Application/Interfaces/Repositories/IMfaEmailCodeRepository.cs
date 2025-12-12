using Domain.Entities.Security;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for managing MFA email codes.
/// Provides data access methods for email-based MFA verification codes.
/// </summary>
public interface IMfaEmailCodeRepository
{
    /// <summary>
    /// Gets an email code by its unique identifier.
    /// </summary>
    /// <param name="id">The email code ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The email code if found, otherwise null</returns>
    Task<MfaEmailCode?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the most recent valid email code for a challenge.
    /// </summary>
    /// <param name="challengeId">The MFA challenge ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The most recent valid email code if found, otherwise null</returns>
    Task<MfaEmailCode?> GetValidCodeByChallengeIdAsync(Guid challengeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent email codes sent to a user within a time window.
    /// Used for rate limiting and security monitoring.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="since">Start time for the search</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of recent email codes</returns>
    Task<IReadOnlyList<MfaEmailCode>> GetRecentCodesByUserIdAsync(
        Guid userId,
        DateTimeOffset since,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of email codes sent to a user within a time window.
    /// Used for rate limiting.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="since">Start time for counting</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of codes sent since the specified time</returns>
    Task<int> GetCodeCountSinceAsync(Guid userId, DateTimeOffset since, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new email code to the repository.
    /// </summary>
    /// <param name="emailCode">The email code to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddAsync(MfaEmailCode emailCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets expired email codes for cleanup.
    /// </summary>
    /// <param name="expiredBefore">Expiration threshold</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of expired codes</returns>
    Task<IReadOnlyList<MfaEmailCode>> GetExpiredCodesAsync(
        DateTimeOffset expiredBefore,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes expired email codes from the database.
    /// Used for periodic cleanup operations.
    /// </summary>
    /// <param name="expiredBefore">Delete codes expired before this time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of codes deleted</returns>
    Task<int> DeleteExpiredCodesAsync(DateTimeOffset expiredBefore, CancellationToken cancellationToken = default);
}
