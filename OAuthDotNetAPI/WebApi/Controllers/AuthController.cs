using Application.Common.Utilities;
using Application.DTOs.Auth;
using Application.DTOs.Mfa;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace OAuthDotNetAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController(IAuthService authService, IMfaConfigurationService mfaService, ILogger<AuthController> logger) : BaseAppController(logger)
{
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(UserLoginDto userLoginDto) => await ResolveAsync(() =>
        authService.Login(userLoginDto.Username, userLoginDto.Password,
            HttpContext.Features.Get<IHttpConnectionFeature>()?.RemoteIpAddress?.ToString() ?? ""));

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(UserLogoutDto request) =>
        await ResolveAsync(() => authService.Logout(request.Username, request.RefreshToken));

    [HttpPost("refresh")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Refresh(UserRefreshTokenDto request) => await ResolveAsync(() =>
        authService.Refresh(request.Username, request.RefreshToken,
            HttpContext.Features.Get<IHttpConnectionFeature>()?.RemoteIpAddress?.ToString() ?? ""));

    [HttpPost("ResetPassword/{emailAddress}")]
    [EnableRateLimiting("password-reset")]
    public async Task<IActionResult> RequestResetPassword(string emailAddress) => await ResolveAsync(() =>
        authService.RequestPasswordReset(emailAddress,
            HttpContext.Features.Get<IHttpConnectionFeature>()?.RemoteIpAddress?.ToString() ?? ""));

    [HttpPost("ResetUserPassword")]
    [EnableRateLimiting("password-reset")]
    public async Task<IActionResult> ApplyResetPassword(PasswordResetSubmissionDto token) =>
        await ResolveAsync(() => authService.ApplyPasswordReset(
                token,
                HttpContext.Features.Get<IHttpConnectionFeature>()?.RemoteIpAddress?.ToString() ?? ""
            )
        );

    [HttpPost("ForcePasswordReset")]
    [Authorize]
    [EnableRateLimiting("api")]
    public async Task<IActionResult> ForceResetPassword([FromBody] string newPassword) =>
        await ResolveAsync(() => authService.ForcePasswordReset(User, newPassword));

    // MFA Authentication Endpoints

    [HttpPost("mfa/complete")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> CompleteMfaAuthentication(CompleteMfaDto completeMfaDto) =>
        await ResolveAsync(() => authService.CompleteMfaAuthentication(completeMfaDto,
            HttpContext.Features.Get<IHttpConnectionFeature>()?.RemoteIpAddress?.ToString() ?? ""));

    // MFA Configuration Endpoints

    [HttpGet("mfa/overview")]
    [Authorize]
    [EnableRateLimiting("api")]
    public async Task<IActionResult> GetMfaOverview() =>
        await ResolveAsync(() => mfaService.GetMfaOverviewAsync(RoleUtility.GetUserIdFromClaims(User)));

    [HttpPost("mfa/setup/totp")]
    [Authorize]
    [EnableRateLimiting("api")]
    public async Task<IActionResult> StartTotpSetup() =>
        await ResolveAsync(() => mfaService.StartTotpSetupAsync(
            RoleUtility.GetUserIdFromClaims(User),
            RoleUtility.GetUserNameFromClaim(User)));

    [HttpPost("mfa/verify/totp")]
    [Authorize]
    [EnableRateLimiting("mfa-setup")]
    public async Task<IActionResult> VerifyTotpSetup(VerifyMfaSetupDto verificationDto) =>
        await ResolveAsync(() => mfaService.VerifyTotpSetupAsync(
            RoleUtility.GetUserIdFromClaims(User),
            verificationDto));

    [HttpPost("mfa/setup/email")]
    [Authorize]
    [EnableRateLimiting("api")]
    public async Task<IActionResult> StartEmailSetup([FromBody] StartEmailMfaSetupDto request) =>
        await ResolveAsync(() => mfaService.StartEmailSetupAsync(
            RoleUtility.GetUserIdFromClaims(User),
            request.EmailAddress));

    [HttpPost("mfa/verify/email")]
    [Authorize]
    [EnableRateLimiting("mfa-setup")]
    public async Task<IActionResult> VerifyEmailSetup(VerifyMfaSetupDto verificationDto) =>
        await ResolveAsync(() => mfaService.VerifyEmailSetupAsync(
            RoleUtility.GetUserIdFromClaims(User),
            verificationDto));

    [HttpDelete("mfa/method/{methodId:guid}")]
    [Authorize]
    [EnableRateLimiting("api")]
    public async Task<IActionResult> RemoveMfaMethod(Guid methodId) =>
        await ResolveAsync(() => mfaService.RemoveMfaMethodAsync(
            RoleUtility.GetUserIdFromClaims(User),
            methodId));

    [HttpPut("mfa/method/{methodId:guid}")]
    [Authorize]
    [EnableRateLimiting("api")]
    public async Task<IActionResult> UpdateMfaMethod(Guid methodId, UpdateMfaMethodDto updateDto) =>
        await ResolveAsync(() => mfaService.UpdateMfaMethodAsync(
            RoleUtility.GetUserIdFromClaims(User),
            methodId,
            updateDto));

    [HttpPost("mfa/method/{methodId:guid}/recovery-codes")]
    [Authorize]
    [EnableRateLimiting("api")]
    public async Task<IActionResult> RegenerateRecoveryCodes(Guid methodId) =>
        await ResolveAsync(() => mfaService.RegenerateRecoveryCodesAsync(
            RoleUtility.GetUserIdFromClaims(User),
            methodId));
}
