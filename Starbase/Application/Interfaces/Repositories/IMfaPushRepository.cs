using Domain.Entities.Security;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Repository interface for managing MFA push notification data.
/// </summary>
public interface IMfaPushRepository
{
    /// <summary>
    /// Gets a push device by its ID.
    /// </summary>
    /// <param name="id">The device ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The push device if found</returns>
    Task<MfaPushDevice?> GetPushDeviceAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a push device by user ID and device ID.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="deviceId">The unique device identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The push device if found</returns>
    Task<MfaPushDevice?> GetPushDeviceByDeviceIdAsync(Guid userId, string deviceId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all push devices for a user.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of push devices</returns>
    Task<IReadOnlyList<MfaPushDevice>> GetUserPushDevicesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new push device.
    /// </summary>
    /// <param name="device">The device to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddPushDeviceAsync(MfaPushDevice device, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a push challenge by its ID.
    /// </summary>
    /// <param name="id">The challenge ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The push challenge if found</returns>
    Task<MfaPushChallenge?> GetPushChallengeAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new push challenge.
    /// </summary>
    /// <param name="challenge">The challenge to add</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task AddPushChallengeAsync(MfaPushChallenge challenge, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the count of recent push challenges for a user.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="window">The time window to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of recent challenges</returns>
    Task<int> GetRecentPushChallengesCountAsync(Guid userId, TimeSpan window, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes expired push challenges.
    /// </summary>
    /// <param name="cutoff">Delete challenges older than this date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of challenges deleted</returns>
    Task<int> DeleteExpiredPushChallengesAsync(DateTime cutoff, CancellationToken cancellationToken = default);
}
