using Application.Common.Configuration;
using Application.Common.Factories;
using Application.Models;
using Application.DTOs.Mfa;
using Application.Interfaces.Repositories;
using Application.Interfaces.Providers;
using Application.Interfaces.Services;
using Domain.Entities.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Application.Services.Mfa;

/// <summary>
/// Service for managing push notification-based multi-factor authentication.
/// </summary>
public class MfaPushService(
    IMfaPushRepository mfaRepository,
    IMfaMethodRepository mfaMethodRepository,
    IPushNotificationProvider pushProvider,
    IOptions<PushMfaOptions> pushMfaOptions,
    IOptions<AppOptions> appOptions,
    ILogger<MfaPushService> logger) : IMfaPushService
{
    private readonly PushMfaOptions _options = pushMfaOptions.Value;
    private readonly AppOptions _appOptions = appOptions.Value;

    /// <inheritdoc />
    public async Task<ServiceResponse<MfaPushDeviceDto>> RegisterDeviceAsync(
        Guid userId,
        RegisterPushDeviceRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate push token
            if (!pushProvider.ValidatePushToken(request.PushToken, request.Platform))
            {
                return ServiceResponseFactory.Error<MfaPushDeviceDto>(
                    "Invalid push token format");
            }

            // Check if device already exists
            var existingDevice = await mfaRepository.GetPushDeviceByDeviceIdAsync(
                userId, request.DeviceId, cancellationToken);

            if (existingDevice != null)
            {
                // Update existing device token
                existingDevice.UpdatePushToken(request.PushToken);

                return ServiceResponseFactory.Success(
                    MapToDto(existingDevice),
                    "Device token updated successfully");
            }

            // Get or create MFA method for push
            var mfaMethod = await mfaMethodRepository.GetByUserAndTypeAsync(userId, MfaType.Push, cancellationToken);

            if (mfaMethod == null)
            {
                mfaMethod = MfaMethod.CreatePush(userId, "Push Notifications");
                await mfaMethodRepository.AddAsync(mfaMethod, cancellationToken);
            }

            // Create new device
            var device = new MfaPushDevice(
                mfaMethod.Id,
                userId,
                request.DeviceId,
                request.DeviceName,
                request.Platform,
                request.PushToken,
                request.PublicKey);

            await mfaRepository.AddPushDeviceAsync(device, cancellationToken);

            logger.LogInformation(
                "Push device registered for user {UserId} on platform {Platform}",
                userId, request.Platform);

            return ServiceResponseFactory.Success(
                MapToDto(device),
                "Device registered successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error registering push device for user {UserId}",
                userId);

            return ServiceResponseFactory.Error<MfaPushDeviceDto>(
                "Failed to register device");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<MfaPushChallengeDto>> SendChallengeAsync(
        Guid userId,
        Guid deviceId,
        PushChallengeSessionInfo sessionInfo,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get the device
            var device = await mfaRepository.GetPushDeviceAsync(deviceId, cancellationToken);

            if (device == null || device.UserId != userId || !device.IsActive)
            {
                return ServiceResponseFactory.Error<MfaPushChallengeDto>(
                    "Device not found or inactive");
            }

            // Check for rate limiting
            var recentChallenges = await mfaRepository.GetRecentPushChallengesCountAsync(
                userId, TimeSpan.FromMinutes(_options.RateLimitWindowMinutes), cancellationToken);

            if (recentChallenges >= _options.MaxChallengesPerWindow)
            {
                return ServiceResponseFactory.Error<MfaPushChallengeDto>(
                    "Too many push requests. Please try again later.");
            }

            // Create challenge
            var challenge = new MfaPushChallenge(
                userId,
                deviceId,
                sessionInfo.SessionId,
                sessionInfo.IpAddress,
                sessionInfo.UserAgent,
                _options.ChallengeExpiryMinutes);

            if (!string.IsNullOrEmpty(sessionInfo.Location))
            {
                challenge.SetLocation(sessionInfo.Location);
            }

            await mfaRepository.AddPushChallengeAsync(challenge, cancellationToken);

            // Send push notification
            var notificationData = new Dictionary<string, string>
            {
                ["challengeId"] = challenge.Id.ToString(),
                ["challengeCode"] = challenge.ChallengeCode,
                ["sessionId"] = challenge.SessionId,
                ["type"] = "mfa_challenge"
            };

            var browserInfo = ParseUserAgent(sessionInfo.UserAgent);
            var locationText = sessionInfo.Location ?? "Unknown location";

            var success = await pushProvider.SendPushNotificationAsync(
                device.PushToken,
                $"{_appOptions.AppName} Login Request",
                $"Approve login from {browserInfo} at {locationText}?",
                notificationData,
                cancellationToken);

            if (!success)
            {
                logger.LogWarning(
                    "Failed to send push notification for challenge {ChallengeId}",
                    challenge.Id);
            }

            return ServiceResponseFactory.Success(new MfaPushChallengeDto
            {
                Id = challenge.Id,
                ChallengeCode = challenge.ChallengeCode,
                ExpiresAt = challenge.ExpiresAt,
                DeviceName = device.DeviceName,
                Location = challenge.Location,
                BrowserInfo = browserInfo
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error sending push challenge for user {UserId}",
                userId);

            return ServiceResponseFactory.Error<MfaPushChallengeDto>(
                "Failed to send push notification");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<MfaPushChallengeStatusDto>> CheckChallengeStatusAsync(
        Guid challengeId,
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var challenge = await mfaRepository.GetPushChallengeAsync(challengeId, cancellationToken);

            if (challenge == null || challenge.SessionId != sessionId)
            {
                return ServiceResponseFactory.Error<MfaPushChallengeStatusDto>(
                    "Challenge not found");
            }

            // Check if expired
            if (challenge.Status == ChallengeStatus.Pending && DateTime.UtcNow > challenge.ExpiresAt)
            {
                challenge.MarkExpired();
            }

            return ServiceResponseFactory.Success(new MfaPushChallengeStatusDto
            {
                Id = challenge.Id,
                Status = challenge.Status.ToString(),
                IsApproved = challenge.Status == ChallengeStatus.Approved,
                IsDenied = challenge.Status == ChallengeStatus.Denied,
                IsExpired = challenge.Status == ChallengeStatus.Expired,
                RespondedAt = challenge.RespondedAt
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error checking challenge status {ChallengeId}",
                challengeId);

            return ServiceResponseFactory.Error<MfaPushChallengeStatusDto>(
                "Failed to check challenge status");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<bool>> RespondToChallengeAsync(
        Guid challengeId,
        PushChallengeResponse response,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var challenge = await mfaRepository.GetPushChallengeAsync(challengeId, cancellationToken);

            if (challenge == null)
            {
                return ServiceResponseFactory.Error<bool>(
                    "Challenge not found");
            }

            // Verify the response came from the correct device
            if (challenge.DeviceId != response.DeviceId)
            {
                logger.LogWarning(
                    "Challenge response from wrong device. Expected {ExpectedDevice}, got {ActualDevice}",
                    challenge.DeviceId, response.DeviceId);

                return ServiceResponseFactory.Error<bool>(
                    "Invalid device");
            }

            // Get device to verify signature
            var device = await mfaRepository.GetPushDeviceAsync(response.DeviceId, cancellationToken);

            if (device == null || !device.IsActive)
            {
                return ServiceResponseFactory.Error<bool>(
                    "Device not found or inactive");
            }

            // Verify signature
            if (!VerifyResponseSignature(challenge, response, device.PublicKey))
            {
                device.RecordSuspiciousActivity();

                logger.LogWarning(
                    "Invalid signature for challenge {ChallengeId} from device {DeviceId}",
                    challengeId, response.DeviceId);

                return ServiceResponseFactory.Error<bool>(
                    "Invalid signature");
            }

            // Process response
            if (response.IsApproved)
            {
                challenge.Approve(response.Signature);
                device.RecordSuccessfulUse();
            }
            else
            {
                challenge.Deny(response.Signature);
            }

            return ServiceResponseFactory.Success(true,
                response.IsApproved ? "Challenge approved" : "Challenge denied");
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error responding to challenge {ChallengeId}",
                challengeId);

            return ServiceResponseFactory.Error<bool>(
                "Failed to process response");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<IEnumerable<MfaPushDeviceDto>>> GetUserDevicesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var devices = await mfaRepository.GetUserPushDevicesAsync(userId, cancellationToken);
            var dtos = devices.Select(MapToDto).ToList();

            return ServiceResponseFactory.Success<IEnumerable<MfaPushDeviceDto>>(dtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error getting push devices for user {UserId}",
                userId);

            return ServiceResponseFactory.Error<IEnumerable<MfaPushDeviceDto>>(
                "Failed to get devices");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<bool>> RemoveDeviceAsync(
        Guid userId,
        Guid deviceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var device = await mfaRepository.GetPushDeviceAsync(deviceId, cancellationToken);

            if (device == null || device.UserId != userId)
            {
                return ServiceResponseFactory.Error<bool>(
                    "Device not found");
            }

            device.Deactivate();

            logger.LogInformation(
                "Push device {DeviceId} removed for user {UserId}",
                deviceId, userId);

            return ServiceResponseFactory.Success(true, "Device removed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error removing push device {DeviceId} for user {UserId}",
                deviceId, userId);

            return ServiceResponseFactory.Error<bool>(
                "Failed to remove device");
        }
    }

    /// <inheritdoc />
    public async Task<ServiceResponse<bool>> UpdateDeviceTokenAsync(
        Guid deviceId,
        string newToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var device = await mfaRepository.GetPushDeviceAsync(deviceId, cancellationToken);

            if (device == null)
            {
                return ServiceResponseFactory.Error<bool>(
                    "Device not found");
            }

            if (!pushProvider.ValidatePushToken(newToken, device.Platform))
            {
                return ServiceResponseFactory.Error<bool>(
                    "Invalid push token format");
            }

            device.UpdatePushToken(newToken);

            return ServiceResponseFactory.Success(true, "Device token updated");
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error updating token for device {DeviceId}",
                deviceId);

            return ServiceResponseFactory.Error<bool>(
                "Failed to update device token");
        }
    }

    /// <inheritdoc />
    public async Task<int> CleanupExpiredChallengesAsync(
        TimeSpan olderThan,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cutoff = DateTime.UtcNow.Subtract(olderThan);
            return await mfaRepository.DeleteExpiredPushChallengesAsync(cutoff, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error cleaning up expired push challenges");
            return 0;
        }
    }

    private static MfaPushDeviceDto MapToDto(MfaPushDevice device)
    {
        return new MfaPushDeviceDto
        {
            Id = device.Id,
            DeviceId = device.DeviceId,
            DeviceName = device.DeviceName,
            Platform = device.Platform,
            RegisteredAt = device.RegisteredAt,
            LastUsedAt = device.LastUsedAt,
            IsActive = device.IsActive,
            TrustScore = device.TrustScore
        };
    }

    private static string ParseUserAgent(string userAgent)
    {
        // Simple user agent parsing - in production use a proper library
        if (userAgent.Contains("Chrome"))
            return "Chrome";
        if (userAgent.Contains("Firefox"))
            return "Firefox";
        if (userAgent.Contains("Safari") && !userAgent.Contains("Chrome"))
            return "Safari";
        if (userAgent.Contains("Edge"))
            return "Edge";

        return "Unknown Browser";
    }

    private bool VerifyResponseSignature(
        MfaPushChallenge challenge,
        PushChallengeResponse response,
        string publicKey)
    {
        try
        {
            // Create the data to verify
            var dataToVerify = JsonSerializer.Serialize(new
            {
                challengeId = challenge.Id,
                challengeCode = challenge.ChallengeCode,
                deviceId = response.DeviceId,
                isApproved = response.IsApproved,
                timestamp = DateTime.UtcNow.ToString("O")
            });

            var dataBytes = Encoding.UTF8.GetBytes(dataToVerify);
            var signatureBytes = Convert.FromBase64String(response.Signature);
            var publicKeyBytes = Convert.FromBase64String(publicKey);

            using var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);

            return rsa.VerifyData(
                dataBytes,
                signatureBytes,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pss);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error verifying response signature");
            return false;
        }
    }
}
