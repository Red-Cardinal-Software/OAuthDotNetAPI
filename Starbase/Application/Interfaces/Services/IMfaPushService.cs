using Application.DTOs.Mfa;
using Application.Models;

namespace Application.Interfaces.Services;

/// <summary>
/// Service for managing push notification-based multi-factor authentication.
/// </summary>
public interface IMfaPushService
{
    /// <summary>
    /// Registers a new device for push notifications.
    /// </summary>
    /// <param name="userId">The user registering the device.</param>
    /// <param name="request">The device registration request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The registration result.</returns>
    Task<ServiceResponse<MfaPushDeviceDto>> RegisterDeviceAsync(
        Guid userId,
        RegisterPushDeviceRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a push notification challenge to a device.
    /// </summary>
    /// <param name="userId">The user to authenticate.</param>
    /// <param name="deviceId">The device to send the challenge to.</param>
    /// <param name="sessionInfo">Session information for context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The challenge details.</returns>
    Task<ServiceResponse<MfaPushChallengeDto>> SendChallengeAsync(
        Guid userId,
        Guid deviceId,
        PushChallengeSessionInfo sessionInfo,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks the status of a push challenge.
    /// </summary>
    /// <param name="challengeId">The challenge to check.</param>
    /// <param name="sessionId">The session ID for validation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The current challenge status.</returns>
    Task<ServiceResponse<MfaPushChallengeStatusDto>> CheckChallengeStatusAsync(
        Guid challengeId,
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Responds to a push challenge from the device.
    /// </summary>
    /// <param name="challengeId">The challenge ID.</param>
    /// <param name="response">The response details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The response result.</returns>
    Task<ServiceResponse<bool>> RespondToChallengeAsync(
        Guid challengeId,
        PushChallengeResponse response,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all registered push devices for a user.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of registered devices.</returns>
    Task<ServiceResponse<IEnumerable<MfaPushDeviceDto>>> GetUserDevicesAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a push device.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="deviceId">The device to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success result.</returns>
    Task<ServiceResponse<bool>> RemoveDeviceAsync(
        Guid userId,
        Guid deviceId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a device's push token.
    /// </summary>
    /// <param name="deviceId">The device ID.</param>
    /// <param name="newToken">The new push token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Success result.</returns>
    Task<ServiceResponse<bool>> UpdateDeviceTokenAsync(
        Guid deviceId,
        string newToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cleans up expired challenges.
    /// </summary>
    /// <param name="olderThan">Remove challenges older than this timespan.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of challenges cleaned up.</returns>
    Task<int> CleanupExpiredChallengesAsync(
        TimeSpan olderThan,
        CancellationToken cancellationToken = default);
}