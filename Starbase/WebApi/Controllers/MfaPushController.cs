using Application.Common.Utilities;
using Application.DTOs.Mfa;
using Application.Interfaces.Services;
using Application.Security;
using Application.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace OAuthDotNetAPI.Controllers;

/// <summary>
/// Controller for managing push notification multi-factor authentication.
/// Handles device registration, challenge sending, and response processing.
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize]
[RequireActiveUser]
[EnableRateLimiting("api")]
public class MfaPushController(
    IMfaPushService mfaPushService,
    ILogger<MfaPushController> logger) : BaseAppController(logger)
{
    /// <summary>
    /// Registers a new device for push notifications.
    /// </summary>
    /// <param name="request">Device registration details</param>
    /// <returns>The registered device information</returns>
    [HttpPost("register-device")]
    [ProducesResponseType(typeof(MfaPushDeviceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegisterDevice([FromBody] RegisterPushDeviceRequest request)
    {
        var userId = RoleUtility.GetUserIdFromClaims(User);
        return await ResolveAsync(() => mfaPushService.RegisterDeviceAsync(userId, request));
    }

    /// <summary>
    /// Gets all registered push devices for the current user.
    /// </summary>
    /// <returns>List of user's push devices</returns>
    [HttpGet("devices")]
    [ProducesResponseType(typeof(IEnumerable<MfaPushDeviceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDevices()
    {
        var userId = RoleUtility.GetUserIdFromClaims(User);
        return await ResolveAsync(() => mfaPushService.GetUserDevicesAsync(userId));
    }

    /// <summary>
    /// Removes a push device.
    /// </summary>
    /// <param name="deviceId">The device ID to remove</param>
    /// <returns>Success result</returns>
    [HttpDelete("devices/{deviceId:guid}")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RemoveDevice(Guid deviceId)
    {
        var userId = RoleUtility.GetUserIdFromClaims(User);
        return await ResolveAsync(() => mfaPushService.RemoveDeviceAsync(userId, deviceId));
    }

    /// <summary>
    /// Updates a device's push token.
    /// </summary>
    /// <param name="deviceId">The device ID to update</param>
    /// <param name="request">The new token information</param>
    /// <returns>Success result</returns>
    [HttpPut("devices/{deviceId:guid}/token")]
    [ValidDto]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateDeviceToken(Guid deviceId, [FromBody] UpdatePushTokenDto request)
    {
        return await ResolveAsync(() => mfaPushService.UpdateDeviceTokenAsync(deviceId, request.NewToken));
    }

    /// <summary>
    /// Sends a push notification challenge to a specific device.
    /// Used during authentication flow when user selects push notification option.
    /// </summary>
    /// <param name="request">Challenge request details</param>
    /// <returns>The challenge information</returns>
    [HttpPost("challenge")]
    [ValidDto]
    [ProducesResponseType(typeof(MfaPushChallengeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SendChallenge([FromBody] SendPushChallengeDto request)
    {
        var userId = RoleUtility.GetUserIdFromClaims(User);

        var sessionInfo = new PushChallengeSessionInfo
        {
            SessionId = request.SessionId,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
            UserAgent = HttpContext.Request.Headers.UserAgent.ToString(),
            Location = request.Location
        };

        return await ResolveAsync(() => mfaPushService.SendChallengeAsync(
            userId, request.DeviceId, sessionInfo));
    }

    /// <summary>
    /// Checks the status of a push challenge.
    /// Used for polling until the user responds on their device.
    /// </summary>
    /// <param name="challengeId">The challenge ID to check</param>
    /// <param name="sessionId">The session ID for validation</param>
    /// <returns>The current challenge status</returns>
    [HttpGet("challenge/{challengeId:guid}/status")]
    [ProducesResponseType(typeof(MfaPushChallengeStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CheckChallengeStatus(Guid challengeId, [FromQuery] string sessionId)
    {
        return await ResolveAsync(() => mfaPushService.CheckChallengeStatusAsync(challengeId, sessionId));
    }

    /// <summary>
    /// Responds to a push challenge from the mobile device.
    /// Called by the authenticator app when user approves/denies.
    /// </summary>
    /// <param name="challengeId">The challenge ID</param>
    /// <param name="response">The response from the device</param>
    /// <returns>Response processing result</returns>
    [HttpPost("challenge/{challengeId:guid}/respond")]
    [AllowAnonymous] // Mobile apps may not have auth context
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RespondToChallenge(Guid challengeId, [FromBody] PushChallengeResponse response)
    {
        return await ResolveAsync(() => mfaPushService.RespondToChallengeAsync(challengeId, response));
    }
}
